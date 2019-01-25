using System;

namespace SpellEditor.Sources.Binding
{
    public class BindingEntry
    {
        public readonly BindingType Type;
        public readonly string Name;

        public BindingEntry(string type, string name)
        {
            Name = name;
            Type = DetermineType(type);
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
