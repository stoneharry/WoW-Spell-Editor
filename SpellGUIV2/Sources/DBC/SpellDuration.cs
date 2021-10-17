using SpellEditor.Sources.Controls;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SpellEditor.Sources.DBC
{
    class SpellDuration : AbstractDBC, IBoxContentProvider
    {
        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        class DurationBox : DBCBoxContainer
        {
            public long baseDuration;

            public DurationBox(long id, string name, int comboBoxIndex, long baseDuration)
                : base(id, name, comboBoxIndex)
                => this.baseDuration = baseDuration;

            public DurationBox(long id, Label nameLabel, int comboBoxIndex, long baseDuration)
                : base(id, nameLabel, comboBoxIndex)
                => this.baseDuration = baseDuration;
        }

        private static int CompareDurationBoxes(DBCBoxContainer x, DBCBoxContainer y)
        {
            if (x == null || ((x as DurationBox) == null))
            {
                if (y == null || ((y as DurationBox) == null))
                    return 0;
                else
                    return - 1;
            }
            else if (y == null)
                return 1;
            else
            {
                var durX = x as DurationBox;
                var durY = y as DurationBox;
                return durX.baseDuration.CompareTo(durY.baseDuration);
            }
        }

        public SpellDuration()
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellDuration.dbc");

            Lookups.Add(new DurationBox(0, "0", 0, 0));

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
                Lookups.Add(new DurationBox(id, label, 0,
                    uint.Parse(record["BaseDuration"].ToString()) +
                    int.Parse(record["MaximumDuration"].ToString())
                    ));
            }
            Lookups.Sort(Comparer<DBCBoxContainer>.Create(CompareDurationBoxes));
            for (int i = 0; i < Lookups.Count; ++i)
                Lookups[i].ComboBoxIndex = i;

            Reader.CleanStringsMap();
        }

        private string GetFriendlyDuration(object data)
        {
            var success = double.TryParse(data.ToString() + ".00", out double floatDur);
            if (!success)
            {
                return data.ToString();
            }
            var trimMillisChars = new char[] { '0' };
            var time = TimeSpan.FromMilliseconds(floatDur);
            var days = time.Days > 0 ? $"{time.Days}day " : string.Empty;
            var hours = time.Hours > 0 ? $"{time.Hours}hour " : string.Empty;
            var minutes = time.Minutes > 0 ? $"{time.Minutes}min " : string.Empty;
            var seconds = (time.Seconds > 0 || time.Milliseconds > 0) ? 
                $"{(time.Milliseconds > 0 ? (time.Seconds + "." + time.Milliseconds.ToString().TrimEnd(trimMillisChars)) : time.Seconds.ToString())}sec" : string.Empty;
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
