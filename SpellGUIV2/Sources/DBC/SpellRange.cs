using SpellEditor.Sources.Config;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SpellEditor.Sources.DBC
{
    class SpellRange : AbstractDBC
    {
        private MainWindow main;
        private DBAdapter adapter;

        public List<SpellRangeLookup> Lookups = new List<SpellRangeLookup>();

        public SpellRange(MainWindow window, DBAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile<SpellRange_DBC_Record>("DBC/SpellRange.dbc");

                int boxIndex = 0;
                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];
                    
                    float MinimumRangeHostile = (float) record["MinimumRangeHostile"];
                    float MaximumRangeHostile = (float) record["MaximumRangeHostile"];
                    float MinimumRangeFriend = (float) record["MinimumRangeFriend"];
                    float MaximumRangeFriend = (float) record["MaximumRangeFriend"];

                    uint offset = ((uint[]) record["Name"])[window.GetLanguage()];
                    if (offset == 0)
                        continue;
                    string name = reader.LookupStringOffset(offset);

                    SpellRangeLookup temp;
                    temp.ID = (uint)record["ID"];
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);

                    main.Range.Items.Add($"{ name }\nHostile: { MinimumRangeHostile } - { MaximumRangeHostile }\t Friend: { MinimumRangeFriend } - { MaximumRangeFriend }");

                    boxIndex++;
                }
                reader.CleanStringsMap();
                // In this DBC we don't actually need to keep the DBC data now that
                // we have extracted the lookup tables. Nulling it out may help with
                // memory consumption.
                reader = null;
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
            uint ID = uint.Parse(adapter.query(string.Format("SELECT `RangeIndex` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0][0].ToString());
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
        };

        public struct SpellRange_DBC_Record
        {
// These fields are used through reflection, disable warning
#pragma warning disable 0649
#pragma warning disable 0169
            public uint ID;
            public float MinimumRangeHostile;
            public float MinimumRangeFriend;
            public float MaximumRangeHostile;
            public float MaximumRangeFriend;
            public int Type;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public uint[] Name;
            public uint NameFlags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public uint[] ShortName;
            public uint ShortNameFlags;
#pragma warning restore 0649
#pragma warning restore 0169
        };
    };
}
