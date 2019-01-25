using System;

namespace SpellEditor.Sources.Binding
{
    public class BindingEntry
    {
        public readonly BindingType Type;
        public readonly string Name;

        public BindingEntry(string[] parts)
        {
            Name = parts[1];
            Type = DetermineType(parts[0]);
            if (parts.Length > 2 && parts[2].Equals("string", StringComparison.InvariantCultureIgnoreCase))
                Type = BindingType.STRING_OFFSET;
            if (Name.Length == 0)
                Type = BindingType.UNKNOWN;
        }

        private BindingType DetermineType(string type)
        {
            if (Enum.TryParse(type.ToUpper(), out BindingType result))
                return result;
            return BindingType.UNKNOWN;
        }
    }
}
