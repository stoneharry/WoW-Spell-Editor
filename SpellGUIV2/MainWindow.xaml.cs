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
        public string LOADED_FILE_STR = "Loaded file: None.";
        private SpellDBC loadedDBC = null;

        private Dictionary<int, TextBox> stringObjectMap = new Dictionary<int, TextBox>();
        private UInt32 selectedID = 1;
        private bool Updating_Strings = false;

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


            // Madness happens if we try to save again so quit program
            Environment.Exit(0);
            return;

            // Try to load file back
            loadedDBC = new SpellDBC();
            if (!loadedDBC.loadDBCFile(fileName))
            {
                await this.ShowMessageAsync("ERROR", "Failed to reload file.");
                return;
            }
            // Update the file loaded
            LOADED_FILE_STR = fileName;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ... Get TabControl reference.
            var item = sender as TabControl;
            // ... Set Title to selected tab header.
            var selected = item.SelectedItem as TabItem;
            // Set title
            this.Title = MAIN_WINDOW_TITLE + selected.Header.ToString();
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
            // Update the file loaded
            LOADED_FILE_STR = fileName;
            populateSelectSpell();
        }
        
        private void populateSelectSpell()
        {
            SelectSpell.Items.Clear();
            for (UInt32 i = 0; i < loadedDBC.body.records.Length; ++i)
            {
                SelectSpell.Items.Add(loadedDBC.body.records[i].Id.ToString() + " - " +
                    loadedDBC.body.strings[loadedDBC.body.records[i].SpellName[0]].value);
            }
        }

        private void updateMainWindow()
        {
            Updating_Strings = true;
            int i;
            for (i = 0; i < 9; ++i)
            {
                TextBox box;
                stringObjectMap.TryGetValue(i, out box);
                box.Text = loadedDBC.body.strings[loadedDBC.body.records[selectedID].SpellName[i]].value;
            }
            for (i = 0; i < 9; ++i)
            {
                TextBox box;
                stringObjectMap.TryGetValue(i + 9, out box);
                box.Text = loadedDBC.body.strings[loadedDBC.body.records[selectedID].Rank[i]].value;
            }
            for (i = 0; i < 9; ++i)
            {
                TextBox box;
                stringObjectMap.TryGetValue(i + 18, out box);
                box.Text = loadedDBC.body.strings[loadedDBC.body.records[selectedID].ToolTip[i]].value;
            }
            for (i = 0; i < 9; ++i)
            {
                TextBox box;
                stringObjectMap.TryGetValue(i + 27, out box);
                box.Text = loadedDBC.body.strings[loadedDBC.body.records[selectedID].Description[i]].value;
            }
            Updating_Strings = false;
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

        private void String_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Updating_Strings || loadedDBC == null)
                return;
            TextBox box = (TextBox)sender;
            string name = box.Name.Substring(5, box.Name.Length - 6);
            UInt32 ID = UInt32.Parse((box.Name[box.Name.Length - 1].ToString()));

            if (name.Equals("Name"))
            {
                loadedDBC.body.strings[loadedDBC.body.records[selectedID].SpellName[ID]].value = box.Text;
            }
            else if (name.Equals("Rank"))
            {
                loadedDBC.body.strings[loadedDBC.body.records[selectedID].Rank[ID]].value = box.Text;
            }
            else if (name.Equals("Tooltip"))
            {
                loadedDBC.body.strings[loadedDBC.body.records[selectedID].ToolTip[ID]].value = box.Text;
            }
            else if (name.Equals("Description"))
            {
                loadedDBC.body.strings[loadedDBC.body.records[selectedID].Description[ID]].value = box.Text;
            }
            else
            {
                throw new Exception("ERROR: Text Box: " + name + " ID: " + ID + " is not supported.");
            }
        }
    }
}
