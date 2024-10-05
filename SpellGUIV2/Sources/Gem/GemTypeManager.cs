using System;
using System.Collections.Generic;
using System.Linq;
using static SpellEditor.Sources.Constants.GemType;

namespace SpellEditor.Sources.Constants
{
    public class GemTypeManager
    {
        public readonly static GemTypeManager Instance = new GemTypeManager();

        public readonly List<GemType> GemTypes = new List<GemType>();

        private GemTypeManager()
        {
            GemTypes.Add(new GemType((uint)GemTypeEnum.Red, 1039, "Red", 170000, 44920, 1001, 40000));
            GemTypes.Add(new GemType((uint)GemTypeEnum.Yellow, 1470, "Yellow", 170002, 44926, 1002, 41000));
            GemTypes.Add(new GemType((uint)GemTypeEnum.Blue, 2451, "Blue", 170001, 44930, 1000, 42000));
            GemTypes.Add(new GemType((uint)GemTypeEnum.Green, 1037, "Green", 170004, 44921, 1005, 44000));
            GemTypes.Add(new GemType((uint)GemTypeEnum.Purple, 1518, "Purple", 0, 43108, 1003, 43000));
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
