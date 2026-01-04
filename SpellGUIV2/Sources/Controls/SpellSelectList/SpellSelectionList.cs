using NLog;
using SpellEditor.Sources.Controls.Common;
using SpellEditor.Sources.Controls.SpellSelectList;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.Locale;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        private bool _initialised = false;
        private MainWindow _main;

        public void Initialise(MainWindow main)
        {
            _Table.Columns.Add("id", typeof(uint));
            _Table.Columns.Add("SpellName" + _Language, typeof(string));
            _Table.Columns.Add("Icon", typeof(uint));
            _initialised = true;
            _main = main;
        }

        public bool IsInitialised() => _initialised;
        public bool HasAdapter() => _Adapter != null;

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

        public List<uint> GetFirstAvailableSpells(uint startId = 1, uint amount = 1)
        {
            List<uint> ids = new List<uint>();

            if (startId < 1) startId = 1;
            if (amount < 1) amount = 1;

            var table = _Adapter.Query($"SELECT `ID` FROM `spell` WHERE `ID` >= {startId} ORDER BY `ID` ASC");

            uint lastId = 0;

            // only possible if the startId is the last id in the table
            bool skip = table.Rows.Count == 1;

            foreach (DataRow row in table.Rows)
            {
                uint currentId = uint.Parse(row["ID"].ToString());
                if ((lastId > 0 && ((currentId - lastId) > amount)) || skip)
                {
                    if (skip) 
                        lastId = currentId;

                    for (uint i = 1; i <= amount; ++i)
                        ids.Add(lastId + i);
                    break;
                }
                else
                    lastId = currentId;
            }

            // only possible if the startId is more than the last id in the table so we can just safely add the ids from here
            if (lastId == 0)
                for (uint i = 0; i < amount; ++i)
                    ids.Add(startId + i);

            return ids;
        }

        public bool HasSpell(uint spellId)
        {
            var result = _Table.Select($"id = {spellId}");
            return result.Length == 1;
        }

        public string GetSpellNameById(uint spellId)
        {
            var result = _Table.Select($"id = {spellId}");
            return result.Length == 1 ? result[0]["SpellName" + (_Language - 1)].ToString() : "";
        }

        public void PopulateSelectSpell(bool clearData = true)
        {
            if (_Adapter == null)
                return;
            if (_Table.Columns.Count == 0)
                return;

            // Refresh language
            LocaleManager.Instance.MarkDirty();

            using (var adapter = AdapterFactory.Instance.GetAdapter(false))
            {
                var newLocale = LocaleManager.Instance.GetLocale(adapter);
                string oldName = "SpellName" + _Language;
                string newName = "SpellName" + newLocale;

                if (newLocale != _Language && oldName != newName && (newLocale != -1 || _Language == -1))
                {
                    if (_Table.Columns.Contains(newName))
                    {
                        // Temporarily rename newName column to avoid conflict
                        _Table.Columns[newName].ColumnName = "__temp__";
                    }

                    _Table.Columns[oldName].ColumnName = newName;

                    if (_Table.Columns.Contains("__temp__"))
                    {
                        _Table.Columns["__temp__"].ColumnName = oldName;
                    }
                }

                var selectSpellWatch = new Stopwatch();
                selectSpellWatch.Start();
                _ContentsIndex = 0;
                _ContentsCount = Items.Count;
                var worker = new SpellListQueryWorker(adapter, selectSpellWatch) { WorkerReportsProgress = true };
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
                    DataRowCollection results = GetSpellNames(lowerBounds, pageSize / 5, locale);
                    lowerBounds += pageSize / 5;
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
                worker.RunWorkerCompleted += (sender, args) =>
                {
                    if (!(sender is SpellListQueryWorker spellListQueryWorker))
                        return;

                    spellListQueryWorker.Watch.Stop();
                    Logger.Info($"Loaded spell selection list contents in {spellListQueryWorker.Watch.ElapsedMilliseconds}ms");
                };
                worker.RunWorkerAsync();
            }
        }

        public void AddNewSpell(uint copyFrom, uint copyTo, bool refresh = true)
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
            if (refresh)
                RefreshSpellList();

            _main.LoadBodyData(3, copyTo);
        }

        public void UpdateSpell(DataRow row)
        {
            // Update UI
            var lang = _Language - 1;
            var changedId = uint.Parse(row[0].ToString());
            foreach (var item in Items)
            {
                var panel = item as SpellSelectionEntry;
                if (panel.GetSpellId() == changedId)
                {
                    panel.RefreshEntry(row, _Language);
                    break;
                }
            }
            // Update Table
            var result = _Table.Select($"id = {changedId}");
            if (result.Length == 1)
            {
                var data = result.First();
                data.BeginEdit();
                data["SpellName" + lang] = row["SpellName" + lang];
                data["SpellIconID"] = row["SpellIconID"];
                data["SpellRank" + lang] = row["SpellRank" + lang];
                data.EndEdit();
            }
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

            _main.LoadBodyData(2, spellId);
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
                entry.SetCopyClickAction(DuplicateAction);
                entry.SetDeleteClickAction(DeleteAction);
                entry.SetPasteClickAction(PasteAction);
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
            using (var newSpellNames = _Adapter.Query(
                string.Format(@"SELECT `id`,`SpellName{1}`,`SpellIconID`,`SpellRank{1}` FROM `{0}` ORDER BY `id` LIMIT {2}, {3}",
                 "spell", locale, lowerBound, pageSize)))
            {
                _Table.Merge(newSpellNames, false, MissingSchemaAction.Add);
                _Table.AcceptChanges();

                return newSpellNames.Rows;
            }
        }

        private void DuplicateAction(IListEntry obj)
        {
            if (obj is SpellSelectionEntry entry)
            {
                var currentId = entry.GetSpellId();
                var newId = entry.GetDuplicateSpellId();

                AddNewSpell(currentId, newId);
            }
        }

        private void DeleteAction(IListEntry obj)
        {
            if (obj is SpellSelectionEntry entry)
            {
                DeleteSpell(entry.GetSpellId());
            }
        }

        private void PasteAction(IListEntry obj)
        {
            if (obj is SpellSelectionEntry entry)
            {
                uint newId = 0;
                using (var newSpellNames = _Adapter.Query(string.Format($"SELECT max(id) FROM spell")))
                {
                    foreach (DataRow row in newSpellNames.Rows)
                    {
                        newId = uint.Parse(row[0].ToString()) + 1;
                    }
                    if (newId == 0)
                    {
                        newId = 1;
                    }
                }
                if (newId > 0)
                {
                    entry.UpdateDuplicateText(newId);
                }
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

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
