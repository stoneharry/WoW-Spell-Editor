using System.Collections.Generic;
using System.IO;

namespace SpellEditor.Sources.Binding
{
    public class Binding
    {
        public readonly BindingEntry[] Fields;
        public readonly string Name;

        public Binding(string fileName, List<BindingEntry> bindingEntryList)
        {
            Name = Path.GetFileNameWithoutExtension(fileName);
            Fields = bindingEntryList.ToArray();
        }
    }
}
