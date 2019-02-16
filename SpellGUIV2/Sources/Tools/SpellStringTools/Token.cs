using SpellEditor.Sources.SpellStringTools;
using System.Text.RegularExpressions;

namespace SpellEditor.Sources.Tools.SpellStringTools
{
    internal class Token
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
            if (Regex.IsMatch(Value, SpellStringParser.REFERENCE_REGEX))
                Type = TokenType.REFERENCE;
            else if (Regex.IsMatch(Value, SpellStringParser.MODIFY_FORMULA_REGEX))
                Type = TokenType.MODIFY_FORMULA;
            else if (Regex.IsMatch(Value, SpellStringParser.NUMBER_REGEX))
                Type = TokenType.NUMBER;
            else if (Regex.IsMatch(Value, SpellStringParser.PLUS_REGEX))
                Type = TokenType.PLUS;
            else if (Regex.IsMatch(Value, SpellStringParser.MINUS_REGEX))
                Type = TokenType.MINUS;
            else if (Regex.IsMatch(Value, SpellStringParser.DIVIDE_REGEX))
                Type = TokenType.DIVIDE;
            else if (Regex.IsMatch(Value, SpellStringParser.MULTIPLY_REGEX))
                Type = TokenType.MULTIPLY;
        }

        public override string ToString()
        {
            return $"Token[{ Value }, { Type }, { ResolvedValue }]";
        }
    }
}
