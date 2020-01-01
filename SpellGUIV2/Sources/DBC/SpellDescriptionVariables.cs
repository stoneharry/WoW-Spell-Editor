using SpellEditor.Sources.Controls;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    class SpellDescriptionVariables : AbstractDBC, IBoxContentProvider
    {
        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        public SpellDescriptionVariables()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellDescriptionVariables.dbc");

            Lookups.Add(new DBCBoxContainer(0, "0", 0));

            int boxIndex = 1;
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];
                uint id = (uint) record["ID"];
                uint offset = (uint) record["Formula"];
                string description = offset == 0 ? "" : Reader.LookupStringOffset(offset);

                Label label = new Label();
                label.Content = id + ": " + (description.Length <= 30 ? description : (description.Substring(0, 29) + "..."));
                label.ToolTip = id + ": " + description;

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

        public int UpdateSpellDescriptionVariablesSelection(uint ID)
        {
            var match = Lookups.Find((entry) => entry.ID == ID);
            return match != null ? match.ComboBoxIndex : 0;
        }
    };
}
