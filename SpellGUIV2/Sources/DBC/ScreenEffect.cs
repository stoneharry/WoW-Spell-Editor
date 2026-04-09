using SpellEditor.Sources.Controls;
using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.DBC
{
    class ScreenEffect : AbstractDBC, IBoxContentProvider
    {
        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        public ScreenEffect()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\ScreenEffect.dbc");
        }

        public List<DBCBoxContainer> GetAllBoxes()
        {
            return Lookups;
        }

        public override void LoadGraphicUserInterface()
        {
            // Lookups.Add(new DBCBoxContainer(0, "None", 0));

            int boxIndex = Lookups.Count;
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];

                uint id = uint.Parse(record["id"].ToString());
                var name = GetStringForField("Name", record);

                Lookups.Add(new DBCBoxContainer(id, name, boxIndex));

                ++boxIndex;
            }

            // In this DBC we don't actually need to keep the DBC data now that
            // we have extracted the lookup tables. Nulling it out may help with
            // memory consumption.
            CleanStringsMap();
            CleanBody();
        }

        public int UpdateScreenEffectSelection(uint ID)
        {
            var match = Lookups.Find((entry) => entry.ID == ID);
            return match != null ? match.ComboBoxIndex : 0;
        }
    };
}
