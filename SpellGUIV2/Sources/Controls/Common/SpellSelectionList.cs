using NLog;
using SpellEditor.Sources.BLP;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.Locale;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls
{
    public class SpellSelectionList : ListBox
    {

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private int _contentsCount;
        private int _contentsIndex;
        private int _language;
        private IDatabaseAdapter _adapter;
        private readonly DataTable _table = new DataTable();

        public void Initialise()
        {
            _table.Columns.Add("id", typeof(uint));
            _table.Columns.Add("SpellName" + _language, typeof(string));
            _table.Columns.Add("Icon", typeof(uint));
        }

        public SpellSelectionList SetLanguage(int language)
        { 
            _language = language;
            return this;
        }

        public SpellSelectionList SetAdapter(IDatabaseAdapter adapter)
        {
            _adapter = adapter;
            return this;
        }

        public int GetLoadedRowCount() => _table.Rows.Count;

        public string GetSpellNameById(uint spellId)
        {
            var result = _table.Select($"id = {spellId}");
            return result.Length == 1 ? result[0]["SpellName" + (_language - 1)].ToString() : "";
        }

        public void PopulateSelectSpell(bool clearData = true)
        {
            if (_adapter == null)
                throw new Exception("Adapter has not been configured");
            if (_table.Columns.Count == 0)
                throw new Exception("Initialise has not been invoked");

            // Refresh language
            LocaleManager.Instance.MarkDirty();
            var newLocale = LocaleManager.Instance.GetLocale(_adapter);
            if (newLocale != _language)
            {
                _table.Columns["SpellName" + _language].ColumnName = "SpellName" + newLocale;
                SetLanguage(newLocale);
            }

            var selectSpellWatch = new Stopwatch();
            selectSpellWatch.Start();
            _contentsIndex = 0;
            _contentsCount = Items.Count;
            var worker = new SpellListQueryWorker(_adapter, selectSpellWatch) { WorkerReportsProgress = true };
            worker.ProgressChanged += _worker_ProgressChanged;

            worker.DoWork += delegate
            {
                // Validate
                if (worker.Adapter == null || !Config.Config.IsInit)
                    return;
                int locale = _language;
                if (locale > 0)
                    locale -= 1;

                // Clear Data
                if (clearData)
                    _table.Rows.Clear();

                const uint pageSize = 5000;
                uint lowerBounds = 0;
                DataRowCollection results = GetSpellNames(lowerBounds, 100, locale);
                lowerBounds += 100;
                // Edge case of empty table after truncating, need to send a event to the handler
                if (results != null && results.Count == 0)
                {
                    worker.ReportProgress(0, results);
                }
                while (results != null && results.Count != 0)
                {
                    worker.ReportProgress(0, results);
                    results = GetSpellNames(lowerBounds, pageSize, locale);
                    lowerBounds += pageSize;
                }
            };
            worker.RunWorkerAsync();
            worker.RunWorkerCompleted += (sender, args) =>
            {
                if (!(sender is SpellListQueryWorker spellListQueryWorker))
                    return;

                spellListQueryWorker.Watch.Stop();
                Logger.Info($"Loaded spell selection list contents in {spellListQueryWorker.Watch.ElapsedMilliseconds}ms");
            };
        }

        public void AddNewSpell(uint copyFrom, uint copyTo)
        {
            // Copy spell in DB
            using (var result = _adapter.Query($"SELECT * FROM `spell` WHERE `ID` = '{copyFrom}' LIMIT 1"))
            {
                var row = result.Rows[0];
                var str = new StringBuilder();
                str.Append($"INSERT INTO `spell` VALUES ('{copyTo}'");
                for (int i = 1; i < row.Table.Columns.Count; ++i)
                    str.Append($", \"{row[i]}\"");
                str.Append(")");
                _adapter.Execute(str.ToString());
            }
            // Merge result with spell list
            using (var result = _adapter.Query($"SELECT `id`,`SpellName{_language - 1}`,`SpellIconID`,`SpellRank{_language - 1}` FROM `spell` WHERE `ID` = '{copyTo}' LIMIT 1"))
            {
                _table.Merge(result, false, MissingSchemaAction.Add);
                _table.AcceptChanges();
            }
            // Refresh UI
            RefreshSpellList();
        }

        public void DeleteSpell(uint spellId)
        {
            // Delete from DB
            _adapter.Execute($"DELETE FROM `spell` WHERE `ID` = '{spellId}'");
            // Delete from spell list
            _table.Select($"id = {spellId}").First().Delete();
            _table.AcceptChanges();
            // Refresh UI
            RefreshSpellList();
        }

        private void RefreshSpellList()
        {
            // Update UI
            _contentsIndex = 0;
            _contentsCount = Items.Count;
            _table.DefaultView.Sort = "id";
            // We have to call ToTable to return a new sorted data table
            // Returning the existing table will have new rows at the end of the collection
            var arg = new ProgressChangedEventArgs(100, _table.DefaultView.ToTable().Rows);
            _worker_ProgressChanged(this, arg);
        }

        private void _worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Ignores spells with a iconId <= 0
            var watch = new Stopwatch();
            watch.Start();
            DataRowCollection collection = (DataRowCollection)e.UserState;
            int rowIndex = 0;
            // Reuse existing UI elements if they exist
            if (_contentsIndex < _contentsCount)
            {
                foreach (DataRow row in collection)
                {
                    if (_contentsIndex == _contentsCount ||
                        _contentsIndex >= Items.Count)
                    {
                        break;
                    }

                    ++rowIndex;

                    if (!(Items[_contentsIndex] is StackPanel stackPanel))
                        continue;

                    ++_contentsIndex;

                    var image = stackPanel.Children[0] as Image;
                    var textBlock = stackPanel.Children[1] as TextBlock;
                    textBlock.Text = BuildText(row);

                    uint.TryParse(row["SpellIconID"].ToString(), out uint iconId);
                    image.ToolTip = iconId.ToString();
                }
            }
            // Spawn any new UI elements required
            var newElements = new List<UIElement>();
            for (; rowIndex < collection.Count; ++rowIndex)
            {
                var row = collection[rowIndex];
                var textBlock = new TextBlock { Text = BuildText(row) };
                var image = new Image();
                uint.TryParse(row["SpellIconID"].ToString(), out uint iconId);
                image.ToolTip = iconId.ToString();
                image.Width = 32;
                image.Height = 32;
                image.Margin = new Thickness(1, 1, 1, 1);
                image.IsVisibleChanged += IsSpellListEntryVisibileChanged;
                var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                stackPanel.Children.Add(image);
                stackPanel.Children.Add(textBlock);
                ++_contentsIndex;
                newElements.Add(stackPanel);
            }
            // Replace the item source directly, adding each item will raise a high amount of events
            var src = ItemsSource;
            var newSrc = new List<object>();
            if (src != null)
            {
                // Don't keep more UI elements than we need
                // This will delete any listbox items we no longer need
                var enumerator = src.GetEnumerator();
                for (int i = 0; i < _contentsIndex; ++i)
                {
                    if (!enumerator.MoveNext())
                        break;
                    newSrc.Add(enumerator.Current);
                }
            }

            newSrc.AddRange(newElements);
            ItemsSource = newSrc;
            watch.Stop();
            Logger.Info($"Worker progress change event took {watch.ElapsedMilliseconds}ms to handle");
        }

        private string BuildText(DataRow row) => $" {row["id"]} - {row[$"SpellName{_language - 1}"]}\n  {row[$"SpellRank{_language - 1}"]}";

        private void IsSpellListEntryVisibileChanged(object o, DependencyPropertyChangedEventArgs args)
        {
            var image = o as Image;
            if (!(bool)args.NewValue)
            {
                image.Source = null;
                return;
            }
            if (image.Source != null)
            {
                return;
            }
            var loadIcons = (SpellIconDBC)DBCManager.GetInstance().FindDbcForBinding("SpellIcon");
            if (loadIcons != null)
            {
                var iconId = uint.Parse(image.ToolTip.ToString());
                var filePath = loadIcons.GetIconPath(iconId) + ".blp";
                image.Source = BlpManager.GetInstance().GetImageSourceFromBlpPath(filePath);
            }
        }

        private DataRowCollection GetSpellNames(uint lowerBound, uint pageSize, int locale)
        {
            DataTable newSpellNames = _adapter.Query(string.Format(@"SELECT `id`,`SpellName{1}`,`SpellIconID`,`SpellRank{2}` FROM `{0}` ORDER BY `id` LIMIT {3}, {4}",
                 "spell", locale, locale, lowerBound, pageSize));

            _table.Merge(newSpellNames, false, MissingSchemaAction.Add);
            _table.AcceptChanges();

            return newSpellNames.Rows;
        }

        private class SpellListQueryWorker : BackgroundWorker
        {
            public readonly IDatabaseAdapter Adapter;
            public readonly Stopwatch Watch;

            public SpellListQueryWorker(IDatabaseAdapter adapter, Stopwatch watch)
            {
                Adapter = adapter;
                Watch = watch;
            }
        }
    }
}
