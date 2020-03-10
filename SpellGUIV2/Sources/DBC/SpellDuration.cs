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
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellDuration.dbc");

            Lookups.Add(new DBCBoxContainer(0, "0", 0));

            int boxIndex = 1;
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];
                var id = (uint) record["ID"];
                var baseDurStr = GetFriendlyDuration(record["BaseDuration"]);
                var maxDurStr = GetFriendlyDuration(record["MaximumDuration"]);
                var perLevelStr = record["PerLevel"].ToString();
                var label = new Label
                {
                    Content = $"ID: {id}\nBaseDur: {baseDurStr}\nMaxDur: {maxDurStr}\nPerLevel: {perLevelStr}"
                };
                Lookups.Add(new DBCBoxContainer(id, label, boxIndex));
                ++boxIndex;
            }
            Reader.CleanStringsMap();
        }

        private string GetFriendlyDuration(object data)
        {
            var success = double.TryParse(data.ToString() + ".00", out double floatDur);
            if (!success)
            {
                return data.ToString();
            }
            var time = TimeSpan.FromMilliseconds(floatDur);
            var days = time.Days > 0 ? $"{time.Days}day " : string.Empty;
            var hours = time.Hours > 0 ? $"{time.Hours}hour " : string.Empty;
            var minutes = time.Minutes > 0 ? $"{time.Minutes}min " : string.Empty;
            var seconds = time.Seconds > 0 ? $"{time.Seconds}sec" : string.Empty;
            return (days + hours + minutes + seconds).TrimEnd() + $" - {data}ms";
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
