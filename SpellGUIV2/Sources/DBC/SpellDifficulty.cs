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

        // needs adapter or spell DBC to lookup spells
        // does not save a reference to the adapter
        public SpellDifficulty(IDatabaseAdapter adapter, int locale)
        {
            ReadDBCFile(Config.Config.DbcDirectory + "\\SpellDifficulty.dbc");

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
                var column = "SpellName" + (locale - 1);
                for (int i = 1; i < Lookups.Count; ++i)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        Logger.Debug($"Aborted SpellDifficulty Tooltips loading after {watch.ElapsedMilliseconds}ms");
                        break;
                    }
                    if (i % 25 == 0)
                    {
                        Logger.Debug($"Loaded {i} / {Lookups.Count} difficulty tooltips");
                    }
                    var record = Body.RecordMaps[i - 1];
                    var label = Lookups[i].ItemLabel();
                    var tooltip = "";
                    var content = ": ";
                    for (int diffIndex = 1; diffIndex <= 4; ++diffIndex)
                    {
                        var difficulty = record["Difficulties" + diffIndex].ToString();
                        content += difficulty + ", ";
                        tooltip += "[" + difficulty + "] ";
                        var result = adapter.QuerySingleValue(string.Format("SELECT {0} FROM `{1}` WHERE `ID` = '{2}' LIMIT 1", column, "spell", difficulty));
                        if (result != null)
                        {
                            tooltip += result.ToString();
                        }
                        tooltip += "\n";
                    }
                    label.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        label.Content += content;
                        label.ToolTip = tooltip;
                    }));
                }
                watch.Stop();
                Logger.Debug($"SpellDifficulty Tooltips finished loading in {watch.ElapsedMilliseconds}ms");

                Reader.CleanStringsMap();
                // In this DBC we don't actually need to keep the DBC data now that
                // we have extracted the lookup tables. Nulling it out may help with
                // memory consumption.
                Reader = null;
                Body = null;
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
