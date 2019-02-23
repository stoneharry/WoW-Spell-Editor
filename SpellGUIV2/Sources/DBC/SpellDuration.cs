using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    public class SpellDuration : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;

        public List<SpellDurationLookup> Lookups = new List<SpellDurationLookup>();

        public SpellDuration(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile("DBC/SpellDuration.dbc");

                int boxIndex = 1;
                main.Duration.Items.Add(0);
                SpellDurationLookup t;
                t.ID = 0;
                t.comboBoxIndex = 0;
                Lookups.Add(t);

                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];

                    SpellDurationLookup temp;
                    temp.ID = (uint) record["ID"];
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);

                    Label label = new Label();
                    label.Content = $"{ record["BaseDuration"] }ms BaseDuration, { record["PerLevel"] } BaseDuration, { record["MaximumDuration"] }ms MaximumDuration, { temp.ID } ID";
                    main.Duration.Items.Add(label);

                    ++boxIndex;
                }
                Reader.CleanStringsMap();
            }
            catch (Exception ex)
            {
                window.HandleErrorMessage(ex.Message);
                return;
            }      
        }

        public void UpdateDurationIndexes()
        {
            uint ID = uint.Parse(adapter.Query(string.Format("SELECT `DurationIndex` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID)).Rows[0][0].ToString());
            if (ID == 0)
            {
                main.Duration.threadSafeIndex = 0;
                return;
            }
            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    main.Duration.threadSafeIndex = Lookups[i].comboBoxIndex;
                    break;
                }
            }
        }

        public struct SpellDurationLookup
        {
            public uint ID;
            public int comboBoxIndex;
        };
    };
}
