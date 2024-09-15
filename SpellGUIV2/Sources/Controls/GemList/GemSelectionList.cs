using NLog;
using SpellEditor.Sources.Constants;
using SpellEditor.Sources.Controls.Common;
using SpellEditor.Sources.Controls.SpellSelectList;
using SpellEditor.Sources.Database;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace SpellEditor.Sources.Controls
{
    public class GemSelectionList : ListBox
    {

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private int _ContentsCount;
        private int _ContentsIndex;
        private IDatabaseAdapter _Adapter;
        private readonly DataTable _Table = new DataTable();
        private bool _initialised = false;
        private Label _SelectedGemText;
        private ThreadSafeComboBox _GemTypeBox;
        private List<UIElement> _Elements;

        public void Initialise(Label selectedGemText, ThreadSafeComboBox gemTypeBox, List<UIElement> elements)
        {
            if (!_initialised)
            {
                _initialised = true;
                _GemTypeBox = gemTypeBox;
                _Elements = elements;
                _SelectedGemText = selectedGemText;
                _Table.Columns.Add("id", typeof(uint));
                _Table.Columns.Add("SpellItemEnchantmentRef", typeof(uint));
                _Table.Columns.Add("gemType", typeof(uint));
                _Table.Columns.Add("sRefName0", typeof(string));
                _Table.Columns.Add("ItemCache", typeof(uint));
                _Table.Columns.Add("TriggerSpell", typeof(uint));
                _Table.Columns.Add("TempLearnSpell", typeof(uint));
                _Table.Columns.Add("Achievement", typeof(uint));
                _Table.Columns.Add("AchievementCriteria", typeof(uint));
                PopulateGemSelect();
                gemTypeBox.ItemsSource = GemTypeManager.Instance.GemTypes;
                SelectionChanged += GemSelectionList_SelectionChanged;
                ((Button)elements[0]).Click += SaveGemChangesClick;
                ((Button)elements[1]).Click += DuplicateGemClick;
                ((Button)elements[2]).Click += DeleteGemClick;
            }
        }

        private void GemSelectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null || sender.GetType() != typeof(GemSelectionList))
                return;

            var selected = (GemSelectionEntry)(e.AddedItems.Count > 0 ? e.AddedItems[0] : null);
            if (selected == null)
                return;

            _SelectedGemText.Content = selected.GemId;
            var gem = selected.GemTypeEntry.Name;
            _GemTypeBox.Text = gem ?? string.Empty;

            ((ThreadSafeTextBox)_Elements[7]).Text = selected.SpellItemEnchantmentEntry.Name;
            ((ThreadSafeTextBox)_Elements[3]).Text = selected.SpellItemEnchantmentEntry.ItemCache.Id.ToString();
            ((ThreadSafeTextBox)_Elements[4]).Text = selected.SpellItemEnchantmentEntry.TriggerSpell.Id.ToString();
            ((ThreadSafeTextBox)_Elements[5]).Text = selected.SpellItemEnchantmentEntry.TempLearnSpell.Id.ToString();
            ((ThreadSafeTextBox)_Elements[6]).Text = selected.AchievementEntry.Id.ToString() + ": " + selected.AchievementCriteriaEntry.Id;

            _Elements.ForEach(element => element.IsEnabled = true);
            _GemTypeBox.IsEnabled = true;
        }


        private void SaveGemChangesClick(object sender, RoutedEventArgs e)
        {
            if (sender != _Elements[0] || SelectedItem == null)
                return;

            if (!(SelectedItem is GemSelectionEntry entry))
                return;

            // Update UI
            var result = _Table.Select("id = " + entry.GemId);
            var data = result.First();
            data.BeginEdit();
            data["gemType"] = GemTypeManager.Instance.LookupGemTypeByName(_GemTypeBox.Text).Type;
            data["TriggerSpell"] = ((ThreadSafeTextBox)_Elements[4]).Text;
            data["TempLearnSpell"] = ((ThreadSafeTextBox)_Elements[5]).Text;
            data["ItemCache"] = ((ThreadSafeTextBox)_Elements[3]).Text;
            data["sRefName0"] = ((ThreadSafeTextBox)_Elements[7]).Text;
            data.EndEdit();
            entry.RefreshEntry(data);

            // GemType
            _Adapter.Execute($"UPDATE gemproperties SET gemType = {entry.GemTypeEntry.Type} WHERE id = {entry.GemId}");

            // SpellItemEnchantmentRef
            _Adapter.Execute($"UPDATE spellitemenchantment SET objectId1 = {entry.SpellItemEnchantmentEntry.TriggerSpell.Id}," +
                $" ItemCache = {entry.SpellItemEnchantmentEntry.ItemCache.Id}," +
                $" sRefName0 = \"{entry.SpellItemEnchantmentEntry.Name}\"" +
                $" WHERE id = {entry.SpellItemEnchantmentEntry.Id}");

            // Spell
            _Adapter.Execute($"UPDATE spell SET EffectTriggerSpell1 = {entry.SpellItemEnchantmentEntry.TempLearnSpell.Id} WHERE id = {entry.SpellItemEnchantmentEntry.TriggerSpell.Id}");

        }

        private void DuplicateGemClick(object sender, RoutedEventArgs e)
        {

        }

        private void DeleteGemClick(object sender, RoutedEventArgs e)
        {

        }

        public bool IsInitialised() => _initialised;
        public bool HasAdapter() => _Adapter != null;

        public GemSelectionList SetAdapter(IDatabaseAdapter adapter)
        {
            _Adapter = adapter;
            return this;
        }

        public int GetLoadedRowCount() => _Table.Rows.Count;

        public void PopulateGemSelect(bool clearData = true)
        {
            if (_Adapter == null)
                return;
            if (_Table.Columns.Count == 0)
                return;

            using (var adapter = AdapterFactory.Instance.GetAdapter(false))
            {
                var watch = new Stopwatch();
                watch.Start();
                _ContentsIndex = 0;
                _ContentsCount = Items.Count;
                var worker = new GemListQueryWorker(adapter, watch) { WorkerReportsProgress = true };
                worker.ProgressChanged += _worker_ProgressChanged;

                worker.DoWork += delegate
                {
                    // Validate
                    if (worker.Adapter == null || !Config.Config.IsInit)
                        return;

                    // Clear Data
                    if (clearData)
                        _Table.Rows.Clear();

                    const uint pageSize = 5000;
                    uint lowerBounds = 0;
                    DataRowCollection results = GetGemData(lowerBounds, 100);
                    lowerBounds += 100;
                    // Edge case of empty table after truncating, need to send a event to the handler
                    if (results != null && results.Count == 0)
                    {
                        worker.ReportProgress(0, results);
                    }
                    while (results != null && results.Count != 0)
                    {
                        worker.ReportProgress(0, results);
                        results = GetGemData(lowerBounds, pageSize);
                        lowerBounds += pageSize;
                    }
                };
                worker.RunWorkerAsync();
                worker.RunWorkerCompleted += (sender, args) =>
                {
                    if (!(sender is GemListQueryWorker spellListQueryWorker))
                        return;

                    spellListQueryWorker.Watch.Stop();
                    Logger.Info($"Loaded spell selection list contents in {spellListQueryWorker.Watch.ElapsedMilliseconds}ms");
                };
            }
        }

        private void RefreshGemList()
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

                    if (!(Items[_ContentsIndex] is GemSelectionEntry entry))
                        continue;

                    ++rowIndex;

                    entry.RefreshEntry(row);

                    ++_ContentsIndex;
                }
            }
            // Spawn any new UI elements required
            var newElements = new List<UIElement>();
            for (; rowIndex < collection.Count; ++rowIndex)
            {
                var row = collection[rowIndex];
                var entry = new GemSelectionEntry();
                entry.RefreshEntry(row);
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

        private DataRowCollection GetGemData(uint lowerBound, uint pageSize)
        {
            using (var gemData = _Adapter.Query(
                string.Format("SELECT g.id, g.SpellItemEnchantmentRef, g.gemType, e.sRefName0, e.ItemCache, " +
                "s.id AS \"TriggerSpell\", s.EffectTriggerSpell1 AS \"TempLearnSpell\", " +
                "a.id AS \"Achievement\", c.id AS \"AchievementCriteria\" " +
                "FROM gemproperties g " +
                "JOIN spellitemenchantment e on g.SpellItemEnchantmentRef = e.id " +
                "JOIN spell s ON e.objectId1 = s.id " + 
                "JOIN achievement_criteria c ON e.ItemCache = c.assetType " +
                "JOIN achievement a ON c.referredAchievement = a.id " +
                "WHERE e.ItemCache >= 70000 " +
                "ORDER BY g.id DESC LIMIT {0}, {1}",
                 lowerBound, pageSize)))
            {
                _Table.Merge(gemData, false, MissingSchemaAction.Add);
                _Table.AcceptChanges();

                return gemData.Rows;
            }
        }

        private class GemListQueryWorker : BackgroundWorker
        {
            public readonly IDatabaseAdapter Adapter;
            public readonly Stopwatch Watch;

            public GemListQueryWorker(IDatabaseAdapter adapter, Stopwatch watch)
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
