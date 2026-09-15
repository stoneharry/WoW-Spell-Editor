using SpellEditor.Sources.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpellEditor.Sources.DBC
{
    public class DBCBodyToSerialize
    {
        public List<Dictionary<string, object>> Records;
        public Dictionary<string, int> OffsetStorage;
        public Dictionary<int, string> ReverseStorage;

        // Returns new header stringBlockOffset
        public int GenerateStringOffsetsMap(Binding.Binding binding)
        {
            // Start at 1 as 0 is hardcoded as '\0'
            int stringBlockOffset = 1;
            // Performance gain by collecting the fields to iterate first
            var fields = binding.Fields.Where(field => field.Type == BindingType.STRING_OFFSET).ToArray();
            OffsetStorage = new Dictionary<string, int>(StringComparer.Ordinal);
            ReverseStorage = new Dictionary<int, string>();
            // Populate string <-> offset lookup maps
            for (int i = 0; i < Records.Count; ++i)
            {
                foreach (var entry in fields)
                {
                    var record = Records.ElementAt(i);
                    string str = record[entry.Name].ToString();
                    if (str.Length == 0)
                        continue;
                    if (!OffsetStorage.ContainsKey(str))
                    {
                        OffsetStorage.Add(str, stringBlockOffset);
                        ReverseStorage.Add(stringBlockOffset, str);
                        stringBlockOffset += Encoding.UTF8.GetByteCount(str) + 1;
                    }
                }
            }
            return stringBlockOffset;
        }
    }
}
