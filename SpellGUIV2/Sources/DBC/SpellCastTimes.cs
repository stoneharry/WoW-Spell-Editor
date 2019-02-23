using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    class SpellCastTimes : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;

        public List<SpellCastTimeLookup> Lookups = new List<SpellCastTimeLookup>();

        public SpellCastTimes(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;
            
            try
            {
                ReadDBCFile("DBC/SpellCastTimes.dbc");

                int boxIndex = 0;

                SpellCastTimeLookup t;
                t.ID = 0;
                t.comboBoxIndex = 0;
                Lookups.Add(t);

                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];

                    Label castTime = new Label();
                    castTime.Content = record["CastingTime"].ToString() + "\t";
                    castTime.ToolTip = "CastTime\t\t" + record["CastingTime"] + "\n" +
                        "PerLevel\t\t" + record["CastingTimePerLevel"] + "\n" +
                        "MinimumCastingTime\t" + record["MinimumCastingTime"] + "\n";

                    SpellCastTimeLookup temp;
                    temp.ID = (uint)record["ID"];
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);

                    main.CastTime.Items.Add(castTime);
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

        public void UpdateCastTimeSelection()
        {
            uint ID = uint.Parse(adapter.Query(string.Format("SELECT `CastingTimeIndex` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID)).Rows[0][0].ToString());
            if (ID == 0)
            {
                main.CastTime.threadSafeIndex = 0;
                return;
            }

            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    main.CastTime.threadSafeIndex = Lookups[i].comboBoxIndex;
                    break;
                }
            }
        }
    }

    public struct SpellCastTimeLookup
    {
        public uint ID;
        public int comboBoxIndex;
    };
}
