using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    class SpellDifficulty : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;

        public List<SpellDifficultyLookup> Lookups = new List<SpellDifficultyLookup>();

        public SpellDifficulty(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile("DBC/SpellDifficulty.dbc");

                int boxIndex = 1;
                main.Difficulty.Items.Add(0);
                SpellDifficultyLookup t;
                t.ID = 0;
                t.comboBoxIndex = 0;
                Lookups.Add(t);

                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];
                    /*
                     * Seems to point to other spells, for example:
                     Id: 6
                     Normal10Men: 50864 = Omar's Seal of Approval, You have Omar's 10 Man Normal Seal of Approval!
                     Normal25Men: 69848 = Omar's Seal of Approval, You have Omar's 25 Man Normal Seal of Approval!
                     Heroic10Men: 69849 = Omar's Seal of Approval, You have Omar's 10 Man Heroic Seal of Approval!
                     Heroic25Men: 69850 = Omar's Seal of Approval, You have Omar's 25 Man Heroic Seal of Approval!
                    */
                    uint id = (uint) record["ID"];

                    SpellDifficultyLookup temp;
                    temp.ID = id;
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);

                    Label label = new Label();
                    string content = id + ": ";
                    

                    string tooltip = "";
                    for (int diffIndex = 1; diffIndex <= 4; ++diffIndex)
                    {
                        var difficulty = record["Difficulties" + diffIndex].ToString();
                        content += difficulty + ", ";
                        tooltip += "[" + difficulty + "] ";
                        var rows = adapter.Query(string.Format("SELECT * FROM `{0}` WHERE `ID` = '{1}' LIMIT 1", "spell", difficulty)).Rows;
                        if (rows.Count > 0)
                        {
                            var row = rows[0];
                            string selectedLocale = "";
                            for (int locale = 0; locale < 8; ++locale)
                            {
                                var name = row["SpellName" + locale].ToString();
                                if (name.Length > 0)
                                {
                                    selectedLocale = name;
                                    break;
                                }
                            }
                            tooltip += selectedLocale;
                        }
                        tooltip += "\n";
                    }
                    label.Content = content.Substring(0, content.Length - 2);
                    label.ToolTip = tooltip;

                    main.Difficulty.Items.Add(label);

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

        public void UpdateDifficultySelection()
        {
            uint ID = uint.Parse(adapter.Query(string.Format("SELECT `SpellDifficultyID` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID)).Rows[0][0].ToString());
            if (ID == 0)
            {
                main.Difficulty.threadSafeIndex = 0;
                return;
            }
            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    main.Difficulty.threadSafeIndex = Lookups[i].comboBoxIndex;
                    break;
                }
            }
        }
    }

    public struct SpellDifficultyLookup
    {
        public uint ID;
        public int comboBoxIndex;
    };
}
