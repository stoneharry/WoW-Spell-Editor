using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{
    class SpellRuneCost : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;

        public List<SpellRuneCostLookup> Lookups = new List<SpellRuneCostLookup>();

        public SpellRuneCost(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile("DBC/SpellRuneCost.dbc");

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
                    uint runeCost1 = (uint)record["RuneCost1"];
                    uint runeCost2 = (uint)record["RuneCost2"];
                    uint runeCost3 = (uint)record["RuneCost3"];
                    uint runepowerGain = (uint) record["RunePowerGain"];

                    SpellRuneCostLookup temp;
                    temp.ID = id;
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);

                    main.RuneCost.Items.Add($"Cost [{runeCost1}, {runeCost2}, {runeCost3}] Gain [{ runepowerGain }] ID [{ id }]");

                    ++boxIndex;
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

        public void UpdateSpellRuneCostSelection()
        {
            uint ID = uint.Parse(adapter.Query(string.Format("SELECT `RuneCostID` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID)).Rows[0][0].ToString());
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
    };
}
