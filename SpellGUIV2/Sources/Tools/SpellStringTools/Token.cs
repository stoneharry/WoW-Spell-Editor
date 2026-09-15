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

        // Interpret what the string token represents. Single character operators are the common case
        // and are settled with a char compare before touching a regex.
        private void DetermineType()
        {
            if (Value.Length == 1)
            {
                switch (Value[0])
                {
                    case '+': Type = TokenType.PLUS; return;
                    case '-': Type = TokenType.MINUS; return;
                    case '/': Type = TokenType.DIVIDE; return;
                    case '*': Type = TokenType.MULTIPLY; return;
                }
            }
            // Reference must be checked before Number because the regex for number can also detect references
            if (SpellStringParser.ReferenceRegex.IsMatch(Value))
                Type = TokenType.REFERENCE;
            else if (SpellStringParser.ModifyFormulaRegex.IsMatch(Value))
                Type = TokenType.MODIFY_FORMULA;
            else if (SpellStringParser.NumberRegex.IsMatch(Value))
                Type = TokenType.NUMBER;
        }

        /**
         * This is a bit of hack to get around the a string like ${$2085d/6}.
         * 
         * This references spell ID 2085's duration which will be returned like "10 seconds".
         * 
         * It then attempts to divide this by 6 but the seconds string causes a format exception to be raised.
         * 
         * Instead we can hack it by returning only the first part of the string if it contains a space.
         */
        public object FriendlyResolvedValue()
        {
            if (ResolvedValue != null && ResolvedValue.ToString().Contains(" "))
            {
                return ResolvedValue.ToString().Split(' ')[0];
            }
            return ResolvedValue;
        }

        public override string ToString()
        {
            return $"Token[{ Value }, { Type }, { ResolvedValue }]";
        }
    }
}
