using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    class SpellDescriptionVariables : AbstractDBC
    {
        private MainWindow main;
        private IDatabaseAdapter adapter;

        public List<SpellDescriptionVariablesLookup> Lookups = new List<SpellDescriptionVariablesLookup>();

        public SpellDescriptionVariables(MainWindow window, IDatabaseAdapter adapter)
        {
            main = window;
            this.adapter = adapter;

            try
            {
                ReadDBCFile("DBC/SpellDescriptionVariables.dbc");

                int boxIndex = 1;
                main.SpellDescriptionVariables.Items.Add(0);
                SpellDescriptionVariablesLookup t;
                t.ID = 0;
                t.comboBoxIndex = 0;
                Lookups.Add(t);

                for (uint i = 0; i < Header.RecordCount; ++i)
                {
                    var record = Body.RecordMaps[i];
                    uint id = (uint) record["ID"];
                    uint offset = (uint) record["Formula"];
                    string description = offset == 0 ? "" : Reader.LookupStringOffset(offset);

                    SpellDescriptionVariablesLookup temp;
                    temp.ID = id;
                    temp.comboBoxIndex = boxIndex;
                    Lookups.Add(temp);
                    Label label = new Label();
                    label.Content = id + ": " + (description.Length <= 30 ? description : (description.Substring(0, 29) + "..."));
                    label.ToolTip = id + ": " + description;
                    main.SpellDescriptionVariables.Items.Add(label);

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

        public void UpdateSpellDescriptionVariablesSelection()
        {
            uint ID = uint.Parse(adapter.Query(string.Format("SELECT `SpellDescriptionVariableID` FROM `{0}` WHERE `ID` = '{1}'", "spell", main.selectedID)).Rows[0][0].ToString());

            if (ID == 0)
            {
                main.SpellDescriptionVariables.threadSafeIndex = 0;
                return;
            }

            for (int i = 0; i < Lookups.Count; ++i)
            {
                if (ID == Lookups[i].ID)
                {
                    main.SpellDescriptionVariables.threadSafeIndex = Lookups[i].comboBoxIndex;
                    break;
                }
            }
        }

        public struct SpellDescriptionVariablesLookup
        {
            public uint ID;
            public int comboBoxIndex;
        };
    };
}
