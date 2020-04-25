using SpellEditor.Sources.Controls;
using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    class SpellDifficulty : AbstractDBC, IBoxContentProvider
    {
        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        // needs adapter or spell DBC to lookup spells
        // does not save a reference to the adapter
        public SpellDifficulty(IDatabaseAdapter adapter)
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellDifficulty.dbc");

            Lookups.Add(new DBCBoxContainer(0, "0", 0));

            int boxIndex = 1;
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

                Label label = new Label();
                string content = id + ": ";
                string tooltip = "";
                for (int diffIndex = 1; diffIndex <= 4; ++diffIndex)
                {
                    var difficulty = record["Difficulties" + diffIndex].ToString();
                    content += difficulty + ", ";
                    tooltip += "[" + difficulty + "] ";
                    // FIXME(Harry): This is VERY slow connecting to a remote server, could do with a refactor
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

                Lookups.Add(new DBCBoxContainer(id, label, boxIndex));

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

        public int UpdateDifficultySelection(uint ID)
        {
            if (ID == 0)
            {
                return 0;
            }
            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    return Lookups[i].ComboBoxIndex;
                }
            }
            return 0;
        }
    }
}
