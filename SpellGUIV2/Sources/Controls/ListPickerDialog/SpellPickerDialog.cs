using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MahApps.Metro.Controls;
using SpellEditor.Sources.Controls.SpellSelectList;

namespace SpellEditor.Sources.Controls.ListPickerDialog
{
    public partial class SpellPickerDialog : ListPickerDialogBase
    {
        private readonly SpellSelectionList _selectSpell;
        private readonly uint _selectedParentId; // selected id from the parent caller control

        public SpellPickerDialog(MainWindow mainWindow, uint selectedParentId, string selectionType)
            : base(mainWindow)
        {
            InitializeComponent();

            // Populate Spell List
            _selectSpell = new SpellSelectionList();
            _selectedParentId = selectedParentId;

            Title = "Spell Picker";

            SetSelectionTypeText(selectionType);

            LoadItemsList();
        }

        private volatile bool imageLoadEventRunning = false;
        protected override void FilterFromText(string input)
        {
            if (imageLoadEventRunning)
                return;
            imageLoadEventRunning = true;

            var badInput = string.IsNullOrEmpty(input);
            if (badInput && _selectSpell.GetLoadedRowCount() == _selectSpell.Items.Count)
            {
                imageLoadEventRunning = false;
                return;
            }

            ICollectionView view = CollectionViewSource.GetDefaultView(_selectSpell.Items);
            view.Filter = o =>
            {
                var panel = (StackPanel)o;
                using (var enumerator = panel.GetChildObjects().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (!(enumerator.Current is TextBlock block))
                            continue;
                        return input.Length == 0 ? true : block.Text.ToLower().Contains(input);
                    }
                }
                return false;
            };

            imageLoadEventRunning = false;
        }

        protected override uint GetSelectedItemId()
        {
            StackPanel panel = (StackPanel)_selectSpell.SelectedItem;
            using (var enumerator = panel.GetChildObjects().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is TextBlock block)
                    {
                        string name = block.Text;
                        SelectedId = uint.Parse(name.Substring(1, name.IndexOf(' ', 1)));

                        return SelectedId;
                    }
                }
            }
            return 0;
        }

        protected override void GoToId(uint id)
        {
            int count = 0;
            foreach (StackPanel obj in _selectSpell.Items)
            {
                foreach (var item in obj.Children)
                    if (item is TextBlock tb)
                    {
                        if (uint.Parse(tb.Text.Split(' ')[1]) == id)
                        {
                            _selectSpell.SelectedIndex = count;
                            _selectSpell.ScrollIntoView(obj);

                            return;
                        }
                    }

                count++;
            }
        }

        protected override void LoadItemsList()
        {
            // set virtualization
            VirtualizingStackPanel.SetIsVirtualizing(_selectSpell, true);
            VirtualizingStackPanel.SetVirtualizationMode(
                _selectSpell,
                VirtualizationMode.Recycling);
            ScrollViewer.SetCanContentScroll(_selectSpell, true);

            _selectSpell.BorderThickness = new Thickness(1);

            _selectSpell.SetAdapter(_mainWindow.GetDBAdapter())
                .SetLanguage(_mainWindow.GetLanguage())
                .Initialise();

            Debug.Assert(_selectSpell.IsInitialised());
            Debug.Assert(_selectSpell.HasAdapter());

            // SelectSpell.PopulateSelectSpell();
            _selectSpell.PopulateFromOther(_mainWindow.SelectSpell);

            SetItemsControl(_selectSpell);

            // bring selected into view
            if (_selectedParentId > 0)
            {
                var items = _selectSpell.Items;
                for (int i = items.Count - 1; i >= 0; --i)
                {
                    SpellSelectionEntry item = items.GetItemAt(i) as SpellSelectionEntry;
                    if (item != null && item.GetSpellId() == _selectedParentId)
                    {
                        _selectSpell.ScrollIntoView(item);
                        _selectSpell.SelectedItem = item;
                        break;
                    }
                }
            }
            else
                _selectSpell.SelectedItem = _selectSpell.Items.GetItemAt(0);
        }
    }
}
