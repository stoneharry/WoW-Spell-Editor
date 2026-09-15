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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SpellEditor.Sources.Controls
{
    public class SpellSelectionList : ListBox
    {

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private int _ContentsIndex;
        private int _Language;
        private IDatabaseAdapter _Adapter;
        private DataTable _Table = new DataTable();
        public DataTable Table { get { return _Table; } }
        private bool _initialised = false;

        // Snapshot of _Table shared with any other list that wants to show the same spells,
        // rebuilt lazily whenever the table changes
        private List<SpellRecord> _RecordCache;

        // Rows waiting to be turned into list entries. Built in small chunks at background
        // dispatcher priority so a large spell table never locks the UI up.
        private const int UiChunkSize = 250;
        private readonly List<DataRow> _PendingRows = new List<DataRow>();
        private bool _PumpScheduled;

        private volatile int _LoadGeneration;
        private bool _LoadFinished;

        // Completes once every row is queried and materialised into the list, so a caller
        // can hold a loading indicator up for the whole cold start
        private TaskCompletionSource<bool> _LoadCompletion;

        public void Initialise()
        {
            if (_initialised)
                return;
            var idColumn = _Table.Columns.Add("id", typeof(uint));
            _Table.Columns.Add("SpellName" + LocaleIndex(_Language), typeof(string));
            _Table.PrimaryKey = new[] { idColumn };
            _initialised = true;
        }

        // The spell table columns are suffixed with the zero based locale, the language
        // is one higher than that for everything except the unset and default cases
        private static int LocaleIndex(int language) => language > 0 ? language - 1 : language;

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

        private void RenameLocaleColumn(string prefix, int oldLanguage, int newLanguage)
        {
            var oldName = prefix + LocaleIndex(oldLanguage);
            var newName = prefix + LocaleIndex(newLanguage);
            if (oldName == newName || _Table.Columns[newName] != null)
                return;
            var column = _Table.Columns[oldName];
            if (column != null)
                column.ColumnName = newName;
        }

        public string GetSpellNameById(uint spellId)
        {
            var result = _Table.Select($"id = {spellId}");
            return result.Length == 1 ? result[0]["SpellName" + (_Language - 1)].ToString() : "";
        }

        public Task PopulateSelectSpell(bool clearData = true)
        {
            if (_Adapter == null)
                return Task.CompletedTask;
            if (_Table.Columns.Count == 0)
                return Task.CompletedTask;

            // Refresh language
            LocaleManager.Instance.MarkDirty();

            using (var adapter = AdapterFactory.Instance.GetAdapter(false))
            {
                var newLocale = LocaleManager.Instance.GetLocale(adapter);
                if (newLocale != _Language && (newLocale != -1 || _Language == -1))
                {
                    RenameLocaleColumn("SpellName", _Language, newLocale);
                    RenameLocaleColumn("SpellRank", _Language, newLocale);
                    SetLanguage(newLocale);
                }

                var selectSpellWatch = new Stopwatch();
                selectSpellWatch.Start();
                _ContentsIndex = 0;
                _PendingRows.Clear();
                _LoadFinished = false;
                // Anything waiting on the previous load is now waiting on a superseded list
                _LoadCompletion?.TrySetResult(true);
                var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _LoadCompletion = completion;
                var generation = ++_LoadGeneration;
                var worker = new SpellListQueryWorker(adapter, selectSpellWatch, generation) { WorkerReportsProgress = true };
                worker.ProgressChanged += _worker_ProgressChanged;

                worker.DoWork += delegate
                {
                    // Validate
                    if (worker.Adapter == null || !Config.Config.IsInit)
                        return;
                    if (generation != _LoadGeneration)
                        return;
                    int locale = _Language;
                    if (locale > 0)
                        locale -= 1;

                    // Clear Data
                    if (clearData)
                        _Table.Rows.Clear();

                    const uint pageSize = 5000;
                    uint lastId = 0;
                    // Smaller first page so the list paints as soon as possible
                    DataRowCollection results = GetSpellNames(lastId, pageSize / 5, locale);
                    // Edge case of empty table after truncating, need to send a event to the handler
                    if (results != null && results.Count == 0)
                    {
                        worker.ReportProgress(0, results);
                    }
                    while (results != null && results.Count != 0)
                    {
                        if (generation != _LoadGeneration)
                            return;
                        lastId = uint.Parse(results[results.Count - 1][0].ToString());
                        worker.ReportProgress(0, results);
                        results = GetSpellNames(lastId, pageSize, locale);
                    }
                    _Table.AcceptChanges();
                };
                worker.RunWorkerCompleted += (sender, args) =>
                {
                    if (!(sender is SpellListQueryWorker spellListQueryWorker))
                        return;

                    spellListQueryWorker.Watch.Stop();
                    Logger.Info($"Loaded spell selection list contents in {spellListQueryWorker.Watch.ElapsedMilliseconds}ms");

                    if (spellListQueryWorker.Generation != _LoadGeneration)
                        return;
                    _LoadFinished = true;
                    PumpPendingRows();
                };
                worker.RunWorkerAsync();

                return completion.Task;
            }
        }

        public List<SpellRecord> GetRecords()
        {
            if (_RecordCache != null)
                return _RecordCache;

            _Table.DefaultView.Sort = "id";
            // ToTable returns a new sorted table, the existing one has new rows at the end
            var rows = _Table.DefaultView.ToTable().Rows;

            var records = new List<SpellRecord>(rows.Count);
            foreach (DataRow row in rows)
                records.Add(new SpellRecord(row, _Language));

            _RecordCache = records;

            return _RecordCache;
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
                _RecordCache = null;
                if (result.Rows.Count > 0)
                    InsertSpellEntry(result.Rows[0]);
            }
        }

        // Splices one entry into the existing item source, rebuilding every entry for a single
        // add is what made creating a spell take seconds on a large table
        private void InsertSpellEntry(DataRow row)
        {
            var entry = new SpellSelectionEntry();
            entry.RefreshEntry(row, _Language);
            entry.SetCopyClickAction(DuplicateAction);
            entry.SetDeleteClickAction(DeleteAction);
            entry.SetPasteClickAction(PasteAction);

            var spellId = entry.GetSpellId();
            var newSrc = CurrentItemSource();
            var index = newSrc.FindIndex(item => item is SpellSelectionEntry existing && existing.GetSpellId() > spellId);
            if (index < 0)
                newSrc.Add(entry);
            else
                newSrc.Insert(index, entry);

            ItemsSource = newSrc;
            _ContentsIndex = newSrc.Count;
        }

        private void RemoveSpellEntry(uint spellId)
        {
            var newSrc = CurrentItemSource();
            var index = newSrc.FindIndex(item => item is SpellSelectionEntry existing && existing.GetSpellId() == spellId);
            if (index < 0)
                return;
            newSrc.RemoveAt(index);

            ItemsSource = newSrc;
            _ContentsIndex = newSrc.Count;
        }

        private List<object> CurrentItemSource() =>
            ItemsSource == null ? new List<object>() : new List<object>(ItemsSource.Cast<object>());

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
                _RecordCache = null;
            }
        }

        public void DeleteSpell(uint spellId)
        {
            // Delete from DB
            _Adapter.Execute($"DELETE FROM `spell` WHERE `ID` = '{spellId}'");
            // Delete from spell list
            _Table.Select($"id = {spellId}").First().Delete();
            _Table.AcceptChanges();
            _RecordCache = null;
            // Refresh UI
            RemoveSpellEntry(spellId);
        }

        private void RefreshSpellList()
        {
            // Update UI
            _ContentsIndex = 0;
            _PendingRows.Clear();
            _LoadFinished = true;
            _Table.DefaultView.Sort = "id";
            // We have to call ToTable to return a new sorted data table
            // Returning the existing table will have new rows at the end of the collection
            var arg = new ProgressChangedEventArgs(100, _Table.DefaultView.ToTable().Rows);
            _worker_ProgressChanged(this, arg);
        }

        private void _worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (sender is SpellListQueryWorker worker && worker.Generation != _LoadGeneration)
                return;

            _RecordCache = null;

            var collection = (DataRowCollection)e.UserState;
            foreach (DataRow row in collection)
                _PendingRows.Add(row);

            if (collection.Count == 0)
                PumpPendingRows();
            else
                SchedulePump();
        }

        private void SchedulePump()
        {
            if (_PumpScheduled)
                return;
            _PumpScheduled = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new System.Action(PumpPendingRows));
        }

        private void PumpPendingRows()
        {
            _PumpScheduled = false;

            var take = _PendingRows.Count < UiChunkSize ? _PendingRows.Count : UiChunkSize;
            // Replace the item source directly, adding each item would raise a high amount of events
            var newSrc = CurrentItemSource();
            for (int i = 0; i < take; ++i)
            {
                var row = _PendingRows[i];
                // Reuse an existing UI element where we have one spare
                if (_ContentsIndex < newSrc.Count && newSrc[_ContentsIndex] is SpellSelectionEntry existing)
                {
                    existing.RefreshEntry(row, _Language);
                    ++_ContentsIndex;
                    continue;
                }
                var entry = new SpellSelectionEntry();
                entry.RefreshEntry(row, _Language);
                entry.SetCopyClickAction(DuplicateAction);
                entry.SetDeleteClickAction(DeleteAction);
                entry.SetPasteClickAction(PasteAction);
                if (_ContentsIndex < newSrc.Count)
                    newSrc[_ContentsIndex] = entry;
                else
                    newSrc.Add(entry);
                ++_ContentsIndex;
            }
            _PendingRows.RemoveRange(0, take);

            // Only drop the leftover tail once every page is in, trimming per chunk would
            // throw away the entries the next chunk is about to reuse
            if (_LoadFinished && _PendingRows.Count == 0 && newSrc.Count > _ContentsIndex)
                newSrc.RemoveRange(_ContentsIndex, newSrc.Count - _ContentsIndex);

            ItemsSource = newSrc;

            if (_PendingRows.Count > 0)
                SchedulePump();
            else if (_LoadFinished)
                _LoadCompletion?.TrySetResult(true);
        }

        // Keyset paged rather than OFFSET paged, MySQL rescans every skipped row on an OFFSET
        private DataRowCollection GetSpellNames(uint lastId, uint pageSize, int locale)
        {
            using (var newSpellNames = _Adapter.Query(
                string.Format(@"SELECT `id`,`SpellName{1}`,`SpellIconID`,`SpellRank{1}` FROM `{0}` WHERE `id` > {2} ORDER BY `id` LIMIT {3}",
                 "spell", locale, lastId, pageSize)))
            {
                // AcceptChanges walks every row already in the table, so it is committed
                // once the last page is in rather than once per page
                _Table.Merge(newSpellNames, false, MissingSchemaAction.Add);

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
            public readonly int Generation;

            public SpellListQueryWorker(IDatabaseAdapter adapter, Stopwatch watch, int generation)
            {
                Adapter = adapter;
                Watch = watch;
                Generation = generation;
            }
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
