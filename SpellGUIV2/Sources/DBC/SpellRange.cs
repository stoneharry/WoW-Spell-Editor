using SpellEditor.Sources.Controls;
using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{
    class SpellRange : AbstractDBC, IBoxContentProvider
    {
        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        public class SpellRangeBoxContainer : DBCBoxContainer {
            public string RangeString;

            public SpellRangeBoxContainer(uint ID, string Name, int ComboBoxIndex) : base(ID, Name, ComboBoxIndex)
            {    
            }
        }

        public SpellRange()
        {
            ReadDBCFile("DBC/SpellRange.dbc");

            int boxIndex = 0;
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];
                    
                float MinimumRangeHostile = (float) record["MinimumRangeHostile"];
                float MaximumRangeHostile = (float) record["MaximumRangeHostile"];
                float MinimumRangeFriend = (float) record["MinimumRangeFriend"];
                float MaximumRangeFriend = (float) record["MaximumRangeFriend"];

                string name = GetAllLocaleStringsForField("Name", record);
                name += $"\nHostile: { MinimumRangeHostile } - { MaximumRangeHostile }\t Friend: { MinimumRangeFriend } - { MaximumRangeFriend }";
                var id = (uint)record["ID"];

                var entry = new SpellRangeBoxContainer(id, name, boxIndex);
                entry.RangeString = MaximumRangeHostile > MaximumRangeFriend ? MaximumRangeHostile.ToString() : MaximumRangeFriend.ToString();
                Lookups.Add(entry);

                boxIndex++;
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

        public int UpdateSpellRangeSelection(uint ID)
        {
            var match = Lookups.Find((entry) => entry.ID == ID);
            return match != null ? match.ComboBoxIndex : 0;
        }
    };
}
