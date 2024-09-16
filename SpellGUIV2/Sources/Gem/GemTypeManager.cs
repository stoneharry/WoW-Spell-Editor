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
            GemTypes.Add(new GemType(2, 1039, "Red", 170000, 44920, 1001, 40000));
            GemTypes.Add(new GemType(4, 1470, "Yellow", 170002, 44926, 1002, 41000));
            GemTypes.Add(new GemType(8, 2451, "Blue", 170001, 44930, 1000, 42000));
            GemTypes.Add(new GemType(12, 1037, "Green", 0, 0, 0, 0)); // TODO: Needs data created
            GemTypes.Add(new GemType(126, 1518, "Purple", 0, 43108, 1003, 43000));
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
