using NLog;
using SpellEditor.Sources.Controls.Common;
using SpellEditor.Sources.Controls.SpellSelectList;
using SpellEditor.Sources.Database;
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

        private int _ContentsCount;
        private int _ContentsIndex;
        private int _Language;
        private IDatabaseAdapter _Adapter;
        private readonly DataTable _Table = new DataTable();

        public void Initialise()
        {
            _Table.Columns.Add("id", typeof(uint));
            _Table.Columns.Add("SpellName" + _Language, typeof(string));
            _Table.Columns.Add("Icon", typeof(uint));
        }

        public SpellSelectionList SetLanguage(int language)
        { 
            _Language = language;
            return this;
        }

        public SpellSelectionList SetAdapter(IDatabaseAdapter adapter)
        {
            _Adapter = adapter;
            return this;
        }

        public int GetLoadedRowCount() => _Table.Rows.Count;

        public string GetSpellNameById(uint spellId)
        {
            var result = _Table.Select($"id = {spellId}");
            return result.Length == 1 ? result[0]["SpellName" + (_Language - 1)].ToString() : "";
        }

        public void PopulateSelectSpell(bool clearData = true)
        {
            if (_Adapter == null)
                throw new Exception("Adapter has not been configured");
            if (_Table.Columns.Count == 0)
                throw new Exception("Initialise has not been invoked");

            // Refresh language
            LocaleManager.Instance.MarkDirty();
            var newLocale = LocaleManager.Instance.GetLocale(_Adapter);
            if (newLocale != _Language)
            {
                _Table.Columns["SpellName" + _Language].ColumnName = "SpellName" + newLocale;
                SetLanguage(newLocale);
            }

            var selectSpellWatch = new Stopwatch();
            selectSpellWatch.Start();
            _ContentsIndex = 0;
            _ContentsCount = Items.Count;
            var worker = new SpellListQueryWorker(_Adapter, selectSpellWatch) { WorkerReportsProgress = true };
            worker.ProgressChanged += _worker_ProgressChanged;

            worker.DoWork += delegate
            {
                // Validate
                if (worker.Adapter == null || !Config.Config.IsInit)
                    return;
                int locale = _Language;
                if (locale > 0)
                    locale -= 1;

                // Clear Data
                if (clearData)
                    _Table.Rows.Clear();

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
            using (var result = _Adapter.Query($"SELECT * FROM `spell` WHERE `ID` = '{copyFrom}' LIMIT 1"))
            {
                var row = result.Rows[0];
                var str = new StringBuilder();
                str.Append($"INSERT INTO `spell` VALUES ('{copyTo}'");
                for (int i = 1; i < row.Table.Columns.Count; ++i)
                    str.Append($", \"{row[i]}\"");
                str.Append(")");
                _Adapter.Execute(str.ToString());
            }
            // Merge result with spell list
            using (var result = _Adapter.Query($"SELECT `id`,`SpellName{_Language - 1}`,`SpellIconID`,`SpellRank{_Language - 1}` FROM `spell` WHERE `ID` = '{copyTo}' LIMIT 1"))
            {
                _Table.Merge(result, false, MissingSchemaAction.Add);
                _Table.AcceptChanges();
            }
            // Refresh UI
            RefreshSpellList();
        }

        public void DeleteSpell(uint spellId)
        {
            // Delete from DB
            _Adapter.Execute($"DELETE FROM `spell` WHERE `ID` = '{spellId}'");
            // Delete from spell list
            _Table.Select($"id = {spellId}").First().Delete();
            _Table.AcceptChanges();
            // Refresh UI
            RefreshSpellList();
        }

        private void RefreshSpellList()
        {
            // Update UI
            _ContentsIndex = 0;
            _ContentsCount = Items.Count;
            _Table.DefaultView.Sort = "id";
            // We have to call ToTable to return a new sorted data table
            // Returning the existing table will have new rows at the end of the collection
            var arg = new ProgressChangedEventArgs(100, _Table.DefaultView.ToTable().Rows);
            _worker_ProgressChanged(this, arg);
        }

        private void _worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var watch = new Stopwatch();
            watch.Start();
            DataRowCollection collection = (DataRowCollection)e.UserState;
            int rowIndex = 0;
            // Reuse existing UI elements if they exist
            if (_ContentsIndex < _ContentsCount)
            {
                foreach (DataRow row in collection)
                {
                    if (_ContentsIndex == _ContentsCount || _ContentsIndex >= Items.Count)
                        break;

                    if (!(Items[_ContentsIndex] is SpellSelectionEntry entry))
                        continue;

                    ++rowIndex;

                    entry.RefreshEntry(row, _Language);

                    ++_ContentsIndex;
                }
            }
            // Spawn any new UI elements required
            var newElements = new List<UIElement>();
            for (; rowIndex < collection.Count; ++rowIndex)
            {
                var row = collection[rowIndex];
                var entry = new SpellSelectionEntry();
                entry.RefreshEntry(row, _Language);
                entry.SetDeleteClickAction(DeleteAction);
                newElements.Add(entry);
                ++_ContentsIndex;
            }
            // Replace the item source directly, adding each item would raise a high amount of events
            var src = ItemsSource;
            var newSrc = new List<object>();
            if (src != null)
            {
                // This will also delete any listbox items we no longer need
                var enumerator = src.GetEnumerator();
                for (int i = 0; i < _ContentsIndex; ++i)
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

        private DataRowCollection GetSpellNames(uint lowerBound, uint pageSize, int locale)
        {
            DataTable newSpellNames = _Adapter.Query(string.Format(@"SELECT `id`,`SpellName{1}`,`SpellIconID`,`SpellRank{2}` FROM `{0}` ORDER BY `id` LIMIT {3}, {4}",
                 "spell", locale, locale, lowerBound, pageSize));

            _Table.Merge(newSpellNames, false, MissingSchemaAction.Add);
            _Table.AcceptChanges();

            return newSpellNames.Rows;
        }

        private void DeleteAction(IListEntry obj)
        {
            if (obj is SpellSelectionEntry entry)
            {
                DeleteSpell(entry.GetSpellId());
            }
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
