using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using SpellEditor.Sources.Controls.SpellSelectList;

namespace SpellEditor.Sources.Controls.ListPickerDialog
{
    public partial class SpellPickerDialog : ListPickerDialogBase
    {
        private readonly SpellSelectionList _selectSpell;
        private readonly MainWindow _mainWindow;
        private readonly uint _selectedParentId; // selected id from the parent caller control

        public SpellPickerDialog(MainWindow mainWindow, uint selectedParentId, string selectionType)
        {
            InitializeComponent();

            // Populate Spell List
            _selectSpell = new SpellSelectionList();
            _mainWindow = mainWindow;
            _selectedParentId = selectedParentId;

            Title = "Spell Picker";

            SetSelectionTypeText(selectionType);

            LoadItemsList();
        }

        protected override void FilterFromText(string text)
        {
            throw new NotImplementedException();
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
            // throw new NotImplementedException("Failed to find item");
        }

        protected override void GoToId(uint id)
        {
            throw new NotImplementedException();
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
