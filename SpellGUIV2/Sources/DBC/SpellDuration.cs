using SpellEditor.Sources.Controls;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    class SpellDuration : AbstractDBC, IBoxContentProvider
    {
        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        public SpellDuration()
        {
            ReadDBCFile("DBC/SpellDuration.dbc");

            Lookups.Add(new DBCBoxContainer(0, "0", 0));

            int boxIndex = 1;
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];
                var id = (uint) record["ID"];

                Label label = new Label();
                label.Content = $"{ record["BaseDuration"] }ms BaseDuration, { record["PerLevel"] } BaseDuration, { record["MaximumDuration"] }ms MaximumDuration, { id } ID";

                Lookups.Add(new DBCBoxContainer(id, label, boxIndex));
                ++boxIndex;
            }
            Reader.CleanStringsMap();
        }

        public List<DBCBoxContainer> GetAllBoxes()
        {
            return Lookups;
        }

        public int UpdateDurationIndexes(uint ID)
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
    };
}
