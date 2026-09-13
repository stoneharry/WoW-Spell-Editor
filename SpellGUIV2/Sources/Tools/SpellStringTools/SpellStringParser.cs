using NLog;
using SpellEditor.Sources.Tools.SpellStringTools;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace SpellEditor.Sources.SpellStringTools
{
    public class SpellStringParser
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Holds a $ inside an error message so the token pass below does not eat it
        private const string DOLLAR_PLACEHOLDER = "\u0001";

        public static readonly string MODIFY_FORMULA_REGEX      = @"\$(\/|\*|\-|)\d+\;\d*\w+";                      // $/10;17057s1 | $/10;s1
        public static readonly string FORMULA_REGEX             = @"\$\{.*?}|\$\w*";                                // ${1 + $s1} | $s1
        public static readonly string EXPRESSION_REGEX          = @"\$\{([^}]*)\}(?:\.([1-9]))?";                   // ${$abs($m1)} | ${$m1/3}.2
        public static readonly string LOCALE_STR_REGEX          = @"\$l(\w+\:)+\w+\;";                              // $lone_thing:two_things:three_things:four_things;
        public static readonly string CONDITIONAL_FORMULA_REGEX = @"\$\?(.+?)\[(.*?)\]\[(.*?)\]";                   // $?(s70937)[true condition][false $70907d]
        public static readonly string FORMULA_TAG_REGEX         = @"\$\<\w+\>";                                     // $<mult>
        public static readonly string ALL_FORMULA_REGEX         = $"{MODIFY_FORMULA_REGEX}|{FORMULA_REGEX}";
        public static readonly string NUMBER_REGEX              = @"\d+\.?\d+|\d+";                                 // 12 | 12.26
        public static readonly string REFERENCE_REGEX           = @"\$\w+";                                         // $s1
        public static readonly string PLUS_REGEX                = @"\+";                                            // +
        public static readonly string MINUS_REGEX               = @"\-";                                            // -
        public static readonly string MULTIPLY_REGEX            = @"\*";                                            // *
        public static readonly string DIVIDE_REGEX              = @"\/";                                            // /
        public static readonly string TOKEN_REGEX =
            $"{MODIFY_FORMULA_REGEX }|{ REFERENCE_REGEX }|{ PLUS_REGEX }|{ MINUS_REGEX }|{ DIVIDE_REGEX }|{ MULTIPLY_REGEX }|{ NUMBER_REGEX }";

        // Compiled once, the static Regex methods only cache 15 patterns and this file alone exceeds that
        public static readonly Regex ModifyFormulaRegex = new Regex(MODIFY_FORMULA_REGEX, RegexOptions.Compiled);
        public static readonly Regex ReferenceRegex = new Regex(REFERENCE_REGEX, RegexOptions.Compiled);
        public static readonly Regex NumberRegex = new Regex(NUMBER_REGEX, RegexOptions.Compiled);
        private static readonly Regex LocaleStrRegex = new Regex(LOCALE_STR_REGEX, RegexOptions.Compiled);
        private static readonly Regex ConditionalFormulaRegex = new Regex(CONDITIONAL_FORMULA_REGEX, RegexOptions.Compiled);
        private static readonly Regex FormulaTagRegex = new Regex(FORMULA_TAG_REGEX, RegexOptions.Compiled);
        private static readonly Regex ExpressionRegex = new Regex(EXPRESSION_REGEX, RegexOptions.Compiled);
        private static readonly Regex AllFormulaRegex = new Regex(ALL_FORMULA_REGEX, RegexOptions.Compiled);
        private static readonly Regex TokenRegex = new Regex(TOKEN_REGEX, RegexOptions.Compiled);

        protected string ResolveReference(string reference, DataRow spell, MainWindow mainWindow)
        {
            return SpellStringReferenceResolver.GetParsedForm(reference, spell, mainWindow);
        }

        // Parse a string like: "Hello world 1 + 5 + 7 = ${1 + 5 + 7}, 5 / 10.15 - 1 + 0.25 = ${5/10.15-1+0.25} and $/10;17057s1"
        // Can parse references like "$s1"
        public string ParseString(string str, DataRow spell, MainWindow mainWindow)
        {
            // Nothing in here does anything without a $, and this runs on every keystroke
            if (string.IsNullOrEmpty(str) || str.IndexOf('$') < 0)
                return str;

            // Replace locale strings first
            foreach (var localeMatch in LocaleStrRegex.Matches(str))
            {
                var localeFormula = localeMatch.ToString();
                // The correct string to use appears to depend on the localisation.
                // For the purposes of the spell editor, always display the first string.
                var useWord = localeFormula.Substring(2, localeFormula.IndexOf(':') - 2);
                str = str.Replace(localeFormula, useWord);
            }
            foreach (Match conditionMatch in ConditionalFormulaRegex.Matches(str))
            {
                // Always take the true condition value because we don't have a player context
                str = str.Replace(conditionMatch.ToString(), conditionMatch.Groups[2].Value);
            }
            foreach (var match in FormulaTagRegex.Matches(str))
            {
                // Strip formula tags
                str = str.Replace(match.ToString(), "0");
            }
            str = ParseExpressions(str, spell, mainWindow);
            // Parse formulas and resolve
            var formulas = FindFormulas(str);
            foreach (var formula in formulas)
            {
                if (Logger.IsTraceEnabled)
                    Logger.Trace(formula + "\t----\t" + "Processing");
                str = str.Replace(formula, ParseFormula(formula, spell, mainWindow));
            }
            return str.Replace(DOLLAR_PLACEHOLDER, "$");
        }

        // Resolve every ${} through the game's formula grammar, see SpellFormula.
        private string ParseExpressions(string str, DataRow spell, MainWindow mainWindow)
        {
            if (str.IndexOf("${", StringComparison.Ordinal) < 0)
                return str;

            // One pass, so a formula that is the prefix of a longer one cannot eat it
            var resolved = new Dictionary<string, string>();
            return ExpressionRegex.Replace(str, match =>
            {
                if (resolved.TryGetValue(match.Value, out var cached))
                    return cached;

                var decimals = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                string replacement;
                if (SpellFormula.TryEvaluate(match.Groups[1].Value, spell, mainWindow, out var value, out var error))
                {
                    replacement = SpellFormula.Format(value, decimals);
                }
                else
                {
                    // A formula the game cannot work out renders as a bare $, so say what is
                    // wrong rather than show a number that will never appear in a tooltip.
                    replacement = "<ERROR: " + error.Replace("$", DOLLAR_PLACEHOLDER) + ">";
                    if (Logger.IsTraceEnabled)
                        Logger.Trace($"{match.Value}\t----\t{error}");
                }

                resolved[match.Value] = replacement;
                return replacement;
            });
        }

        // Find ${} and $vars in the formula string
        protected List<string> FindFormulas(string str)
        {
            var regexMatches = AllFormulaRegex.Matches(str);
            var tokenList = new List<string>(regexMatches.Count);
            // The caller replaces every occurrence at once, so a repeated formula resolves once
            var seen = new HashSet<string>();
            foreach (var tokenStr in regexMatches)
            {
                var formula = tokenStr.ToString();
                if (seen.Add(formula))
                    tokenList.Add(formula);
            }
            return tokenList;
        }

        // Parse a formula string resolving all references and calculating arithmetic
        private string ParseFormula(string formula, DataRow spell, MainWindow mainWindow)
        {
            var matches = TokenRegex.Matches(formula);
            var tokens = TokenizeFormulaMatches(matches, spell, mainWindow);
            // Derive token values
            for (int index = 0; index < tokens.Count; ++index)
            {
                ProcessTokenArithmetic(tokens, index);
            }
            // Replace tokens with derived token values in formula
            for (int index = 0; index < tokens.Count; ++index)
            {
                if (Logger.IsTraceEnabled)
                    Logger.Trace($"> Token '{ tokens[index].Value }' derived value '{ tokens[index].ResolvedValue }'");
                if (tokens[index].ResolvedValue != null)
                {
                    formula = ReplaceFirst(formula, tokens[index].Value, tokens[index].ResolvedValue.ToString());
                }
            }
            // Strip prefix ${ and suffix }
            if (formula.StartsWith("${") && formula.EndsWith("}"))
                return formula.Substring(2, formula.Length - 3).Trim();
            // Strip $
            else if (formula.StartsWith("$"))
                return formula.Substring(1).Trim();
            return formula.Trim();
        }

        private static string ReplaceFirst(string str, string find, string replace)
        {
            var at = str.IndexOf(find, StringComparison.Ordinal);
            if (at < 0)
                return str;
            return str.Substring(0, at) + replace + str.Substring(at + find.Length);
        }

        // Return true if the token is a arithmetic operator
        private bool IsTokenTypeOperator(TokenType type)
        {
            return type == TokenType.DIVIDE ||
                type == TokenType.MULTIPLY ||
                type == TokenType.PLUS ||
                type == TokenType.MINUS;
        }

        private bool IsValidPointers(Token token, Token prevToken, Token nextToken)
        {
            // If previous and next is not a token we log an error and return
            // All reference tokens should be resolved at this point
            if (prevToken == null ||
                nextToken == null ||
                prevToken.Type != TokenType.NUMBER ||
                nextToken.Type != TokenType.NUMBER)
            {
                if (!Logger.IsTraceEnabled)
                    return false;
                if (prevToken == null || nextToken == null)
                    Logger.Trace($"Unexpected null token: [{ prevToken }][{ token.Type }][{ nextToken }]");
                else
                    Logger.Trace($"Unexpected tokens [{ prevToken.Value }, { prevToken.Type }] { token.Type } [{ nextToken.Value }, { nextToken.Type }]");
                return false;
            }
            return true;
        }

        private Token FindResolvedPrevToken(Token token, Token prevToken, int index, List<Token> tokens)
        {
            // If prev value has been cleared because it was used in a calc already then find the previous valid token to use
            int tries = 1;
            while (prevToken.ResolvedValue is string && ((string)prevToken.ResolvedValue).Length == 0)
            {
                ++tries;
                int newIndex = index - tries;
                if (newIndex < 0)
                {
                    Logger.Info("Unable to find previous resolved token for " + token);
                    return prevToken;
                }
                prevToken = tokens[newIndex];
            }
            return prevToken;
        }

        // Calculate any arithmetic in the token list. Requires all references to be resolved
        private void ProcessTokenArithmetic(List<Token> tokens, int index)
        {
            var token = tokens[index];
            var prevToken = index - 1 < 0 ? null : tokens[index - 1];
            var nextToken = index + 1 == tokens.Count ? null : tokens[index + 1];
            // Validation
            if (!IsTokenTypeOperator(token.Type))
                return;
            if (!IsValidPointers(token, prevToken, nextToken))
                return;
            prevToken = FindResolvedPrevToken(token, prevToken, index, tokens);
            // Casting and calc setup
            double nextValue;
            double prevValue;
            if (nextToken.ResolvedValue is string && ((string)nextToken.ResolvedValue).Length > 0)
                double.TryParse((string)nextToken.FriendlyResolvedValue(), out nextValue);
            else 
                nextValue = (double)nextToken.ResolvedValue;
            if (prevToken.ResolvedValue is string && ((string)prevToken.ResolvedValue).Length > 0)
                double.TryParse((string)prevToken.FriendlyResolvedValue(), out prevValue);
            else
                prevValue = (double)prevToken.ResolvedValue;
            // Calculation
            if (token.Type == TokenType.DIVIDE)
                token.ResolvedValue = prevValue / nextValue;
            else if (token.Type == TokenType.PLUS)
                token.ResolvedValue = prevValue + nextValue;
            else if (token.Type == TokenType.MULTIPLY)
                token.ResolvedValue = prevValue * nextValue;
            else if (token.Type == TokenType.MINUS)
                token.ResolvedValue = prevValue - nextValue;
            // Clear used tokens
            prevToken.ResolvedValue = "";
            nextToken.ResolvedValue = "";
        }

        // Tokenises all the token string matches found in the formula and resolves any references
        private List<Token> TokenizeFormulaMatches(MatchCollection matches, DataRow spell, MainWindow mainWindow)
        {
            var tokens = new List<Token>(matches.Count);
            foreach (var currentMatch in matches)
            {
                var token = new Token(currentMatch.ToString());
                switch (token.Type)
                {
                    case TokenType.DIVIDE:
                    case TokenType.PLUS:
                    case TokenType.MULTIPLY:
                    case TokenType.MINUS:
                        {
                            break;
                        }
                    case TokenType.NUMBER:
                        {
                            double temp;
                            if (double.TryParse(token.Value, out temp))
                                token.ResolvedValue = temp;
                            else
                                token.ResolvedValue = 0D;
                            break;
                        }
                    case TokenType.REFERENCE:
                        {
                            token.ResolvedValue = ResolveReference(token.Value, spell, mainWindow);
                            if (token.ResolvedValue != null && !token.ResolvedValue.ToString().StartsWith("$"))
                                token.Type = TokenType.NUMBER;
                            break;
                        }
                    case TokenType.MODIFY_FORMULA:
                        {
                            token.ResolvedValue = ResolveModifyFormula(token.Value, spell, mainWindow);
                            break;
                        }
                    default:
                        {
                            Logger.Info($"Unknown token: '{ token.Value }'");
                            break;
                        }
                }
                tokens.Add(token);
                if (Logger.IsTraceEnabled)
                    Logger.Trace($"Token: [{ token.Value }, { token.Type }, { token.ResolvedValue }]");
            }
            return tokens;
        }

        private string ResolveModifyFormula(string value, DataRow spell, MainWindow mainWindow)
        {
            var valueParts = value.Split(';');
            if (valueParts.Length != 2)
            {
                return "<ERROR: Expected one ; character, got a different amount>";
            }
            var modifier = valueParts[0].Replace('$', ' ').TrimStart();
            var reference = "$" + valueParts[1];
            var resolvedRef = ResolveReference(reference, spell, mainWindow);

            if (modifier.Length > 0 &&
                int.TryParse(modifier.Substring(1), out int number) &&
                int.TryParse(resolvedRef, out int refNumber))
            {
                var token = new Token("" + modifier[0]);
                if (token.Type == TokenType.DIVIDE)
                    resolvedRef = (refNumber / number).ToString();
                else if (token.Type == TokenType.MULTIPLY)
                    resolvedRef = (refNumber * number).ToString();
                else if (token.Type == TokenType.PLUS)
                    resolvedRef = (refNumber + number).ToString();
                else if (token.Type == TokenType.MINUS)
                    resolvedRef = (refNumber - number).ToString();
            }
            return resolvedRef;
        }
    }
}
