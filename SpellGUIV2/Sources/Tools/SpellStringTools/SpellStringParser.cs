using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace SpellEditor.Sources.SpellStringTools
{
    class SpellStringParser
    {
        //
        private static string FORMULA_REGEX = "\\$\\{.*?}|\\$\\w*"; // \\$\\w|\\$\\{.*?}
        private static string NUMBER_REGEX = "\\d+\\.?\\d+|\\d+";
        private static string REFERENCE_REGEX = "\\$\\w+";
        private static string PLUS_REGEX = "\\+";
        private static string MINUS_REGEX = "\\-";
        private static string MULTIPLY_REGEX = "\\*";
        private static string DIVIDE_REGEX = "\\/";
        private static string TOKEN_REGEX = 
            $"{ REFERENCE_REGEX }|{ PLUS_REGEX }|{ MINUS_REGEX }|{ DIVIDE_REGEX }|{ MULTIPLY_REGEX }|{ NUMBER_REGEX }";

        private string ResolveReference(string reference, DataRow spell, MainWindow mainWindow)
        {
            return SpellStringReferenceResolver.GetParsedForm(reference, spell, mainWindow);
        }

        // Parse a string like: "Hello world 1 + 5 + 7 = ${1 + 5 + 7}, 5 / 10.15 - 1 + 0.25 = ${5/10.15-1+0.25}"
        // Can parse references like "$s1"
        public string ParseString(string str, DataRow spell, MainWindow mainWindow)
        {
            var formulas = FindFormulas(str);
            foreach (var formula in formulas)
            {
                Console.WriteLine(formula + "\t----\t" + "Processing");
                str = str.Replace(formula, ParseFormula(formula, spell, mainWindow));
            }
            return str;
        }

        // Find ${} and $vars in the formula string
        private List<string> FindFormulas(string str)
        {
            var regexMatches = Regex.Matches(str, FORMULA_REGEX);
            var tokenList = new List<string>(regexMatches.Count);
            foreach (var tokenStr in regexMatches)
            {
                tokenList.Add(tokenStr.ToString());
            }
            return tokenList;
        }

        // Parse a formula string resolving all references and calculating arithmetic
        private string ParseFormula(string formula, DataRow spell, MainWindow mainWindow)
        {
            var matches = Regex.Matches(formula, TOKEN_REGEX);
            var tokens = TokenizeFormulaMatches(matches, spell, mainWindow);
            // Derive token values
            for (int index = 0; index < tokens.Count; ++index)
            {
                ProcessTokenArithmetic(tokens, index);
            }
            // Replace tokens with derived token values in formula
            for (int index = 0; index < tokens.Count; ++index)
            {
                Console.WriteLine($"> Token '{ tokens[index].Value }' derived value '{ tokens[index].ResolvedValue }'");
                if (tokens[index].ResolvedValue != null)
                {
                    var regex = new Regex(Regex.Escape(tokens[index].Value));
                    formula = regex.Replace(formula, tokens[index].ResolvedValue.ToString(), 1);
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
                if (prevToken == null || nextToken == null)
                    Console.WriteLine($"Unexpected null token: [{ prevToken }][{ token.Type }][{ nextToken }]");
                else
                    Console.WriteLine($"Unexpected tokens [{ prevToken.Value }, { prevToken.Type }] { token.Type } [{ nextToken.Value }, { nextToken.Type }]");
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
                    Console.WriteLine("Unable to find previous resolved token for " + token);
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
                nextValue = double.Parse((string)nextToken.ResolvedValue);
            else 
                nextValue = (double)nextToken.ResolvedValue;
            if (prevToken.ResolvedValue is string && ((string)prevToken.ResolvedValue).Length > 0)
                prevValue = double.Parse((string)prevToken.ResolvedValue);
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
                        break;
                    case TokenType.NUMBER:
                        double temp;
                        if (double.TryParse(token.Value, out temp))
                            token.ResolvedValue = temp;
                        else
                            token.ResolvedValue = 0D;
                        break;
                    case TokenType.REFERENCE:
                        token.ResolvedValue = ResolveReference(token.Value, spell, mainWindow);
                        if (token.ResolvedValue != null && !token.ResolvedValue.ToString().StartsWith("$"))
                            token.Type = TokenType.NUMBER;
                        break;
                    default:
                        Console.WriteLine($"Unknown token: '{ token.Value }'");
                        break;
                }
                tokens.Add(token);

                Console.WriteLine($"Token: [{ token.Value }, {token.Type.ToString() }, { token.ResolvedValue }]");
            }
            return tokens;
        }

        private class Token
        {
            // Value read from the formula string
            public string Value;
            public TokenType Type = TokenType.UNKNOWN;
            // Value derived
            public object ResolvedValue;

            public Token(string value)
            {
                Value = value;
                DetermineType();
            }

            // Interpret what the string token represents with regex
            private void DetermineType()
            {
                // Reference must be checked before Number because the regex for number can also detect references
                if (Regex.IsMatch(Value, REFERENCE_REGEX))
                    Type = TokenType.REFERENCE;
                else if (Regex.IsMatch(Value, NUMBER_REGEX))
                    Type = TokenType.NUMBER;
                else if (Regex.IsMatch(Value, PLUS_REGEX))
                    Type = TokenType.PLUS;
                else if (Regex.IsMatch(Value, MINUS_REGEX))
                    Type = TokenType.MINUS;
                else if (Regex.IsMatch(Value, DIVIDE_REGEX))
                    Type = TokenType.DIVIDE;
                else if (Regex.IsMatch(Value, MULTIPLY_REGEX))
                    Type = TokenType.MULTIPLY;
            }

            public override string ToString()
            {
                return $"Token[{ Value }, { Type }, { ResolvedValue }]";
            }
        }

        private enum TokenType
        {
            NUMBER,
            PLUS,
            MINUS,
            MULTIPLY,
            DIVIDE,
            REFERENCE,
            UNKNOWN
        };
    }
}
