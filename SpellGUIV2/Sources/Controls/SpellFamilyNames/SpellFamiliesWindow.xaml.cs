using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using SpellEditor.Sources.Controls.Common;
using SpellEditor.Sources.Controls.SpellFamilyNames;
using SpellEditor.Sources.Tools.SpellFamilyClassMaskStoreParser;
using SpellEditor.Sources.VersionControl;

namespace SpellEditor.Sources.Controls.SpellFamilyNames
{
    public partial class SpellFamiliesWindow : MetroWindow
    {
        // store effects UI stuff in arrays for easy access
        private readonly UIntTextBox[] _familyMaskControls; // [3]

        public List<CheckBox> _maskCheckBoxes = new List<CheckBox>(); // 32 x 3

        private readonly bool[] _family_has_definitions_cache = new bool[3 * 32];
        private readonly int[] _family_spell_counts = new int[3 * 32];

        private readonly uint[] _original_families_values = new uint[3];
        public readonly uint[] _active_families_values; // reference to the original array in MainWindow
        public MainWindow _mainwindow;
        private readonly uint _familyId;
        private readonly uint _effectId;
        private readonly bool _isBaseFamilies; // wheteher it's spell effect families or base
        public readonly uint _maskCount; // how many family masks there are in array (3 in wotlk, 2 in tbc/vanilla for base, 1 in vanilla for effect item_type


        public SpellFamiliesWindow(uint[] families, uint familyId, MainWindow mainwindow, uint effectId, bool baseFamilies, uint mask_count, bool default_filter_talents)
        {
            _familyId = familyId;
            _active_families_values = families;
            _mainwindow = mainwindow;

            _original_families_values = new uint[mask_count];
            for (int i = 0; i < mask_count; i++)
            {
                _original_families_values[i] = _active_families_values[i];
            }


            _effectId = effectId;
            _isBaseFamilies = baseFamilies;
            _maskCount = mask_count;

            InitializeComponent();

            _familyMaskControls = new UIntTextBox[3] { SpellMask1, SpellMask2, SpellMask3 }; // must be after InitializeComponent()

            // 1.12 only has 2 masks in base, 1 in effects through item_type
            if (mask_count < 2)
                _familyMaskControls[1].IsEnabled = false;
            if (mask_count < 3)
                _familyMaskControls[2].IsEnabled = false;

            if (!WoWVersionManager.IsWotlkOrGreaterSelected)
            {
                FilterTabControl.IsEnabled = false; // WOTLK only, as earlier versions don't have the class mask fields.
                FilterTabControl.SelectedIndex = 0;
            }

            if (WoWVersionManager.IsWotlkOrGreaterSelected && default_filter_talents)
                FilterTabControl.SelectedIndex = 1;

            if (baseFamilies)
                Title += " [Base]";
            else
                Title += $" [Effect {effectId}]";

            CreateFamilyCheckboxes();

            Load(_active_families_values);
        }

        // load from data in _families
        private void Load(uint[] family_values)
        {
            // Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            //     _mainwindow.spellFamilyClassMaskParser?.UpdateSpellFamilyClassMask(this, _familyId, WoWVersionManager.IsWotlkOrGreaterSelected, _mainwindow.GetDBAdapter(), null)));

            for (int category = 0; category < _maskCount; category++)
            {
                uint family = family_values[category];

                // set textboxes
                _familyMaskControls[category].ValueChanged -= SpellMask_text_ValueChanged;
                _familyMaskControls[category].Value = family;
                _familyMaskControls[category].ValueChanged += SpellMask_text_ValueChanged;

                // set checkboxes
                for (int i = 0; i < 32; i++)
                {
                    bool isSet = (family & (1u << i)) != 0;
                    var cb = _maskCheckBoxes[(32 * category) + i];

                    cb.IsChecked = isSet; // isChecked triggers event which handles checkbox style changes
                }
            }
        }

        private void CreateFamilyCheckboxes()
        {
            for (int category = 0; category < _maskCount; category++)
            {
                for (int i = 0; i < 32; i++)
                {
                    uint mask = 1u << i;
                    int index = (32 * category) + i;

                    var tb = new TextBlock
                    {
                        // when checkbox has been modified from base values,
                        // Background = new SolidColorBrush(Color.FromArgb(125, 158, 14, 64)),
                        Padding = new Thickness(2)
                    };

                    var cb = new CheckBox
                    {
                        Content = tb,
                        Margin = new Thickness(5), // margin from checkbox to border
                        Tag = (group: category, bit: i),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    cb.ContextMenu = BuildContextMenu(cb);

                    var bordered = new Border
                    {
                        BorderThickness = new Thickness(1),
                        BorderBrush = Brushes.DarkGray,
                        CornerRadius = new CornerRadius(2),
                        Padding = new Thickness(2),
                        Margin = new Thickness(8, 8, 0, 0), // spacing between items
                        Child = cb
                    };


                    // cb.Style = (Style)Application.Current.FindResource("MahApps.Styles.CheckBox");

                    // generate tooltips (copied form spellFamilyClassMaskParser)
                    ArrayList al = _mainwindow.spellFamilyClassMaskParser.GetSpellList(_familyId, (uint)category, (uint)i);
                    _family_spell_counts[index] = al == null ? 0 : al.Count;

                    string _tooltipStr = $"Spell Class Mask {category}: 0x{mask:X8}, (bit {i})\n";
                    _tooltipStr += $"Users : ({_family_spell_counts[index]})\n";
                    if (al != null)
                    {
                        foreach (uint spellId in al)
                        {
                            _tooltipStr += spellId.ToString() + " - " + _mainwindow.GetSpellNameById(spellId) + "\n";
                        }
                    }
                    cb.ToolTip = _tooltipStr;

                    RefreshBitLabel(cb, category, i);

                    if (_family_has_definitions_cache[index] || _family_spell_counts[index] != 0)
                        bordered.Background = new SolidColorBrush(Color.FromArgb(30, 0, 120, 215)); // very light blue overlay
                    else
                    {
                        // TODO
                        // if (ShowUnusedCheckbox.IsChecked == false)
                        //     bordered.Visibility = Visibility.Collapsed;
                    }

                    cb.Checked += CheckBoxBitChanged;
                    cb.Unchecked += CheckBoxBitChanged;

                    _maskCheckBoxes.Add(cb);
                    MaskList.Items.Add(bordered);
                }
            }
        }

        // definition name if we have one, otherwise the raw mask, always suffixed with the user count
        private void RefreshBitLabel(CheckBox cb, int group, int bit)
        {
            int index = (32 * group) + bit;

            string name = SpellFamilyNames.GetFamilyFlagName((int)_familyId, index + 1);
            _family_has_definitions_cache[index] = !string.IsNullOrEmpty(name);

            if (!_family_has_definitions_cache[index])
                name = $"{group}: 0x{1u << bit:X8}";

            ((TextBlock)cb.Content).Text = $"{name} ({_family_spell_counts[index]})";
        }

        private string GetBitLabel(CheckBox cb) => ((TextBlock)cb.Content).Text;

        private void CheckBoxBitChanged(object sender, RoutedEventArgs e)
        {
            var cb = (CheckBox)sender;
            var (group, bit) = ((int g, int bit))cb.Tag;
            // handle change

            uint mask = 1u << bit;

            if (cb.IsChecked == true)
            {
                // Set the bit
                _active_families_values[group] |= mask;
            }
            else
            {
                // Clear the bit
                _active_families_values[group] &= ~mask;
            }

            // set background to green if active
            var border = VisualTreeHelper.GetParent(cb) as Border;
            if (cb.IsChecked == true)
                border.Background = Brushes.DarkGreen;
            else
            {
                var old_color = border.Background;
                if (old_color == Brushes.DarkGreen)
                {
                    // figure out color again
                    int index = (32 * group) + bit;
                    if (_family_has_definitions_cache[index] || _family_spell_counts[index] != 0)
                        border.Background = new SolidColorBrush(Color.FromArgb(30, 0, 120, 215));
                    else
                        border.ClearValue(Border.BackgroundProperty);
                }
            }

            // indicate change from original value by coloring background to range
            bool original_bit_set = (_original_families_values[group] & (1u << bit)) != 0;
            TextBlock textblock = cb.Content as TextBlock;
            if (original_bit_set != cb.IsChecked)
            {
                textblock.Background = new SolidColorBrush(Color.FromArgb(200, 158, 14, 64));
            }
            else
                textblock.ClearValue(TextBlock.BackgroundProperty);


            // update matching numerictext, avoid event chain
            _familyMaskControls[group].ValueChanged -= SpellMask_text_ValueChanged;
            _familyMaskControls[group].Value = _active_families_values[group];
            _familyMaskControls[group].ValueChanged += SpellMask_text_ValueChanged;

            UpdateSpellFamilyClassMaskListbox();
        }

        private void FamiliesSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search_text = FamiliesSearch.Text.ToLower();

            int id = 0;
            if (int.TryParse(search_text, out int value))
            {
                id = value;
            }

            // filter checkboxes from search text
            foreach (var box in _maskCheckBoxes)
            {
                var border = VisualTreeHelper.GetParent(box) as Border;

                var (group, bit) = ((int g, int bit))box.Tag;
                // search bitmask if user searched a number
                if (id > 0)
                {
                    uint mask = 1u << bit;
                    if ((id & mask) != 0)
                    {
                        border.Visibility = Visibility.Visible;
                        continue;
                    }
                }

                var label = GetBitLabel(box);
                if (label.Length <= 0 || label.ToLower().Contains(search_text))
                {
                    // family name matches
                    border.Visibility = Visibility.Visible;
                    continue;
                }

                // search linked spells. Only bother doing it if user input more than 3 characters. (eg "fire" works, but not "fir"
                bool found = false;
                if (label.Length > 3)
                {
                    var spells_list = _mainwindow.spellFamilyClassMaskParser.GetSpellList(_familyId, (uint)group, (uint)bit);
                    if (spells_list != null && spells_list.Count != 0)
                    {
                        foreach (uint spellId in spells_list)
                        {
                            string spell_name = _mainwindow.GetSpellNameById(spellId);
                            if (spell_name != null && spell_name.ToLower().Contains(search_text))
                            {
                                border.Visibility = Visibility.Visible;
                                found = true;
                                break;
                            }
                        }
                    }
                }
                if (!found)
                    border.Visibility = Visibility.Collapsed;

            }
        }

        private void SpellMask_text_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            uint id = 0; // 0-3
            if (sender == SpellMask2)
                id = 1;
            else if (sender == SpellMask3)
                id = 2;

            if (!e.NewValue.HasValue)
                return;

            uint value = (uint)e.NewValue;

            // immediatly update main value ?
            _active_families_values[id] = value;

            // reload all from _families
            Load(_active_families_values);

            UpdateSpellFamilyClassMaskListbox();
        }

        // update the listboxes in background
        private void UpdateSpellFamilyClassMaskListbox()
        {
            // update this window's spell list listbox
            UpdateSpellsListBox();

            // update mainwindow families list
            if (_isBaseFamilies)
            {
                // TODO new function, or pass the listbox as arg
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    _mainwindow.spellFamilyClassMaskParser.UpdateMainWindowBaseFamiliesList(_mainwindow, _familyId, _mainwindow.GetDBAdapter())));
            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    _mainwindow.spellFamilyClassMaskParser.UpdateMainWindowEffectFamiliesList(_mainwindow, _familyId, _mainwindow.GetDBAdapter(), (int)_effectId)));
            }

        }

        private void UpdateSpellsListBox()
        {
            // checkbox checked in XAML triggers this before tabcontrol is initialized
            if (FilterTabControl == null)
                return;

            bool filterduplicates = FilterSpellsDuplicatesCheckbox.IsChecked == true;
            if (FilterTabControl.SelectedIndex == 0)
            { // show target spells
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    _mainwindow.spellFamilyClassMaskParser.UpdateEffectTargetSpellsList(this, _familyId, _mainwindow.GetDBAdapter(), filterduplicates)));
            }
            else if (FilterTabControl.SelectedIndex == 1
                && WoWVersionManager.IsWotlkOrGreaterSelected)
            { // show spells that use the families as target from effects (talents)
                // currently only support WOTLK effect classk masks
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    _mainwindow.spellFamilyClassMaskParser.UpdateEffectModifiersSpellsList(this, _familyId, _mainwindow.GetDBAdapter(), filterduplicates)));
            }
            ApplyTextBoxSpellsFilter();
        }

        private ContextMenu BuildContextMenu(CheckBox cb)
        {
            // TODO, list of talents/spelsl that target this family

            var menu = new ContextMenu();

            var rename = new MenuItem { Header = "Rename Family Bit", Tag = cb };
            rename.Click += Menu_rename_Click;

            menu.Items.Add(rename);

            return menu;
        }

        private async void Menu_rename_Click(object sender, RoutedEventArgs e)
        {
            var cb = (CheckBox)((MenuItem)sender).Tag;
            var (group, bit) = ((int g, int bit))cb.Tag;
            int flagId = (32 * group) + bit + 1;

            string current = SpellFamilyNames.GetFamilyFlagName((int)_familyId, flagId) ?? "";

            string input = await this.ShowInputAsync("Rename Family Bit",
                $"Name for spell class mask {group}, bit {bit} (0x{1u << bit:X8}).\nLeave empty to remove the name.",
                new MetroDialogSettings { DefaultText = current });

            // cancelled
            if (input == null)
                return;

            input = input.Trim();
            if (input == current)
                return;

            try
            {
                SpellFamilyNames.SetFamilyFlagName((int)_familyId, flagId, input);
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync("Rename Family Bit", "Failed to save the family definition:\n" + ex.Message);
                return;
            }

            RefreshBitLabel(cb, group, bit);
        }

        private void FilterClassMaskSpells_TextChanged(object sender, TextChangedEventArgs e)
        {
            Debug.Assert(sender == FilterClassMaskSpells);
            ApplyTextBoxSpellsFilter();

        }
        private void ApplyTextBoxSpellsFilter()
        {
            var input = FilterClassMaskSpells.Text.ToLower();
            ICollectionView view = CollectionViewSource.GetDefaultView(EffectTargetSpellsList.Items);
            view.Filter = o => input.Length == 0 ? true : o.ToString().ToLower().Contains(input);
        }

        private void clear_Button_Click(object sender, RoutedEventArgs e)
        {

            foreach (var cb in _maskCheckBoxes)
            {
                cb.IsChecked = false;
            }
        }

        private void reset_Button_Click(object sender, RoutedEventArgs e)
        {
            Load(_original_families_values);
        }

        private void enable_Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in _maskCheckBoxes)
            {
                cb.IsChecked = true;
            }
        }

        private void FilterTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSpellsListBox();
        }

        private void FilterSpellsDuplicatesCheckbox_StateChanged(object sender, RoutedEventArgs e)
        {
            UpdateSpellsListBox();
        }
    }
}
