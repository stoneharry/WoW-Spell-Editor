using SpellEditor.Sources.Config;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SpellEditor.Sources.DBC
{
    class SpellRuneCost : AbstractDBC
    {
        private MainWindow main;
        private DBAdapter adapter;

        public List<SpellRuneCostLookup> Lookups = new List<SpellRuneCostLookup>();

        public SpellRuneCost(MainWindow window, DBAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile<SpellRuneCost_DBC_Record>("DBC/SpellRuneCost.dbc");

                int boxIndex = 1;
                main.RuneCost.Items.Add(0);
                SpellRuneCostLookup t;
                t.ID = 0;
                t.comboBoxIndex = 0;
                Lookups.Add(t);

                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];

                    uint id = (uint) record["ID"];
                    uint[] runeCosts = (uint[]) record["RuneCost"];
                    uint runepowerGain = (uint) record["RunePowerGain"];

                    SpellRuneCostLookup temp;
                    temp.ID = id;
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);

                    main.RuneCost.Items.Add($"Cost [{ string.Join(", ", runeCosts) }] Gain [{ runepowerGain }] ID [{ id }]");

                    ++boxIndex;
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

        public void UpdateSpellRuneCostSelection()
        {
			uint ID = uint.Parse(adapter.query(string.Format("SELECT `RuneCostID` FROM `{0}` WHERE `ID` = '{1}'", adapter.Table, main.selectedID)).Rows[0][0].ToString());
            if (ID == 0)
            {
                main.RuneCost.threadSafeIndex = 0;
                return;
            }
            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    main.RuneCost.threadSafeIndex = Lookups[i].comboBoxIndex;
                    break;
                }
            }
        }

        public struct SpellRuneCostLookup
        {
            public uint ID;
            public int comboBoxIndex;
        };

        public struct SpellRuneCost_DBC_Record
        {
#pragma warning disable 0649
#pragma warning disable 0169
            public uint ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] RuneCost;
            public uint RunePowerGain;
#pragma warning restore 0649
#pragma warning restore 0169
        };
    };
}
