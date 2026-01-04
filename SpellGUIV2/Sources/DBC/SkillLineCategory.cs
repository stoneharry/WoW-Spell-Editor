using SpellEditor.Sources.Controls;
using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{
    class SkillLineCategory : AbstractDBC, IBoxContentProvider
    {
        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        public SkillLineCategory()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SkillLineCategory.dbc");
        }

        public List<DBCBoxContainer> GetAllBoxes()
        {
            return Lookups;
        }

        public override void LoadGraphicUserInterface()
        {
            // Lookups.Add(new DBCBoxContainer(0, "0", 0));

            int boxIndex = 1;
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];

                uint id = (uint)record["ID"];
                var name = GetAllLocaleStringsForField("Name", record);

                Lookups.Add(new DBCBoxContainer(id, name, boxIndex));

                ++boxIndex;
            }

            // In this DBC we don't actually need to keep the DBC data now that
            // we have extracted the lookup tables. Nulling it out may help with
            // memory consumption.
            CleanStringsMap();
            CleanBody();
        }

        public int UpdateSkillLineCategorySelection(uint ID)
        {
            var match = Lookups.Find((entry) => entry.ID == ID);
            return match != null ? match.ComboBoxIndex : 0;
        }
    };
}
