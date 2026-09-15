using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SpellEditor.Sources.Controls.SpellSelectList;

namespace SpellEditor.Sources.Controls.ListPickerDialog
{
    public partial class SpellPickerDialog : ListPickerDialogBase
    {
        private readonly ListBox _selectSpell;
        private readonly uint _selectedParentId; // selected id from the parent caller control
        private List<SpellRecord> _records;

        public SpellPickerDialog(MainWindow mainWindow, uint selectedParentId, string selectionType)
            : base(mainWindow)
        {
            InitializeComponent();

            _selectSpell = new ListBox();
            _selectedParentId = selectedParentId;

            Title = "Spell Picker";

            SetSelectionTypeText(selectionType);

            LoadItemsList();
        }

        protected override void FilterFromText(string input)
        {
            var view = CollectionViewSource.GetDefaultView(_selectSpell.ItemsSource);
            if (view == null)
                return;

            if (string.IsNullOrEmpty(input))
            {
                view.Filter = null;
                return;
            }

            var lower = input.ToLower();
            view.Filter = o => o is SpellRecord record && record.Name.ToLower().Contains(lower);
        }

        protected override uint GetSelectedItemId()
        {
            if (!(_selectSpell.SelectedItem is SpellRecord record))
                return 0;

            SelectedId = record.Id;

            return SelectedId;
        }

        protected override void GoToId(uint id)
        {
            foreach (var record in _records)
            {
                if (record.Id != id)
                    continue;
                _selectSpell.SelectedItem = record;
                _selectSpell.ScrollIntoView(record);
                return;
            }
        }

        protected override void LoadItemsList()
        {
            VirtualizingStackPanel.SetIsVirtualizing(_selectSpell, true);
            VirtualizingStackPanel.SetVirtualizationMode(_selectSpell, VirtualizationMode.Recycling);
            ScrollViewer.SetCanContentScroll(_selectSpell, true);

            _selectSpell.BorderThickness = new Thickness(1);
            _selectSpell.ItemTemplate = (DataTemplate)Application.Current.Resources["SpellRecordTemplate"];

            _records = _mainWindow.SelectSpell.GetRecords();
            // Own view so filtering here does not disturb the main window list
            _selectSpell.ItemsSource = new CollectionViewSource { Source = _records }.View;

            SetItemsControl(_selectSpell);

            SelectInitialItem();
        }

        private void SelectInitialItem()
        {
            if (_records.Count == 0)
                return;

            if (_selectedParentId > 0)
            {
                for (int i = _records.Count - 1; i >= 0; --i)
                {
                    if (_records[i].Id != _selectedParentId)
                        continue;
                    _selectSpell.SelectedItem = _records[i];
                    _selectSpell.ScrollIntoView(_records[i]);
                    return;
                }
            }

            _selectSpell.SelectedItem = _records[0];
        }
    }
}
