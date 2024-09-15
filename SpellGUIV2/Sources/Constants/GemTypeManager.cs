using System;
using System.Collections.Generic;
using System.Linq;

namespace SpellEditor.Sources.Constants
{
    public class GemTypeManager
    {
        public readonly static GemTypeManager Instance = new GemTypeManager();

        public readonly List<GemType> GemTypes = new List<GemType>();

        private GemTypeManager()
        {
            GemTypes.Add(new GemType(2, 1039, "Red"));
            GemTypes.Add(new GemType(4, 1470, "Yellow"));
            GemTypes.Add(new GemType(8, 2451, "Blue"));
            GemTypes.Add(new GemType(12, 1037, "Green"));
            GemTypes.Add(new GemType(126, 1518, "Purple"));
        }

        public GemType LookupGemType(uint id) => GemTypes.FirstOrDefault(type => type.Type == id);

        public GemType LookupGemTypeByName(string name) => GemTypes.FirstOrDefault(type => type.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public int LookupIndexByType(uint type)
        {
            for (int i = 0; i < GemTypes.Count; i++)
            {
                if (GemTypes[i].Type == type)
                    return i;
            }
            return -1;
        }
    }
}
