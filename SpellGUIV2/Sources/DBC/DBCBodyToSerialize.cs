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
        public Dictionary<int, int> OffsetStorage;
        public Dictionary<int, string> ReverseStorage;
        public Dictionary<uint, int> SpellToIndex;

        // Returns new header stringBlockOffset
        public int GenerateStringOffsetsMap(Binding.Binding binding)
        {
            // Start at 1 as 0 is hardcoded as '\0'
            int stringBlockOffset = 1;
            // Performance gain by collecting the fields to iterate first
            var fields = binding.Fields.Where(field => field.Type == BindingType.STRING_OFFSET).ToArray();
            OffsetStorage = new Dictionary<int, int>();
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
                    var key = str.GetHashCode();
                    if (!OffsetStorage.ContainsKey(key))
                    {
                        OffsetStorage.Add(key, stringBlockOffset);
                        ReverseStorage.Add(stringBlockOffset, str);
                        stringBlockOffset += Encoding.UTF8.GetByteCount(str) + 1;
                    }
                }
            }
            return stringBlockOffset;
        }

        public void BuildSpellToIndex()
        {
            SpellToIndex = new Dictionary<uint, int>(Records.Count);

            for (int i = 0; i < Records.Count; i++)
            {
                var record = Records[i];

                if (!record.TryGetValue("ID", out var spellId))
                    continue;

                if (spellId == null)
                    continue;

                SpellToIndex[(uint)spellId] = i;
            }
        }

        public void RemoveSpell(uint spellId)
        {
            if (!SpellToIndex.TryGetValue(spellId, out int removedIndex))
                return;

            Records.RemoveAt(removedIndex);

            SpellToIndex.Remove(spellId);

            foreach (var key in SpellToIndex.Keys.ToList())
            {
                if (SpellToIndex[key] > removedIndex)
                    SpellToIndex[key]--;
            }
        }

        public void AddSpell(uint spellId, Dictionary<string, object> data)
        {
            if (SpellToIndex.ContainsKey(spellId))
                return;

            uint check = spellId;
            var newIndex = 0;

            while (check > 0)
            {
                check--;

                if (SpellToIndex.TryGetValue(check, out int existingIndex))
                {
                    newIndex = existingIndex + 1;
                    break;
                }
            }

            foreach (var key in SpellToIndex.Keys.ToList())
            {
                if (SpellToIndex[key] >= newIndex)
                    SpellToIndex[key]++;
            }

            SpellToIndex[spellId] = newIndex;

            Records.Insert(newIndex, data);
        }

        public int GetIndexFromSpell(uint spellId)
        {
            if (!SpellToIndex.TryGetValue(spellId, out int index))
                return 0;

            return index;
        }
    }
}
