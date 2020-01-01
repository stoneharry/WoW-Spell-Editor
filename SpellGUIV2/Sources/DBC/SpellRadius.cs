using SpellEditor.Sources.Controls;
using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{
    class SpellRadius : AbstractDBC, IBoxContentProvider
    {
        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        public SpellRadius()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellRadius.dbc");

            Lookups.Add(new DBCBoxContainer(0, "0 - 0\t(Radius - MaximumRadius)", 0));

            int boxIndex = 1;
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];

                float radius = (float) record["Radius"];
                float maximumRadius = (float) record["MaximumRadius"];
                uint id = (uint) record["ID"];
                string label = $"{ radius } - { maximumRadius}";

                Lookups.Add(new DBCBoxContainer(id, label, boxIndex));

                ++boxIndex;
            }
            Reader.CleanStringsMap();
            // In this DBC we don't actually need to keep the DBC data now that
            // we have extracted the lookup tables. Nulling it out may help with
            // memory consumption.
            Reader = null;
            Body = null;
        }

        public List<DBCBoxContainer> GetAllBoxes()
        {
            return Lookups;
        }

        public int UpdateRadiusIndexes(uint ID)
        {
            var match = Lookups.Find((entry) => entry.ID == ID);
            return match != null ? match.ComboBoxIndex : 0;
        }
    };
}
