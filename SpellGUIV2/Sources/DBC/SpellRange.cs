using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{
    class SpellRange : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;

        public List<SpellRangeLookup> Lookups = new List<SpellRangeLookup>();

        public SpellRange(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
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

                    uint offset = (uint) record["Name" + (window.GetLanguage() + 1)];
                    if (offset == 0)
                        continue;
                    string name = Reader.LookupStringOffset(offset);

                    SpellRangeLookup temp;
                    temp.ID = (uint)record["ID"];
                    temp.comboBoxIndex = boxIndex;
                    temp.RangeString = MaximumRangeHostile > MaximumRangeFriend ? MaximumRangeHostile.ToString() : MaximumRangeFriend.ToString();
                    Lookups.Add(temp);

                    main.Range.Items.Add($"{ name }\nHostile: { MinimumRangeHostile } - { MaximumRangeHostile }\t Friend: { MinimumRangeFriend } - { MaximumRangeFriend }");

                    boxIndex++;
                }
                Reader.CleanStringsMap();
                // In this DBC we don't actually need to keep the DBC data now that
                // we have extracted the lookup tables. Nulling it out may help with
                // memory consumption.
                Reader = null;
                Body = null;
            }
            catch (Exception ex)
            {
                window.HandleErrorMessage(ex.Message);
                return;
            }
        }

        public void UpdateSpellRangeSelection()
        {
            uint ID = uint.Parse(adapter.Query(string.Format("SELECT `RangeIndex` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID)).Rows[0][0].ToString());
            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    main.Range.threadSafeIndex = Lookups[i].comboBoxIndex;
                    break;
                }
            }
        }

        public struct SpellRangeLookup
        {
            public uint ID;
            public int comboBoxIndex;
            public string RangeString;
        };
    };
}
