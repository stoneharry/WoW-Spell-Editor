using SpellEditor.Sources.Controls;
using SpellEditor.Sources.VersionControl;
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
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellRange.dbc");

            int boxIndex = 0;
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];

                var versionId = WoWVersionManager.GetInstance().SelectedVersion().Identity;
                // Ugly hardcode
                string name = "";
                string rangeString = "";
                // 335 handling
                if (versionId == 335)
                {
                    float MinimumRangeHostile = (float)record["MinimumRangeHostile"];
                    float MaximumRangeHostile = (float)record["MaximumRangeHostile"];
                    float MinimumRangeFriend = (float)record["MinimumRangeFriend"];
                    float MaximumRangeFriend = (float)record["MaximumRangeFriend"];

                    name = GetAllLocaleStringsForField("Name", record);
                    name += $"\nHostile: { MinimumRangeHostile } - { MaximumRangeHostile }\t Friend: { MinimumRangeFriend } - { MaximumRangeFriend }";
                    rangeString = MaximumRangeHostile > MaximumRangeFriend ? MaximumRangeHostile.ToString() : MaximumRangeFriend.ToString();
                }
                else
                {
                    // 112 versionId fields
                    name = GetAllLocaleStringsForField("Name", record);
                    float minRange = (float)record["MinimumRange"];
                    float maxRange = (float)record["MaximumRange"];
                    name += $"\n{minRange} - {maxRange}";
                    rangeString = maxRange.ToString();
                }

                var id = (uint)record["ID"];

                var entry = new SpellRangeBoxContainer(id, name, boxIndex);
                entry.RangeString = rangeString;
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
