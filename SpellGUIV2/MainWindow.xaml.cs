using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace SpellGUIV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary> 

    public partial class MainWindow
    {
        public const string MAIN_WINDOW_TITLE = "Stoneharry's Spell Editor V2 - ";
        private SpellDBC loadedDBC = null;
        private SpellIconDBC loadedIconDBC = null;
        private SpellDispelType loadedDispelDBC = null;
        private SpellMechanic loadedMechanic = null;
        private SpellCastTimes loadedCastTime = null;
        private SpellDuration loadedDuration = null;
        private SpellRange loadedRange = null;

        private Dictionary<int, TextBox> stringObjectMap = new Dictionary<int, TextBox>();
        public UInt32 selectedID = 1;
        private bool Updating_Strings = false;
        public TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        public UInt32 NewIconID = 1;

        public string ERROR_STR = "";

        public MainWindow()
        {
            InitializeComponent();

            stringObjectMap.Add(0, SpellName0);
            stringObjectMap.Add(1, SpellName1);
            stringObjectMap.Add(2, SpellName2);
            stringObjectMap.Add(3, SpellName3);
            stringObjectMap.Add(4, SpellName4);
            stringObjectMap.Add(5, SpellName5);
            stringObjectMap.Add(6, SpellName6);
            stringObjectMap.Add(7, SpellName7);
            stringObjectMap.Add(8, SpellName8);
            stringObjectMap.Add(9, SpellRank0);
            stringObjectMap.Add(10, SpellRank1);
            stringObjectMap.Add(11, SpellRank2);
            stringObjectMap.Add(12, SpellRank3);
            stringObjectMap.Add(13, SpellRank4);
            stringObjectMap.Add(14, SpellRank5);
            stringObjectMap.Add(15, SpellRank6);
            stringObjectMap.Add(16, SpellRank7);
            stringObjectMap.Add(17, SpellRank8);
            stringObjectMap.Add(18, SpellTooltip0);
            stringObjectMap.Add(19, SpellTooltip1);
            stringObjectMap.Add(20, SpellTooltip2);
            stringObjectMap.Add(21, SpellTooltip3);
            stringObjectMap.Add(22, SpellTooltip4);
            stringObjectMap.Add(23, SpellTooltip5);
            stringObjectMap.Add(24, SpellTooltip6);
            stringObjectMap.Add(25, SpellTooltip7);
            stringObjectMap.Add(26, SpellTooltip8);
            stringObjectMap.Add(27, SpellDescription0);
            stringObjectMap.Add(28, SpellDescription1);
            stringObjectMap.Add(29, SpellDescription2);
            stringObjectMap.Add(30, SpellDescription3);
            stringObjectMap.Add(31, SpellDescription4);
            stringObjectMap.Add(32, SpellDescription5);
            stringObjectMap.Add(33, SpellDescription6);
            stringObjectMap.Add(34, SpellDescription7);
            stringObjectMap.Add(35, SpellDescription8);

            string[] school_strings = {"Mana", "Rage", "Focus", "Energy", "Happiness",
                                          "Runes", "Runic Power", "Steam", "Pyrite",
                                          "Heat", "Ooze", "Blood", "Wrath"};
            for (int i = 0; i < school_strings.Length; ++i)
            {
                PowerType.Items.Add(school_strings[i]);
            }
        }

        private async void SaveToNewDBC(object sender, RoutedEventArgs e)
        {
            if (loadedDBC == null)
            {
                await this.ShowMessageAsync("ERROR", "There is no DBC loaded.");
            }
            string fileName = await this.ShowInputAsync("Save DBC File", "What do you want to call your new spell DBC?");
            if (fileName == null || fileName.Length < 1)
            {
                await this.ShowMessageAsync("ERROR", "File name is bad.");
                return;
            }
            if (!fileName.ToLower().EndsWith(".dbc"))
            {
                fileName += ".dbc";
            }
            if (!loadedDBC.SaveDBCFile(fileName))
            {
                await this.ShowMessageAsync("ERROR", "Failed to save file.");
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ... Get TabControl reference.
            var item = sender as TabControl;
            // ... Set Title to selected tab header.
            var selected = item.SelectedItem as TabItem;
            // Set title
            this.Title = MAIN_WINDOW_TITLE + selected.Header.ToString();

            if (item.SelectedIndex == 0)
                populateSelectSpell();
            else if (item.SelectedIndex == 2)
                prepareIconEditor();
        }

        private async void prepareIconEditor()
        {
            loadedIconDBC = new SpellIconDBC(this, loadedDBC);

            await loadedIconDBC.loadImages();

            if (ERROR_STR.Length != 0)
            {
                await this.ShowMessageAsync("ERROR", ERROR_STR);
                ERROR_STR = "";
            }
        }

        private async void LoadNewDBCFile(object sender, RoutedEventArgs e)
        {
            string fileName = await this.ShowInputAsync("Load DBC File", "What is the name of your Spell DBC? It must be in the same directory as this program.");
            if (fileName == null || fileName.Length < 1)
            {
                await this.ShowMessageAsync("ERROR", "File name is bad.");
                return;
            }
            if (!fileName.ToLower().EndsWith(".dbc"))
            {
                fileName += ".dbc";
            }
            loadedDBC = new SpellDBC();
            if (!loadedDBC.loadDBCFile(fileName))
            {
                await this.ShowMessageAsync("ERROR", "Failed to load file.");
                return;
            }
            populateSelectSpell();
        }
        
        private void populateSelectSpell()
        {
            if (loadedDBC == null)
                return;
            SelectSpell.Items.Clear();
            for (UInt32 i = 0; i < loadedDBC.body.records.Length; ++i)
            {
                SelectSpell.Items.Add(loadedDBC.body.records[i].record.Id.ToString() + " - " +
                    loadedDBC.body.records[i].spellName[0]);
            }
        }

        private async void updateMainWindow()
        {
            Updating_Strings = true;
            int i;
            for (i = 0; i < 9; ++i)
            {
                TextBox box;
                stringObjectMap.TryGetValue(i, out box);
                box.Text = loadedDBC.body.records[selectedID].spellName[i];
            }
            for (i = 0; i < 9; ++i)
            {
                TextBox box;
                stringObjectMap.TryGetValue(i + 9, out box);
                box.Text = loadedDBC.body.records[selectedID].spellRank[i];
            }
            for (i = 0; i < 9; ++i)
            {
                TextBox box;
                stringObjectMap.TryGetValue(i + 18, out box);
                box.Text = loadedDBC.body.records[selectedID].spellTool[i];
            }
            for (i = 0; i < 9; ++i)
            {
                TextBox box;
                stringObjectMap.TryGetValue(i + 27, out box);
                box.Text = loadedDBC.body.records[selectedID].spellDesc[i];
            }
            Updating_Strings = false;

            CategoryTxt.Text = loadedDBC.body.records[selectedID].record.Category.ToString();
            SpellLevel.Text = loadedDBC.body.records[selectedID].record.spellLevel.ToString();
            BaseLevel.Text = loadedDBC.body.records[selectedID].record.baseLevel.ToString();
            MaxLevel.Text = loadedDBC.body.records[selectedID].record.maxLevel.ToString();
            SpellVisual1.Text = loadedDBC.body.records[selectedID].record.SpellVisual1.ToString();
            SpellVisual2.Text = loadedDBC.body.records[selectedID].record.SpellVisual2.ToString();
            RecoveryTime.Text = loadedDBC.body.records[selectedID].record.RecoveryTime.ToString();
            CategoryRecoveryTime.Text = loadedDBC.body.records[selectedID].record.CategoryRecoveryTime.ToString();
            PowerType.SelectedIndex = (int)loadedDBC.body.records[selectedID].record.powerType;
            ManaCost.Text = loadedDBC.body.records[selectedID].record.manaCost.ToString();
            ManaCostPerLevel.Text = loadedDBC.body.records[selectedID].record.manaCostPerlevel.ToString();
            ManaCostPerSecond.Text = loadedDBC.body.records[selectedID].record.manaPerSecond.ToString();
            PerSecondPerLevel.Text = loadedDBC.body.records[selectedID].record.manaPerSecondPerLevel.ToString();
            ManaCostPercent.Text = loadedDBC.body.records[selectedID].record.ManaCostPercentage.ToString();
            SpellFamilyName.Text = loadedDBC.body.records[selectedID].record.SpellFamilyName.ToString();
            MaxTargets.Text = loadedDBC.body.records[selectedID].record.MaxAffectedTargets.ToString();

            UInt32 mask = loadedDBC.body.records[selectedID].record.SchoolMask;
            /*SCHOOL_MASK_NONE = 0x00;
            SCHOOL_MASK_PHYSICAL = 0x01;
            SCHOOL_MASK_HOLY = 0x02;
            SCHOOL_MASK_FIRE = 0x04;
            SCHOOL_MASK_NATURE = 0x08;
            SCHOOL_MASK_FROST = 0x10;
            SCHOOL_MASK_SHADOW = 0x20;
            SCHOOL_MASK_ARCANE = 0x40;*/
            S1.IsChecked = ((mask & 0x01) != 0) ? true : false;
            S2.IsChecked = ((mask & 0x02) != 0) ? true : false;
            S3.IsChecked = ((mask & 0x04) != 0) ? true : false;
            S4.IsChecked = ((mask & 0x08) != 0) ? true : false;
            S5.IsChecked = ((mask & 0x10) != 0) ? true : false;
            S6.IsChecked = ((mask & 0x20) != 0) ? true : false;
            S7.IsChecked = ((mask & 0x40) != 0) ? true : false;

            if (loadedDispelDBC == null)
            {
                loadedDispelDBC = new SpellDispelType(this, loadedDBC);
                if (ERROR_STR.Length != 0)
                {
                    await this.ShowMessageAsync("ERROR", ERROR_STR);
                    ERROR_STR = "";
                    return;
                }
            }
            loadedDispelDBC.UpdateDispelSelection();
            if (loadedMechanic == null)
            {
                loadedMechanic = new SpellMechanic(this, loadedDBC);
                if (ERROR_STR.Length != 0)
                {
                    await this.ShowMessageAsync("ERROR", ERROR_STR);
                    ERROR_STR = "";
                    return;
                }
            }
            loadedMechanic.updateMechanicSelection();
            if (loadedCastTime == null)
            {
                loadedCastTime = new SpellCastTimes(this, loadedDBC);
                if (ERROR_STR.Length != 0)
                {
                    await this.ShowMessageAsync("ERROR", ERROR_STR);
                    ERROR_STR = "";
                    return;
                }
            }
            loadedCastTime.updateCastTimeSelection();
            if (loadedDuration == null)
            {
                loadedDuration = new SpellDuration(this, loadedDBC);
                if (ERROR_STR.Length != 0)
                {
                    await this.ShowMessageAsync("ERROR", ERROR_STR);
                    ERROR_STR = "";
                    return;
                }
            }
            loadedDuration.updateDurationIndexes();
            if (loadedRange == null)
            {
                loadedRange = new SpellRange(this, loadedDBC);
                if (ERROR_STR.Length != 0)
                {
                    await this.ShowMessageAsync("ERROR", ERROR_STR);
                    ERROR_STR = "";
                    return;
                }
            }
            loadedRange.updateSpellRangeSelection();
        }

        private async void SelectSpell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var addedItems = e.AddedItems;
            if (addedItems.Count > 1)
            {
                await this.ShowMessageAsync("ERROR", "Only one spell can be selected at a time.");
                ((ListBox)sender).UnselectAll();
                return;
            }
            if (addedItems.Count == 1)
            {
                selectedID = (UInt32)(((ListBox)sender).SelectedIndex);
                updateMainWindow();
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDBC.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        // Required so that the same string is not inserted twice due to fast typing
        static object Lock = new object();
        private void String_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Updating_Strings || loadedDBC == null)
                return;

            lock (Lock)
            {
                TextBox box = (TextBox)sender;
                string name = box.Name.Substring(5, box.Name.Length - 6);
                UInt32 ID = UInt32.Parse((box.Name[box.Name.Length - 1].ToString()));

                if (name.Equals("Name"))
                {
                    loadedDBC.body.records[selectedID].spellName[ID] = box.Text;
                }
                else if (name.Equals("Rank"))
                {
                    loadedDBC.body.records[selectedID].spellRank[ID] = box.Text;
                }
                else if (name.Equals("Tooltip"))
                {
                    loadedDBC.body.records[selectedID].spellTool[ID] = box.Text;
                }
                else if (name.Equals("Description"))
                {
                    loadedDBC.body.records[selectedID].spellDesc[ID] = box.Text;
                }
                else
                {
                    throw new Exception("ERROR: Text Box: " + name + " ID: " + ID + " is not supported.");
                }
            }
        }

        private void TextBox_TextChanged(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            try
            {
                TextBox box = (TextBox)sender;
                int ID = Int32.Parse(box.Text);

                for (int i = 0; i < SelectSpell.Items.Count; ++i)
                {
                    string item = SelectSpell.Items.GetItemAt(i).ToString();
                    if (Int32.Parse(item.Split(' ')[0]) == ID)
                    {
                        SelectSpell.SelectedIndex = i;
                        SelectSpell.ScrollIntoView(SelectSpell.Items.GetItemAt(i));
                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                // Not waiting async here, but ah well.
                this.ShowMessageAsync("ERROR", ex.Message);
            }
        }

        private void NewIconClick(object sender, RoutedEventArgs e)
        {
            if (loadedDBC != null)
                loadedDBC.body.records[selectedID].record.SpellIconID = NewIconID;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (loadedDBC == null)
                return;
            string errorMsg = "";
            try
            {
                loadedDBC.body.records[selectedID].record.Category = UInt32.Parse(CategoryTxt.Text);
                loadedDBC.body.records[selectedID].record.spellLevel = UInt32.Parse(SpellLevel.Text);
                loadedDBC.body.records[selectedID].record.baseLevel = UInt32.Parse(BaseLevel.Text);
                loadedDBC.body.records[selectedID].record.maxLevel = UInt32.Parse(MaxLevel.Text);
                loadedDBC.body.records[selectedID].record.SpellVisual1 = UInt32.Parse(SpellVisual1.Text);
                loadedDBC.body.records[selectedID].record.SpellVisual2 = UInt32.Parse(SpellVisual2.Text);
                loadedDBC.body.records[selectedID].record.RecoveryTime = UInt32.Parse(RecoveryTime.Text);
                loadedDBC.body.records[selectedID].record.CategoryRecoveryTime = UInt32.Parse(CategoryRecoveryTime.Text);
                loadedDBC.body.records[selectedID].record.powerType = (UInt32)PowerType.SelectedIndex;
                loadedDBC.body.records[selectedID].record.manaCost = UInt32.Parse(ManaCost.Text);
                loadedDBC.body.records[selectedID].record.manaCostPerlevel = UInt32.Parse(ManaCostPerLevel.Text);
                loadedDBC.body.records[selectedID].record.manaPerSecond = UInt32.Parse(ManaCostPerSecond.Text);
                loadedDBC.body.records[selectedID].record.manaPerSecondPerLevel = UInt32.Parse(PerSecondPerLevel.Text);
                loadedDBC.body.records[selectedID].record.ManaCostPercentage = UInt32.Parse(ManaCostPercent.Text);
                loadedDBC.body.records[selectedID].record.SpellFamilyName = UInt32.Parse(SpellFamilyName.Text);
                loadedDBC.body.records[selectedID].record.MaxAffectedTargets = UInt32.Parse(MaxTargets.Text);
                loadedDBC.body.records[selectedID].record.SchoolMask = 
                    (S1.IsChecked.Value ? (UInt32)0x01 : (UInt32)0x00) +
                    (S2.IsChecked.Value ? (UInt32)0x02 : (UInt32)0x00) +
                    (S3.IsChecked.Value ? (UInt32)0x04 : (UInt32)0x00) +
                    (S4.IsChecked.Value ? (UInt32)0x08 : (UInt32)0x00) +
                    (S5.IsChecked.Value ? (UInt32)0x10 : (UInt32)0x00) +
                    (S6.IsChecked.Value ? (UInt32)0x20 : (UInt32)0x00) +
                    (S7.IsChecked.Value ? (UInt32)0x40 : (UInt32)0x00);
            }
            catch (Exception ex)
            {
                errorMsg = "Could not save data. Probably one of the "
                    + "inputs was in a bad format, like putting letters "
                    + "where a number is expected. Error message: " + ex.Message;
            }
            if (errorMsg.Length != 0)
            {
                await this.ShowMessageAsync("ERROR", errorMsg);
                errorMsg = "";
            }
            else
                await this.ShowMessageAsync("Success", "Record was saved successfully. Remember to save the DBC file once you are done otherwise your changes will be lost!");
        }

        private void DispelType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loadedDBC != null)
                loadedDBC.body.records[selectedID].record.Dispel = loadedDispelDBC.IndexToIDMap[((ComboBox)sender).SelectedIndex];
        }

        private void MechanicType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loadedDBC != null)
            {
                for (int i = 0; i < loadedMechanic.body.lookup.Count; ++i)
                {
                    if (loadedMechanic.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        loadedDBC.body.records[selectedID].record.Mechanic = (UInt32)loadedMechanic.body.lookup[i].ID;
                        break;
                    }
                }
            }
        }

        private void CastTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loadedDBC != null)
            {
                for (int i = 0; i < loadedCastTime.body.lookup.Count; ++i)
                {
                    if (loadedCastTime.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        loadedDBC.body.records[selectedID].record.CastingTimeIndex = (UInt32)loadedCastTime.body.lookup[i].ID;
                        break;
                    }
                }               
            }
        }

        private void SpellDuration_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loadedDBC != null)
            {
                for (int i = 0; i < loadedDuration.body.lookup.Count; ++i)
                {
                    if (loadedDuration.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        loadedDBC.body.records[selectedID].record.DurationIndex = (UInt32)loadedDuration.body.lookup[i].ID;
                        break;
                    }
                }
            }
        }

        private void SpellRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loadedDBC != null)
            {
                for (int i = 0; i < loadedRange.body.lookup.Count; ++i)
                {
                    if (loadedRange.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        loadedDBC.body.records[selectedID].record.rangeIndex = (UInt32)loadedRange.body.lookup[i].ID;
                        break;
                    }
                }
            }
        }
    }
}
