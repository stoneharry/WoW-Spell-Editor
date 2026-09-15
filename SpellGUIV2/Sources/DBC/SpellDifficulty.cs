using NLog;
using SpellEditor.Sources.Controls;
using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SpellEditor.Sources.DBC
{
    class SpellDifficulty : AbstractDBC, IBoxContentProvider
    {
        private static CancellationTokenSource _CancelTokenSource;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public List<DBCBoxContainer> Lookups = new List<DBCBoxContainer>();

        private IDatabaseAdapter _adapter;
        private int _locale;

        // needs adapter or spell DBC to lookup spells
        public SpellDifficulty(IDatabaseAdapter adapter, int locale)
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellDifficulty.dbc");

            _adapter = adapter;
            _locale = locale;
        }

        public override void LoadGraphicUserInterface()
        {
            if (Body.RecordMaps == null)
                return;

            Lookups.Add(new DBCBoxContainer(0, new Label { Content = "0" }, 0));

            // Build quick stuff immediately
            int boxIndex = 1;
            for (int i = 0; i < Header.RecordCount; ++i)
            {
                var record = Body.RecordMaps[i];
                var id = (uint)record["ID"];
                var content = id + ": ";
                var label = new Label
                {
                    Content = content.Substring(0, content.Length - 2)
                };

                Lookups.Add(new DBCBoxContainer(id, label, boxIndex));

                boxIndex++;
            }

            // Lazily load tooltips
            /* Seems to point to other spells, for example:
                Id: 6
                Normal10Men: 50864 = Omar's Seal of Approval, You have Omar's 10 Man Normal Seal of Approval!
                Normal25Men: 69848 = Omar's Seal of Approval, You have Omar's 25 Man Normal Seal of Approval!
                Heroic10Men: 69849 = Omar's Seal of Approval, You have Omar's 10 Man Heroic Seal of Approval!
                Heroic25Men: 69850 = Omar's Seal of Approval, You have Omar's 25 Man Heroic Seal of Approval!
            */
            _CancelTokenSource?.Cancel();
            _CancelTokenSource = new CancellationTokenSource();
            var cancelToken = _CancelTokenSource.Token;
            Task.Factory.StartNew(() =>
            {
                var watch = new Stopwatch();
                watch.Start();
                Logger.Debug("Loading SpellDifficulty tooltips lazily");
                var column = "SpellName" + Math.Max(0, _locale - 1);

                // One query for every referenced spell instead of four per record
                var referenced = new HashSet<string>();
                for (int i = 1; i < Lookups.Count; ++i)
                {
                    var record = Body.RecordMaps[i - 1];
                    for (int diffIndex = 1; diffIndex <= 4; ++diffIndex)
                    {
                        var difficulty = record["Difficulties" + diffIndex].ToString();
                        if (difficulty != "0")
                            referenced.Add(difficulty);
                    }
                }
                var names = new Dictionary<string, string>();
                if (referenced.Count > 0)
                {
                    var table = _adapter.Query($"SELECT `ID`, {column} FROM `spell` WHERE `ID` IN ({string.Join(",", referenced)})");
                    foreach (System.Data.DataRow row in table.Rows)
                        names[row[0].ToString()] = row[1].ToString();
                }

                if (cancelToken.IsCancellationRequested)
                {
                    Logger.Debug($"Aborted SpellDifficulty Tooltips loading after {watch.ElapsedMilliseconds}ms");
                    return;
                }

                var updates = new List<Action>(Lookups.Count);
                for (int i = 1; i < Lookups.Count; ++i)
                {
                    var record = Body.RecordMaps[i - 1];
                    var label = Lookups[i].ItemLabel();
                    var tooltip = "";
                    var content = ": ";
                    for (int diffIndex = 1; diffIndex <= 4; ++diffIndex)
                    {
                        var difficulty = record["Difficulties" + diffIndex].ToString();
                        content += difficulty + ", ";
                        tooltip += "[" + difficulty + "] ";
                        if (names.TryGetValue(difficulty, out var name))
                            tooltip += name;
                        tooltip += "\n";
                    }
                    updates.Add(() =>
                    {
                        label.Content += content;
                        label.ToolTip = tooltip;
                    });
                }
                if (updates.Count > 0)
                {
                    Lookups[1].ItemLabel().Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        foreach (var update in updates)
                            update();
                    }));
                }
                watch.Stop();
                Logger.Debug($"SpellDifficulty Tooltips finished loading in {watch.ElapsedMilliseconds}ms");

                // In this DBC we don't actually need to keep the DBC data now that
                // we have extracted the lookup tables. Nulling it out may help with
                // memory consumption.
                CleanStringsMap();
                CleanBody();
            });
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
