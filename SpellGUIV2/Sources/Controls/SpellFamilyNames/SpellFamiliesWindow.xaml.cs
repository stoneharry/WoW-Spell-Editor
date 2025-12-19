using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using SpellEditor.Sources.Controls.Common;
using SpellEditor.Sources.Controls.SpellFamilyNames;
using SpellEditor.Sources.Tools.SpellFamilyClassMaskStoreParser;

namespace SpellEditor.Sources.Controls.SpellFamilyNames
{
    public partial class SpellFamiliesWindow : MetroWindow
    {
        // store effects UI stuff in arrays for easy access
        private readonly TextOnlyNumericUpDown[] _familyMaskControls; // [3]

        public List<CheckBox> _maskCheckBoxes = new List<CheckBox>(); // 32 x 3

        private readonly bool[] _family_has_definitions_cache = new bool[3 * 32];

        private readonly uint[] _original_families_values = new uint[3];
        public readonly uint[] _active_families_values; // reference to the original array in MainWindow
        public MainWindow _mainwindow;
        private readonly uint _familyId;
        private readonly uint _effectId;
        private readonly bool _isBaseFamilies; // wheteher it's spell effect families or base
        public readonly uint _maskCount; // how many family masks there are in array (3 in wotlk, 2 in tbc/vanilla for base, 1 in vanilla for effect item_type


        public SpellFamiliesWindow(uint[] families, uint familyId, MainWindow mainwindow, uint effectId, bool baseFamilies, uint mask_count)
        {
            _familyId = familyId;
            _active_families_values = families;
            _mainwindow = mainwindow;

            _original_families_values = new uint[mask_count];
            for (int i = 0; i < mask_count; i++)
            {
                _original_families_values[i] = _active_families_values[i];
            }
            if (mask_count < 2)
                _familyMaskControls[1].IsEnabled = false;
            if (mask_count < 3)
                _familyMaskControls[2].IsEnabled = false;

            _effectId = effectId;
            _isBaseFamilies = baseFamilies;
            _maskCount = mask_count;

            InitializeComponent();

            if (baseFamilies)
                Title += " [Base]";
            else
                Title += $" [Effect {effectId}]";

            CreateFamilyCheckboxes();

            _familyMaskControls = new TextOnlyNumericUpDown[3] { SpellMask1, SpellMask2, SpellMask3 }; // must be after InitializeComponent()

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
            bool has_definition = SpellFamilyNames.familyFlagsNames.ContainsKey((int)_familyId);
            Dictionary<int, string> definitions = new Dictionary<int, string>();

            if (has_definition)
                definitions = SpellFamilyNames.familyFlagsNames[(int)_familyId];

            for (int category = 0; category < _maskCount; category++)
            {
                for (int i = 0; i < 32; i++)
                {
                    uint mask = 1u << i;

                    int dict_index = (32 * category) + i + 1;
                    string content = "";
                    if (has_definition && definitions.ContainsKey(dict_index))
                    {
                        string data = definitions[dict_index];
                        if (!string.IsNullOrEmpty(data))
                            content = data;
                    }
                    // couldn't load name definition
                    bool bit_has_definition = !string.IsNullOrEmpty(content);
                    _family_has_definitions_cache[dict_index -1] = bit_has_definition;
                    if (!bit_has_definition)
                        // content = $"family{category}: 0x{mask:X8}";
                        content = $"{category}: 0x{mask:X8}";

                    var tb = new TextBlock
                    {
                        Text = content,
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
                    string _tooltipStr = "";
                    _tooltipStr += $"Spell Class Mask {category}: 0x{mask:X8}, (bit {i})\n";
                    if (al != null && al.Count != 0)
                    {
                        // cb.Content += $" ({al.Count})";
                        tb.Text = content += $" ({al.Count})";
                        _tooltipStr += $"Users : ({al.Count})\n";

                        foreach (uint spellId in al)
                        {
                            _tooltipStr += spellId.ToString() + " - " + _mainwindow.GetSpellNameById(spellId) + "\n";
                        }
                    }
                    else
                    {
                        // cb.Content += $" (0)";
                        tb.Text = content + " (0)";
                        _tooltipStr += $"Users : (0)\n";
                    }
                    cb.ToolTip = _tooltipStr;

                    bool used = bit_has_definition || (al != null && al.Count != 0);
                    if (used)
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
                    // use tooltip to check the number of users instead of looking up spells again
                    var lol = cb.ToolTip.ToString().Split('\n');
                    if (_family_has_definitions_cache[index] || cb.ToolTip.ToString().Split('\n').Length > 3) // we have two default tooltip lines
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

                if (box.Content.ToString().Length <= 0 || box.Content.ToString().ToLower().Contains(search_text))
                {
                    // family name matches
                    border.Visibility = Visibility.Visible;
                    continue;
                }

                // search linked spells. Only bother doing it if user input more than 3 characters. (eg "fire" works, but not "fir"
                bool found = false;
                if (box.Content.ToString().Length > 3)
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
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                _mainwindow.spellFamilyClassMaskParser.UpdateEffectTargetSpellsList(this, _familyId, _mainwindow.GetDBAdapter())));

            // update mainwindow families list
            if (_isBaseFamilies)
            {
                // TODO new function, or pass the listbox as arg
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    _mainwindow.spellFamilyClassMaskParser.UpdateMainWindowBaseFamiliesList(this._mainwindow, _familyId, _mainwindow.GetDBAdapter())));
            }
            else
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    _mainwindow.spellFamilyClassMaskParser.UpdateMainWindowEffectFamiliesList(this._mainwindow, _familyId, _mainwindow.GetDBAdapter(), (int)_effectId)));
            }

        }

        private void FilterClassMaskSpells_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filterBox = sender as ThreadSafeTextBox;
            var input = filterBox.Text.ToLower();
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
    
    }
}
