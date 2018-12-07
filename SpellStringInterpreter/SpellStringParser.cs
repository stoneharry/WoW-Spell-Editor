using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpellStringInterpreter
{
    class SpellStringParser
    {
        private static string FORMULA_REGEX = "\\$\\w|\\$\\{.*?}";
        private static string NUMBER_REGEX = "\\d+\\.?\\d+|\\d+";
        private static string REFERENCE_REGEX = "\\$\\w+";
        private static string PLUS_REGEX = "\\+";
        private static string MINUS_REGEX = "\\-";
        private static string MULTIPLY_REGEX = "\\*";
        private static string DIVIDE_REGEX = "\\/";
        private static string TOKEN_REGEX = 
            $"{ REFERENCE_REGEX }|{ PLUS_REGEX }|{ MINUS_REGEX }|{ DIVIDE_REGEX }|{ MULTIPLY_REGEX }|{ NUMBER_REGEX }";

        public string ParseString(string str)
        {
            var formulas = FindFormulas(str);
            foreach (var formula in formulas)
            {
                Console.WriteLine(formula + "\t----\t" + "Processing");
                str = str.Replace(formula, ParseFormula(formula));
            }
            return str;
        }

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

        private string ParseFormula(string formula)
        {
            var matches = Regex.Matches(formula, TOKEN_REGEX);
            var tokens = TokenizeFormulaMatches(matches);
            // Derive token values
            for (int index = 0; index < tokens.Count; ++index)
            {
                ProcessTokenArithmetic(tokens, index);
            }
            // Replace tokens with derived token values in formula
            for (int index = 0; index < tokens.Count; ++index)
            {
                Console.WriteLine($"> Token '{ tokens[index].Value }' derived value '{ tokens[index].ResolvedValue }'");
                var regex = new Regex(Regex.Escape(tokens[index].Value));
                formula = regex.Replace(formula, tokens[index].ResolvedValue.ToString(), 1);
            }
            // Strip prefix ${ and suffix }
            return formula.Substring(2, formula.Length - 3).Trim();
        }

        private void ProcessTokenArithmetic(List<Token> tokens, int index)
        {
            var token = tokens[index];
            var prevToken = index - 1 < 0 ? null : tokens[index - 1];
            var nextToken = index + 1 == tokens.Count ? null : tokens[index + 1];
            var type = token.Type;
            // If not any of these token types return
            if (!(type == TokenType.DIVIDE ||
                type == TokenType.MULTIPLY ||
                type == TokenType.PLUS ||
                type == TokenType.MINUS))
                return;
            // If previous and next is not a token we log an error and return
            // All references should be resolved
            if (prevToken == null ||
                nextToken == null ||
                prevToken.Type != TokenType.NUMBER ||
                nextToken.Type != TokenType.NUMBER)
            {
                if (prevToken == null || nextToken == null)
                    Console.WriteLine($"Unexpected null token: [{ prevToken }][{ token.Type }][{ nextToken }]");
                else
                    Console.WriteLine($"Unexpected tokens [{ prevToken.Value }, { prevToken.Type }] { token.Type } [{ nextToken.Value }, { nextToken.Type }]");
                return;
            }
            // If prev value has been wiped out as it was used for a calc then find the prev valid token
            int tries = 1;
            while (prevToken.ResolvedValue is string && ((string)prevToken.ResolvedValue).Length == 0)
            {
                ++tries;
                int newIndex = index - tries;
                if (newIndex < 0)
                {
                    Console.WriteLine("Unable to find previous resolved token for " + token);
                    return;
                }
                prevToken = tokens[newIndex];
            }
            // We should be able to do the math now. Calculate it on current node
            double nextValue;
            double prevValue;
            // The resolved value could be a string or already a double
            if (nextToken.ResolvedValue is string && ((string)nextToken.ResolvedValue).Length > 0)
                nextValue = double.Parse((string)nextToken.ResolvedValue);
            else
                nextValue = (double)nextToken.ResolvedValue;
            if (prevToken.ResolvedValue is string && ((string)prevToken.ResolvedValue).Length > 0)
                prevValue = double.Parse((string)prevToken.ResolvedValue);
            else
                prevValue = (double)prevToken.ResolvedValue;
            if (token.Type == TokenType.DIVIDE)
                token.ResolvedValue = prevValue / nextValue;
            else if (token.Type == TokenType.PLUS)
                token.ResolvedValue = prevValue + nextValue;
            else if (token.Type == TokenType.MULTIPLY)
                token.ResolvedValue = prevValue * nextValue;
            else if (token.Type == TokenType.MINUS)
                token.ResolvedValue = prevValue - nextValue;
            prevToken.ResolvedValue = "";
            nextToken.ResolvedValue = "";
        }

        private List<Token> TokenizeFormulaMatches(MatchCollection matches)
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
                        token.ResolvedValue = ResolveReference(token.Value);
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

        private string ResolveReference(string reference)
        {
            // NOT IMPLEMENTED, must return a valid double in string form
            return "150";
        }

        private class Token
        {
            public string Value;
            public TokenType Type = TokenType.UNKNOWN;
            public object ResolvedValue;

            public Token(string value)
            {
                Value = value;
                DetermineType();
            }

            private void DetermineType()
            {
                // Reference must come before Number because the regex for number can also detect references
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
