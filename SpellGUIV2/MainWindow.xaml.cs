using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using SpellEditor.Sources.Constants;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.Controls;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Threading;
using SpellEditor.Sources.Config;
using System.Data;
using MySql.Data.MySqlClient;
using System.ComponentModel;
using SpellEditor.Sources.SpellStringTools;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.Tools.SpellFamilyClassMaskStoreParser;
using SpellEditor.Sources.Binding;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using SpellEditor.Sources.BLP;
using SpellEditor.Sources.VersionControl;

namespace SpellEditor
{
    partial class MainWindow
    {
        #region Boxes
        private Dictionary<int, ThreadSafeTextBox> stringObjectMap = new Dictionary<int, ThreadSafeTextBox>();
        private List<ThreadSafeCheckBox> attributes0 = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> attributes1 = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> attributes2 = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> attributes3 = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> attributes4 = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> attributes5 = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> attributes6 = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> attributes7 = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> stancesBoxes = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> targetCreatureTypeBoxes = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> targetBoxes = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> procBoxes = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> interrupts1 = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> interrupts2 = new List<ThreadSafeCheckBox>();
        private List<ThreadSafeCheckBox> interrupts3 = new List<ThreadSafeCheckBox>();
        public List<ThreadSafeCheckBox> equippedItemInventoryTypeMaskBoxes = new List<ThreadSafeCheckBox>();
        public List<ThreadSafeCheckBox> equippedItemSubClassMaskBoxes = new List<ThreadSafeCheckBox>();
        #endregion

        #region MemberVariables
        private IDatabaseAdapter adapter;
        public uint selectedID = 0;
        public uint newIconID = 1;
        private bool updating;
        public TaskScheduler UIScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        private DataTable spellTable = new DataTable();
        private int storedLocale = -1;
        private SpellStringParser SpellStringParser = new SpellStringParser();

        private List<ThreadSafeTextBox> spellDescGenFields = new List<ThreadSafeTextBox>();
        private List<ThreadSafeTextBox> spellTooltipGenFields = new List<ThreadSafeTextBox>();
        public SpellFamilyClassMaskParser spellFamilyClassMaskParser;
        #endregion

        public IDatabaseAdapter GetDBAdapter()
        {
            return adapter;
        }

        public MainWindow()
        {
            // If no debugger is attached then output console text to a file
            if (!Debugger.IsAttached)
            {
                var ostrm = new FileStream("debug_output.txt", FileMode.OpenOrCreate, FileAccess.Write);
                var writer = new StreamWriter(ostrm);
                Console.SetOut(writer);
            }
            // Ensure the decimal seperator used is always a full stop
            var customCulture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = customCulture;
            // Banner
            Console.WriteLine("######################################################");
            Console.WriteLine($"Starting WoW Spell Editor - {DateTime.Now.ToString()}");
            Console.WriteLine("######################################################");
            // Config must be initialised fast
            Config.Init();
            InitializeComponent();
        }

        public async void HandleErrorMessage(string msg)
        {
            if (Dispatcher.CheckAccess())
                await this.ShowMessageAsync(TryFindResource("SpellEditor").ToString(), msg);
            else
                Dispatcher.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => this.ShowMessageAsync(TryFindResource("SpellEditor").ToString(), msg)));
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Console.WriteLine("ERROR: " + e.Exception + "\n" + e.Exception.InnerException);
            File.WriteAllText("error.txt", e.Exception + "\n" + e.Exception.InnerException, UTF8Encoding.GetEncoding(0));
            HandleErrorMessage(e.Exception + "\n\n" + e.Exception.InnerException);
            e.Handled = true;
            Console.Out.Flush();
        }

        public int GetLanguage() {
            // FIXME(Harry)
            // Disabled returning Locale_langauge until it can at least support multiple client types
            return GetLocale() == -1 ? 0 : GetLocale();
            //return (int)Locale_language;
        }

        #region LanguageSwitch
        private void RefreshAllUIElements()
        {
            Attributes1.Children.Clear();
            attributes0.Clear();
            string[] attFlags = TryFindResource("attFlags_strings").ToString().Split('|');
            for (var i = 0; i < attFlags.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();
                box.Content = attFlags[i];
                box.ToolTip = attFlags[i];
                box.Margin = new Thickness(0, 5, 0, 0);

                Attributes1.Children.Add(box);
                attributes0.Add(box);
            }

            Attributes2.Children.Clear();
            attributes1.Clear();
            attFlags = TryFindResource("attFlagsEx_strings").ToString().Split('|');
            for (var i = 0; i < attFlags.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();
                box.Content = attFlags[i];
                box.ToolTip = attFlags[i];
                box.Margin = new Thickness(0, 5, 0, 0);

                Attributes2.Children.Add(box);
                attributes1.Add(box);
            }

            Attributes3.Children.Clear();
            attributes2.Clear();
            attFlags = TryFindResource("attFlagsEx1_strings").ToString().Split('|');
            for (int i = 0; i < attFlags.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();
                box.Content = attFlags[i];
                box.ToolTip = attFlags[i];
                box.Margin = new Thickness(0, 5, 0, 0);

                Attributes3.Children.Add(box);
                attributes2.Add(box);
            }

            Attributes4.Children.Clear();
            attributes3.Clear();
            attFlags = TryFindResource("attFlagsEx2_strings").ToString().Split('|');
            for (int i = 0; i < attFlags.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();
                box.Content = attFlags[i];
                box.ToolTip = attFlags[i];
                box.Margin = new Thickness(0, 5, 0, 0);

                Attributes4.Children.Add(box);
                attributes3.Add(box);
            }

            Attributes5.Children.Clear();
            attributes4.Clear();
            attFlags = TryFindResource("attFlagsEx3_strings").ToString().Split('|');
            for (int i = 0; i < attFlags.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();
                box.Content = attFlags[i];
                box.ToolTip = attFlags[i];
                box.Margin = new Thickness(0, 5, 0, 0);

                Attributes5.Children.Add(box);
                attributes4.Add(box);
            }

            Attributes6.Children.Clear();
            attributes5.Clear();
            attFlags = TryFindResource("attFlagsEx4_strings").ToString().Split('|');
            for (int i = 0; i < attFlags.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();
                box.Content = attFlags[i];
                box.ToolTip = attFlags[i];
                box.Margin = new Thickness(0, 5, 0, 0);

                Attributes6.Children.Add(box);
                attributes5.Add(box);
            }

            Attributes7.Children.Clear();
            attributes6.Clear();
            attFlags = TryFindResource("attFlagsEx5_strings").ToString().Split('|');
            for (int i = 0; i < attFlags.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();
                box.Content = attFlags[i];
                box.ToolTip = attFlags[i];
                box.Margin = new Thickness(0, 5, 0, 0);

                Attributes7.Children.Add(box);
                attributes6.Add(box);
            }

            Attributes8.Children.Clear();
            attributes7.Clear();
            attFlags = TryFindResource("attFlagsEx6_strings").ToString().Split('|');
            for (int i = 0; i < attFlags.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();
                box.Content = attFlags[i];
                box.ToolTip = attFlags[i];
                box.Margin = new Thickness(0, 5, 0, 0);

                Attributes8.Children.Add(box);
                attributes7.Add(box);
            }

            StancesGrid.Children.Clear();
            stancesBoxes.Clear();
            string[] stances_strings = TryFindResource("stances_strings").ToString().Split('|');
            for (int i = 0; i < stances_strings.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                box.Content = stances_strings[i];
                box.ToolTip = stances_strings[i];
                box.Margin = new Thickness(0, 5, 0, 0);

                StancesGrid.Children.Add(box);
                stancesBoxes.Add(box);
            }

            TargetCreatureType.Children.Clear();
            targetCreatureTypeBoxes.Clear();
            string[] creature_type_strings = TryFindResource("creature_type_strings").ToString().Split('|');
            for (int i = 0; i < creature_type_strings.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                box.Content = creature_type_strings[i];
                box.ToolTip = creature_type_strings[i];
                box.Margin = new Thickness(0, 5, 0, 0);

                TargetCreatureType.Children.Add(box);
                targetCreatureTypeBoxes.Add(box);
            }

            CasterAuraState.Items.Clear();
            string[] caster_aura_state_strings = TryFindResource("caster_aura_state_strings").ToString().Split('|');
            for (int i = 0; i < caster_aura_state_strings.Length; ++i) { CasterAuraState.Items.Add(caster_aura_state_strings[i]); }

            TargetAuraState.Items.Clear();
            string[] target_aura_state_strings = TryFindResource("target_aura_state_strings").ToString().Split('|');
            for (int i = 0; i < target_aura_state_strings.Length; ++i) { TargetAuraState.Items.Add(target_aura_state_strings[i]); }

            EquippedItemInventoryTypeGrid.Children.Clear();
            equippedItemInventoryTypeMaskBoxes.Clear();
            string[] equipped_item_inventory_type_mask_strings = TryFindResource("equipped_item_inventory_type_mask_strings").ToString().Split('|');
            for (int i = 0; i < equipped_item_inventory_type_mask_strings.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                box.Content = equipped_item_inventory_type_mask_strings[i];
                box.Margin = new Thickness(0, 5, 0, 0);

                EquippedItemInventoryTypeGrid.Children.Add(box);
                equippedItemInventoryTypeMaskBoxes.Add(box);
            }

            EquippedItemSubClassGrid.Children.Clear();
            equippedItemSubClassMaskBoxes.Clear();
            for (int i = 0; i < 29; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                box.Content = TryFindResource("None").ToString();
                box.Margin = new Thickness(0, 5, 0, 0);
                box.Visibility = Visibility.Hidden;
                EquippedItemSubClassGrid.Children.Add(box);
                equippedItemSubClassMaskBoxes.Add(box);
            }

            PowerType.Items.Clear();
            string[] school_strings = TryFindResource("school_strings").ToString().Split('|');
            for (int i = 0; i < school_strings.Length; ++i) { PowerType.Items.Add(school_strings[i]); }

            SpellDamageType.Items.Clear();
            PreventionType.Items.Clear();
            string[] damage_prevention_types = TryFindResource("damage_prevention_types").ToString().Split('|');
            for (int i = 0; i < damage_prevention_types.Length; ++i)
            {
                if (i < 4)
                {
                    SpellDamageType.Items.Add(damage_prevention_types[i]);
                }
                else
                {
                    PreventionType.Items.Add(damage_prevention_types[i]);
                }
            }

            TargetEditorGrid.Children.Clear();
            targetBoxes.Clear();
            string[] target_strings = TryFindResource("target_strings").ToString().Split('|');
            for (int i = 0; i < target_strings.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                box.Content = target_strings[i];
                box.Margin = new Thickness(0, 5, 0, 0);

                TargetEditorGrid.Children.Add(box);
                targetBoxes.Add(box);
            }

            ProcEditorGrid.Children.Clear();
            procBoxes.Clear();
            string[] proc_strings = TryFindResource("proc_strings").ToString().Split('|');
            for (int i = 0; i < proc_strings.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                box.Content = proc_strings[i];
                box.Margin = new Thickness(0, 5, 0, 0);


                ProcEditorGrid.Children.Add(box);
                procBoxes.Add(box);
            }

            ApplyAuraName1.Items.Clear();
            ApplyAuraName2.Items.Clear();
            ApplyAuraName3.Items.Clear();
            string[] spell_aura_effect_names = TryFindResource("spell_aura_effect_names").ToString().Split('|');
            for (int i = 0; i < spell_aura_effect_names.Length; ++i)
            {
                ApplyAuraName1.Items.Add(i + " - " + spell_aura_effect_names[i]);
                ApplyAuraName2.Items.Add(i + " - " + spell_aura_effect_names[i]);
                ApplyAuraName3.Items.Add(i + " - " + spell_aura_effect_names[i]);
            }

            SpellEffect1.Items.Clear();
            SpellEffect2.Items.Clear();
            SpellEffect3.Items.Clear();
            string[] spell_effect_names = TryFindResource("spell_effect_names").ToString().Split('|');
            for (int i = 0; i < spell_effect_names.Length; ++i)
            {
                SpellEffect1.Items.Add(i + " - " + spell_effect_names[i]);
                SpellEffect2.Items.Add(i + " - " + spell_effect_names[i]);
                SpellEffect3.Items.Add(i + " - " + spell_effect_names[i]);
            }

            Mechanic1.Items.Clear();
            Mechanic2.Items.Clear();
            Mechanic3.Items.Clear();
            string[] mechanic_names = TryFindResource("mechanic_names").ToString().Split('|');
            for (int i = 0; i < mechanic_names.Length; ++i)
            {
                Mechanic1.Items.Add(mechanic_names[i]);
                Mechanic2.Items.Add(mechanic_names[i]);
                Mechanic3.Items.Add(mechanic_names[i]);
            }

            if (TargetA1.Items.Count == 0)
            {
                int number = 0;
                foreach (Targets t in Enum.GetValues(typeof(Targets)))
                {
                    string toDisplay = number + " - " + t;
                    TargetA1.Items.Add(toDisplay);
                    TargetB1.Items.Add(toDisplay);
                    TargetA2.Items.Add(toDisplay);
                    TargetB2.Items.Add(toDisplay);
                    TargetA3.Items.Add(toDisplay);
                    TargetB3.Items.Add(toDisplay);

                    //ChainTarget1.Items.Add(toDisplay);
                    //ChainTarget2.Items.Add(toDisplay);
                    //ChainTarget3.Items.Add(toDisplay);
                    ++number;
                }
            }

            InterruptFlagsGrid.Children.Clear();
            interrupts1.Clear();
            string[] interrupt_strings = TryFindResource("interrupt_strings").ToString().Split('|');
            for (int i = 0; i < interrupt_strings.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                box.Content = interrupt_strings[i];
                box.Margin = new Thickness(0, 5, 0, 0);


                InterruptFlagsGrid.Children.Add(box);
                interrupts1.Add(box);
            }

            AuraInterruptFlagsGrid.Children.Clear();
            interrupts2.Clear();
            string[] aura_interrupt_strings = TryFindResource("aura_interrupt_strings").ToString().Split('|');
            for (int i = 0; i < aura_interrupt_strings.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                box.Content = aura_interrupt_strings[i];
                box.Margin = new Thickness(0, 5, 0, 0);

                AuraInterruptFlagsGrid.Children.Add(box);
                interrupts2.Add(box);
            }

            ChannelInterruptFlagsGrid.Children.Clear();
            interrupts3.Clear();
            string[] channel_interrupt_strings = TryFindResource("channel_interrupt_strings").ToString().Split('|');
            for (int i = 0; i < channel_interrupt_strings.Length; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                box.Content = channel_interrupt_strings[i];
                box.Margin = new Thickness(0, 5, 0, 0);


                ChannelInterruptFlagsGrid.Children.Add(box);
                interrupts3.Add(box);
            }

            // FIXME(Harry) Doesn't retain count after changing language
            SpellsLoadedLabel.Content = string.Format(TryFindResource("Highest_Spell_ID").ToString(), "");
        }
        #endregion

        #region Loaded
        private void _Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);

            try
            {
                Title = $"{Title} ({WoWVersionManager.GetInstance().SelectedVersion().Version})";

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

                spellDescGenFields.Add(SpellDescriptionGen0);
                spellDescGenFields.Add(SpellDescriptionGen1);
                spellDescGenFields.Add(SpellDescriptionGen2);
                spellDescGenFields.Add(SpellDescriptionGen3);
                spellDescGenFields.Add(SpellDescriptionGen4);
                spellDescGenFields.Add(SpellDescriptionGen5);
                spellDescGenFields.Add(SpellDescriptionGen6);
                spellDescGenFields.Add(SpellDescriptionGen7);
                spellDescGenFields.Add(SpellDescriptionGen8);
                spellTooltipGenFields.Add(SpellTooltipGen0);
                spellTooltipGenFields.Add(SpellTooltipGen1);
                spellTooltipGenFields.Add(SpellTooltipGen2);
                spellTooltipGenFields.Add(SpellTooltipGen3);
                spellTooltipGenFields.Add(SpellTooltipGen4);
                spellTooltipGenFields.Add(SpellTooltipGen5);
                spellTooltipGenFields.Add(SpellTooltipGen6);
                spellTooltipGenFields.Add(SpellTooltipGen7);
                spellTooltipGenFields.Add(SpellTooltipGen8);

                RefreshAllUIElements();

                for (int i = 0; i < 32; ++i)
                {
                    uint mask = (uint)Math.Pow(2, i);

                    SpellMask11.Items.Add(new ThreadSafeCheckBox() { Content = "0x" + mask.ToString("x8") });
                    SpellMask12.Items.Add(new ThreadSafeCheckBox() { Content = "0x" + mask.ToString("x8") });
                    SpellMask13.Items.Add(new ThreadSafeCheckBox() { Content = "0x" + mask.ToString("x8") });
                    SpellMask21.Items.Add(new ThreadSafeCheckBox() { Content = "0x" + mask.ToString("x8") });
                    SpellMask22.Items.Add(new ThreadSafeCheckBox() { Content = "0x" + mask.ToString("x8") });
                    SpellMask23.Items.Add(new ThreadSafeCheckBox() { Content = "0x" + mask.ToString("x8") });
                    SpellMask31.Items.Add(new ThreadSafeCheckBox() { Content = "0x" + mask.ToString("x8") });
                    SpellMask32.Items.Add(new ThreadSafeCheckBox() { Content = "0x" + mask.ToString("x8") });
                    SpellMask33.Items.Add(new ThreadSafeCheckBox() { Content = "0x" + mask.ToString("x8") });
                }

                foreach (ThreadSafeCheckBox cb in SpellMask11.Items) { cb.Checked += HandspellFamilyClassMask_Checked; cb.Unchecked += HandspellFamilyClassMask_Checked; }
                foreach (ThreadSafeCheckBox cb in SpellMask12.Items) { cb.Checked += HandspellFamilyClassMask_Checked; cb.Unchecked += HandspellFamilyClassMask_Checked; }
                foreach (ThreadSafeCheckBox cb in SpellMask13.Items) { cb.Checked += HandspellFamilyClassMask_Checked; cb.Unchecked += HandspellFamilyClassMask_Checked; }
                foreach (ThreadSafeCheckBox cb in SpellMask21.Items) { cb.Checked += HandspellFamilyClassMask_Checked; cb.Unchecked += HandspellFamilyClassMask_Checked; }
                foreach (ThreadSafeCheckBox cb in SpellMask22.Items) { cb.Checked += HandspellFamilyClassMask_Checked; cb.Unchecked += HandspellFamilyClassMask_Checked; }
                foreach (ThreadSafeCheckBox cb in SpellMask23.Items) { cb.Checked += HandspellFamilyClassMask_Checked; cb.Unchecked += HandspellFamilyClassMask_Checked; }
                foreach (ThreadSafeCheckBox cb in SpellMask31.Items) { cb.Checked += HandspellFamilyClassMask_Checked; cb.Unchecked += HandspellFamilyClassMask_Checked; }
                foreach (ThreadSafeCheckBox cb in SpellMask32.Items) { cb.Checked += HandspellFamilyClassMask_Checked; cb.Unchecked += HandspellFamilyClassMask_Checked; }
                foreach (ThreadSafeCheckBox cb in SpellMask33.Items) { cb.Checked += HandspellFamilyClassMask_Checked; cb.Unchecked += HandspellFamilyClassMask_Checked; }


                // TODO: This should happen when the language has been established 
                /*
                switch ((LocaleConstant)GetLanguage())
                {
                    case LocaleConstant.LOCALE_enUS:
                        TabItem_English.Focus();
                        break;
                    case LocaleConstant.LOCALE_koKR:
                        TabItem_Korean.Focus();
                        break;
                    case LocaleConstant.LOCALE_frFR:
                        TabItem_French.Focus();
                        break;
                    case LocaleConstant.LOCALE_deDE:
                        TabItem_Deutsch.Focus();
                        break;
                    case LocaleConstant.LOCALE_zhCN:
                        TabItem_Chinese.Focus();
                        break;
                    case LocaleConstant.LOCALE_zhTW:
                        TabItem_Taiwanese.Focus();
                        break;
                    case LocaleConstant.LOCALE_esES:
                        TabItem_Mexican.Focus();
                        break;
                    case LocaleConstant.LOCALE_esMX:
                        TabItem_Portuguese.Focus();
                        break;
                    case LocaleConstant.LOCALE_ruRU:
                        TabItem_Russian.Focus();
                        break;
                    default:
                        break;
                }
                */

                loadAllData();
            }

            catch (Exception ex)
            {
                HandleErrorMessage(ex.Message);
            }
        }

        private void HandspellFamilyClassMask_Checked(object obj, RoutedEventArgs e)
        {
            ThreadSafeComboBox father = (ThreadSafeComboBox)((ThreadSafeCheckBox)obj).Parent;

            uint Mask = 0;
            for (uint i = 0; i < 32; i++)
            {
                ThreadSafeCheckBox cb = (ThreadSafeCheckBox)father.Items.GetItemAt((int)i);
                Mask += cb.IsChecked == true ? (uint)Math.Pow(2, i) : 0;
            }
            father.Text = Mask.ToString();
        }

        #endregion

        public delegate void UpdateProgressFunc(double value);
        public delegate void UpdateTextFunc(string value);

        #region ImportExportSpellDBC
        private async void ImportExportSpellDbcButton(object sender, RoutedEventArgs e)
        {
            var window = new ImportExportWindow(adapter);
            var controller = await this.ShowProgressAsync(TryFindResource("Import/Export").ToString(), TryFindResource("String1").ToString());
            controller.SetCancelable(false);
            window.Show();
            window.Width = window.Width / 2;
            while (window.IsVisible && !window.IsDataSelected())
                await Task.Delay(100);
            if (window.IsVisible)
                window.Close();
            var isImport = window.BindingImportList.Count > 0;
            var bindingList = isImport ? window.BindingImportList : window.BindingExportList;
            var manager = DBCManager.GetInstance();
            foreach (var bindingName in bindingList)
            {
                controller.SetMessage($"{(isImport ? "Importing" : "Exporting")} {bindingName}.dbc...");
                manager.ClearDbcBinding(bindingName);
                var abstractDbc = manager.FindDbcForBinding(bindingName);
                if (abstractDbc == null)
                {
                    try
                    {
                        abstractDbc = new GenericDbc($"{ Config.DbcDirectory }\\{ bindingName }.dbc");
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine($"ERROR: Failed to load {Config.DbcDirectory}\\{bindingName}.dbc: {exception.Message}\n{exception}\n{exception.InnerException}");
                        ShowFlyoutMessage($"Failed to load {Config.DbcDirectory}\\{bindingName}.dbc");
                        continue;
                    }
                }
                if (isImport && !abstractDbc.HasData())
                    abstractDbc.ReloadContents();
                if (isImport)
                    await abstractDbc.ImportToSql(adapter, new UpdateProgressFunc(controller.SetProgress), "ID", bindingName);
                else
                    await abstractDbc.ExportToDbc(adapter, new UpdateProgressFunc(controller.SetProgress), "ID", bindingName);
            }
            controller.SetMessage(TryFindResource("ReloadingUI").ToString());
            PopulateSelectSpell();
            await controller.CloseAsync();
        }
        #endregion

        #region ConfigButton
        private ConfigWindow ConfigWindowInstance = null;

        private void ConfigButtonClick(object sender, RoutedEventArgs e)
        {
            if (ConfigWindowInstance != null && ConfigWindowInstance.IsLoaded)
            {
                ConfigWindowInstance.Activate();
                return;
            }
            var window = new ConfigWindow(adapter is MySQL ? 
                ConfigWindow.DatabaseIdentifier.MySQL : ConfigWindow.DatabaseIdentifier.SQLite);
            window.Show();
            window.Width = window.Width * 0.6;
            window.Height = window.Height * 0.7;
            ConfigWindowInstance = window;
        }
        #endregion

        #region InitialiseMemberVariables
        private async void loadAllData()
        {
            await GetConfig();
            if (!Config.isInit)
            {
                await this.ShowMessageAsync(TryFindResource("ERROR").ToString(), TryFindResource("String2").ToString());
                return;
            }
            try
            {
                switch (Config.connectionType)
                {
                    case Config.ConnectionType.MySQL:
                        adapter = new MySQL();
                        break;
                    case Config.ConnectionType.SQLite:
                        adapter = new SQLite();
                        break;
                    default:
                        throw new Exception("Unknown connection type, valid types: MySQL, SQLite");
                }
            }
            catch (Exception e)
            {
                await this.ShowMessageAsync(TryFindResource("ERROR").ToString(), string.Format("{0}\n{1}",
                    TryFindResource("Input_MySQL_Error").ToString(), e.Message + "\n" + e.InnerException?.Message));
                return;
            }
            try
            {
                // Load required DBC's. First the ones with dependencies and inject them into the manager
                var manager = DBCManager.GetInstance();
                manager.LoadRequiredDbcs();
                if (WoWVersionManager.IsWotlkOrGreaterSelected)
                {
                    manager.InjectLoadedDbc("AreaGroup", new AreaGroup(((AreaTable)manager.FindDbcForBinding("AreaTable")).Lookups));
                    manager.InjectLoadedDbc("SpellDifficulty", new SpellDifficulty(adapter));
                }
                manager.InjectLoadedDbc("SpellIcon", new SpellIconDBC(this, adapter));
                spellFamilyClassMaskParser = new SpellFamilyClassMaskParser(this);
            }
            catch (Exception e)
            {
                await this.ShowMessageAsync(TryFindResource("ERROR").ToString(), string.Format("{0}\n\n{1}\n{2}",
                    TryFindResource("LoadDBCFromBinding_Error_1").ToString(),
                    e, e.InnerException));
                return;
            }

            var controller = await this.ShowProgressAsync(TryFindResource("PleaseWait").ToString(), TryFindResource("PleaseWait_2").ToString());
            controller.SetCancelable(false);
            await Task.Delay(500);
            try
            {
                using (var d = Dispatcher.DisableProcessing())
                {
                    spellTable.Columns.Add("id", typeof(uint));
                    spellTable.Columns.Add("SpellName" + GetLocale(), typeof(string));
                    spellTable.Columns.Add("Icon", typeof(uint));

                    PopulateSelectSpell();

                    // Populate UI based on DBC data
                    Category.ItemsSource = ConvertBoxListToLabels(((SpellCategory)
                        DBCManager.GetInstance().FindDbcForBinding("SpellCategory")).GetAllBoxes());
                    DispelType.ItemsSource = ConvertBoxListToLabels(((SpellDispelType)
                        DBCManager.GetInstance().FindDbcForBinding("SpellDispelType")).GetAllBoxes());
                    MechanicType.ItemsSource = ConvertBoxListToLabels(((SpellMechanic)
                        DBCManager.GetInstance().FindDbcForBinding("SpellMechanic")).GetAllBoxes());
                    RequiresSpellFocus.ItemsSource = ConvertBoxListToLabels(((SpellFocusObject)
                        DBCManager.GetInstance().FindDbcForBinding("SpellFocusObject")).GetAllBoxes());
                    CastTime.ItemsSource = ConvertBoxListToLabels(((SpellCastTimes)
                        DBCManager.GetInstance().FindDbcForBinding("SpellCastTimes")).GetAllBoxes());
                    Duration.ItemsSource = ConvertBoxListToLabels(((SpellDuration)
                        DBCManager.GetInstance().FindDbcForBinding("SpellDuration")).GetAllBoxes());
                    Range.ItemsSource = ConvertBoxListToLabels(((SpellRange)
                        DBCManager.GetInstance().FindDbcForBinding("SpellRange")).GetAllBoxes());
                    var radiusLabels = ConvertBoxListToLabels(((SpellRadius)
                        DBCManager.GetInstance().FindDbcForBinding("SpellRadius")).GetAllBoxes());
                    RadiusIndex1.ItemsSource = radiusLabels;
                    RadiusIndex2.ItemsSource = radiusLabels;
                    RadiusIndex3.ItemsSource = radiusLabels;
                    EquippedItemClass.ItemsSource = ConvertBoxListToLabels(((ItemClass)
                        DBCManager.GetInstance().FindDbcForBinding("ItemClass")).GetAllBoxes());
                    var isTbcOrGreater = WoWVersionManager.IsTbcOrGreaterSelected;
                    var isWotlkOrGreater = WoWVersionManager.IsWotlkOrGreaterSelected;
                    if (isTbcOrGreater)
                    {
                        var totemLabels = ConvertBoxListToLabels(((TotemCategory)
                            DBCManager.GetInstance().FindDbcForBinding("TotemCategory")).GetAllBoxes());
                        TotemCategory1.ItemsSource = totemLabels;
                        TotemCategory2.ItemsSource = totemLabels;
                    }
                    if (isWotlkOrGreater)
                    {
                        AreaGroup.ItemsSource = ConvertBoxListToLabels(((AreaGroup)
                            DBCManager.GetInstance().FindDbcForBinding("AreaGroup")).GetAllBoxes());
                        Difficulty.ItemsSource = ConvertBoxListToLabels(((SpellDifficulty)
                            DBCManager.GetInstance().FindDbcForBinding("SpellDifficulty")).GetAllBoxes());
                        RuneCost.ItemsSource = ConvertBoxListToLabels(((SpellRuneCost)
                            DBCManager.GetInstance().FindDbcForBinding("SpellRuneCost")).GetAllBoxes());
                        SpellDescriptionVariables.ItemsSource = ConvertBoxListToLabels(((SpellDescriptionVariables)
                            DBCManager.GetInstance().FindDbcForBinding("SpellDescriptionVariables")).GetAllBoxes());
                    }
                    AreaGroup.IsEnabled = isWotlkOrGreater;
                    Difficulty.IsEnabled = isWotlkOrGreater;
                    TotemCategory1.IsEnabled = isTbcOrGreater;
                    TotemCategory2.IsEnabled = isTbcOrGreater;
                    RuneCost.IsEnabled = isWotlkOrGreater;
                    SpellDescriptionVariables.IsEnabled = isWotlkOrGreater;

                    PrepareIconEditor();
                }
            }
            catch (Exception e)
            {
                await controller.CloseAsync();
                await this.ShowMessageAsync(TryFindResource("ERROR").ToString(), string.Format("{0}\n\n{1}\n{2}",
                    TryFindResource("LoadDBCFromBinding_Error_1").ToString(),
                    e, e.InnerException));
                return;
            }

            await controller.CloseAsync();
        }

        private List<Label> ConvertBoxListToLabels(List<DBCBoxContainer> boxes) => boxes.Select(entry => entry.ItemLabel()).ToList();

        private async Task GetConfig()
        {
            if (!Config.isInit)
            {
                var settings = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "SQLite",
                    NegativeButtonText = "MySQL",
                    AnimateHide = true,
                    AnimateShow = true,
                    ColorScheme = MetroDialogColorScheme.Accented,
                };
                MessageDialogResult exitCode = await this.ShowMessageAsync(TryFindResource("SpellEditor").ToString(),
                    TryFindResource("Welcome").ToString(),
                    MessageDialogStyle.AffirmativeAndNegative, settings);
                bool isSqlite = exitCode == MessageDialogResult.Affirmative;

                if (!isSqlite)
                {
                    if (Config.needInitMysql)
                    {
                        string host = await this.ShowInputAsync(TryFindResource("Input_MySQL_Details").ToString(), TryFindResource("Input_MySQL_Details_1").ToString());
                        string user = await this.ShowInputAsync(TryFindResource("Input_MySQL_Details").ToString(), TryFindResource("Input_MySQL_Details_2").ToString());
                        string pass = await this.ShowInputAsync(TryFindResource("Input_MySQL_Details").ToString(), TryFindResource("Input_MySQL_Details_3").ToString());
                        string port = await this.ShowInputAsync(TryFindResource("Input_MySQL_Details").ToString(), TryFindResource("Input_MySQL_Details_4").ToString());
                        string db = await this.ShowInputAsync(TryFindResource("Input_MySQL_Details").ToString(), TryFindResource("Input_MySQL_Details_5").ToString());
                        
                        if (host == null || user == null || pass == null || port == null || db == null ||
                            host.Length == 0 || user.Length == 0 || port.Length == 0 || db.Length == 0 ||
                            !uint.TryParse(port, out var result))
                        {
                            throw new Exception(TryFindResource("Input_MySQL_Error_2").ToString());
                        }

                        Config.Host = host;
                        Config.User = user;
                        Config.Pass = pass;
                        Config.Port = port;
                        Config.Database = db;
                    }
                }
                Config.connectionType = isSqlite ? Config.ConnectionType.SQLite : Config.ConnectionType.MySQL;
                Config.isInit = true;
            }
        }
        #endregion

        #region KeyHandlers
        private volatile Boolean imageLoadEventRunning = false;

        private void _KeyUp(object sender, KeyEventArgs e)
        {
            if (sender == FilterSpellNames && e.Key == Key.Back)
            {
                _KeyDown(sender, new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Space));
            }
            else if (sender == FilterIcons && e.Key == Key.Back)
            {
                _KeyDown(sender, new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Space));
            }
        }

        private async void _KeyDown(object sender, KeyEventArgs e)
        {
            if (sender == this)
            {
                if (e.Key == Key.Escape)
                {
                    MetroDialogSettings settings = new MetroDialogSettings();

                    settings.AffirmativeButtonText = TryFindResource("Yes").ToString();
                    settings.NegativeButtonText = TryFindResource("No").ToString();

                    MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
                    MessageDialogResult exitCode = await this.ShowMessageAsync(TryFindResource("SpellEditor").ToString(), TryFindResource("Exit").ToString(), style, settings);

                    if (exitCode == MessageDialogResult.Affirmative)
                    {
                        Environment.Exit(0x1);
                    }
                    else if (exitCode == MessageDialogResult.Negative)
                    {
                        return;
                    }
                }
                else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.S))
                {
                    Button_Click(SaveSpellChanges, e);
                }
            }
            else if (sender == NavigateToSpell)
            {
                if (e.Key != Key.Enter)
                {
                    return;
                }
                try
                {
                    TextBox box = (TextBox)sender;

                    int ID = int.Parse(box.Text);

                    int count = 0;
                    foreach (StackPanel obj in SelectSpell.Items)
                    {
                        foreach (var item in obj.Children)
                            if (item is TextBlock)
                            {
                                TextBlock tb = (TextBlock)item;

                                if (int.Parse(tb.Text.Split(' ')[1]) == ID)
                                {
                                    SelectSpell.SelectedIndex = count;
                                    SelectSpell.ScrollIntoView(obj);

                                    break;
                                }
                            }
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    HandleErrorMessage(ex.Message);
                }
            }
            else if (sender == FilterSpellNames)
            {
                if (imageLoadEventRunning)
                    return;
                imageLoadEventRunning = true;
                var locale = GetLocale();
                var input = FilterSpellNames.Text;
                bool badInput = string.IsNullOrEmpty(input);
                if (badInput && spellTable.Rows.Count == SelectSpell.Items.Count)
                {
                    imageLoadEventRunning = false;
                    return;
                }

                ICollectionView view = CollectionViewSource.GetDefaultView(SelectSpell.Items);
                view.Filter = (o) =>
                {
                    StackPanel panel = (StackPanel) o;
                    using (var enumerator = panel.GetChildObjects().GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current is TextBlock)
                            {
                                TextBlock block = (TextBlock)enumerator.Current;
                                string name = block.Text;
                                string spellName = name.Substring(name.IndexOf(' ', 4) + 1);
                                if (spellName.ToLower().Contains(input))
                                {
                                    enumerator.Dispose();
                                    return true;
                                }
                            }
                        }
                        enumerator.Dispose();
                    }
                    return false;
                };

                imageLoadEventRunning = false;
            }
            else if (sender == FilterIcons)
            {
                var input = FilterIcons.Text.ToLower();
                foreach (Image image in IconGrid.Children)
                {
                    var name = image.ToolTip.ToString().ToLower();
                    image.Visibility = name.Contains(input) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }
        #endregion

        #region ButtonClicks (and load spell god-function)
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (adapter == null)
            {
                loadAllData();
                return;
            }
            
            if (sender == TruncateTable)
            {
                MetroDialogSettings settings = new MetroDialogSettings();
                settings.AffirmativeButtonText = TryFindResource("Yes").ToString();
                settings.NegativeButtonText = TryFindResource("No").ToString();
                MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
                var res = await this.ShowMessageAsync(TryFindResource("TruncateTable1").ToString(), TryFindResource("TruncateTable2").ToString(), style, settings);
                if (res == MessageDialogResult.Affirmative)
                {
                    foreach (var binding in BindingManager.GetInstance().GetAllBindings())
                        adapter.Execute(string.Format("delete from `{0}`", binding.Name));
                    PopulateSelectSpell();
                }
                return;
            }

            if (sender == InsertANewRecord)
            {
                MetroDialogSettings settings = new MetroDialogSettings();

                settings.AffirmativeButtonText = TryFindResource("Yes").ToString();
                settings.NegativeButtonText = TryFindResource("No").ToString();

                MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
                MessageDialogResult copySpell = await this.ShowMessageAsync(TryFindResource("SpellEditor").ToString(), TryFindResource("CopySpellRecord1").ToString(), style, settings);

                uint oldIDIndex = uint.MaxValue;

                if (copySpell == MessageDialogResult.Affirmative)
                {
                    uint oldID = 0;

                    string inputCopySpell = await this.ShowInputAsync(TryFindResource("SpellEditor").ToString(), TryFindResource("CopySpellRecord2").ToString());
                    if (inputCopySpell == null) { return; }

                    if (!uint.TryParse(inputCopySpell, out oldID))
                    {
                        HandleErrorMessage(TryFindResource("CopySpellRecord3").ToString());
                        return;
                    }
                    oldIDIndex = oldID;
                }

                string inputNewRecord = await this.ShowInputAsync(TryFindResource("SpellEditor").ToString(), TryFindResource("CopySpellRecord4").ToString());
                if (inputNewRecord == null) { return; }

                uint newID = 0;
                if (!uint.TryParse(inputNewRecord, out newID))
                {
                    HandleErrorMessage(TryFindResource("CopySpellRecord5").ToString());
                    return;
                }

                if (uint.Parse(adapter.Query(string.Format("SELECT COUNT(*) FROM `spell` WHERE `ID` = '{0}'", newID)).Rows[0][0].ToString()) > 0)
                {
                    HandleErrorMessage(TryFindResource("CopySpellRecord6").ToString());
                    return;
                }

                if (oldIDIndex != uint.MaxValue)
                {
                    // Copy old spell to new spell
                    var row = adapter.Query(string.Format("SELECT * FROM `spell` WHERE `ID` = '{0}' LIMIT 1", oldIDIndex)).Rows[0];
                    StringBuilder str = new StringBuilder();
                    str.Append(string.Format("INSERT INTO `spell` VALUES ('{0}'", newID));
                    for (int i = 1; i < row.Table.Columns.Count; ++i)
                        str.Append(string.Format(", \"{0}\"", MySqlHelper.EscapeString(row[i].ToString())));
                    str.Append(")");
                    adapter.Execute(str.ToString());
                }
                else
                {
                    // Create new spell
                    HandleErrorMessage(TryFindResource("CopySpellRecord7").ToString());
                    return;
                }

                PopulateSelectSpell();

                ShowFlyoutMessage(string.Format(TryFindResource("CopySpellRecord8").ToString(), inputNewRecord));
                return;
            }

            if (sender == DeleteARecord)
            {
                string input = await this.ShowInputAsync(TryFindResource("SpellEditor").ToString(), TryFindResource("DeleteSpellRecord1").ToString());

                if (input == null) { return; }

                uint spellID = 0;
                if (!uint.TryParse(input, out spellID))
                {
                    HandleErrorMessage(TryFindResource("DeleteSpellRecord2").ToString());
                    return;
                }

                adapter.Execute(string.Format("DELETE FROM `spell` WHERE `ID` = '{0}'", spellID));
                
                selectedID = 0;

                PopulateSelectSpell();

                ShowFlyoutMessage(TryFindResource("DeleteSpellRecord3").ToString());
                return;
            }

            if (sender == SaveSpellChanges)
            {
                string query = string.Format("SELECT * FROM `spell` WHERE `ID` = '{0}' LIMIT 1", selectedID);
                var q = adapter.Query(query);
                if (q.Rows.Count == 0)
                    return;
                var row = q.Rows[0];
                var isWotlkOrGreater = WoWVersionManager.IsWotlkOrGreaterSelected;
                var isTbcOrGreater = WoWVersionManager.IsTbcOrGreaterSelected;
                row.BeginEdit();
                try
                {

                    uint maskk = 0;
                    uint flagg = 1;

                    for (int f = 0; f < attributes0.Count; ++f)
                    {
                        if (attributes0[f].IsChecked.Value == true) { maskk = maskk + flagg; }
                        flagg = flagg + flagg;
                    }

                    row["Attributes"] = maskk;

                    maskk = 0;
                    flagg = 1;

                    for (int f = 0; f < attributes1.Count; ++f)
                    {
                        if (attributes1[f].IsChecked.Value == true) { maskk = maskk + flagg; }
                        flagg = flagg + flagg;
                    }

                   row["AttributesEx"] = maskk;

                    maskk = 0;
                    flagg = 1;

                    for (int f = 0; f < attributes2.Count; ++f)
                    {
                        if (attributes2[f].IsChecked.Value == true) { maskk = maskk + flagg; }
                        flagg = flagg + flagg;
                    }

                    row["AttributesEx2"] = maskk;


                    maskk = 0;
                    flagg = 1;

                    for (int f = 0; f < attributes3.Count; ++f)
                    {
                        if (attributes3[f].IsChecked.Value == true) { maskk = maskk + flagg; }
                        flagg = flagg + flagg;
                    }

                    row["AttributesEx3"] = maskk;

                    maskk = 0;
                    flagg = 1;

                    for (int f = 0; f < attributes4.Count; ++f)
                    {
                        if (attributes4[f].IsChecked.Value == true) { maskk = maskk + flagg; }

                        flagg = flagg + flagg;
                    }

                    row["AttributesEx4"] = maskk;

                    maskk = 0;
                    flagg = 1;

                    if (isTbcOrGreater)
                    {
                        for (int f = 0; f < attributes5.Count; ++f)
                        {
                            if (attributes5[f].IsChecked.Value == true) { maskk = maskk + flagg; }

                            flagg = flagg + flagg;
                        }

                        row["AttributesEx5"] = maskk;

                        maskk = 0;
                        flagg = 1;

                        for (int f = 0; f < attributes6.Count; ++f)
                        {
                            if (attributes6[f].IsChecked.Value == true) { maskk = maskk + flagg; }

                            flagg = flagg + flagg;
                        }

                        row["AttributesEx6"] = maskk;

                        if (stancesBoxes[0].IsChecked.Value == true) { row["Stances"] = 0; }
                        else
                        {
                            uint mask = 0;
                            uint flag = 1;

                            for (int f = 1; f < stancesBoxes.Count; ++f)
                            {
                                if (stancesBoxes[f].IsChecked.Value == true) { mask = mask + flag; }

                                flag = flag + flag;
                            }

                            row["Stances"] = mask;
                        }
                    }

                    if (isWotlkOrGreater)
                    {
                        maskk = 0;
                        flagg = 1;

                        for (int f = 0; f < attributes7.Count; ++f)
                        {
                            if (attributes7[f].IsChecked.Value == true) { maskk = maskk + flagg; }

                            flagg = flagg + flagg;
                        }

                        row["AttributesEx7"] = maskk;
                    }

                    if (targetBoxes[0].IsChecked.Value == true) { row["Targets"] = 0; }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 1; f < targetBoxes.Count; ++f)
                        {
                            if (targetBoxes[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        row["Targets"] = mask;
                    }

                    if (targetCreatureTypeBoxes[0].IsChecked.Value == true) { row["TargetCreatureType"] = 0; }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 1; f < targetCreatureTypeBoxes.Count; ++f)
                        {
                            if (targetCreatureTypeBoxes[f].IsChecked.Value == true) { mask = mask + flag; }
                            flag = flag + flag;
                        }

                        row["TargetCreatureType"] = mask;
                    }

                    if (isTbcOrGreater)
                    {
                        row["FacingCasterFlags"] = FacingFrontFlag.IsChecked.Value ? (uint)0x1 : (uint)0x0;

                        switch (CasterAuraState.SelectedIndex)
                        {
                            case 0: // None
                                {
                                    row["CasterAuraState"] = 0;

                                    break;
                                }

                            case 1: // Defense
                                {
                                    row["CasterAuraState"] = 1;

                                    break;
                                }

                            case 2: // Healthless 20%
                                {
                                    row["CasterAuraState"] = 2;

                                    break;
                                }

                            case 3: // Berserking
                                {
                                    row["CasterAuraState"] = 3;

                                    break;
                                }

                            case 4: // Judgement
                                {
                                    row["CasterAuraState"] = 5;

                                    break;
                                }

                            case 5: // Hunter Parry
                                {
                                    row["CasterAuraState"] = 7;

                                    break;
                                }

                            case 6: // Victory Rush
                                {
                                    row["CasterAuraState"] = 10;

                                    break;
                                }

                            case 7: // Unknown 1
                                {
                                    row["CasterAuraState"] = 11;

                                    break;
                                }

                            case 8: // Healthless 35%
                                {
                                    row["CasterAuraState"] = 13;

                                    break;
                                }

                            case 9: // Enrage
                                {
                                    row["CasterAuraState"] = 17;

                                    break;
                                }

                            case 10: // Unknown 2
                                {
                                    row["CasterAuraState"] = 22;

                                    break;
                                }

                            case 11: // Health Above 75%
                                {
                                    row["CasterAuraState"] = 23;

                                    break;
                                }

                            default:
                                {
                                    break;
                                }
                        }
                    }

                    switch (TargetAuraState.SelectedIndex)
                    {
                        case 0: // None
                        {
                            row["TargetAuraState"] = 0;

                            break;
                        }

                        case 1: // Healthless 20%
                        {
                            row["TargetAuraState"] = 2;

                            break;
                        }

                        case 2: // Berserking
                        {
                            row["TargetAuraState"] = 3;

                            break;
                        }

                        case 3: // Healthless 35%
                        {
                            row["TargetAuraState"] = 13;

                            break;
                        }

                        case 4: // Conflagrate
                        {
                            row["TargetAuraState"] = 14;

                            break;
                        }

                        case 5: // Swiftmend
                        {
                            row["TargetAuraState"] = 15;

                            break;
                        }

                        case 6: // Deadly Poison
                        {
                            row["TargetAuraState"] = 16;

                            break;
                        }

                        case 7: // Bleeding
                        {
                            row["TargetAuraState"] = 18;

                            break;
                        }

                        default:
                        {
                            break;
                        }
                    }

                    row["RecoveryTime"] = uint.Parse(RecoveryTime.Text);
                    row["CategoryRecoveryTime"] = uint.Parse(CategoryRecoveryTime.Text);

                    if (interrupts1[0].IsChecked.Value == true) { row["InterruptFlags"] = 0; }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 1; f < interrupts1.Count; ++f)
                        {
                            if (interrupts1[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        row["InterruptFlags"] = mask;
                    }

                    if (interrupts2[0].IsChecked.Value == true) { row["AuraInterruptFlags"] = 0; }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 1; f < interrupts2.Count; ++f)
                        {
                            if (interrupts2[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        row["AuraInterruptFlags"] = mask;
                    }

                    if (interrupts3[0].IsChecked.Value == true) { row["ChannelInterruptFlags"] = 0; }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 1; f < interrupts3.Count; ++f)
                        {
                            if (interrupts3[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        row["ChannelInterruptFlags"] = mask;
                    }

                    if (procBoxes[0].IsChecked.Value == true) { row["ProcFlags"] = 0; }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 1; f < procBoxes.Count; ++f)
                        {
                            if (procBoxes[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        row["ProcFlags"] = mask;
                    }

                    row["ProcChance"] = uint.Parse(ProcChance.Text);
                    row["ProcCharges"] = uint.Parse(ProcCharges.Text);
                    row["MaximumLevel"] = uint.Parse(MaximumLevel.Text);
                    row["BaseLevel"] = uint.Parse(BaseLevel.Text);
                    row["SpellLevel"] = uint.Parse(SpellLevel.Text);
                    // Handle 'Health' power type manually
                    row["PowerType"] = PowerType.SelectedIndex == 13 ? (uint.MaxValue - 1) : (uint)PowerType.SelectedIndex;
                    row["ManaCost"] = uint.Parse(PowerCost.Text);
                    row["ManaCostPerLevel"] = uint.Parse(ManaCostPerLevel.Text);
                    row["ManaPerSecond"] = uint.Parse(ManaCostPerSecond.Text);
                    row["ManaPerSecondPerLevel"] = uint.Parse(PerSecondPerLevel.Text);
                    row["Speed"] = float.Parse(Speed.Text);
                    row["StackAmount"] = uint.Parse(Stacks.Text);
                    row["Totem1"] = uint.Parse(Totem1.Text);
                    row["Totem2"] = uint.Parse(Totem2.Text);
                    row["Reagent1"] = int.Parse(Reagent1.Text);
                    row["Reagent2"] = int.Parse(Reagent2.Text);
                    row["Reagent3"] = int.Parse(Reagent3.Text);
                    row["Reagent4"] = int.Parse(Reagent4.Text);
                    row["Reagent5"] = int.Parse(Reagent5.Text);
                    row["Reagent6"] = int.Parse(Reagent6.Text);
                    row["Reagent7"] = int.Parse(Reagent7.Text);
                    row["Reagent8"] = int.Parse(Reagent8.Text);
                    row["ReagentCount1"] = uint.Parse(ReagentCount1.Text);
                    row["ReagentCount2"] = uint.Parse(ReagentCount2.Text);
                    row["ReagentCount3"] = uint.Parse(ReagentCount3.Text);
                    row["ReagentCount4"] = uint.Parse(ReagentCount4.Text);
                    row["ReagentCount5"] = uint.Parse(ReagentCount5.Text);
                    row["ReagentCount6"] = uint.Parse(ReagentCount6.Text);
                    row["ReagentCount7"] = uint.Parse(ReagentCount7.Text);
                    row["ReagentCount8"] = uint.Parse(ReagentCount8.Text);

                    if (equippedItemInventoryTypeMaskBoxes[0].IsChecked.Value == true) { row["EquippedItemInventoryTypeMask"] = 0; }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 0; f < equippedItemInventoryTypeMaskBoxes.Count; ++f)
                        {
                            if (equippedItemInventoryTypeMaskBoxes[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        row["EquippedItemInventoryTypeMask"] = (int)mask;
                    }

                    if (EquippedItemClass.Text == TryFindResource("None").ToString())
                    {
                        row["EquippedItemClass"] = -1;
                        row["EquippedItemSubClassMask"] = 0;
                    }
                    else
                    {
                        uint Mask = 0;
                        for (int i = 0; i < equippedItemSubClassMaskBoxes.Count; i++)
                            Mask += equippedItemSubClassMaskBoxes[i].IsChecked.Value ? (uint)Math.Pow(2, i) : 0;
                        
                        row["EquippedItemSubClassMask"] = Mask;
                    }

                    row["Effect1"] = (uint)SpellEffect1.SelectedIndex;
                    row["Effect2"] = (uint)SpellEffect2.SelectedIndex;
                    row["Effect3"] = (uint)SpellEffect3.SelectedIndex;
                    row["EffectDieSides1"] = int.Parse(DieSides1.Text);
                    row["EffectDieSides2"] = int.Parse(DieSides2.Text);
                    row["EffectDieSides3"] = int.Parse(DieSides3.Text);
                    row["EffectRealPointsPerLevel1"] = float.Parse(BasePointsPerLevel1.Text);
                    row["EffectRealPointsPerLevel2"] = float.Parse(BasePointsPerLevel2.Text);
                    row["EffectRealPointsPerLevel3"] = float.Parse(BasePointsPerLevel3.Text);
                    row["EffectBasePoints1"] = int.Parse(BasePoints1.Text);
                    row["EffectBasePoints2"] = int.Parse(BasePoints2.Text);
                    row["EffectBasePoints3"] = int.Parse(BasePoints3.Text);
                    row["EffectMechanic1"] = (uint)Mechanic1.SelectedIndex;
                    row["EffectMechanic2"] = (uint)Mechanic2.SelectedIndex;
                    row["EffectMechanic3"] = (uint)Mechanic3.SelectedIndex;
                    row["EffectImplicitTargetA1"] = (uint)TargetA1.SelectedIndex;
                    row["EffectImplicitTargetA2"] = (uint)TargetA2.SelectedIndex;
                    row["EffectImplicitTargetA3"] = (uint)TargetA3.SelectedIndex;
                    row["EffectImplicitTargetB1"] = (uint)TargetB1.SelectedIndex;
                    row["EffectImplicitTargetB2"] = (uint)TargetB2.SelectedIndex;
                    row["EffectImplicitTargetB3"] = (uint)TargetB3.SelectedIndex;
                    row["EffectApplyAuraName1"] = (uint)ApplyAuraName1.SelectedIndex;
                    row["EffectApplyAuraName2"] = (uint)ApplyAuraName2.SelectedIndex;
                    row["EffectApplyAuraName3"] = (uint)ApplyAuraName3.SelectedIndex;
                    row["EffectAmplitude1"] = uint.Parse(Amplitude1.Text);
                    row["EffectAmplitude2"] = uint.Parse(Amplitude2.Text);
                    row["EffectAmplitude3"] = uint.Parse(Amplitude3.Text);
                    row["EffectMultipleValue1"] = float.Parse(MultipleValue1.Text);
                    row["EffectMultipleValue2"] = float.Parse(MultipleValue1.Text);
                    row["EffectMultipleValue3"] = float.Parse(MultipleValue1.Text);
                    row["EffectChainTarget1"] = uint.Parse(ChainTarget1.Text);
                    row["EffectChainTarget2"] = uint.Parse(ChainTarget2.Text);
                    row["EffectChainTarget3"] = uint.Parse(ChainTarget3.Text);
                    row["EffectItemType1"] = uint.Parse(ItemType1.Text);
                    row["EffectItemType2"] = uint.Parse(ItemType2.Text);
                    row["EffectItemType3"] = uint.Parse(ItemType3.Text);
                    row["EffectMiscValue1"] = int.Parse(MiscValueA1.Text);
                    row["EffectMiscValue2"] = int.Parse(MiscValueA2.Text);
                    row["EffectMiscValue3"] = int.Parse(MiscValueA3.Text);
                    if (isTbcOrGreater)
                    {
                        row["EffectMiscValueB1"] = int.Parse(MiscValueB1.Text);
                        row["EffectMiscValueB2"] = int.Parse(MiscValueB2.Text);
                        row["EffectMiscValueB3"] = int.Parse(MiscValueB3.Text);
                    }
                    row["EffectTriggerSpell1"] = uint.Parse(TriggerSpell1.Text);
                    row["EffectTriggerSpell2"] = uint.Parse(TriggerSpell2.Text);
                    row["EffectTriggerSpell3"] = uint.Parse(TriggerSpell3.Text);
                    row["EffectPointsPerComboPoint1"] = float.Parse(PointsPerComboPoint1.Text);
                    row["EffectPointsPerComboPoint2"] = float.Parse(PointsPerComboPoint2.Text);
                    row["EffectPointsPerComboPoint3"] = float.Parse(PointsPerComboPoint3.Text);
                    if (isWotlkOrGreater)
                    {
                        row["EffectSpellClassMaskA1"] = uint.Parse(SpellMask11.Text);
                        row["EffectSpellClassMaskA2"] = uint.Parse(SpellMask21.Text);
                        row["EffectSpellClassMaskA3"] = uint.Parse(SpellMask31.Text);
                        row["EffectSpellClassMaskB1"] = uint.Parse(SpellMask12.Text);
                        row["EffectSpellClassMaskB2"] = uint.Parse(SpellMask22.Text);
                        row["EffectSpellClassMaskB3"] = uint.Parse(SpellMask32.Text);
                        row["EffectSpellClassMaskC1"] = uint.Parse(SpellMask13.Text);
                        row["EffectSpellClassMaskC2"] = uint.Parse(SpellMask23.Text);
                        row["EffectSpellClassMaskC3"] = uint.Parse(SpellMask33.Text);
                    }
                    else
                    {
                        row["EffectImplicitTargetA1"] = uint.Parse(SpellMask11.Text);
                        row["EffectImplicitTargetA2"] = uint.Parse(SpellMask21.Text);
                        row["EffectImplicitTargetA3"] = uint.Parse(SpellMask31.Text);
                        row["EffectImplicitTargetB1"] = uint.Parse(SpellMask12.Text);
                        row["EffectImplicitTargetB2"] = uint.Parse(SpellMask22.Text);
                        row["EffectImplicitTargetB3"] = uint.Parse(SpellMask32.Text);
                    }
                    row["SpellVisual1"] = uint.Parse(SpellVisual1.Text);
                    row["SpellVisual2"] = uint.Parse(SpellVisual2.Text);
                    row["ManaCostPercentage"] = uint.Parse(ManaCostPercent.Text);
                    row["StartRecoveryCategory"] = uint.Parse(StartRecoveryCategory.Text);
                    row["StartRecoveryTime"] = uint.Parse(StartRecoveryTime.Text);
                    row["MaximumTargetLevel"] = uint.Parse(MaxTargetsLevel.Text);
                    // Before WOTLK there are only two flags, we misnamed them in WOTLK as the last flag handles A3/B3/C3 of the affecting spells
                    if (!isWotlkOrGreater)
                    {
                        row["SpellFamilyName"] = uint.Parse(SpellFamilyName.Text);
                        row["SpellFamilyFlags1"] = uint.Parse(SpellFamilyFlags.Text);
                        row["SpellFamilyFlags2"] = uint.Parse(SpellFamilyFlags1.Text);
                    }
                    else
                    {
                        row["SpellFamilyName"] = uint.Parse(SpellFamilyName.Text);
                        row["SpellFamilyFlags"] = uint.Parse(SpellFamilyFlags.Text);
                        row["SpellFamilyFlags1"] = uint.Parse(SpellFamilyFlags1.Text);
                        row["SpellFamilyFlags2"] = uint.Parse(SpellFamilyFlags2.Text);
                    }
                    row["MaximumAffectedTargets"] = uint.Parse(MaxTargets.Text);
                    row["DamageClass"] = (uint)SpellDamageType.SelectedIndex;
                    row["PreventionType"] = (uint)PreventionType.SelectedIndex;
                    row["EffectDamageMultiplier1"] = float.Parse(EffectDamageMultiplier1.Text);
                    row["EffectDamageMultiplier2"] = float.Parse(EffectDamageMultiplier2.Text);
                    row["EffectDamageMultiplier3"] = float.Parse(EffectDamageMultiplier3.Text);
                    row["SchoolMask"] = (S1.IsChecked.Value ? (uint)0x01 : (uint)0x00) + (S2.IsChecked.Value ? (uint)0x02 : (uint)0x00) + (S3.IsChecked.Value ? (uint)0x04 : (uint)0x00) + (S4.IsChecked.Value ? (uint)0x08 : (uint)0x00) + (S5.IsChecked.Value ? (uint)0x10 : (uint)0x00) + (S6.IsChecked.Value ? (uint)0x20 : (uint)0x00) + (S7.IsChecked.Value ? (uint)0x40 : (uint)0x00);
                    if (isWotlkOrGreater)
                    {
                        row["SpellMissileID"] = uint.Parse(SpellMissileID.Text);
                        row["EffectBonusMultiplier1"] = float.Parse(EffectBonusMultiplier1.Text);
                        row["EffectBonusMultiplier2"] = float.Parse(EffectBonusMultiplier2.Text);
                        row["EffectBonusMultiplier3"] = float.Parse(EffectBonusMultiplier3.Text);
                    }

                    var numColumns = WoWVersionManager.GetInstance().SelectedVersion().NumLocales;
                    TextBox[] boxes = stringObjectMap.Values.ToArray();
                    for (int i = 0; i < (numColumns > 9 ? 9 : numColumns); ++i)
                        row["SpellName" + i] = boxes[i].Text;
                    for (int i = 0; i < (numColumns > 9 ? 9 : numColumns); ++i)
                        row["SpellRank" + i] = boxes[i + 9].Text;
                    for (int i = 0; i < (numColumns > 9 ? 9 : numColumns); ++i)
                        row["SpellTooltip" + i] = boxes[i + 18].Text;
                    for (int i = 0; i < (numColumns > 9 ? 9 : numColumns); ++i)
                        row["SpellDescription" + i] = boxes[i + 27].Text;
                    // 3.3.5a: This seems to mimic Blizzlike values correctly, though I don't understand it at all.
                    // Discussed on modcraft IRC - these fields are not even read by the client.
                    // The structure used in this program is actually incorrect. All the string columns are
                    //   for different locales apart from the last one which is the flag column. So there are
                    //   not multiple flag columns, hence why we only write to the last one here. The current
                    //   released clients only use 9 locales hence the confusion with the other columns.
                    // Not sure on the correct behaviour in 1.12.1
                    var suffix = isWotlkOrGreater ? "7" : isTbcOrGreater ? "" : "0";
                    row["SpellNameFlag" + suffix] = (uint)(TextFlags.NOT_EMPTY);
                    row["SpellRankFlags" + suffix] = (uint)(TextFlags.NOT_EMPTY);
                    row["SpellToolTipFlags" + suffix] = (uint)(TextFlags.NOT_EMPTY);
                    row["SpellDescriptionFlags" + suffix] = (uint)(TextFlags.NOT_EMPTY);

                    row.EndEdit();
                    adapter.CommitChanges(query, q.GetChanges());

                    ShowFlyoutMessage($"Saved spell {selectedID}.");

                    PopulateSelectSpell();
                }
                catch (Exception ex)
                {
                    row.CancelEdit();
                    HandleErrorMessage(ex + "\n\n" + ex.InnerException);
                }
                return;
            }

            if (sender == SaveIcon)
            {
                MetroDialogSettings settings = new MetroDialogSettings();

                settings.AffirmativeButtonText = TryFindResource("Yes").ToString();
                settings.NegativeButtonText = TryFindResource("No").ToString();

                MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
                MessageDialogResult spellOrActive = await this.ShowMessageAsync(TryFindResource("SpellEditor").ToString(), TryFindResource("SaveIcon").ToString(), style, settings);

                string column = null;
                if (spellOrActive == MessageDialogResult.Affirmative)
                    column = "SpellIconID";
                else if (spellOrActive == MessageDialogResult.Negative)
                    column = "ActiveIconID";
                adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'", "spell", column, newIconID, selectedID));
                return;
            }

            if (sender == ResetSpellIconID)
            {
                adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'", "spell", "SpellIconID", 1, selectedID));
                return;
            }
            if (sender == ResetActiveIconID)
            {
                adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'", "spell", "ActiveIconID", 0, selectedID));
                return;
            }
        }
        #endregion

        #region Utilities
        public void ShowFlyoutMessage(string message)
        {
            Flyout.IsOpen = true;
            FlyoutText.Text = message;
        }

        static public T DeepCopy<T>(T obj)
        {
            BinaryFormatter s = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                s.Serialize(ms, obj);
                ms.Position = 0;
                T t = (T)s.Deserialize(ms);

                return t;
            }
        }

        private void PrepareIconEditor()
        {
            var loadIcons = (SpellIconDBC)DBCManager.GetInstance().FindDbcForBinding("SpellIcon");
            loadIcons.LoadImages(64);
            loadIcons.updateIconSize(64, new Thickness(16, 0, 0, 0));
        }

        private class SpellListQueryWorker : BackgroundWorker
        {
            public IDatabaseAdapter __adapter;
            public Stopwatch __watch;

            public SpellListQueryWorker(IDatabaseAdapter _adapter, Stopwatch watch)
            {
                __adapter = _adapter;
                __watch = watch;
            }
        }

        public int GetLocale()
        {
            if (storedLocale != -1)
                return storedLocale;

            // Attempt localisation on Death Touch, HACKY
            var aboveClassic = WoWVersionManager.GetInstance().SelectedVersion().Identity > 112;
            var name8 = aboveClassic ? ",`SpellName8` " : "";
            DataRowCollection res = adapter.Query("SELECT `id`,`SpellName0`,`SpellName1`,`SpellName2`,`SpellName3`,`SpellName4`," +
                "`SpellName5`,`SpellName6`,`SpellName7`" + name8 + " FROM `spell` WHERE `ID` = '5'").Rows;
            if (res == null || res.Count == 0)
                return -1;
            int locale = 0;
            if (res[0] != null)
            {
                for (int i = 1; i < res[0].Table.Columns.Count; ++i)
                {
                    if (res[0][i].ToString().Length > 3)
                    {
                        locale = i;
                        break;
                    }
                }
            }
            storedLocale = locale;
            return locale;
        }
        #endregion

        private int SelectSpellContentsCount;
        private int SelectSpellContentsIndex;

        #region PopulateSelectSpell
        private void PopulateSelectSpell()
        {
            var selectSpellWatch = new Stopwatch();
            selectSpellWatch.Start();
            SelectSpellContentsIndex = 0;
            SelectSpellContentsCount = SelectSpell.Items.Count;
            SpellsLoadedLabel.Content = TryFindResource("no_spells_loaded").ToString();
            var _worker = new SpellListQueryWorker(adapter, selectSpellWatch);
            _worker.WorkerReportsProgress = true;
            _worker.ProgressChanged += new ProgressChangedEventHandler(_worker_ProgressChanged);

            FilterSpellNames.IsEnabled = false;

            _worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                if (_worker.__adapter == null || !Config.isInit)
                    return;
                int locale = GetLocale();
                if (locale > 0)
                    locale -= 1;

                spellTable.Rows.Clear();

                uint lowerBounds = 0;
                uint pageSize = 5000;
                uint targetSize = pageSize;
                DataRowCollection results = GetSpellNames(lowerBounds, 100, locale);
                lowerBounds += 100;
                // Edge case of empty table after truncating, need to send a event to the handler
                if (results != null && results.Count == 0)
                {
                    _worker.ReportProgress(0, results);
                }
                while (results != null && results.Count != 0)
                {
                    _worker.ReportProgress(0, results);
                    results = GetSpellNames(lowerBounds, pageSize, locale);
                    lowerBounds += pageSize;
                }

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => FilterSpellNames.IsEnabled = true));
            };
            _worker.RunWorkerAsync();
            _worker.RunWorkerCompleted += (sender, args) =>
            {
                var worker = sender as SpellListQueryWorker;
                worker.__watch.Stop();
                Console.WriteLine($"Loaded spell selection list contents in {worker.__watch.ElapsedMilliseconds}ms");
            };
        }

        private void _worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Ignores spells with a iconId <= 0
            var watch = new Stopwatch();
            watch.Start();
            DataRowCollection collection = (DataRowCollection)e.UserState;
            int rowIndex = 0;
            // Reuse existing UI elements if they exist
            if (SelectSpellContentsIndex < SelectSpellContentsCount)
            {
                foreach (DataRow row in collection)
                {
                    ++rowIndex;
                    if (SelectSpellContentsIndex == SelectSpellContentsCount ||
                        SelectSpellContentsIndex >= SelectSpell.Items.Count)
                    {
                        break;
                    }
                    var stackPanel = SelectSpell.Items[SelectSpellContentsIndex] as StackPanel;
                    var image = stackPanel.Children[0] as Image;
                    var textBlock = stackPanel.Children[1] as TextBlock;
                    var spellName = row[1].ToString();
                    textBlock.Text = string.Format(" {0} - {1}", row[0], spellName);
                    var iconId = uint.Parse(row[2].ToString());
                    if (iconId > 0)
                    {
                        image.ToolTip = iconId.ToString();
                        ++SelectSpellContentsIndex;
                    }
                }
            }
            // Spawn any new UI elements required
            var newElements = new List<UIElement>();
            for (; rowIndex < collection.Count; ++rowIndex)
            {
                var row = collection[rowIndex];
                var spellName = row[1].ToString();
                var textBlock = new TextBlock();
                textBlock.Text = string.Format(" {0} - {1}", row[0], spellName);
                var image = new Image();
                var iconId = uint.Parse(row[2].ToString());
                //if (iconId > 0)
                //{
                    image.ToolTip = iconId.ToString();
                    image.Width = 32;
                    image.Height = 32;
                    image.Margin = new Thickness(1, 1, 1, 1);
                    image.IsVisibleChanged += IsSpellListEntryVisibileChanged;
                    var stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };
                    stackPanel.Children.Add(image);
                    stackPanel.Children.Add(textBlock);
                    ++SelectSpellContentsIndex;
                //}
                newElements.Add(stackPanel);
            }
            SpellsLoadedLabel.Content = string.Format(TryFindResource("Highest_Spell_ID").ToString(), 
                collection.Count > 0 ? collection[collection.Count - 1][0] : "n/a");
            // Replace the item source directly, adding each item will raise a high amount of events
            var src = SelectSpell.ItemsSource;
            var newSrc = new List<object>();
            if (src != null)
            {
                // Don't keep more UI elements than we need
                var enumerator = src.GetEnumerator();
                for (int i = 0; i < SelectSpellContentsIndex; ++i)
                {
                    if (!enumerator.MoveNext())
                        break;
                    newSrc.Add(enumerator.Current);
                }
            }
            foreach (var element in newElements)
                newSrc.Add(element);
            SelectSpell.ItemsSource = newSrc;
            watch.Stop();
            Console.WriteLine($"Worker progress change event took {watch.ElapsedMilliseconds}ms to handle");
        }

        private void IsSpellListEntryVisibileChanged(object o, DependencyPropertyChangedEventArgs args)
        {
            var image = o as Image;
            if (!(bool)args.NewValue)
            {
                image.Source = null;
                return;
            }
            if (image.Source != null)
            {
                return;
            }
            var loadIcons = (SpellIconDBC)DBCManager.GetInstance().FindDbcForBinding("SpellIcon");
            var iconId = uint.Parse(image.ToolTip.ToString());
            var filePath = loadIcons.GetIconPath(iconId) + ".blp";
            image.Source = BlpManager.GetInstance().GetImageSourceFromBlpPath(filePath);
        }

        private DataRowCollection GetSpellNames(uint lowerBound, uint pageSize, int locale)
        {
            DataTable newSpellNames = adapter.Query(string.Format(@"SELECT `id`,`SpellName{1}`,`SpellIconID` FROM `{0}` ORDER BY `id` LIMIT {2}, {3}",
                 "spell", locale, lowerBound, pageSize));

            spellTable.Merge(newSpellNames, false, MissingSchemaAction.Add);

            return newSpellNames.Rows;
        }
        #endregion

        #region NewIconClick & UpdateMainWindow
        private async void NewIconClick(object sender, RoutedEventArgs e)
        {
            if (adapter == null) { return; }

            MetroDialogSettings settings = new MetroDialogSettings();
            settings.AffirmativeButtonText = TryFindResource("SpellIconID").ToString();
            settings.NegativeButtonText = TryFindResource("ActiveIconID").ToString();

            MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
            MessageDialogResult spellOrActive = await this.ShowMessageAsync(TryFindResource("SpellEditor").ToString(), TryFindResource("String4").ToString(), style, settings);

            string column = null;
            if (spellOrActive == MessageDialogResult.Affirmative)
                column = "SpellIconID";
            else if (spellOrActive == MessageDialogResult.Negative)
                column = "ActiveIconID";
            adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'", "spell", column, newIconID, selectedID));
        }

        private async void UpdateMainWindow()
        {
            ProgressDialogController controller = null;
            try
            {
                updating = true;

                controller = await this.ShowProgressAsync(TryFindResource("UpdateMainWindow1").ToString(), string.Format(TryFindResource("UpdateMainWindow2").ToString(), selectedID));
                controller.SetCancelable(false);

               /* Timeline.DesiredFrameRateProperty.OverrideMetadata(
                    typeof(Timeline),
                    new FrameworkPropertyMetadata { DefaultValue = 30 }
                );*/

                loadSpell(new UpdateTextFunc(controller.SetMessage));

                await controller.CloseAsync();

                updating = false;
            }

            catch (Exception ex)
            {
                updating = false;
                if (controller != null)
                    await controller.CloseAsync();
                HandleErrorMessage(ex.Message);
            }
        }
        #endregion

        #region SpellStringParsing
        /**
         * Very slow debug method for parsing all spell descriptions and tooltips in enUS/enGB locale and
         * writing the ones that failed to parse.
         */
        private void DebugFuncWriteAllUnparsedStrings()
        {
            DataRowCollection rowResult = adapter.Query(string.Format("SELECT SpellDescription0 || SpellTooltip0 FROM `spell`", selectedID)).Rows;
            if (rowResult == null || rowResult.Count == 0)
                throw new Exception("An error occurred trying to select spell ID: " + selectedID.ToString());
            var unparsedStrings = new List<string>();
            foreach (DataRow row in rowResult)
            {
                var str = row[0].ToString();
                var parsedStr = SpellStringParser.ParseString(str, row, this);
                if (parsedStr.Contains("$"))
                {
                    unparsedStrings.Add(str + " | " + parsedStr);
                }
            }
            File.WriteAllLines("debug_unparsed_strings.txt", unparsedStrings);
        }

        private void SpellDescriptionGen_TextChanged(object sender, TextChangedEventArgs e) => SpellGenRefresh(sender as ThreadSafeTextBox, 0);
        private void SpellTooltipGen_TextChanged(object sender, TextChangedEventArgs e) => SpellGenRefresh(sender as ThreadSafeTextBox, 1);
        private void SpellGenRefresh(ThreadSafeTextBox sender, int type)
        {
            if (!int.TryParse(sender.Name[sender.Name.Length - 1].ToString(), out int locale))
                return;
            var spell = GetSpellRowById(selectedID);
            var text = SpellStringParser.ParseString(sender.Text, spell, this);
            if (type == 0)
                spellDescGenFields[locale].threadSafeText = text;
            else if (type == 1)
                spellTooltipGenFields[locale].threadSafeText = text;
        }
        #endregion

        #region LoadSpell (load spell god-function)
        private void loadSpell(UpdateTextFunc updateProgress)
        {
            adapter.Updating = true;
            updateProgress("Querying MySQL data...");
            DataRowCollection rowResult = adapter.Query(string.Format("SELECT * FROM `spell` WHERE `ID` = '{0}'", selectedID)).Rows;
            if (rowResult == null || rowResult.Count != 1)
                throw new Exception("An error occurred trying to select spell ID: " + selectedID.ToString());
            var row = rowResult[0];
            var numColumns = (int)WoWVersionManager.GetInstance().SelectedVersion().NumLocales;
            var isWotlkOrGreater = WoWVersionManager.IsWotlkOrGreaterSelected;
            var isTbcOrGreater = WoWVersionManager.IsTbcOrGreaterSelected;
            try
            {
                updateProgress("Updating text control's...");
                int i;
                var maxColumns = numColumns >= spellDescGenFields.Count ? spellDescGenFields.Count - 1 : numColumns;
                for (i = 0; i < maxColumns; ++i)
                {
                    spellDescGenFields[i].threadSafeText = SpellStringParser.ParseString(row["SpellDescription" + i].ToString(), row, this);
                    spellTooltipGenFields[i].threadSafeText = SpellStringParser.ParseString(row["SpellTooltip" + i].ToString(), row, this);
                }
                for (i = 0; i < maxColumns; ++i)
                {
                    ThreadSafeTextBox box;
                    stringObjectMap.TryGetValue(i, out box);
                    box.threadSafeText = row[string.Format("SpellName{0}", i)];
                }
                for (i = 0; i < maxColumns; ++i)
                {
                    ThreadSafeTextBox box;
                    stringObjectMap.TryGetValue(i + 9, out box);
                    box.threadSafeText = row[string.Format("SpellRank{0}", i)];
                }

                for (i = 0; i < maxColumns; ++i)
                {
                    ThreadSafeTextBox box;
                    stringObjectMap.TryGetValue(i + 18, out box);
                    box.threadSafeText = row[string.Format("SpellTooltip{0}", i)];
                }

                for (i = 0; i < maxColumns; ++i)
                {
                    ThreadSafeTextBox box;
                    stringObjectMap.TryGetValue(i + 27, out box);
                    box.threadSafeText = row[string.Format("SpellDescription{0}", i)];
                }

                updateProgress("Updating category & dispel & mechanic...");
                var loadCategories = (SpellCategory)DBCManager.GetInstance().FindDbcForBinding("SpellCategory");
                Category.threadSafeIndex = loadCategories.UpdateCategorySelection(uint.Parse(
                    adapter.Query(string.Format("SELECT `Category` FROM `{0}` WHERE `ID` = '{1}'", "spell", selectedID)).Rows[0][0].ToString()));

                var loadDispels = (SpellDispelType)DBCManager.GetInstance().FindDbcForBinding("SpellDispelType");
                DispelType.threadSafeIndex = loadDispels.UpdateDispelSelection(uint.Parse(
                    adapter.Query(string.Format("SELECT `Dispel` FROM `{0}` WHERE `ID` = '{1}'", "spell", selectedID)).Rows[0][0].ToString()));

                var loadMechanics = (SpellMechanic)DBCManager.GetInstance().FindDbcForBinding("SpellMechanic");
                MechanicType.SelectedIndex = loadMechanics.UpdateMechanicSelection(uint.Parse(
                    adapter.Query(string.Format("SELECT `Mechanic` FROM `{0}` WHERE `ID` = '{1}'", "spell", selectedID)).Rows[0][0].ToString()));

                updateProgress("Updating attributes...");
                uint mask = uint.Parse(row["Attributes"].ToString());
                uint flagg = 1;

                for (int f = 0; f < attributes0.Count; ++f)
                {
                    attributes0[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                    flagg = flagg + flagg;
                }

                mask = uint.Parse(row["AttributesEx"].ToString());
                flagg = 1;

                for (int f = 0; f < attributes1.Count; ++f)
                {
                    attributes1[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                    flagg = flagg + flagg;
                }

                mask = uint.Parse(row["AttributesEx2"].ToString());
                flagg = 1;

                for (int f = 0; f < attributes2.Count; ++f)
                {
                    attributes2[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                    flagg = flagg + flagg;
                }

                mask = uint.Parse(row["AttributesEx3"].ToString());
                flagg = 1;

                for (int f = 0; f < attributes3.Count; ++f)
                {
                    attributes3[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                    flagg = flagg + flagg;
                }

                mask = uint.Parse(row["AttributesEx4"].ToString());
                flagg = 1;

                for (int f = 0; f < attributes4.Count; ++f)
                {
                    attributes4[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                    flagg = flagg + flagg;
                }

                if (isTbcOrGreater)
                {
                    mask = uint.Parse(row["AttributesEx5"].ToString());
                    flagg = 1;

                    for (int f = 0; f < attributes5.Count; ++f)
                    {
                        attributes5[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                        flagg = flagg + flagg;
                    }

                    mask = uint.Parse(row["AttributesEx6"].ToString());
                    flagg = 1;

                    for (int f = 0; f < attributes6.Count; ++f)
                    {
                        attributes6[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                        flagg = flagg + flagg;
                    }

                    updateProgress("Updating stances...");
                    mask = uint.Parse(row["Stances"].ToString());
                    if (mask == 0)
                    {
                        stancesBoxes[0].threadSafeChecked = true;
                        for (int f = 1; f < stancesBoxes.Count; ++f) { stancesBoxes[f].threadSafeChecked = false; }
                    }
                    else
                    {
                        stancesBoxes[0].threadSafeChecked = false;
                        uint flag = 1;
                        for (int f = 1; f < stancesBoxes.Count; ++f)
                        {
                            stancesBoxes[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
                            flag = flag + flag;
                        }
                    }
                }
                if (isWotlkOrGreater)
                {
                    mask = uint.Parse(row["AttributesEx7"].ToString());
                    flagg = 1;

                    for (int f = 0; f < attributes7.Count; ++f)
                    {
                        attributes7[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                        flagg = flagg + flagg;
                    }
                }
                attributes5.ForEach(box => box.IsEnabled = isTbcOrGreater);
                attributes6.ForEach(box => box.IsEnabled = isTbcOrGreater);
                attributes7.ForEach(box => box.IsEnabled = isWotlkOrGreater);
                stancesBoxes.ForEach(box => box.IsEnabled = isTbcOrGreater);

                updateProgress("Updating targets...");
                mask = uint.Parse(row["Targets"].ToString());
                if (mask == 0)
                {
                    targetBoxes[0].threadSafeChecked = true;
                    for (int f = 1; f < targetBoxes.Count; ++f) { targetBoxes[f].threadSafeChecked = false; }
                }
                else
                {
                    targetBoxes[0].threadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 1; f < targetBoxes.Count; ++f)
                    {
                        targetBoxes[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }

                mask = uint.Parse(row["TargetCreatureType"].ToString());

                if (mask == 0)
                {
                    targetCreatureTypeBoxes[0].threadSafeChecked = true;
                    for (int f = 1; f < targetCreatureTypeBoxes.Count; ++f) { targetCreatureTypeBoxes[f].threadSafeChecked = false; }
                }
                else
                {
                    targetCreatureTypeBoxes[0].threadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 1; f < targetCreatureTypeBoxes.Count; ++f)
                    {
                        targetCreatureTypeBoxes[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }
                updateProgress("Updating spell focus object selection...");
                var loadFocusObjects = (SpellFocusObject)DBCManager.GetInstance().FindDbcForBinding("SpellFocusObject");
                RequiresSpellFocus.threadSafeIndex = loadFocusObjects.UpdateSpellFocusObjectSelection(uint.Parse(
                    adapter.Query(string.Format("SELECT `RequiresSpellFocus` FROM `{0}` WHERE `ID` = '{1}'", "spell", selectedID)).Rows[0][0].ToString()));

                if (isTbcOrGreater)
                {
                    mask = uint.Parse(row["FacingCasterFlags"].ToString());
                    FacingFrontFlag.threadSafeChecked = ((mask & 0x1) != 0) ? true : false;
                }
                FacingFrontFlag.IsEnabled = isTbcOrGreater;

                updateProgress("Updating caster aura state...");
                switch (uint.Parse(row["CasterAuraState"].ToString()))
                {
                    case 0: // None
                        {
                            CasterAuraState.threadSafeIndex = 0;
                            break;
                        }

                    case 1: // Defense
                        {
                            CasterAuraState.threadSafeIndex = 1;

                            break;
                        }

                    case 2: // Healthless 20%
                        {
                            CasterAuraState.threadSafeIndex = 2;

                            break;
                        }

                    case 3: // Berserking
                        {
                            CasterAuraState.threadSafeIndex = 3;

                            break;
                        }

                    case 5: // Judgement
                        {
                            CasterAuraState.threadSafeIndex = 4;

                            break;
                        }

                    case 7: // Hunter Parry
                        {
                            CasterAuraState.threadSafeIndex = 5;

                            break;
                        }

                    case 10: // Victory Rush
                        {
                            CasterAuraState.threadSafeIndex = 6;

                            break;
                        }

                    case 11: // Unknown 1
                        {
                            CasterAuraState.threadSafeIndex = 7;

                            break;
                        }

                    case 13: // Healthless 35%
                        {
                            CasterAuraState.threadSafeIndex = 8;

                            break;
                        }

                    case 17: // Enrage
                        {
                            CasterAuraState.threadSafeIndex = 9;

                            break;
                        }

                    case 22: // Unknown 2
                        {
                            CasterAuraState.threadSafeIndex = 10;

                            break;
                        }

                    case 23: // Health Above 75%
                        {
                            CasterAuraState.threadSafeIndex = 11;

                            break;
                        }

                    default: { break; }
                }

                switch (uint.Parse(row["TargetAuraState"].ToString()))
                {
                    case 0: // None
                        {
                            TargetAuraState.threadSafeIndex = 0;

                            break;
                        }

                    case 2: // Healthless 20%
                        {
                            TargetAuraState.threadSafeIndex = 1;

                            break;
                        }

                    case 3: // Berserking
                        {
                            TargetAuraState.threadSafeIndex = 2;

                            break;
                        }

                    case 13: // Healthless 35%
                        {
                            TargetAuraState.threadSafeIndex = 3;

                            break;
                        }

                    case 14: // Conflagrate
                        {
                            TargetAuraState.threadSafeIndex = 4;

                            break;
                        }

                    case 15: // Swiftmend
                        {
                            TargetAuraState.threadSafeIndex = 5;

                            break;
                        }

                    case 16: // Deadly Poison
                        {
                            TargetAuraState.threadSafeIndex = 6;

                            break;
                        }

                    case 18: // Bleeding
                        {
                            TargetAuraState.threadSafeIndex = 17;

                            break;
                        }

                    default: { break; }
                }
                updateProgress("Updating cast time selection...");
                var loadCastTimes = (SpellCastTimes)DBCManager.GetInstance().FindDbcForBinding("SpellCastTimes");
                CastTime.threadSafeIndex = loadCastTimes.UpdateCastTimeSelection(uint.Parse(adapter.Query(
                    string.Format("SELECT `CastingTimeIndex` FROM `{0}` WHERE `ID` = '{1}'", "spell", selectedID)).Rows[0][0].ToString()));
                updateProgress("Updating other stuff...");
                RecoveryTime.threadSafeText = uint.Parse(row["RecoveryTime"].ToString());
                CategoryRecoveryTime.threadSafeText = uint.Parse(row["CategoryRecoveryTime"].ToString());

                mask = uint.Parse(row["InterruptFlags"].ToString());
                if (mask == 0)
                {
                    interrupts1[0].threadSafeChecked = true;
                    for (int f = 1; f < interrupts1.Count; ++f) { interrupts1[f].threadSafeChecked = false; }
                }
                else
                {
                    interrupts1[0].threadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 1; f < interrupts1.Count; ++f)
                    {
                        interrupts1[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;

                        flag = flag + flag;
                    }
                }

                mask = uint.Parse(row["AuraInterruptFlags"].ToString());
                if (mask == 0)
                {
                    interrupts2[0].threadSafeChecked = true;
                    for (int f = 1; f < interrupts2.Count; ++f) { interrupts2[f].threadSafeChecked = false; }
                }
                else
                {
                    interrupts2[0].threadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 1; f < interrupts2.Count; ++f)
                    {
                        interrupts2[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }

                mask = uint.Parse(row["ChannelInterruptFlags"].ToString());
                if (mask == 0)
                {
                    interrupts3[0].threadSafeChecked = true;
                    for (int f = 1; f < interrupts3.Count; ++f) { interrupts3[f].threadSafeChecked = false; }
                }
                else
                {
                    interrupts3[0].threadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 1; f < interrupts3.Count; ++f)
                    {
                        interrupts3[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }

                mask = uint.Parse(row["ProcFlags"].ToString());
                if (mask == 0)
                {
                    procBoxes[0].threadSafeChecked = true;
                    for (int f = 1; f < procBoxes.Count; ++f) { procBoxes[f].threadSafeChecked = false; }
                }
                else
                {
                    procBoxes[0].threadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 1; f < procBoxes.Count; ++f)
                    {
                        procBoxes[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }

                ProcChance.threadSafeText = uint.Parse(row["ProcChance"].ToString());
                ProcCharges.threadSafeText = uint.Parse(row["ProcCharges"].ToString());
                MaximumLevel.threadSafeText = uint.Parse(row["MaximumLevel"].ToString());
                BaseLevel.threadSafeText = uint.Parse(row["BaseLevel"].ToString());
                SpellLevel.threadSafeText = uint.Parse(row["SpellLevel"].ToString());

                var loadDurations = (SpellDuration)DBCManager.GetInstance().FindDbcForBinding("SpellDuration");
                Duration.threadSafeIndex = loadDurations.UpdateDurationIndexes(uint.Parse(adapter.Query(
                    string.Format("SELECT `DurationIndex` FROM `{0}` WHERE `ID` = '{1}'", "spell", selectedID)).Rows[0][0].ToString()));

                uint powerType = uint.Parse(row["PowerType"].ToString());
                // Manually handle 'Health' power type
                if (powerType == (uint.MaxValue - 1))
                    powerType = 13;
                PowerType.threadSafeIndex = powerType;
                PowerCost.threadSafeText = uint.Parse(row["ManaCost"].ToString());
                ManaCostPerLevel.threadSafeText = uint.Parse(row["ManaCostPerLevel"].ToString());
                ManaCostPerSecond.threadSafeText = uint.Parse(row["ManaPerSecond"].ToString());
                PerSecondPerLevel.threadSafeText = uint.Parse(row["ManaPerSecondPerLevel"].ToString());
                updateProgress("Updating spell range selection...");
                var loadRanges = (SpellRange)DBCManager.GetInstance().FindDbcForBinding("SpellRange");
                Range.threadSafeIndex = loadRanges.UpdateSpellRangeSelection(uint.Parse(adapter.Query(
                    string.Format("SELECT `RangeIndex` FROM `{0}` WHERE `ID` = '{1}'", "spell", selectedID)).Rows[0][0].ToString()));

                updateProgress("Updating speed, stacks, totems, reagents...");
                Speed.threadSafeText = row["Speed"].ToString();
                Stacks.threadSafeText = row["StackAmount"].ToString();
                Totem1.threadSafeText = row["Totem1"].ToString();
                Totem2.threadSafeText = row["Totem2"].ToString();
                Reagent1.threadSafeText = row["Reagent1"].ToString();
                Reagent2.threadSafeText = row["Reagent2"].ToString();
                Reagent3.threadSafeText = row["Reagent3"].ToString();
                Reagent4.threadSafeText = row["Reagent4"].ToString();
                Reagent5.threadSafeText = row["Reagent5"].ToString();
                Reagent6.threadSafeText = row["Reagent6"].ToString();
                Reagent7.threadSafeText = row["Reagent7"].ToString();
                Reagent8.threadSafeText = row["Reagent8"].ToString();
                ReagentCount1.threadSafeText = row["ReagentCount1"].ToString();
                ReagentCount2.threadSafeText = row["ReagentCount2"].ToString();
                ReagentCount3.threadSafeText = row["ReagentCount3"].ToString();
                ReagentCount4.threadSafeText = row["ReagentCount4"].ToString();
                ReagentCount5.threadSafeText = row["ReagentCount5"].ToString();
                ReagentCount6.threadSafeText = row["ReagentCount6"].ToString();
                ReagentCount7.threadSafeText = row["ReagentCount7"].ToString();
                ReagentCount8.threadSafeText = row["ReagentCount8"].ToString();

                updateProgress("Updating item class selection...");
                int ID = int.Parse(adapter.Query(
                    string.Format("SELECT `EquippedItemClass` FROM `{0}` WHERE `ID` = '{1}'", "spell", selectedID)).Rows[0][0].ToString());
                if (ID == -1)
                {
                    EquippedItemClass.threadSafeIndex = 0;
                    //foreach (ThreadSafeCheckBox box in main.equippedItemInventoryTypeMaskBoxes)
                    //  box.threadSafeChecked = false;
                    Dispatcher.Invoke(DispatcherPriority.Send, TimeSpan.Zero, new Func<object>(()
                        => EquippedItemInventoryTypeGrid.IsEnabled = false));
                }
                else if (ID == 2 || ID == 4)
                {
                    Dispatcher.Invoke(DispatcherPriority.Send, TimeSpan.Zero, new Func<object>(()
                        => EquippedItemInventoryTypeGrid.IsEnabled = true));
                }
                else
                {
                    foreach (ThreadSafeCheckBox box in equippedItemInventoryTypeMaskBoxes)
                        box.threadSafeChecked = false;
                    Dispatcher.Invoke(DispatcherPriority.Send, TimeSpan.Zero, new Func<object>(()
                        => EquippedItemInventoryTypeGrid.IsEnabled = false));
                }
                var loadItemClasses = (ItemClass)DBCManager.GetInstance().FindDbcForBinding("ItemClass");
                EquippedItemClass.threadSafeIndex = loadItemClasses.UpdateItemClassSelection(ID);

                UpdateItemSubClass(long.Parse(row["EquippedItemClass"].ToString()));

                updateProgress("Updating item subclass mask...");
                int intMask = int.Parse(row["EquippedItemSubClassMask"].ToString());
                if (intMask == 0 || intMask == -1)
                {
                    equippedItemSubClassMaskBoxes[0].threadSafeChecked = true;
                    for (int f = 1; f < equippedItemSubClassMaskBoxes.Count; ++f) { equippedItemSubClassMaskBoxes[f].threadSafeChecked = false; }
                }
                else
                {
                    equippedItemSubClassMaskBoxes[0].threadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 0; f < equippedItemSubClassMaskBoxes.Count; ++f)
                    {
                        equippedItemSubClassMaskBoxes[f].threadSafeChecked = ((intMask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }

                updateProgress("Updating inventory type...");
                intMask = int.Parse(row["EquippedItemInventoryTypeMask"].ToString());
                if (intMask == 0 || intMask == -1)
                {
                    equippedItemInventoryTypeMaskBoxes[0].threadSafeChecked = true;
                    for (int f = 1; f < equippedItemInventoryTypeMaskBoxes.Count; ++f) { equippedItemInventoryTypeMaskBoxes[f].threadSafeChecked = false; }
                }
                else
                {
                    equippedItemInventoryTypeMaskBoxes[0].threadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 0; f < equippedItemInventoryTypeMaskBoxes.Count; ++f)
                    {
                        equippedItemInventoryTypeMaskBoxes[f].threadSafeChecked = ((intMask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }
                updateProgress("Updating effects 1-3...");
                SpellEffect1.threadSafeIndex = int.Parse(row["Effect1"].ToString());
                SpellEffect2.threadSafeIndex = int.Parse(row["Effect2"].ToString());
                SpellEffect3.threadSafeIndex = int.Parse(row["Effect3"].ToString());
                DieSides1.threadSafeText = row["EffectDieSides1"].ToString();
                DieSides2.threadSafeText = row["EffectDieSides2"].ToString();
                DieSides3.threadSafeText = row["EffectDieSides3"].ToString();
                BasePointsPerLevel1.threadSafeText = row["EffectRealPointsPerLevel1"].ToString();
                BasePointsPerLevel2.threadSafeText = row["EffectRealPointsPerLevel2"].ToString();
                BasePointsPerLevel3.threadSafeText = row["EffectRealPointsPerLevel3"].ToString();
                BasePoints1.threadSafeText = row["EffectBasePoints1"].ToString();
                BasePoints2.threadSafeText = row["EffectBasePoints2"].ToString();
                BasePoints3.threadSafeText = row["EffectBasePoints3"].ToString();
                Mechanic1.threadSafeIndex = int.Parse(row["EffectMechanic1"].ToString());
                Mechanic2.threadSafeIndex = int.Parse(row["EffectMechanic2"].ToString());
                Mechanic3.threadSafeIndex = int.Parse(row["EffectMechanic3"].ToString());
                TargetA1.threadSafeIndex = uint.Parse(row["EffectImplicitTargetA1"].ToString());
                TargetA2.threadSafeIndex = uint.Parse(row["EffectImplicitTargetA2"].ToString());
                TargetA3.threadSafeIndex = uint.Parse(row["EffectImplicitTargetA3"].ToString());
                TargetB1.threadSafeIndex = uint.Parse(row["EffectImplicitTargetB1"].ToString());
                TargetB2.threadSafeIndex = uint.Parse(row["EffectImplicitTargetB2"].ToString());
                TargetB3.threadSafeIndex = uint.Parse(row["EffectImplicitTargetB3"].ToString());

                updateProgress("Updating radius index...");
                var loadRadiuses = (SpellRadius)DBCManager.GetInstance().FindDbcForBinding("SpellRadius");
                var result = adapter.Query(string.Format(
                    "SELECT `EffectRadiusIndex1`, `EffectRadiusIndex2`, `EffectRadiusIndex3` FROM `{0}` WHERE `ID` = '{1}'", "spell", selectedID)).Rows[0];
                uint[] IDs = { uint.Parse(result[0].ToString()), uint.Parse(result[1].ToString()), uint.Parse(result[2].ToString()) };
                RadiusIndex1.threadSafeIndex = loadRadiuses.UpdateRadiusIndexes(IDs[0]);
                RadiusIndex2.threadSafeIndex = loadRadiuses.UpdateRadiusIndexes(IDs[1]);
                RadiusIndex3.threadSafeIndex = loadRadiuses.UpdateRadiusIndexes(IDs[2]);

                updateProgress("Updating effect 1-3 data...");
                ApplyAuraName1.threadSafeIndex = int.Parse(row["EffectApplyAuraName1"].ToString());
                ApplyAuraName2.threadSafeIndex = int.Parse(row["EffectApplyAuraName2"].ToString());
                ApplyAuraName3.threadSafeIndex = int.Parse(row["EffectApplyAuraName3"].ToString());
                Amplitude1.threadSafeText = row["EffectAmplitude1"].ToString();
                Amplitude2.threadSafeText = row["EffectAmplitude2"].ToString();
                Amplitude3.threadSafeText = row["EffectAmplitude3"].ToString();
                MultipleValue1.threadSafeText = row["EffectMultipleValue1"].ToString();
                MultipleValue2.threadSafeText = row["EffectMultipleValue2"].ToString();
                MultipleValue3.threadSafeText = row["EffectMultipleValue3"].ToString();
                ChainTarget1.threadSafeText = row["EffectChainTarget1"].ToString();
                ChainTarget2.threadSafeText = row["EffectChainTarget2"].ToString();
                ChainTarget3.threadSafeText = row["EffectChainTarget3"].ToString();
                ItemType1.threadSafeText = row["EffectItemType1"].ToString();
                ItemType2.threadSafeText = row["EffectItemType2"].ToString();
                ItemType3.threadSafeText = row["EffectItemType3"].ToString();
                MiscValueA1.threadSafeText = row["EffectMiscValue1"].ToString();
                MiscValueA2.threadSafeText = row["EffectMiscValue2"].ToString();
                MiscValueA3.threadSafeText = row["EffectMiscValue3"].ToString();
                if (isTbcOrGreater)
                {
                    MiscValueB1.threadSafeText = row["EffectMiscValueB1"].ToString();
                    MiscValueB2.threadSafeText = row["EffectMiscValueB2"].ToString();
                    MiscValueB3.threadSafeText = row["EffectMiscValueB3"].ToString();
                }
                MiscValueB1.IsEnabled = isTbcOrGreater;
                MiscValueB2.IsEnabled = isTbcOrGreater;
                MiscValueB3.IsEnabled = isTbcOrGreater;
                TriggerSpell1.threadSafeText = row["EffectTriggerSpell1"].ToString();
                TriggerSpell2.threadSafeText = row["EffectTriggerSpell2"].ToString();
                TriggerSpell3.threadSafeText = row["EffectTriggerSpell3"].ToString();
                PointsPerComboPoint1.threadSafeText = row["EffectPointsPerComboPoint1"].ToString();
                PointsPerComboPoint2.threadSafeText = row["EffectPointsPerComboPoint2"].ToString();
                PointsPerComboPoint3.threadSafeText = row["EffectPointsPerComboPoint3"].ToString();

                if (!isWotlkOrGreater)
                {
                    /*
                    uint EffectImplicitTargetA1
                    uint EffectImplicitTargetA2
                    uint EffectImplicitTargetA3
                    uint EffectImplicitTargetB1
                    uint EffectImplicitTargetB2
                    uint EffectImplicitTargetB3
                    */
                    SpellMask11.threadSafeText = row["EffectImplicitTargetA1"].ToString();
                    SpellMask21.threadSafeText = row["EffectImplicitTargetA2"].ToString();
                    SpellMask31.threadSafeText = row["EffectImplicitTargetA3"].ToString();
                    SpellMask12.threadSafeText = row["EffectImplicitTargetB1"].ToString();
                    SpellMask22.threadSafeText = row["EffectImplicitTargetB2"].ToString();
                    SpellMask32.threadSafeText = row["EffectImplicitTargetB3"].ToString();

                    uint familyName = uint.Parse(row["SpellFamilyName"].ToString());
                    SpellFamilyName.threadSafeText = familyName.ToString();
                    SpellFamilyFlags.threadSafeText = row["SpellFamilyFlags1"].ToString();
                    SpellFamilyFlags1.threadSafeText = row["SpellFamilyFlags2"].ToString();

                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectImplicitTargetA1"].ToString()), SpellMask11);
                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectImplicitTargetA2"].ToString()), SpellMask21);
                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectImplicitTargetA3"].ToString()), SpellMask31);
                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectImplicitTargetB1"].ToString()), SpellMask12);
                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectImplicitTargetB2"].ToString()), SpellMask22);
                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectImplicitTargetB3"].ToString()), SpellMask32);

                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => 
                        spellFamilyClassMaskParser.UpdateSpellFamilyClassMask(this, familyName, isWotlkOrGreater)));
                }
                else
                {
                    SpellMask11.threadSafeText = row["EffectSpellClassMaskA1"].ToString();
                    SpellMask21.threadSafeText = row["EffectSpellClassMaskA2"].ToString();
                    SpellMask31.threadSafeText = row["EffectSpellClassMaskA3"].ToString();
                    SpellMask12.threadSafeText = row["EffectSpellClassMaskB1"].ToString();
                    SpellMask22.threadSafeText = row["EffectSpellClassMaskB2"].ToString();
                    SpellMask32.threadSafeText = row["EffectSpellClassMaskB3"].ToString();
                    SpellMask13.threadSafeText = row["EffectSpellClassMaskC1"].ToString();
                    SpellMask23.threadSafeText = row["EffectSpellClassMaskC2"].ToString();
                    SpellMask33.threadSafeText = row["EffectSpellClassMaskC3"].ToString();

                    uint familyName = uint.Parse(row["SpellFamilyName"].ToString());
                    SpellFamilyName.threadSafeText = familyName.ToString();
                    SpellFamilyFlags.threadSafeText = row["SpellFamilyFlags"].ToString();
                    SpellFamilyFlags1.threadSafeText = row["SpellFamilyFlags1"].ToString();
                    SpellFamilyFlags2.threadSafeText = row["SpellFamilyFlags2"].ToString();

                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectSpellClassMaskA1"].ToString()), SpellMask11);
                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectSpellClassMaskA2"].ToString()), SpellMask21);
                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectSpellClassMaskA3"].ToString()), SpellMask31);
                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectSpellClassMaskB1"].ToString()), SpellMask12);
                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectSpellClassMaskB2"].ToString()), SpellMask22);
                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectSpellClassMaskB3"].ToString()), SpellMask32);
                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectSpellClassMaskC1"].ToString()), SpellMask13);
                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectSpellClassMaskC2"].ToString()), SpellMask23);
                    UpdateSpellMaskCheckBox(uint.Parse(row["EffectSpellClassMaskC3"].ToString()), SpellMask33);

                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => 
                        spellFamilyClassMaskParser.UpdateSpellFamilyClassMask(this, familyName, isWotlkOrGreater)));
                }
                SpellFamilyFlags2.IsEnabled = isWotlkOrGreater;
                ToggleAllSpellMaskCheckBoxes(isWotlkOrGreater);

                SpellVisual1.threadSafeText = row["SpellVisual1"].ToString();
                SpellVisual2.threadSafeText = row["SpellVisual2"].ToString();
                ManaCostPercent.threadSafeText = row["ManaCostPercentage"].ToString();
                StartRecoveryCategory.threadSafeText = row["StartRecoveryCategory"].ToString();
                StartRecoveryTime.threadSafeText = row["StartRecoveryTime"].ToString();
                MaxTargetsLevel.threadSafeText = row["MaximumTargetLevel"].ToString();
                MaxTargets.threadSafeText = row["MaximumAffectedTargets"].ToString();
                SpellDamageType.threadSafeIndex = int.Parse(row["DamageClass"].ToString());
                PreventionType.threadSafeIndex = int.Parse(row["PreventionType"].ToString());
                EffectDamageMultiplier1.threadSafeText = row["EffectDamageMultiplier1"].ToString();
                EffectDamageMultiplier2.threadSafeText = row["EffectDamageMultiplier2"].ToString();
                EffectDamageMultiplier3.threadSafeText = row["EffectDamageMultiplier3"].ToString();

                if (isTbcOrGreater)
                {
                    updateProgress("Updating totem categories & load area groups...");
                    var loadTotemCategories = (TotemCategory)DBCManager.GetInstance().FindDbcForBinding("TotemCategory");
                    result = adapter.Query(string.Format(
                        "SELECT `TotemCategory1`, `TotemCategory2` FROM `{0}` WHERE `ID` = '{1}'", "spell", selectedID)).Rows[0];
                    IDs = new uint[] { uint.Parse(result[0].ToString()), uint.Parse(result[1].ToString()) };
                    TotemCategory1.threadSafeIndex = loadTotemCategories.UpdateTotemCategoriesSelection(IDs[0]);
                    TotemCategory2.threadSafeIndex = loadTotemCategories.UpdateTotemCategoriesSelection(IDs[1]);
                }
                TotemCategory1.IsEnabled = isTbcOrGreater;
                TotemCategory2.IsEnabled = isTbcOrGreater;
                if (isWotlkOrGreater)
                {
                    var loadAreaGroups = (AreaGroup)DBCManager.GetInstance().FindDbcForBinding("AreaGroup");
                    AreaGroup.threadSafeIndex = loadAreaGroups.UpdateAreaGroupSelection(uint.Parse(adapter.Query(
                        string.Format("SELECT `AreaGroupID` FROM `{0}` WHERE `ID` = '{1}'", "spell", selectedID)).Rows[0][0].ToString()));
                }
                AreaGroup.IsEnabled = isWotlkOrGreater;

                updateProgress("Updating school mask...");
                mask = uint.Parse(row["SchoolMask"].ToString());
                S1.threadSafeChecked = ((mask & 0x01) != 0) ? true : false;
                S2.threadSafeChecked = ((mask & 0x02) != 0) ? true : false;
                S3.threadSafeChecked = ((mask & 0x04) != 0) ? true : false;
                S4.threadSafeChecked = ((mask & 0x08) != 0) ? true : false;
                S5.threadSafeChecked = ((mask & 0x10) != 0) ? true : false;
                S6.threadSafeChecked = ((mask & 0x20) != 0) ? true : false;
                S7.threadSafeChecked = ((mask & 0x40) != 0) ? true : false;

                if (isWotlkOrGreater)
                {
                    updateProgress("Updating rune costs...");
                    var loadRuneCosts = (SpellRuneCost)DBCManager.GetInstance().FindDbcForBinding("SpellRuneCost");
                    RuneCost.threadSafeIndex = loadRuneCosts.UpdateSpellRuneCostSelection(uint.Parse(adapter.Query(
                        string.Format("SELECT `RuneCostID` FROM `{0}` WHERE `ID` = '{1}'", "spell", selectedID)).Rows[0][0].ToString()));
                }
                RuneCost.IsEnabled = isWotlkOrGreater;

                updateProgress("Updating spell missile & effect bonus multiplier...");
                if (isWotlkOrGreater)
                {
                    EffectBonusMultiplier1.threadSafeText = row["EffectBonusMultiplier1"].ToString();
                    EffectBonusMultiplier2.threadSafeText = row["EffectBonusMultiplier2"].ToString();
                    EffectBonusMultiplier3.threadSafeText = row["EffectBonusMultiplier3"].ToString();
                }
                EffectBonusMultiplier1.IsEnabled = isWotlkOrGreater;
                EffectBonusMultiplier2.IsEnabled = isWotlkOrGreater;
                EffectBonusMultiplier3.IsEnabled = isWotlkOrGreater;
                if (isWotlkOrGreater)
                {
                    SpellMissileID.threadSafeText = row["SpellMissileID"].ToString();

                    updateProgress("Updating spell description variables & difficulty selection...");
                    var loadDifficulties = (SpellDifficulty)DBCManager.GetInstance().FindDbcForBinding("SpellDifficulty");
                    var loadDescriptionVariables = (SpellDescriptionVariables)DBCManager.GetInstance().FindDbcForBinding("SpellDescriptionVariables");
                    SpellDescriptionVariables.threadSafeIndex = loadDescriptionVariables.UpdateSpellDescriptionVariablesSelection(
                        uint.Parse(adapter.Query(
                            string.Format("SELECT `SpellDescriptionVariableID` FROM `{0}` WHERE `ID` = '{1}'", "spell",
                                selectedID)).Rows[0][0].ToString()));

                    Difficulty.threadSafeIndex = loadDifficulties.UpdateDifficultySelection(uint.Parse(adapter.Query(
                        string.Format("SELECT `SpellDifficultyID` FROM `{0}` WHERE `ID` = '{1}'", "spell", selectedID)).Rows[0][0].ToString()));
                }
                SpellMissileID.IsEnabled = isWotlkOrGreater;
                SpellDescriptionVariables.IsEnabled = isWotlkOrGreater;
                Difficulty.IsEnabled = isWotlkOrGreater;
            }
            catch (Exception e)
            {
                HandleErrorMessage(string.Format("{0}\n\n{1}", "", e, e.InnerException));
            }
            adapter.Updating = false;
        }

        private void UpdateSpellMaskCheckBox(uint Mask, ThreadSafeComboBox ComBox)
        {
            for (int i = 0; i < 32; i++)
            {
                uint _mask = (uint)Math.Pow(2, i);

                ThreadSafeCheckBox safeCheckBox = (ThreadSafeCheckBox)ComBox.Items.GetItemAt(i);
                
                safeCheckBox.threadSafeChecked = false;
                if ((Mask & _mask) != 0)
                    safeCheckBox.threadSafeChecked = true;
            }
        }

        private void ToggleAllSpellMaskCheckBoxes(bool enabled)
        {
            SpellMask13.IsEnabled = enabled;
            SpellMask23.IsEnabled = enabled;
            SpellMask33.IsEnabled = enabled;
        }
        #endregion

        #region SelectionChanges
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (updating || adapter == null || !Config.isInit)
                return;
            var item = sender as TabControl;

            if (item.SelectedIndex == item.Items.Count - 1) { PrepareIconEditor(); }
        }

        private async void SelectSpell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var added_items = e.AddedItems;
            if (added_items.Count > 1)
            {
                await this.ShowMessageAsync(TryFindResource("SpellEditor").ToString(), TryFindResource("String5").ToString());
                ((ListBox)sender).UnselectAll();
                return;
            }
            if (added_items.Count == 1)
            {
                ListBox box = (ListBox)sender;

                StackPanel panel = (StackPanel) box.SelectedItem;
                using (var enumerator = panel.GetChildObjects().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current is TextBlock)
                        {
                            TextBlock block = (TextBlock)enumerator.Current;
                            string name = block.Text;
                            selectedID = uint.Parse(name.Substring(1, name.IndexOf(' ', 1)));
                            UpdateMainWindow();
                            return;
                        }
                    }
                }
            }
        }
        
        // TODO(Harry): Remove unrequired hook
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (adapter == null || updating)
                return;
            if (sender == RequiresSpellFocus)
            {
                var loadFocusObjects = (SpellFocusObject)DBCManager.GetInstance().FindDbcForBinding("SpellFocusObject");
                for (int i = 0; i < loadFocusObjects.Lookups.Count; ++i)
                {
                    if (loadFocusObjects.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "RequiresSpellFocus", loadFocusObjects.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == AreaGroup)
            {
                var loadAreaGroups = (AreaGroup)DBCManager.GetInstance().FindDbcForBinding("AreaGroup");
                for (int i = 0; i < loadAreaGroups.Lookups.Count; ++i)
                {
                    if (loadAreaGroups.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "AreaGroupID", loadAreaGroups.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == Category)
            {
                var loadCategories = (SpellCategory)DBCManager.GetInstance().FindDbcForBinding("SpellCategory");
                for (int i = 0; i < loadCategories.Lookups.Count; ++i)
                {
                    if (loadCategories.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "Category", loadCategories.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == DispelType)
            {
                var loadDispels = (SpellDispelType)DBCManager.GetInstance().FindDbcForBinding("SpellDispelType");
                for (int i = 0; i < loadDispels.Lookups.Count; ++i)
                {
                    if (loadDispels.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "Dispel", loadDispels.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == MechanicType)
            {
                var loadMechanics = (SpellMechanic)DBCManager.GetInstance().FindDbcForBinding("SpellMechanic");
                for (int i = 0; i < loadMechanics.Lookups.Count; ++i)
                {
                    if (loadMechanics.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "Mechanic", loadMechanics.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == CastTime)
            {
                var loadCastTimes = (SpellCastTimes)DBCManager.GetInstance().FindDbcForBinding("SpellCastTimes");
                for (int i = 0; i < loadCastTimes.Lookups.Count; ++i)
                {
                    if (loadCastTimes.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "CastingTimeIndex", loadCastTimes.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == Duration)
            {
                var loadDurations = (SpellDuration)DBCManager.GetInstance().FindDbcForBinding("SpellDuration");
                for (int i = 0; i < loadDurations.Lookups.Count; ++i)
                {
                    if (loadDurations.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "DurationIndex", loadDurations.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == Difficulty)
            {
                var loadDifficulties = (SpellDifficulty)DBCManager.GetInstance().FindDbcForBinding("SpellDifficulty");
                for (int i = 0; i < loadDifficulties.Lookups.Count; ++i)
                {
                    if (loadDifficulties.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "SpellDifficultyID", loadDifficulties.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == Range)
            {
                var loadRanges = (SpellRange)DBCManager.GetInstance().FindDbcForBinding("SpellRange");
                for (int i = 0; i < loadRanges.Lookups.Count; ++i)
                {
                    if (loadRanges.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "RangeIndex", loadRanges.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == RadiusIndex1)
            {
                var loadRadiuses = (SpellRadius)DBCManager.GetInstance().FindDbcForBinding("SpellRadius");
                for (int i = 0; i < loadRadiuses.Lookups.Count; ++i)
                {
                    if (loadRadiuses.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "EffectRadiusIndex1", loadRadiuses.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == RadiusIndex2)
            {
                var loadRadiuses = (SpellRadius)DBCManager.GetInstance().FindDbcForBinding("SpellRadius");
                for (int i = 0; i < loadRadiuses.Lookups.Count; ++i)
                {
                    if (loadRadiuses.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "EffectRadiusIndex2", loadRadiuses.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == RadiusIndex3)
            {
                var loadRadiuses = (SpellRadius)DBCManager.GetInstance().FindDbcForBinding("SpellRadius");
                for (int i = 0; i < loadRadiuses.Lookups.Count; ++i)
                {
                    if (loadRadiuses.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "EffectRadiusIndex3", loadRadiuses.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == EquippedItemClass)
            {
                var loadItemClasses = (ItemClass)DBCManager.GetInstance().FindDbcForBinding("ItemClass");
                long itemSubClass = loadItemClasses.Lookups[EquippedItemClass.SelectedIndex].ID;
                UpdateItemSubClass(itemSubClass);
                for (int i = 0; i < loadItemClasses.Lookups.Count; ++i)
                {
                    if (EquippedItemClass.SelectedIndex == 5 || EquippedItemClass.SelectedIndex == 3)
                    {
                        EquippedItemInventoryTypeGrid.IsEnabled = true;
                    } 
                    else
                    {
                        EquippedItemInventoryTypeGrid.IsEnabled = false;
                    }

                    if (loadItemClasses.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "EquippedItemClass", loadItemClasses.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == TotemCategory1)
            {
                var loadTotemCategories = (TotemCategory)DBCManager.GetInstance().FindDbcForBinding("TotemCategory");
                for (int i = 0; i < loadTotemCategories.Lookups.Count; ++i)
                {
                    if (loadTotemCategories.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "TotemCategory1", loadTotemCategories.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == TotemCategory2)
            {
                var loadTotemCategories = (TotemCategory)DBCManager.GetInstance().FindDbcForBinding("TotemCategory");
                for (int i = 0; i < loadTotemCategories.Lookups.Count; ++i)
                {
                    if (loadTotemCategories.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "TotemCategory2", loadTotemCategories.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == RuneCost)
            {
                var loadRuneCosts = (SpellRuneCost)DBCManager.GetInstance().FindDbcForBinding("SpellRuneCost");
                for (int i = 0; i < loadRuneCosts.Lookups.Count; ++i)
                {
                    if (loadRuneCosts.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "RuneCostID", loadRuneCosts.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == SpellDescriptionVariables)
            {
                var loadDescriptionVariables = (SpellDescriptionVariables)DBCManager.GetInstance().FindDbcForBinding("SpellDescriptionVariables");
                for (int i = 0; i < loadDescriptionVariables.Lookups.Count; ++i)
                {
                    if (loadDescriptionVariables.Lookups[i].ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute(string.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            "spell", "SpellDescriptionVariableID", loadDescriptionVariables.Lookups[i].ID, selectedID));
                        break;
                    }
                }
            }
        }

        public void UpdateItemSubClass(long classId)
        {
            if (selectedID == 0)
                return;
            if (classId == -1)
            {
                Dispatcher.Invoke(DispatcherPriority.Send, TimeSpan.Zero, new Func<object>(() 
                    => EquippedItemInventoryTypeGrid.IsEnabled = false));

                foreach (ThreadSafeCheckBox box in equippedItemSubClassMaskBoxes)
                {
                    box.threadSafeContent = TryFindResource("None").ToString();
                    box.threadSafeVisibility = Visibility.Hidden;
                    //box.threadSafeEnabled = false;
                }
                return;
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Send, TimeSpan.Zero, new Func<object>(() 
                    => EquippedItemSubClassGrid.IsEnabled = true));
            }
            uint num = 0;
            var loadItemSubClasses = (ItemSubClass)DBCManager.GetInstance().FindDbcForBinding("ItemSubClass");
            foreach (ThreadSafeCheckBox box in equippedItemSubClassMaskBoxes)
            {
                ItemSubClass.ItemSubClassLookup itemLookup = (ItemSubClass.ItemSubClassLookup) loadItemSubClasses.LookupClassAndSubclass(classId, num);
                if (itemLookup.Name != null)
                {
                    box.threadSafeContent = itemLookup.Name;
                    //box.threadSafeEnabled = true;
                    box.threadSafeVisibility = Visibility.Visible;
                }
                else
                {
                    box.threadSafeContent = TryFindResource("None").ToString(); ;
                    box.threadSafeVisibility = Visibility.Hidden;
                    //box.threadSafeEnabled = false;
                }
                box.threadSafeChecked = false;
                num++;
            }
        }
        private class ItemDetail
        {
            private DataRow userState;

            public ItemDetail(DataRow userState)
            {
                this.userState = userState;
            }
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IconGrid == null || !IconGrid.IsInitialized)
            {
                return;
            }
            double newSize = e.NewValue / 4;
            var margin = new Thickness(newSize, 0, 0, 0);
            ((SpellIconDBC)DBCManager.GetInstance().FindDbcForBinding("SpellIcon"))?.updateIconSize(newSize, margin);
            foreach (Image image in IconGrid.Children)
            {
                image.Margin = margin;
                image.Width = e.NewValue;
                image.Height = e.NewValue;
            }
        }

        public DataRow GetSpellRowById(uint spellId) => adapter.Query(string.Format("SELECT * FROM `{0}` WHERE `ID` = '{1}' LIMIT 1", "spell", spellId)).Rows[0];

        public string GetSpellNameById(uint spellId)
        {
            var dr = spellTable.Select(string.Format("id = {0}", spellId));
            if (dr.Length == 1)
                return dr[0]["SpellName" + (GetLocale() - 1)].ToString();
            return "";
        }
        #endregion

        #region Experimental window resizing
        private void window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Disable experimental window size updating. This is quite hacky, it was to
            // try and workaround the fact that some of the components I am using do not
            // support automatic resizing, such as TabControl's.
            return;

            MainTabControl.Width = e.NewSize.Width;
            MainTabControl.Height = e.NewSize.Height;

            // Experimental resize all child elements
            double xChange = 1, yChange = 1;

            if (e.PreviousSize.Width != 0)
                xChange = (e.NewSize.Width / e.PreviousSize.Width);

            if (e.PreviousSize.Height != 0)
                yChange = (e.NewSize.Height / e.PreviousSize.Height);

            ResizeChildElements(xChange, yChange, IconScrollViewer);
            foreach (FrameworkElement fe in SelectSpellTabGrid.Children)
                ResizeChildElements(xChange, yChange, fe);
            foreach (FrameworkElement fe in BaseTabGrid.Children)
                ResizeChildElements(xChange, yChange, fe);
            foreach (FrameworkElement fe in AttributesTabGrid.Children)
                ResizeChildElements(xChange, yChange, fe);
            foreach (FrameworkElement fe in Attributes2TabGrid.Children)
                ResizeChildElements(xChange, yChange, fe);
        }

        private void ResizeChildElements(double originalWidth, double originalHeight, FrameworkElement parent, IEnumerable<FrameworkElement> children)
        {
            // Experimental resize all child elements
            double xChange = 1, yChange = 1;

            if (originalWidth != 0)
                xChange = (parent.Width / originalWidth);

            if (originalHeight != 0)
                yChange = (parent.Height / originalHeight);

            foreach (FrameworkElement fe in children)
            {
                ResizeChildElements(xChange, yChange, fe);
            }
        }

        private void ResizeChildElements(double xChange, double yChange, FrameworkElement fe)
        {
            double originalWidth = fe.ActualWidth;
            double originalHeight = fe.ActualHeight;

            if (!(fe is ThreadSafeTextBox))
            {
                fe.Height = fe.ActualHeight * yChange;
                fe.Width = fe.ActualWidth * xChange;

                Canvas.SetTop(fe, Canvas.GetTop(fe) * yChange);
                Canvas.SetLeft(fe, Canvas.GetLeft(fe) * xChange);
            }

            if (fe is TabControl ||
                fe is Canvas ||
                fe is WrapPanel ||
                fe is StackPanel)
            {
                ResizeChildElements(originalWidth, originalHeight, fe, fe.FindChildren<FrameworkElement>());
            }
        }
        #endregion

        private void MultilingualSwitch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string language = e.AddedItems[0].ToString();
            string path = string.Format("pack://{0}:,,,/Languages/{1}.xaml", "SiteOfOrigin", language);
            Application.Current.Resources.MergedDictionaries[0].Source = new Uri(path);
            Config.Language = language;
            RefreshAllUIElements();
        }

        private void MultilingualSwitch_Initialized(object sender, EventArgs e)
        {
            string ConfigLanguage = Config.Language;
            ConfigLanguage = ConfigLanguage == "" ? "enUS" : ConfigLanguage;

            MultilingualSwitch.Items.Add("enUS");
            int index = 0;
            foreach (var item in Directory.GetFiles("Languages"))
            {
                FileInfo f = new FileInfo(item);
                if (f.Extension != ".xaml")
                    continue;
                string fileName = new FileInfo(item).Name.Replace(f.Extension, "");
                if (fileName != "enUS")
                    MultilingualSwitch.Items.Add(fileName);

                if (fileName == ConfigLanguage)
                    index = MultilingualSwitch.Items.Count - 1;
            }
            // We want the selection changed event to fire first if the index is > 0
            if (index > 0)
            {
                MultilingualSwitch.SelectionChanged += MultilingualSwitch_SelectionChanged;
            }
            MultilingualSwitch.SelectedIndex = index;
            if (index == 0)
            {
                MultilingualSwitch.SelectionChanged += MultilingualSwitch_SelectionChanged;
            }
        }
    };
};
