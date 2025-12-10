using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MySql.Data.MySqlClient;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using SpellEditor.Sources.Binding;
using SpellEditor.Sources.Config;
using SpellEditor.Sources.Constants;
using SpellEditor.Sources.Controls;
using SpellEditor.Sources.Controls.Common;
using SpellEditor.Sources.Controls.SpellFamilyNames;
using SpellEditor.Sources.Controls.Visual;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.Locale;
using SpellEditor.Sources.SpellStringTools;
using SpellEditor.Sources.Tools.SpellFamilyClassMaskStoreParser;
using SpellEditor.Sources.Tools.VisualTools;
using SpellEditor.Sources.VersionControl;

namespace SpellEditor
{
    public partial class MainWindow : MetroWindow

    {
        #region Boxes
        private readonly Dictionary<int, ThreadSafeTextBox> stringObjectMap = new Dictionary<int, ThreadSafeTextBox>();
        private readonly List<ThreadSafeCheckBox> attributes0 = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> attributes1 = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> attributes2 = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> attributes3 = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> attributes4 = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> attributes5 = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> attributes6 = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> attributes7 = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> stancesBoxes = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> targetCreatureTypeBoxes = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> targetBoxes = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> procBoxes = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> interrupts1 = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> interrupts2 = new List<ThreadSafeCheckBox>();
        private readonly List<ThreadSafeCheckBox> interrupts3 = new List<ThreadSafeCheckBox>();
        public readonly List<ThreadSafeCheckBox> equippedItemInventoryTypeMaskBoxes = new List<ThreadSafeCheckBox>();
        public readonly List<ThreadSafeCheckBox> equippedItemSubClassMaskBoxes = new List<ThreadSafeCheckBox>();
        #endregion

        #region MemberVariables
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private IDatabaseAdapter adapter;
        public uint selectedID;
        public uint newIconID = 1;
        private bool updating;
        private readonly SpellStringParser SpellStringParser = new SpellStringParser();

        private readonly List<ThreadSafeTextBox> spellDescGenFields = new List<ThreadSafeTextBox>();
        private readonly List<ThreadSafeTextBox> spellTooltipGenFields = new List<ThreadSafeTextBox>();
        public SpellFamilyClassMaskParser spellFamilyClassMaskParser;
        private VisualController _currentVisualController;

        // store effects UI stuff in arrays for easy access
        private FilteredComboBox[] spellEffectTypeBoxes;
        private ThreadSafeTextBox[] effectMultipleValueBoxes;
        private FilteredComboBox[] effectAuraNameBoxes;
        private ThreadSafeTextBox[] effectChainTargetBoxes;
        private ThreadSafeTextBox[] effectItemTypeBoxes;
        private ThreadSafeTextBox[] effectTriggerSpellBoxes;
        private ThreadSafeTextBox[] effectAmplitudeBoxes;
        private Label[] miscValueLabelA;
        private ContentControl[] miscValueDynamicContentsA;
        private Label[] miscValueLabelB;
        private ContentControl[] miscValueDynamicContentsB;

        #endregion

        public IDatabaseAdapter GetDBAdapter()
        {
            return adapter;
        }

        private void VerifyStaticData()
        {
            MiscValueConstants.VerifyData();
        }

        public MainWindow()
        {
            // Ensure the decimal seperator used is always a full stop
            var customCulture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = customCulture;

            try
            {
                File.Delete("debug_output.txt");
                File.Delete("query_log.txt");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to delete debug_output.txt");
            }

            // Setup logging
            var config = new LoggingConfiguration();
            var logfile = new FileTarget("logfile") { FileName = "debug_output.txt" };
            var querylog = new FileTarget("logfile") { FileName = "query_log.txt" };
            var logconsole = new ConsoleTarget("logconsole");
            var layout = Layout.FromString("[${time}|${level:uppercase=true}|${logger}] ${message}");
            var queryLayout = Layout.FromString("[${time}] ${message}");
            logfile.Layout = layout;
            logconsole.Layout = layout;
            querylog.Layout = queryLayout;
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            config.AddRuleForOneLevel(LogLevel.Trace, querylog);
            LogManager.Configuration = config;

            // Banner
            Logger.Info("######################################################");
            Logger.Info($"Starting WoW Spell Editor - {DateTime.Now.ToString()}");
            Logger.Info("######################################################");

            // Config must be initialised fast
            Config.Init();
            InitializeComponent();

            SpellFamilyNames.Init();

            VerifyStaticData();

            // store effects Ui elements in arrays for easy access
            spellEffectTypeBoxes = new[] { SpellEffect1, SpellEffect2, SpellEffect3 };
            effectMultipleValueBoxes = new[] { MultipleValue1, MultipleValue2, MultipleValue3 };
            effectAuraNameBoxes = new[] { ApplyAuraName1, ApplyAuraName2, ApplyAuraName3 };
            effectChainTargetBoxes = new[] { ChainTarget1, ChainTarget2, ChainTarget3 };
            effectItemTypeBoxes = new[] { ItemType1, ItemType2, ItemType3 };
            effectTriggerSpellBoxes = new[] { TriggerSpell1, TriggerSpell2, TriggerSpell3 };
            effectAmplitudeBoxes = new[] { Amplitude1, Amplitude2, Amplitude3 };
            miscValueLabelA = new[] { LabMiscValueA1, LabMiscValueA2, LabMiscValueA3 };
            miscValueDynamicContentsA = new[] { MiscValueA1DynamicContent, MiscValueA2DynamicContent, MiscValueA3DynamicContent };
            miscValueLabelB = new[] { LabMiscValueB1, LabMiscValueB2, LabMiscValueB3 };
            miscValueDynamicContentsB = new[] { MiscValueB1DynamicContent, MiscValueB2DynamicContent, MiscValueB3DynamicContent };
        }

        ~MainWindow()
        {
            GetDBAdapter()?.Dispose();
            Logger.Info("######################################################");
            Logger.Info("### SHUTTING DOWN                                    #");
            Logger.Info("######################################################");
            Console.Out.Flush();
        }

        public async void HandleErrorMessage(string msg)
        {
            if (Dispatcher != null && Dispatcher.CheckAccess())
                await this.ShowMessageAsync(SafeTryFindResource("SpellEditor"), msg);
            else
                Dispatcher?.Invoke(DispatcherPriority.Normal, TimeSpan.Zero, new Func<object>(() => this.ShowMessageAsync(SafeTryFindResource("SpellEditor"), msg)));
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var errorMsg = $"ERROR: {e.Exception}\n{e.Exception.StackTrace}\n{e.Exception.InnerException}\n{e.Exception.InnerException?.StackTrace}";
            Logger.Fatal(errorMsg);
            File.WriteAllText("error.txt", errorMsg);
            HandleErrorMessage(e.Exception + "\n\n" + e.Exception.InnerException);
            e.Handled = true;
            Console.Out.Flush();
        }

        public int GetLanguage() {
            // FIXME(Harry)
            // Disabled returning Locale_langauge until it can at least support multiple client types
            // Get a new adapter to avoid lock blocking calling thread
            using (var adapter = AdapterFactory.Instance.GetAdapter(false))
            {
                var locale = LocaleManager.Instance.GetLocale(adapter);
                return locale == -1 ? 0 : locale;
            }
            //return (int)Locale_language;
        }

        #region LanguageSwitch
        private void RefreshAllUIElements()
        {
            Attributes1.Children.Clear();
            attributes0.Clear();
            string[] attFlags = SafeTryFindResource("attFlags_strings").Split('|');
            foreach (string attFlag in attFlags)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = attFlag, ToolTip = attFlag, Margin = new Thickness(0, 5, 0, 0)
                };

                Attributes1.Children.Add(box);
                attributes0.Add(box);
            }

            Attributes2.Children.Clear();
            attributes1.Clear();
            attFlags = SafeTryFindResource("attFlagsEx_strings").Split('|');
            foreach (string attFlag in attFlags)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = attFlag, ToolTip = attFlag, Margin = new Thickness(0, 5, 0, 0)
                };

                Attributes2.Children.Add(box);
                attributes1.Add(box);
            }

            Attributes3.Children.Clear();
            attributes2.Clear();
            attFlags = SafeTryFindResource("attFlagsEx1_strings").Split('|');
            foreach (string attFlag in attFlags)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = attFlag, ToolTip = attFlag, Margin = new Thickness(0, 5, 0, 0)
                };

                Attributes3.Children.Add(box);
                attributes2.Add(box);
            }

            Attributes4.Children.Clear();
            attributes3.Clear();
            attFlags = SafeTryFindResource("attFlagsEx2_strings").Split('|');
            foreach (string attFlag in attFlags)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = attFlag, ToolTip = attFlag, Margin = new Thickness(0, 5, 0, 0)
                };

                Attributes4.Children.Add(box);
                attributes3.Add(box);
            }

            Attributes5.Children.Clear();
            attributes4.Clear();
            attFlags = SafeTryFindResource("attFlagsEx3_strings").Split('|');
            foreach (string attFlag in attFlags)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = attFlag, ToolTip = attFlag, Margin = new Thickness(0, 5, 0, 0)
                };

                Attributes5.Children.Add(box);
                attributes4.Add(box);
            }

            Attributes6.Children.Clear();
            attributes5.Clear();
            attFlags = SafeTryFindResource("attFlagsEx4_strings").Split('|');
            foreach (string attFlag in attFlags)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = attFlag, ToolTip = attFlag, Margin = new Thickness(0, 5, 0, 0)
                };

                Attributes6.Children.Add(box);
                attributes5.Add(box);
            }

            Attributes7.Children.Clear();
            attributes6.Clear();
            attFlags = SafeTryFindResource("attFlagsEx5_strings").Split('|');
            foreach (string attFlag in attFlags)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = attFlag, ToolTip = attFlag, Margin = new Thickness(0, 5, 0, 0)
                };

                Attributes7.Children.Add(box);
                attributes6.Add(box);
            }

            Attributes8.Children.Clear();
            attributes7.Clear();
            attFlags = SafeTryFindResource("attFlagsEx6_strings").Split('|');
            foreach (string attFlag in attFlags)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = attFlag, ToolTip = attFlag, Margin = new Thickness(0, 5, 0, 0)
                };

                Attributes8.Children.Add(box);
                attributes7.Add(box);
            }

            CasterAuraState.Items.Clear();
            string[] caster_aura_state_strings = SafeTryFindResource("caster_aura_state_strings").Split('|');
            foreach (string casterAuraState in caster_aura_state_strings) { CasterAuraState.Items.Add(casterAuraState); }

            TargetAuraState.Items.Clear();
            string[] target_aura_state_strings = SafeTryFindResource("target_aura_state_strings").Split('|');
            foreach (string targetAuraState in target_aura_state_strings) { TargetAuraState.Items.Add(targetAuraState); }

            EquippedItemInventoryTypeGrid.Children.Clear();
            equippedItemInventoryTypeMaskBoxes.Clear();
            string[] equipped_item_inventory_type_mask_strings = SafeTryFindResource("equipped_item_inventory_type_mask_strings").Split('|');
            foreach (string equippedItemInventoryTypeMask in equipped_item_inventory_type_mask_strings)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = equippedItemInventoryTypeMask, Margin = new Thickness(0, 5, 0, 0)
                };


                EquippedItemInventoryTypeGrid.Children.Add(box);
                equippedItemInventoryTypeMaskBoxes.Add(box);
            }

            EquippedItemSubClassGrid.Children.Clear();
            equippedItemSubClassMaskBoxes.Clear();
            for (int i = 0; i < 29; ++i)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = SafeTryFindResource("None"),
                    Margin = new Thickness(0, 5, 0, 0),
                    Visibility = Visibility.Hidden
                };

                EquippedItemSubClassGrid.Children.Add(box);
                equippedItemSubClassMaskBoxes.Add(box);
            }

            PowerType.Items.Clear();
            string[] school_strings = SafeTryFindResource("power_type_strings").Split('|');
            foreach (string schoolString in school_strings) { PowerType.Items.Add(schoolString); }

            SpellDamageType.Items.Clear();
            PreventionType.Items.Clear();
            string[] damage_prevention_types = SafeTryFindResource("damage_prevention_types").Split('|');
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
            string[] target_strings = SafeTryFindResource("target_strings").Split('|');
            foreach (string targetString in target_strings)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = targetString, Margin = new Thickness(0, 5, 0, 0)
                };


                TargetEditorGrid.Children.Add(box);
                targetBoxes.Add(box);
            }

            ProcEditorGrid.Children.Clear();
            procBoxes.Clear();
            string[] proc_strings = SafeTryFindResource("proc_strings").Split('|');
            foreach (string procString in proc_strings)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = procString, Margin = new Thickness(0, 5, 0, 0)
                };



                ProcEditorGrid.Children.Add(box);
                procBoxes.Add(box);
            }

            string[] spell_aura_effect_names = SafeTryFindResource("spell_aura_effect_names").Split('|');
            string[] comboEntries = new string[spell_aura_effect_names.Length];
            for (int i = 0; i < spell_aura_effect_names.Length; ++i)
            {
                comboEntries[i] = i + " - " + spell_aura_effect_names[i];
            }
            ApplyAuraName1.ItemsSource = new List<string>(comboEntries);
            ApplyAuraName2.ItemsSource = new List<string>(comboEntries);
            ApplyAuraName3.ItemsSource = new List<string>(comboEntries);
            FilterAuraCombo.ItemsSource = new List<string>(comboEntries);

            string[] spell_effect_names = SafeTryFindResource("spell_effect_names_clear").Split('|');
            comboEntries = new string[spell_effect_names.Length];
            for (int i = 0; i < spell_effect_names.Length; ++i)
            {
                comboEntries[i] = i + " - " + spell_effect_names[i];
            }
            SpellEffect1.ItemsSource = new List<string>(comboEntries);
            SpellEffect2.ItemsSource = new List<string>(comboEntries);
            SpellEffect3.ItemsSource = new List<string>(comboEntries);
            FilterSpellEffectCombo.ItemsSource = new List<string>(comboEntries);

            Mechanic1.Items.Clear();
            Mechanic2.Items.Clear();
            Mechanic3.Items.Clear();

            if (TargetA1.Items.Count == 0)
            {
                int number = 0;
                var comboList = new List<string>();
                foreach (Targets t in System.Enum.GetValues(typeof(Targets)))
                {
                    comboList.Add(number + " - " + t);
                    ++number;
                }

                TargetA1.ItemsSource = new List<string>(comboList);
                TargetB1.ItemsSource = new List<string>(comboList);
                TargetA2.ItemsSource = new List<string>(comboList);
                TargetB2.ItemsSource = new List<string>(comboList);
                TargetA3.ItemsSource = new List<string>(comboList);
                TargetB3.ItemsSource = new List<string>(comboList);
                FilterSpellTargetA.ItemsSource = new List<string>(comboList);
                FilterSpellTargetB.ItemsSource = new List<string>(comboList);
            }

            InterruptFlagsGrid.Children.Clear();
            interrupts1.Clear();
            string[] interrupt_strings = SafeTryFindResource("interrupt_strings").Split('|');
            foreach (string interruptString in interrupt_strings)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = interruptString, Margin = new Thickness(0, 5, 0, 0)
                };

                InterruptFlagsGrid.Children.Add(box);
                interrupts1.Add(box);
            }

            AuraInterruptFlagsGrid.Children.Clear();
            interrupts2.Clear();
            string[] aura_interrupt_strings = SafeTryFindResource("aura_interrupt_strings").Split('|');
            foreach (string auraInterruptString in aura_interrupt_strings)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = auraInterruptString, Margin = new Thickness(0, 5, 0, 0)
                };

                AuraInterruptFlagsGrid.Children.Add(box);
                interrupts2.Add(box);
            }

            ChannelInterruptFlagsGrid.Children.Clear();
            interrupts3.Clear();
            string[] channel_interrupt_strings = SafeTryFindResource("channel_interrupt_strings").Split('|');
            foreach (string channelInterruptString in channel_interrupt_strings)
            {
                ThreadSafeCheckBox box = new ThreadSafeCheckBox
                {
                    Content = channelInterruptString, Margin = new Thickness(0, 5, 0, 0)
                };

                ChannelInterruptFlagsGrid.Children.Add(box);
                interrupts3.Add(box);
            }

            if (WoWVersionManager.IsTbcOrGreaterSelected)
            {
                S1.Content = SafeTryFindResource("checkboxPhysical");
                S2.Content = SafeTryFindResource("checkboxHoly");
                S3.Content = SafeTryFindResource("checkboxFire");
                S4.Content = SafeTryFindResource("checkboxNature");
                S5.Content = SafeTryFindResource("checkboxFrost");
                S6.Content = SafeTryFindResource("checkboxShadow");
                S7.Content = SafeTryFindResource("checkboxArcane");
            }
            else
            {
                S1.Content = SafeTryFindResource("checkboxHoly");
                S2.Content = SafeTryFindResource("checkboxFire");
                S3.Content = SafeTryFindResource("checkboxNature");
                S4.Content = SafeTryFindResource("checkboxFrost");
                S5.Content = SafeTryFindResource("checkboxShadow");
                S6.Content = SafeTryFindResource("checkboxArcane");
                S7.Content = "Unused";
            }
            S7.IsEnabled = WoWVersionManager.IsTbcOrGreaterSelected;

            SpellFamilyName.Items.Clear();
            SpellFamilyName.ItemsSource = SpellFamilyNames.familyNamesitemSource;
            SpellFamilyName.SelectedIndex = 0;
        }

        #endregion

        #region Loaded
        private void _Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;

            try
            {
                var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
                Title = $"{Title} - {WoWVersionManager.GetInstance().SelectedVersion().Version} - V{version.Substring(0, version.Length - 2)}";

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
                    uint mask = 1u << i;

                    SpellMask11.Items.Add(new ThreadSafeCheckBox { Content = "0x" + mask.ToString("x8") });
                    SpellMask12.Items.Add(new ThreadSafeCheckBox { Content = "0x" + mask.ToString("x8") });
                    SpellMask13.Items.Add(new ThreadSafeCheckBox { Content = "0x" + mask.ToString("x8") });
                    SpellMask21.Items.Add(new ThreadSafeCheckBox { Content = "0x" + mask.ToString("x8") });
                    SpellMask22.Items.Add(new ThreadSafeCheckBox { Content = "0x" + mask.ToString("x8") });
                    SpellMask23.Items.Add(new ThreadSafeCheckBox { Content = "0x" + mask.ToString("x8") });
                    SpellMask31.Items.Add(new ThreadSafeCheckBox { Content = "0x" + mask.ToString("x8") });
                    SpellMask32.Items.Add(new ThreadSafeCheckBox { Content = "0x" + mask.ToString("x8") });
                    SpellMask33.Items.Add(new ThreadSafeCheckBox { Content = "0x" + mask.ToString("x8") });
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

                LoadAllData();

                FilterSpellEffectCombo.SelectionChanged += FilterSpellEffectCombo_SelectionChanged;
                FilterAuraCombo.SelectionChanged += FilterAuraCombo_SelectionChanged;
                FilterSpellTargetA.SelectionChanged += FilterSpellTargetA_SelectionChanged;
                FilterSpellTargetB.SelectionChanged += FilterSpellTargetB_SelectionChanged;
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
                if (cb.IsChecked == true)
                    Mask |= (1u << (int)i);
            }
            father.Text = Mask.ToString();

            SpellMask_SelectionChanged(father, null);
        }

        private void RefreshMaskComboboxText(ThreadSafeComboBox combobox)
        {
           uint Mask = 0;
            List<string> item_names = new List<string> {};
            foreach (ThreadSafeCheckBox cb in combobox.Items)
            // foreach (var item in father.Items)
            {
                // if (item is Grid grid && grid.Children[0] is ThreadSafeCheckBox cb)
                {
                    // note : index must start at 0
                    // if (cb.Tag is uint index)

                    string cb_text = (string)cb.Content;
                    uint index = (uint)GetNumberPrefixFromText(cb_text);
                    {
                        // Mask += cb.IsChecked == true ? (uint)Math.Pow(2, index) : 0;
                        if (cb.IsChecked == true)
                            Mask |= (1u << ((int)index - 1));

                        if (cb.IsChecked == true)
                        {
                            // remove the Id prefix
                            string result = Regex.Replace(cb_text, @"^\s*\d+\s*-\s*", "");
                            // Trim ending spaces
                            result = result.TrimEnd();
                            item_names.Add(result);
                        }

                    }
                    // else
                    //     throw new Exception($"error with checkbox tag value.");
                }

            }
            string controlText = Mask.ToString();

            if (item_names.Count > 0)
            {
                controlText += " - ";
                foreach (string item in item_names)
                {
                    controlText += item + ", ";
                }
                
                controlText = controlText.TrimEnd(); // remove spaces
                controlText = controlText.Substring(0, controlText.Length - 1);// remove last "," char, lazy way
            }

            combobox.Text = controlText;
        }

        private void HandMaskCombobox_Checked(object obj, RoutedEventArgs e)
        {
            ThreadSafeComboBox father = (ThreadSafeComboBox)((ThreadSafeCheckBox)obj).Parent; // old without grid

            // ThreadSafeCheckBox checkbox = (ThreadSafeCheckBox)obj;
            // Grid containerGrid = (Grid)checkbox.Parent;
            // // note : only works if there's only one parent
            // ThreadSafeComboBox father = (ThreadSafeComboBox)LogicalTreeHelper.GetParent(containerGrid);

            RefreshMaskComboboxText(father);
        }

        #endregion

        public delegate void UpdateProgressFunc(double value, int taskIdOverride = 0);
        public delegate void UpdateTextFunc(string value);

        private ImportExportWindow _ImportExportWindow;

        #region ImportExportSpellDBC
        private void ImportExportSpellDbcButton(object sender, RoutedEventArgs e)
        {
            if (_ImportExportWindow != null && _ImportExportWindow.IsVisible)
            {
                _ImportExportWindow.Show();
                return;
            }
            var window = new ImportExportWindow(adapter, PopulateSelectSpell, LoadAllRequiredDbcs);
            window.Show();
            window.Height += 40;
            window.Width /= 2;
            _ImportExportWindow = window;
        }

        #endregion

        #region ConfigButton
        private ConfigWindow ConfigWindowInstance;

        private void ConfigButtonClick(object sender, RoutedEventArgs e)
        {
            if (ConfigWindowInstance != null && ConfigWindowInstance.IsLoaded)
            {
                ConfigWindowInstance.Activate();
                return;
            }
            var window = new ConfigWindow(adapter is MySQL ? 
                ConfigWindow.DatabaseIdentifier.MySQL : ConfigWindow.DatabaseIdentifier.SQLite);
            // window.ShowDialog();
            window.Show();
            window.Width *= 0.7;
            window.Height *= 0.8;
            ConfigWindowInstance = window;
        }
        #endregion

        #region InitialiseMemberVariables
        private void LoadAllRequiredDbcs(Action<string> setMessage)
        {
            // Load required DBC's. First the ones with dependencies and inject them into the manager
            var manager = DBCManager.GetInstance();
            var tasks = manager.LoadRequiredDbcs();
            var lang = GetLanguage();

            if (WoWVersionManager.IsWotlkOrGreaterSelected)
            {
                tasks.Add(Task.Run(() =>
                    manager.InjectLoadedDbc("SpellDifficulty", new SpellDifficulty(adapter, lang))));
            }
            
            tasks.Add(Task.Run(() =>
                manager.InjectLoadedDbc("SpellIcon", new SpellIconDBC(this, adapter))));

            var incompleteCount = tasks.Where(t => !t.IsCompleted).Count();
            while (incompleteCount > 0)
            {
                setMessage?.Invoke("Remaining DBCs: " + incompleteCount);
                Thread.Sleep(50);
                incompleteCount = tasks.Where(t => !t.IsCompleted).Count();
            }

            setMessage?.Invoke("Loading dependent DBCs...");

            if (WoWVersionManager.IsWotlkOrGreaterSelected)
            {
                // Has a dependency on the earlier tasks completing
                manager.InjectLoadedDbc("AreaGroup", new AreaGroup(((AreaTable)manager.FindDbcForBinding("AreaTable")).Lookups));
            }

            Task.Run(() =>
            {
                spellFamilyClassMaskParser = new SpellFamilyClassMaskParser(GetDBAdapter());
            });
        }

        private async void LoadAllData()
        {
            await GetConfig();
            if (!Config.IsInit)
            {
                await this.ShowMessageAsync(SafeTryFindResource("ERROR"), SafeTryFindResource("String2"));
                return;
            }
            var controller = await this.ShowProgressAsync(SafeTryFindResource("PleaseWait"), SafeTryFindResource("PleaseWait_2"));
            controller.SetCancelable(false);

            var taskList = new List<Task>();
            bool shouldReturn = false;

            controller.SetMessage("Loading SQL connection...");
            await Task.Run(() =>
            {
                try
                {
                    adapter = AdapterFactory.Instance.GetAdapter(true);
                }
                catch (Exception e)
                {
                    Logger.Error("ERROR: " + e.Message + "\n" + e.InnerException?.Message + "\n" + e);
                    Dispatcher.InvokeAsync(() =>
                    {
                        controller.CloseAsync();
                        this.ShowMessageAsync(SafeTryFindResource("ERROR"),
                           $"{SafeTryFindResource("Input_MySQL_Error")}\n{e.Message + "\n" + e.InnerException?.Message}");
                        shouldReturn = true;
                    });
                }
            });

            if (shouldReturn)
                return;

            controller.SetMessage("Loading DBC data...");
            // Creating all MySQL tables from bindings
            //  We actually don't need to block the UI on this completing
            var createTask = Task.Run(() =>
            {
                try
                {
                    using (var innerAdapter = AdapterFactory.Instance.GetAdapter(false))
                        innerAdapter.CreateAllTablesFromBindings();
                }
                catch (Exception e)
                {
                    Logger.Error("ERROR: " + e.Message + "\n" + e.InnerException?.Message + "\n" + e);
                    Dispatcher.InvokeAsync(() =>
                    {
                        controller?.CloseAsync();
                        this.ShowMessageAsync(SafeTryFindResource("ERROR"),
                           $"{SafeTryFindResource("Input_MySQL_Error")}\n{e.Message + "\n" + e.InnerException?.Message}");
                    });
                }
            });

            // Load DBC data
            await Task.Run(() =>
            {
                try
                {
                    LoadAllRequiredDbcs(controller.SetMessage);
                }
                catch (MySqlException e)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        controller.CloseAsync();
                        this.ShowMessageAsync(SafeTryFindResource("ERROR"),
                            $"{SafeTryFindResource("LoadDBCFromBinding_Error_1")}: {e.Message}\n{e.StackTrace}\n{e.InnerException}");
                    });
                    shouldReturn = true;
                }
                catch (SQLiteException e)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        controller.CloseAsync();
                        this.ShowMessageAsync(SafeTryFindResource("ERROR"),
                            $"{SafeTryFindResource("LoadDBCFromBinding_Error_1")}: {e.Message}\n{e.StackTrace}\n{e.InnerException}");
                    });
                    shouldReturn = true;
                }
                catch (Exception e)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        controller.CloseAsync();
                        this.ShowMessageAsync(SafeTryFindResource("ERROR"),
                            $"{SafeTryFindResource("LoadDBCFromBinding_Error_1")}: {e.Message}\n{e.StackTrace}\n{e.InnerException}");
                    });
                    shouldReturn = true;
                }
            });

            if (shouldReturn)
                return;

            controller.SetMessage("Loading DBC UI data...");
            DBCManager.GetInstance().LoadGraphicUserInterface();

            controller.SetMessage("Loading content UI data...");
            try
            {
                // Initialise select spell list
                SelectSpell.SetAdapter(GetDBAdapter())
                    .SetLanguage(GetLanguage())
                    .Initialise();

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


                var shapeshift_labels = ConvertBoxListToLabels(((SpellShapeshiftForm)
                    DBCManager.GetInstance().FindDbcForBinding("SpellShapeshiftForm")).GetAllBoxes());
                foreach (var stance in shapeshift_labels)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox
                    {
                        Content = stance.ToString(),
                        ToolTip = stance.ToString(),
                        Margin = new Thickness(0, 5, 0, 0)
                    };

                    StancesGrid.Children.Add(box);
                    stancesBoxes.Add(box);
                }

                var creature_type_labels = ConvertBoxListToLabels(((CreatureType)
                        DBCManager.GetInstance().FindDbcForBinding("CreatureType")).GetAllBoxes());
                foreach (var creatureType in creature_type_labels)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox
                    {
                        Content = creatureType,
                        ToolTip = creatureType,
                        Margin = new Thickness(0, 5, 0, 0)
                    };

                    TargetCreatureType.Children.Add(box);
                    targetCreatureTypeBoxes.Add(box);
                }


                var mechanic_names_labels = ConvertBoxListToLabels(((SpellMechanic)
                    DBCManager.GetInstance().FindDbcForBinding("SpellMechanic")).GetAllBoxes());
                foreach (var mechanicType in mechanic_names_labels)
                {
                    Mechanic1.Items.Add(mechanicType.Content);
                    Mechanic2.Items.Add(mechanicType.Content);
                    Mechanic3.Items.Add(mechanicType.Content);
                }

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

                VisualSettingsGrid.ContextMenu = new ListContextMenu((item, args) => PasteVisualKitAction(), true);
                VisualEffectsListGrid.ContextMenu = new ListContextMenu((item, args) => PasteVisualEffectAction(), true);
                InitialiseSpellVisualEffectList();

                prepareIconEditor();
            }
            catch (Exception e)
            {
                await controller.CloseAsync();
                await this.ShowMessageAsync(SafeTryFindResource("ERROR"),
                    $"{SafeTryFindResource("LoadDBCFromBinding_Error_1")}\n\n{e}\n{e.InnerException}");
                return;
            }

            /*if (!createTask.IsCompleted)
            {
                controller.SetMessage("Waiting for table creation to finish...");
                await createTask;
            }*/
            await controller.CloseAsync();
            PopulateSelectSpell();
        }

        private void FocusLanguage()
        {
            switch ((LocaleConstant)(GetLanguage() - 1))
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
        }

        private List<Label> ConvertBoxListToLabels(List<DBCBoxContainer> boxes) => boxes.Select(entry => entry.ItemLabel()).ToList();

        private List<Label> ConvertBoxListToLabelsWithIds(List<DBCBoxContainer> boxes) => boxes.Select(entry => entry.ItemLabelWithId()).ToList();

        private async Task GetConfig()
        {
            if (!Config.IsInit)
            {
                var settings = new MetroDialogSettings
                {
                    AffirmativeButtonText = "SQLite",
                    NegativeButtonText = "MySQL",
                    AnimateHide = true,
                    AnimateShow = true,
                    ColorScheme = MetroDialogColorScheme.Accented
                };
                MessageDialogResult exitCode = await this.ShowMessageAsync(SafeTryFindResource("SpellEditor"),
                    SafeTryFindResource("Welcome"),
                    MessageDialogStyle.AffirmativeAndNegative, settings);
                bool isSqlite = exitCode == MessageDialogResult.Affirmative;

                if (!isSqlite)
                {
                    if (Config.NeedInitMysql)
                    {
                        string host = await this.ShowInputAsync(SafeTryFindResource("Input_MySQL_Details"), SafeTryFindResource("Input_MySQL_Details_1"));
                        string user = await this.ShowInputAsync(SafeTryFindResource("Input_MySQL_Details"), SafeTryFindResource("Input_MySQL_Details_2"));
                        string pass = await this.ShowInputAsync(SafeTryFindResource("Input_MySQL_Details"), SafeTryFindResource("Input_MySQL_Details_3"));
                        string port = await this.ShowInputAsync(SafeTryFindResource("Input_MySQL_Details"), SafeTryFindResource("Input_MySQL_Details_4"));
                        string db = await this.ShowInputAsync(SafeTryFindResource("Input_MySQL_Details"), SafeTryFindResource("Input_MySQL_Details_5"));
                        
                        if (host == null || user == null || pass == null || port == null || db == null ||
                            host.Length == 0 || user.Length == 0 || port.Length == 0 || db.Length == 0 ||
                            !uint.TryParse(port, out var result))
                        {
                            throw new Exception(SafeTryFindResource("Input_MySQL_Error_2"));
                        }

                        Config.Host = host;
                        Config.User = user;
                        Config.Pass = pass;
                        Config.Port = port;
                        Config.Database = db;
                    }
                }
                Config.connectionType = isSqlite ? Config.ConnectionType.SQLite : Config.ConnectionType.MySQL;
                Config.IsInit = true;
            }
        }
        #endregion

        #region KeyHandlers
        private volatile bool imageLoadEventRunning;

        private void _KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back && (
                sender == FilterSpellNames || 
                sender == FilterIcons ||
                sender == Attributes1Search ||
                sender == Attributes2Search))
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
                    MetroDialogSettings settings = new MetroDialogSettings
                    {
                        AffirmativeButtonText = SafeTryFindResource("Yes"),
                        NegativeButtonText = SafeTryFindResource("No")
                    };


                    MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
                    MessageDialogResult exitCode = await this.ShowMessageAsync(SafeTryFindResource("SpellEditor"), SafeTryFindResource("Exit"), style, settings);

                    if (exitCode == MessageDialogResult.Affirmative)
                    {
                        Environment.Exit(0x1);
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
                            if (item is TextBlock tb)
                            {
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
                //var locale = GetLocale();
                var input = FilterSpellNames.Text.ToLower();
                var badInput = string.IsNullOrEmpty(input);
                if (badInput && SelectSpell.GetLoadedRowCount() == SelectSpell.Items.Count)
                {
                    imageLoadEventRunning = false;
                    return;
                }

                ICollectionView view = CollectionViewSource.GetDefaultView(SelectSpell.Items);
                view.Filter = o =>
                {
                    var panel = (StackPanel) o;
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
            else if (sender == FilterIcons)
            {
                var input = FilterIcons.Text.ToLower();
                foreach (System.Windows.Controls.Image image in IconGrid.Children)
                {
                    var name = image.ToolTip.ToString().ToLower();
                    image.Visibility = name.Contains(input) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            else if (sender == Attributes1Search || sender == Attributes2Search)
            {
                var input = sender == Attributes1Search ? 
                    Attributes1Search.Text.ToLower() : 
                    Attributes2Search.Text.ToLower();
                var controls = sender == Attributes1Search ? 
                    new StackPanel[] { Attributes1, Attributes2, Attributes3, Attributes4 } :
                    new StackPanel[] { Attributes5, Attributes6, Attributes7, Attributes8 };

                for (int i = 0; i < controls.Length; ++i)
                {
                    foreach (ThreadSafeCheckBox box in controls[i].Children)
                    {
                        box.Visibility = box.Content.ToString().Length <= 0 || box.Content.ToString().ToLower().Contains(input) ?
                            Visibility.Visible :
                            Visibility.Collapsed;
                    }
                }
            }
        }
        #endregion

        #region ButtonClicks (and load spell god-function)
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (adapter == null)
            {
                LoadAllData();
                return;
            }
            
            if (sender == TruncateTable)
            {
                MetroDialogSettings settings = new MetroDialogSettings
                {
                    AffirmativeButtonText = SafeTryFindResource("Yes"),
                    NegativeButtonText = SafeTryFindResource("No")
                };

                MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
                var res = await this.ShowMessageAsync(SafeTryFindResource("TruncateTable1"), SafeTryFindResource("TruncateTable2"), style, settings);
                if (res == MessageDialogResult.Affirmative)
                {
                    foreach (var binding in BindingManager.GetInstance().GetAllBindings())
                        adapter.Execute($"drop table `{binding.Name.ToLower()}`");
                    adapter.CreateAllTablesFromBindings();
                    selectedID = 0;
                    PopulateSelectSpell();
                }
                return;
            }

            if (sender == InsertANewRecord)
            {
                MetroDialogSettings settings = new MetroDialogSettings
                {
                    AffirmativeButtonText = SafeTryFindResource("Yes"),
                    NegativeButtonText = SafeTryFindResource("No")
                };


                MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
                MessageDialogResult copySpell = await this.ShowMessageAsync(SafeTryFindResource("SpellEditor"), SafeTryFindResource("CopySpellRecord1"), style, settings);

                uint oldIDIndex = uint.MaxValue;

                if (copySpell == MessageDialogResult.Affirmative)
                {
                    string inputCopySpell = await this.ShowInputAsync(SafeTryFindResource("SpellEditor"), SafeTryFindResource("CopySpellRecord2"));
                    if (inputCopySpell == null) { return; }

                    if (!uint.TryParse(inputCopySpell, out var oldID))
                    {
                        HandleErrorMessage(SafeTryFindResource("CopySpellRecord3"));
                        return;
                    }
                    oldIDIndex = oldID;
                }

                string inputNewRecord = await this.ShowInputAsync(SafeTryFindResource("SpellEditor"), SafeTryFindResource("CopySpellRecord4"));
                if (inputNewRecord == null) { return; }

                if (!uint.TryParse(inputNewRecord, out var newID))
                {
                    HandleErrorMessage(SafeTryFindResource("CopySpellRecord5"));
                    return;
                }

                if (uint.Parse(adapter.Query($"SELECT COUNT(*) FROM `spell` WHERE `ID` = '{newID}'").Rows[0][0].ToString()) > 0)
                {
                    HandleErrorMessage(SafeTryFindResource("CopySpellRecord6"));
                    return;
                }

                if (oldIDIndex != uint.MaxValue)
                {
                    // Copy old spell to new spell
                    SelectSpell.AddNewSpell(oldIDIndex, newID);
                }
                else
                {
                    // Create new spell
                    HandleErrorMessage(SafeTryFindResource("CopySpellRecord7"));
                    return;
                }

                ShowFlyoutMessage(string.Format(SafeTryFindResource("CopySpellRecord8"), inputNewRecord));
                return;
            }

            if (sender == DeleteARecord)
            {
                string input = await this.ShowInputAsync(SafeTryFindResource("SpellEditor"), SafeTryFindResource("DeleteSpellRecord1"));

                if (input == null) { return; }

                if (!uint.TryParse(input, out var spellID))
                {
                    HandleErrorMessage(SafeTryFindResource("DeleteSpellRecord2"));
                    return;
                }

                SelectSpell.DeleteSpell(spellID);
                
                selectedID = 0;

                ShowFlyoutMessage(SafeTryFindResource("DeleteSpellRecord3"));
                return;
            }

            if (sender == SaveSpellChanges)
            {
                string query = $"SELECT * FROM `spell` WHERE `ID` = '{selectedID}' LIMIT 1";
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

                    foreach (ThreadSafeCheckBox attribute0 in attributes0)
                    {
                        if (attribute0.IsChecked.Value) { maskk += flagg; }
                        flagg += flagg;
                    }

                    row["Attributes"] = maskk;

                    maskk = 0;
                    flagg = 1;

                    foreach (ThreadSafeCheckBox attribute1 in attributes1)
                    {
                        if (attribute1.IsChecked.Value) { maskk += flagg; }
                        flagg += flagg;
                    }

                    row["AttributesEx"] = maskk;

                    maskk = 0;
                    flagg = 1;

                    foreach (ThreadSafeCheckBox attribute2 in attributes2)
                    {
                        if (attribute2.IsChecked.Value) { maskk += flagg; }
                        flagg += flagg;
                    }

                    row["AttributesEx2"] = maskk;


                    maskk = 0;
                    flagg = 1;

                    foreach (ThreadSafeCheckBox attribute3 in attributes3)
                    {
                        if (attribute3.IsChecked.Value) { maskk += flagg; }
                        flagg += flagg;
                    }

                    row["AttributesEx3"] = maskk;

                    maskk = 0;
                    flagg = 1;

                    foreach (ThreadSafeCheckBox attribute4 in attributes4)
                    {
                        if (attribute4.IsChecked.Value) { maskk += flagg; }

                        flagg += flagg;
                    }

                    row["AttributesEx4"] = maskk;

                    maskk = 0;
                    flagg = 1;

                    if (isTbcOrGreater)
                    {
                        foreach (ThreadSafeCheckBox attribute5 in attributes5)
                        {
                            if (attribute5.IsChecked.Value) { maskk += flagg; }

                            flagg += flagg;
                        }

                        row["AttributesEx5"] = maskk;

                        maskk = 0;
                        flagg = 1;

                        foreach (ThreadSafeCheckBox attribute6 in attributes6)
                        {
                            if (attribute6.IsChecked.Value) { maskk += flagg; }

                            flagg += flagg;
                        }

                        row["AttributesEx6"] = maskk;

                        if (stancesBoxes[0].IsChecked.Value)
                        {
                            row["Stances"] = 0;
                        }
                        else
                        {
                            uint mask = 0;
                            uint flag = 1;

                            for (int f = 1; f < stancesBoxes.Count; ++f)
                            {
                                if (stancesBoxes[f].IsChecked.Value) { mask += flag; }

                                flag += flag;
                            }

                            row["Stances"] = mask;
                        }
                    }

                    if (isWotlkOrGreater)
                    {
                        maskk = 0;
                        flagg = 1;

                        foreach (ThreadSafeCheckBox attribute7 in attributes7)
                        {
                            if (attribute7.IsChecked.Value) { maskk += flagg; }

                            flagg += flagg;
                        }

                        row["AttributesEx7"] = maskk;
                    }

                    if (targetBoxes[0].IsChecked.Value)
                    {
                        row["Targets"] = 0;
                    }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 1; f < targetBoxes.Count; ++f)
                        {
                            if (targetBoxes[f].IsChecked.Value) { mask += flag; }

                            flag += flag;
                        }

                        row["Targets"] = mask;
                    }

                    if (targetCreatureTypeBoxes[0].IsChecked.Value)
                    {
                        row["TargetCreatureType"] = 0;
                    }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 1; f < targetCreatureTypeBoxes.Count; ++f)
                        {
                            if (targetCreatureTypeBoxes[f].IsChecked.Value) { mask += flag; }
                            flag += flag;
                        }

                        row["TargetCreatureType"] = mask;
                    }

                    if (isTbcOrGreater)
                    {
                        row["FacingCasterFlags"] = FacingFrontFlag.IsChecked.Value ? (uint)0x1 : (uint)0x0;

                        switch (CasterAuraState.SelectedIndex)
                        {
                            case 0: // None
                                row["CasterAuraState"] = 0;
                                break;
                            case 1: // Defense
                                row["CasterAuraState"] = 1;
                                break;
                            case 2: // Healthless 20%
                                row["CasterAuraState"] = 2;
                                break;
                            case 3: // Berserking
                                row["CasterAuraState"] = 3;
                                break;
                            case 4: // Judgement
                                row["CasterAuraState"] = 5;
                                break;
                            case 5: // Hunter Parry
                                row["CasterAuraState"] = 7;
                                break;
                            case 6: // Victory Rush
                                row["CasterAuraState"] = 10;
                                break;
                            case 7: // Unknown 1
                                row["CasterAuraState"] = 11;
                                break;
                            case 8: // Healthless 35%
                                row["CasterAuraState"] = 13;
                                break;
                            case 9: // Enrage
                                row["CasterAuraState"] = 17;
                                break;
                            case 10: // Unknown 2
                                row["CasterAuraState"] = 22;
                                break;
                            case 11: // Health Above 75%
                                row["CasterAuraState"] = 23;
                                break;
                        }
                    }

                    switch (TargetAuraState.SelectedIndex)
                    {
                        case 0: // None
                            row["TargetAuraState"] = 0;
                            break;
                        case 1: // Healthless 20%
                            row["TargetAuraState"] = 2;
                            break;
                        case 2: // Berserking
                            row["TargetAuraState"] = 3;
                            break;
                        case 3: // Healthless 35%
                            row["TargetAuraState"] = 13;
                            break;
                        case 4: // Conflagrate
                            row["TargetAuraState"] = 14;
                            break;
                        case 5: // Swiftmend
                            row["TargetAuraState"] = 15;
                            break;
                        case 6: // Deadly Poison
                            row["TargetAuraState"] = 16;
                            break;
                        case 7: // Bleeding
                            row["TargetAuraState"] = 18;
                            break;
                    }

                    row["RecoveryTime"] = uint.Parse(RecoveryTime.Text);
                    row["CategoryRecoveryTime"] = uint.Parse(CategoryRecoveryTime.Text);

                    if (interrupts1[0].IsChecked.Value)
                    {
                        row["InterruptFlags"] = 0;
                    }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 1; f < interrupts1.Count; ++f)
                        {
                            if (interrupts1[f].IsChecked.Value) { mask += flag; }

                            flag += flag;
                        }

                        row["InterruptFlags"] = mask;
                    }

                    if (interrupts2[0].IsChecked.Value)
                    {
                        row["AuraInterruptFlags"] = 0;
                    }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 1; f < interrupts2.Count; ++f)
                        {
                            if (interrupts2[f].IsChecked.Value) { mask += flag; }

                            flag += flag;
                        }

                        row["AuraInterruptFlags"] = mask;
                    }

                    if (interrupts3[0].IsChecked.Value)
                    {
                        row["ChannelInterruptFlags"] = 0;
                    }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 1; f < interrupts3.Count; ++f)
                        {
                            if (interrupts3[f].IsChecked.Value) { mask += flag; }

                            flag += flag;
                        }

                        row["ChannelInterruptFlags"] = mask;
                    }

                    if (procBoxes[0].IsChecked.Value)
                    {
                        row["ProcFlags"] = 0;
                    }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 1; f < procBoxes.Count; ++f)
                        {
                            if (procBoxes[f].IsChecked.Value) { mask += flag; }

                            flag += flag;
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
                    row["SpellPriority"] = int.Parse(SpellPriority.Text);
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

                    if (equippedItemInventoryTypeMaskBoxes[0].IsChecked.Value)
                    {
                        row["EquippedItemInventoryTypeMask"] = 0;
                    }
                    else
                    {
                        uint mask = 0;
                        uint flag = 1;

                        for (int f = 0; f < equippedItemInventoryTypeMaskBoxes.Count; ++f)
                        {
                            if (equippedItemInventoryTypeMaskBoxes[f].IsChecked.Value) { mask += flag; }

                            flag += flag;
                        }

                        row["EquippedItemInventoryTypeMask"] = (int)mask;
                    }

                    if (EquippedItemClass.Text == SafeTryFindResource("None"))
                    {
                        row["EquippedItemClass"] = -1;
                        row["EquippedItemSubClassMask"] = 0;
                    }
                    else
                    {
                        uint Mask = 0;
                        for (int i = 0; i < equippedItemSubClassMaskBoxes.Count; i++)
                        {
                            if (equippedItemSubClassMaskBoxes[i].IsChecked == true)
                                Mask |= (1u << i);
                        }
                        row["EquippedItemSubClassMask"] = Mask;
                    }
                    
                    row["Effect1"] = SpellEffect1.GetNumberPrefixFromText();
                    row["Effect2"] = SpellEffect2.GetNumberPrefixFromText();
                    row["Effect3"] = SpellEffect3.GetNumberPrefixFromText();
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
                    row["EffectApplyAuraName1"] = ApplyAuraName1.GetNumberPrefixFromText();
                    row["EffectApplyAuraName2"] = ApplyAuraName2.GetNumberPrefixFromText();
                    row["EffectApplyAuraName3"] = ApplyAuraName3.GetNumberPrefixFromText();
                    row["EffectAmplitude1"] = uint.Parse(Amplitude1.Text);
                    row["EffectAmplitude2"] = uint.Parse(Amplitude2.Text);
                    row["EffectAmplitude3"] = uint.Parse(Amplitude3.Text);
                    row["EffectMultipleValue1"] = float.Parse(MultipleValue1.Text);
                    row["EffectMultipleValue2"] = float.Parse(MultipleValue2.Text);
                    row["EffectMultipleValue3"] = float.Parse(MultipleValue3.Text);
                    row["EffectChainTarget1"] = uint.Parse(ChainTarget1.Text);
                    row["EffectChainTarget2"] = uint.Parse(ChainTarget2.Text);
                    row["EffectChainTarget3"] = uint.Parse(ChainTarget3.Text);
                    row["EffectItemType1"] = uint.Parse(ItemType1.Text);
                    row["EffectItemType2"] = uint.Parse(ItemType2.Text);
                    row["EffectItemType3"] = uint.Parse(ItemType3.Text);

                    row["EffectMiscValue1"] = GetMiscValue(1, 1);
                    row["EffectMiscValue2"] = GetMiscValue(2, 1);
                    row["EffectMiscValue3"] = GetMiscValue(3, 1);

                    if (isTbcOrGreater) // miscvalueB
                    {
                        row["EffectMiscValueB1"] = GetMiscValue(1, 2);
                        row["EffectMiscValueB2"] = GetMiscValue(2, 2);
                        row["EffectMiscValueB3"] = GetMiscValue(3, 2);
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
                    row["SpellVisual1"] = uint.Parse(SpellVisual1.Text);
                    row["SpellVisual2"] = uint.Parse(SpellVisual2.Text);
                    row["ManaCostPercentage"] = uint.Parse(ManaCostPercent.Text);
                    row["StartRecoveryCategory"] = uint.Parse(StartRecoveryCategory.Text);
                    row["StartRecoveryTime"] = uint.Parse(StartRecoveryTime.Text);
                    row["MaximumTargetLevel"] = uint.Parse(MaxTargetsLevel.Text);

                    // Before WOTLK there are only two flags, we misnamed them in WOTLK as the last flag handles A3/B3/C3 of the affecting spells
                    // row["SpellFamilyName"] = uint.Parse(SpellFamilyName.Text);
                    // row["SpellFamilyName"] = (uint)GetNumberPrefixFromText(SpellFamilyName.Text);
                    row["SpellFamilyName"] = SpellFamilyName.GetNumberPrefixFromText();
                    // or
                    // row["SpellFamilyName"] = (uint)((ComboBoxItem)SpellFamilyName.SelectedItem).Tag;
                    if (!isWotlkOrGreater)
                    {
                        row["SpellFamilyFlags1"] = uint.Parse(SpellFamilyFlags.Text);
                        row["SpellFamilyFlags2"] = uint.Parse(SpellFamilyFlags1.Text);
                    }
                    else
                    {
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

                    uint schoolMask = 0;
                    if (isTbcOrGreater)
                    {
                        schoolMask = (S1.IsChecked.Value ? 0x01 : (uint)0x00) +
                            (S2.IsChecked.Value ? 0x02 : (uint)0x00) +
                            (S3.IsChecked.Value ? 0x04 : (uint)0x00) +
                            (S4.IsChecked.Value ? 0x08 : (uint)0x00) +
                            (S5.IsChecked.Value ? 0x10 : (uint)0x00) +
                            (S6.IsChecked.Value ? 0x20 : (uint)0x00) +
                            (S7.IsChecked.Value ? 0x40 : (uint)0x00);
                    }
                    else
                    {
                        var schoolMaskBoxes = new List<CheckBox>();
                        schoolMaskBoxes.Add(S1);
                        schoolMaskBoxes.Add(S2);
                        schoolMaskBoxes.Add(S3);
                        schoolMaskBoxes.Add(S4);
                        schoolMaskBoxes.Add(S5);
                        schoolMaskBoxes.Add(S6);
                        var checkedMaskCount = schoolMaskBoxes.Where((box) => box.IsChecked.HasValue && box.IsChecked.Value).Count();
                        if (checkedMaskCount > 1)
                        {
                            throw new Exception($"A maximum of 1 damage school can be selected, you have selected { checkedMaskCount }.");
                        }
                        if (checkedMaskCount > 0)
                        {
                            if (S1.IsChecked.HasValue && S1.IsChecked.Value)
                                schoolMask = 1;
                            else if (S2.IsChecked.HasValue && S2.IsChecked.Value)
                                schoolMask = 2;
                            else if (S3.IsChecked.HasValue && S3.IsChecked.Value)
                                schoolMask = 3;
                            else if (S4.IsChecked.HasValue && S4.IsChecked.Value)
                                schoolMask = 4;
                            else if (S5.IsChecked.HasValue && S5.IsChecked.Value)
                                schoolMask = 5;
                            else if (S6.IsChecked.HasValue && S6.IsChecked.Value)
                                schoolMask = 6;
                        }
                    }
                    row["SchoolMask"] = schoolMask;

                    if (isWotlkOrGreater)
                    {
                        row["SpellMissileID"] = uint.Parse(SpellMissileID.Text);
                        row["EffectBonusMultiplier1"] = float.Parse(EffectBonusMultiplier1.Text);
                        row["EffectBonusMultiplier2"] = float.Parse(EffectBonusMultiplier2.Text);
                        row["EffectBonusMultiplier3"] = float.Parse(EffectBonusMultiplier3.Text);
                    }

                    var numColumns = WoWVersionManager.GetInstance().SelectedVersion().NumLocales;
                    ThreadSafeTextBox[] boxes = stringObjectMap.Values.ToArray();
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

                    SelectSpell.UpdateSpell(row);
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
                MetroDialogSettings settings = new MetroDialogSettings
                {
                    AffirmativeButtonText = SafeTryFindResource("Yes"),
                    NegativeButtonText = SafeTryFindResource("No")
                };


                MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
                MessageDialogResult spellOrActive = await this.ShowMessageAsync(SafeTryFindResource("SpellEditor"), SafeTryFindResource("SaveIcon"), style, settings);

                string column = null;
                if (spellOrActive == MessageDialogResult.Affirmative)
                    column = "SpellIconID";
                else if (spellOrActive == MessageDialogResult.Negative)
                    column = "ActiveIconID";
                adapter.Execute($"UPDATE `{"spell"}` SET `{column}` = '{newIconID}' WHERE `ID` = '{selectedID}'");
                return;
            }

            if (sender == ResetSpellIconID)
            {
                adapter.Execute($"UPDATE `{"spell"}` SET `{"SpellIconID"}` = '{1}' WHERE `ID` = '{selectedID}'");
                return;
            }
            if (sender == ResetActiveIconID)
            {
                adapter.Execute($"UPDATE `{"spell"}` SET `{"ActiveIconID"}` = '{0}' WHERE `ID` = '{selectedID}'");
            }
        }

        #endregion

        #region Utilities
        public void ShowFlyoutMessage(string message)
        {
            Flyout.IsOpen = true;
            FlyoutText.Text = message;
        }

        public static T DeepCopy<T>(T obj)
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

        private void prepareIconEditor()
        {
            var loadIcons = (SpellIconDBC)DBCManager.GetInstance().FindDbcForBinding("SpellIcon");
            loadIcons.LoadImages(64);
            loadIcons.updateIconSize(64, new Thickness(16, 0, 0, 0));
        }

        #endregion

        #region PopulateSelectSpell

        private void PopulateSelectSpell()
        {
            if (!SelectSpell.IsInitialised())
            {
                SelectSpell.SetAdapter(GetDBAdapter())
                    .SetLanguage(GetLanguage())
                    .Initialise();
            }
            if (!SelectSpell.HasAdapter())
            {
                SelectSpell.SetAdapter(GetDBAdapter());
            }
            SelectSpell.PopulateSelectSpell();
            FocusLanguage();
        }
        #endregion

        #region NewIconClick & UpdateMainWindow
        private async void NewIconClick(object sender, RoutedEventArgs e)
        {
            if (adapter == null) { return; }

            MetroDialogSettings settings = new MetroDialogSettings
            {
                AffirmativeButtonText = SafeTryFindResource("SpellIconID"),
                NegativeButtonText = SafeTryFindResource("ActiveIconID")
            };

            const MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
            MessageDialogResult spellOrActive = await this.ShowMessageAsync(SafeTryFindResource("SpellEditor"), SafeTryFindResource("String4"), style, settings);

            string column = null;
            if (spellOrActive == MessageDialogResult.Affirmative)
                column = "SpellIconID";
            else if (spellOrActive == MessageDialogResult.Negative)
                column = "ActiveIconID";
            adapter.Execute($"UPDATE `{"spell"}` SET `{column}` = '{newIconID}' WHERE `ID` = '{selectedID}'");
        }

        private async void UpdateMainWindow()
        {
            ProgressDialogController controller = null;
            try
            {
                updating = true;

                loadSpell(LoadSpellReporter);

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

        public void LoadSpellReporter(string value)
        {
            Logger.Debug("[Spell Load] " + value);
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
                throw new Exception("An error occurred trying to select spell ID: " + selectedID);
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
                spellDescGenFields[locale].ThreadSafeText = text;
            else if (type == 1)
                spellTooltipGenFields[locale].ThreadSafeText = text;
        }
        #endregion

        #region VisualMapBuilder
        private void ButtonClickVisualMapBuild(object sender, RoutedEventArgs e)
        {
            if (!WoWVersionManager.IsWotlkOrGreaterSelected)
            {
                ShowFlyoutMessage("This feature is only supported for patch 3.3.5 WOTLK.");
                return;
            }
            var mapBuilder = new VisualMapBuilder
            {
                DbStartingId = uint.Parse(MapBuilderDbStartingId.Text),
                MapId = uint.Parse(MapBuilderMapId.Text),
                TopLeftX = int.Parse(MapBuilderTopLeftCoordX.Text),
                TopLeftY = int.Parse(MapBuilderTopLeftCoordY.Text),
                BottomRightX = int.Parse(MapBuilderBottomRightCoordX.Text),
                BottomRightY = int.Parse(MapBuilderBottomRightCoordY.Text),
                CoordZ = int.Parse(MapBuilderCoordZ.Text),
                CellSize = int.Parse(MapBuilderCellSize.Text),
                CreatureQuery = GetQueryFromTemplateFile("VisualMapBuilder\\creature.sql_template.txt"),
                CreatureDisplayInfoQuery = GetQueryFromTemplateFile("VisualMapBuilder\\creature_model_info.sql_template.txt"),
                CreatureTemplateQuery = GetQueryFromTemplateFile("VisualMapBuilder\\creature_template.sql_template.txt")
            };
            mapBuilder.RunBuild();
            ShowFlyoutMessage("Created Map Builder files successfully in the Export folder.");
        }

        private string GetQueryFromTemplateFile(string path)
        {
            var queryLines = new List<string>();
            foreach (var line in File.ReadAllLines(path))
            {
                if (!line.StartsWith("#"))
                {
                    queryLines.Add(line);
                }
            }
            return string.Join(" ", queryLines);
        }

        private void MapBuilderTemplate_Initialized(object sender, EventArgs e)
        {
            if (sender == MapBuilderCreatureTemplate)
            {
                MapBuilderCreatureTemplate.Text = File.ReadAllText("VisualMapBuilder\\creature.sql_template.txt");
            }
            else if (sender == MapBuilderCreatureModelInfoTemplate)
            {
                MapBuilderCreatureModelInfoTemplate.Text = File.ReadAllText("VisualMapBuilder\\creature_model_info.sql_template.txt");
            }
            else if (sender == MapBuilderCreatureTemplateTemplate)
            {
                MapBuilderCreatureTemplateTemplate.Text = File.ReadAllText("VisualMapBuilder\\creature_template.sql_template.txt");
            }
        }
        #endregion

        #region LoadSpell (load spell god-function)


        // store current misc value data type so we don't have to reverse it to access.
        private ControlType miscvalueA1data = ControlType.TextBox;
        private ControlType miscvalueB1data = ControlType.TextBox;
        private ControlType miscvalueA2data = ControlType.TextBox;
        private ControlType miscvalueB2data = ControlType.TextBox;
        private ControlType miscvalueA3data = ControlType.TextBox;
        private ControlType miscvalueB3data = ControlType.TextBox;


        private int GetNumberPrefixFromText(string text)
        {
            text = text.TrimStart();
            var numStr = "";
            for (int i = 0; i < text.Length; ++i)
            {
                if (char.IsDigit(text[i]) 
                    | (text[i] == '-' && i == 0)) // support negative values
                {
                    numStr += text[i];
                }
                else
                {
                    break;
                }
            }
            if (int.TryParse(numStr, out var num))
            {
                return num;
            }
            return 0;
        }

        private bool IsAuraSpellEffect(int spellEffectTypeId)
        {
            if (spellEffectTypeId == 0)
                return false;

            SpellEffectData spellEffectData = new SpellEffectData();
            bool EffectHasData = MiscValueConstants.spellEffectsData.TryGetValue((spellEffectTypes)spellEffectTypeId, out spellEffectData);
            Debug.Assert(EffectHasData);

            // didn't have aura pre Wrath
            if ( (!WoWVersionManager.IsWotlkOrGreaterSelected) && spellEffectTypeId == 65)
            {
                return false;
            }

            // if (spellEffectTypeId == 6 || spellEffectTypeId == 27 || spellEffectTypeId == 35 ||
            //     spellEffectTypeId == 119 || spellEffectTypeId == 128 || spellEffectTypeId == 143)
            if (spellEffectData.usesAura)
            {
                return true;
            }

            return false;
        }

        // refreshes the UI to load the right miscvalue ui control, but doesn't load its value
        private void SetupMiscValueControl(int effectIndex /* 1 to 3 */, int spellEffectType, int auraType, int miscValue = 0)
        {
            // setup UI controls, disable everything by default
            effectMultipleValueBoxes[effectIndex - 1].IsEnabled = false;
            effectAuraNameBoxes[effectIndex - 1].IsEnabled = false;
            effectChainTargetBoxes[effectIndex - 1].IsEnabled = false;
            effectItemTypeBoxes[effectIndex - 1].IsEnabled = false;
            effectTriggerSpellBoxes[effectIndex - 1].IsEnabled = false;
            effectAmplitudeBoxes[effectIndex - 1].IsEnabled = false;

            ContentControl DynamicContentA = miscValueDynamicContentsA[effectIndex - 1];
            ContentControl DynamicContentB = miscValueDynamicContentsB[effectIndex - 1];

            ref ControlType currMiscValueAData = ref miscvalueA1data; // temporary for initialization
            ref ControlType currMiscValueBData = ref miscvalueB1data;

            Label currLabelA = miscValueLabelA[effectIndex - 1];
            Label currLabelB = miscValueLabelB[effectIndex - 1];

            switch (effectIndex)
            {
                case 1:
                    currMiscValueAData = ref miscvalueA1data;
                    currMiscValueBData = ref miscvalueB1data;
                    break;
                case 2:
                    currMiscValueAData = ref miscvalueA2data;
                    currMiscValueBData = ref miscvalueB2data;
                    break;
                case 3:
                    currMiscValueAData = ref miscvalueA3data;
                    currMiscValueBData = ref miscvalueB3data;
                    break;
            }
            // reset ui control
            DynamicContentA.Content = null;
            DynamicContentB.Content = null;
            DynamicContentA.IsEnabled = false;
            DynamicContentB.IsEnabled = false;
            currMiscValueAData = ControlType.TextBox;
            currMiscValueBData = ControlType.TextBox;
            currLabelA.Content = SafeTryFindResource("labMiscValueA");
            currLabelB.Content = SafeTryFindResource("labMiscValueB");
            ////
            ///

            // if custom musc values fields is disabled or unsupported
            if (!Config.DynamicMiscValueFields || !WoWVersionManager.IsWotlkOrGreaterSelected)
            {
                // just enable every fields
                effectMultipleValueBoxes[effectIndex - 1].IsEnabled = true;
                effectAuraNameBoxes[effectIndex - 1].IsEnabled = true;
                effectChainTargetBoxes[effectIndex - 1].IsEnabled = true;
                effectItemTypeBoxes[effectIndex - 1].IsEnabled = true;
                effectTriggerSpellBoxes[effectIndex - 1].IsEnabled = true;
                effectAmplitudeBoxes[effectIndex - 1].IsEnabled = true;

                DynamicContentA.IsEnabled = true;
                if (WoWVersionManager.IsTbcOrGreaterSelected)
                    DynamicContentB.IsEnabled = true;

                DynamicContentA.Content = new ThreadSafeTextBox { TextWrapping = TextWrapping.Wrap };
                DynamicContentB.Content = new ThreadSafeTextBox { TextWrapping = TextWrapping.Wrap };
                return;
            }

            // no spell effect, we're done, everything disabled
            if (spellEffectType == (int)spellEffectTypes.NONE)
            {
                DynamicContentA.Content = new ThreadSafeTextBox { TextWrapping = TextWrapping.Wrap };
                DynamicContentB.Content = new ThreadSafeTextBox { TextWrapping = TextWrapping.Wrap };
                return;
            }

            // effect data, maybe move to another function
            SpellEffectData spellEffectData = new SpellEffectData();

            MiscValuePair miscValues;
            bool hasMiscValues = false;
            bool isAura = false;

            bool hasData = MiscValueConstants.spellEffectsData.TryGetValue((spellEffectTypes)spellEffectType, out spellEffectData);
            if (!hasData && spellEffectType != (int)spellEffectTypes.NONE)
                throw new NotImplementedException($"Spell Effect {spellEffectType} does not have data. Report to devs.");
            


            // if (IsAuraSpellEffect(spellEffectType))
            if (spellEffectData.usesAura)
            {
                // get miscvalue from aura instead of effect
                isAura = true;
                hasMiscValues = MiscValueConstants.auraMiscValues.TryGetValue(auraType, out miscValues);
            }
            else // get misc values from effect. // TODO : get more data from aura : multiplevalue, spelltrigger etc
            {
                miscValues = new MiscValuePair(spellEffectData.MiscValueA, spellEffectData.MiscValueB);
                if (miscValues.ValueA != MiscValueType.None || miscValues.ValueB != MiscValueType.None)
                    hasMiscValues = true;
            }

            // enable fields based on spell effect
            if (spellEffectData.usesAura)
            {   // enable everything for auras for now, gotta analyse in the future how auras use those fields.
                effectMultipleValueBoxes[effectIndex - 1].IsEnabled = true;
                effectAuraNameBoxes[effectIndex - 1].IsEnabled = true;
                effectChainTargetBoxes[effectIndex - 1].IsEnabled = true;
                effectItemTypeBoxes[effectIndex - 1].IsEnabled = true;
                effectTriggerSpellBoxes[effectIndex - 1].IsEnabled = true;
                effectAmplitudeBoxes[effectIndex - 1].IsEnabled = true;
            }
            else
            {
                effectMultipleValueBoxes[effectIndex - 1].IsEnabled = spellEffectData.usesMultipleValue;
                effectAuraNameBoxes[effectIndex - 1].IsEnabled = spellEffectData.usesAura;
                effectChainTargetBoxes[effectIndex - 1].IsEnabled = spellEffectData.UsesChainTarget;
                effectItemTypeBoxes[effectIndex - 1].IsEnabled = spellEffectData.UsesItemType;
                effectTriggerSpellBoxes[effectIndex - 1].IsEnabled = spellEffectData.UsesSpell;
                effectAmplitudeBoxes[effectIndex - 1].IsEnabled = spellEffectData.UsesAmplitude;
            }

            // dev helpers to find possible used fields, ignore auras for now
            if ((spellEffectData.usesAura == false))
            {
                if (spellEffectData.usesMultipleValue == false)
                    Debug.Assert(effectMultipleValueBoxes[effectIndex - 1].Text == "0"
                              || effectMultipleValueBoxes[effectIndex - 1].Text == "1");
                if (spellEffectData.usesAura == false)
                    Debug.Assert(effectAuraNameBoxes[effectIndex - 1].GetNumberPrefixFromText() == 0u);
                if (spellEffectData.UsesChainTarget == false)
                    Debug.Assert(effectChainTargetBoxes[effectIndex - 1].Text == "0");
                if (spellEffectData.UsesItemType == false)
                    Debug.Assert(effectItemTypeBoxes[effectIndex - 1].Text == "0");
                if (spellEffectData.UsesSpell == false)
                    Debug.Assert(effectTriggerSpellBoxes[effectIndex - 1].Text == "0");
                if (spellEffectData.UsesAmplitude == false)
                    Debug.Assert(effectAmplitudeBoxes[effectIndex - 1].Text == "0");
            }



            ////
            // misc value Aura specific stuff -------

            // no spell effect, we're done
           if (!hasMiscValues)
            {
                DynamicContentA.Content = new ThreadSafeTextBox { TextWrapping = TextWrapping.Wrap };
                DynamicContentB.Content = new ThreadSafeTextBox { TextWrapping = TextWrapping.Wrap };
                return;
            }

            Debug.Assert(miscValues.ValueA != MiscValueType.None);

            // initialize dynamic UI control for both A and B
            for (int misc_value_idx = 0; misc_value_idx < 2; misc_value_idx++)
            {
                if (WoWVersionManager.IsTbcOrGreaterSelected == false && misc_value_idx == 1)
                {
                    DynamicContentB.IsEnabled = false;
                    continue;
                }

                MiscValueType currMiscValue;
                ContentControl currContentControl;

                if (misc_value_idx == 0)
                {
                    currMiscValue = miscValues.ValueA;
                    currContentControl = DynamicContentA;
                }
                else
                {
                    currMiscValue = miscValues.ValueB;
                    currContentControl = DynamicContentB;
                }

                if (currMiscValue == MiscValueType.None)
                {
                    // use default textbox
                    currContentControl.Content = new ThreadSafeTextBox { TextWrapping = TextWrapping.Wrap };
                    continue;
                }

                // set description label to misc value type
                // add a space where uppercase is found, eg : SpellAura becomes Spell Aura.
                string misc_value_type_name = Regex.Replace(currMiscValue.ToString(), "(?<!^)([A-Z])", " $1");
                if (misc_value_idx == 0)
                    currLabelA.Content = misc_value_type_name;
                else
                    currLabelB.Content = misc_value_type_name;


                currContentControl.IsEnabled = true;

                if (!MiscValueConstants.MiscValueTypesData.ContainsKey(currMiscValue))
                {
                    throw new Exception($"Unimplemented misc value type {currMiscValue.ToString()}.");
                }

                MiscValueTypeData miscValueTypeData = MiscValueConstants.MiscValueTypesData[currMiscValue];
                ControlType controltype = miscValueTypeData.ControlType;
                RessourceType ressourcetype = miscValueTypeData.RessourceType;
                string dbc_name = "";
                string ressource_string_name = "";

                if (controltype == ControlType.TextBox)
                {
                    // shouldn't need any ressource
                    currContentControl.Content = new ThreadSafeTextBox { TextWrapping = TextWrapping.Wrap };
                    continue;
                }

                // load ressources mapping
                if (ressourcetype == RessourceType.DBC)
                {
                    if (!MiscValueConstants.miscValueMappedDbcRessource.TryGetValue(currMiscValue, out dbc_name))
                        throw new Exception($"Unimplemented DBC ressource for misc value type {currMiscValue.ToString()}.");
                }
                else if (ressourcetype == RessourceType.StringList)
                {
                    if (!MiscValueConstants.miscValueMappedStringsRessource.TryGetValue(currMiscValue, out ressource_string_name))
                    {
                        // currContentControl.Content = new ThreadSafeTextBox { TextWrapping = TextWrapping.Wrap };
                        // continue;
                        throw new Exception($"Unimplemented XAML strings ressource for misc value type {currMiscValue.ToString()}.");

                    }
                }
                else if (ressourcetype == RessourceType.DB || ressourcetype == RessourceType.None)
                {
                    // todo
                    // unimplemented, create default text box
                    currContentControl.Content = new ThreadSafeTextBox { TextWrapping = TextWrapping.Wrap };
                    continue;
                }

                if (controltype == ControlType.ComboboxList || controltype == ControlType.ComboboxMask)
                {
                    ThreadSafeComboBox comboBox = new ThreadSafeComboBox();

                    // setup special mode for mask combobox
                    if (controltype == ControlType.ComboboxMask)
                    {
                        comboBox.IsEditable = true;
                        comboBox.IsReadOnly = true;
                        comboBox.IsTextSearchEnabled = false; // Disable WPF trying to search by text

                        // comboBox.SelectionChanged += (s, e) => ((ComboBox)s).SelectedItem = null;
                    }

                    // load dbc ressource
                    if (ressourcetype == RessourceType.DBC)
                    {
                        string dbcClassName = $"SpellEditor.Sources.DBC.{dbc_name}"; // too lazy to map the classes
                        System.Type type = System.Type.GetType(dbcClassName);

                        object dbcInstance = DBCManager.GetInstance().FindDbcForBinding(dbc_name);
                        if (dbcInstance == null)
                        {
                            // currContentControl.Content = new ThreadSafeTextBox { TextWrapping = TextWrapping.Wrap };
                            // continue;
                            throw new Exception($"DBC could not be loaded : {dbc_name}.");
                        }

                        dynamic dynamicDbcInstance = Convert.ChangeType(dbcInstance, type);

                        var labels = ConvertBoxListToLabelsWithIds(dynamicDbcInstance.GetAllBoxes());

                        // verify first line has index, expected format : "420 - text"
                        if (!Regex.IsMatch(labels[0].ToString(), @"^\d+ -"))
                            throw new Exception($"DBC ressource for misc value type {currMiscValue.ToString()} (xkey:{ressource_string_name}) doesn't use expected indexing format.");


                        if (controltype == ControlType.ComboboxList)
                        {
                            // comboBox.ItemsSource = ConvertBoxListToLabels(dynamicDbcInstance.GetAllBoxes());
                            comboBox.ItemsSource = labels;
                        }
                        else if (controltype == ControlType.ComboboxMask)
                        {
                            // TODO implement mask combobox from dbc

                            foreach (var label in labels)
                            {
                                string text = label.ToString();
                                uint id = (uint)GetNumberPrefixFromText(text);

                                // ISSUE : DBCs start at id 1, enums for bitmasks start at id 0.


                                // ignore 0/none, don't need that in a mask combobox
                                if (id == 0)
                                    continue;

                                ThreadSafeCheckBox chk = new ThreadSafeCheckBox
                                {
                                    Content = text,
                                    Tag = id, // store index

                                    // to make the checkbox fill horizontally
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    HorizontalContentAlignment = HorizontalAlignment.Left,
                                    VerticalAlignment = VerticalAlignment.Center,
                                };
                                chk.Checked += HandMaskCombobox_Checked;
                                chk.Unchecked += HandMaskCombobox_Checked;

                                comboBox.Items.Add(chk);


                            }
                            // solve the bug of text changing when clicking outside the checkbox
                            comboBox.DropDownClosed += MiscValueMask_DropDownClosed;
                        }

                    }
                    else if (ressourcetype == RessourceType.StringList) // string list ressource
                    {
                        string[] ressource_strings = SafeTryFindResource(ressource_string_name).Split('|');
                        int length = ressource_strings.Length;

                        if (length < 2)
                        {
                            throw new Exception($"Missing or invalid XAML string ressource for misc value type {currMiscValue.ToString()}, xkey {ressource_string_name} .");
                        }

                        // manually add prefix to some ressource string that are missing it, can add them to file instead.
                        
                        if (currMiscValue == MiscValueType.SpellEffect || currMiscValue == MiscValueType.AuraState)
                        {

                            string[] comboEntries = new string[length];
                            for (int i = 0; i < length; ++i)
                            {
                                comboEntries[i] = i + " - " + ressource_strings[i];
                            }
                            ressource_strings = comboEntries;
                        }
                        // verify first line has index, expected format : "420 - text"
                        if (!Regex.IsMatch(ressource_strings[0], @"^\d+ -"))
                            throw new Exception($"XAML string ressource for misc value type {currMiscValue.ToString()} (xkey:{ressource_string_name}) doesn't use expected indexing format.");

                        if (controltype == ControlType.ComboboxList)
                        {
                            foreach (string ressource_string in ressource_strings)
                            {
                                comboBox.Items.Add(ressource_string);
                            }
                        }
                        else if (controltype == ControlType.ComboboxMask)
                        {
                            for (uint i = 0; i < ressource_strings.Length; ++i)
                            {
                                string text = ressource_strings[i];
                                uint id = (uint)GetNumberPrefixFromText(text);

                                Debug.Assert(id > 0, "error");
                                if (id == 0)
                                    continue;

                                ThreadSafeCheckBox chk = new ThreadSafeCheckBox
                                {
                                    Content = text,
                                    Tag = id, // store index

                                    // to make the checkbox fill horizontally
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    HorizontalContentAlignment = HorizontalAlignment.Left,
                                    VerticalAlignment = VerticalAlignment.Center,
                                };

                                chk.Checked += HandMaskCombobox_Checked;
                                chk.Unchecked += HandMaskCombobox_Checked;

                                comboBox.Items.Add(chk);
                            }

                            comboBox.DropDownClosed += MiscValueMask_DropDownClosed;
                        }

                    }

                    currContentControl.Content = comboBox;

                    // store curr UI type for quick access
                    if (controltype == ControlType.ComboboxList)
                    {
                        if (misc_value_idx == 0)
                            currMiscValueAData = ControlType.ComboboxList;
                        else
                            currMiscValueBData = ControlType.ComboboxList;
                    }
                    else if (controltype == ControlType.ComboboxMask)
                    {
                        if (misc_value_idx == 0)
                            currMiscValueAData = ControlType.ComboboxMask;
                        else
                            currMiscValueBData = ControlType.ComboboxMask;
                    }

                }
                else if (controltype == ControlType.ListSelector) // NYI
                {
                    currContentControl.Content = new ThreadSafeTextBox { TextWrapping = TextWrapping.Wrap };
                }

            }
            ///
        }

        private ContentControl GetMiscValueDynamicContent(int effectIndex, int miscvalueId /* 1 or 2 (A or B)*/)
        {
            ContentControl DynamicContent = null;

            switch (effectIndex)
            {
                case 1:
                    if (miscvalueId == 1) 
                        DynamicContent = MiscValueA1DynamicContent;// A
                    else
                        DynamicContent = MiscValueB1DynamicContent; // B
                    break;
                case 2:
                    if (miscvalueId == 1)
                        DynamicContent = MiscValueA2DynamicContent;// A
                    else
                        DynamicContent = MiscValueB2DynamicContent; // B
                    break;
                case 3:
                    if (miscvalueId == 1)
                        DynamicContent = MiscValueA3DynamicContent;// A
                    else
                        DynamicContent = MiscValueB3DynamicContent; // B
                    break;
            }

            return DynamicContent;
        }

        private ControlType GetMiscValueControlType(int effectIndex, int miscvalueId /* 1 or 2 (A or B)*/)
        {
            ControlType currMiscValueData = miscvalueA1data;

            switch (effectIndex)
            {
                case 1:
                    if (miscvalueId == 1)
                        currMiscValueData = miscvalueA1data;// A
                    else
                        currMiscValueData = miscvalueB1data; // B
                    break;
                case 2:
                    if (miscvalueId == 1)
                        currMiscValueData = miscvalueA2data;// A
                    else
                        currMiscValueData = miscvalueB2data; // B
                    break;
                case 3:
                    if (miscvalueId == 1)
                        currMiscValueData = miscvalueA3data;// A
                    else
                        currMiscValueData = miscvalueB3data; // B
                    break;
            }

            return currMiscValueData;
        }


        private MiscValuePair GetCurrentMiscValueType(int effectIndex /*1-3*/)
        {
            uint spellEffectType = 0;
            uint auraType = 0;

            switch (effectIndex)
            {
                case 1:
                    spellEffectType = SpellEffect1.GetNumberPrefixFromText();
                    auraType = ApplyAuraName1.GetNumberPrefixFromText();
                    break;
                case 2:
                    spellEffectType = SpellEffect2.GetNumberPrefixFromText();
                    auraType = ApplyAuraName2.GetNumberPrefixFromText();
                    break;
                case 3:
                    spellEffectType = SpellEffect3.GetNumberPrefixFromText();
                    auraType = ApplyAuraName3.GetNumberPrefixFromText();
                    break;
            }

            MiscValuePair miscValues;
            bool hasMiscValues = false;

            if (IsAuraSpellEffect((int)spellEffectType))
            {
                // get miscvalue from aura instead of effect
                hasMiscValues = MiscValueConstants.auraMiscValues.TryGetValue((int)auraType, out miscValues);
            }
            else // get misc values from effect
            {
                SpellEffectData spellEffectData = new SpellEffectData();
                bool hasData = MiscValueConstants.spellEffectsData.TryGetValue((spellEffectTypes)spellEffectType, out spellEffectData);
                if (!hasData)
                    throw new NotImplementedException($"Spell Effect {spellEffectType} does not have data. Report to devs.");

                miscValues = new MiscValuePair(spellEffectData.MiscValueA, spellEffectData.MiscValueB);
            }


            return miscValues;
        }

        private void SetMiscValue(int effectIndex, int miscvalueId /* 1 or 2 (A or B)*/, int miscValue, bool reset_combobox = false)
        {
            ContentControl DynamicContent = GetMiscValueDynamicContent(effectIndex, miscvalueId);
            ControlType currMiscValueData = GetMiscValueControlType(effectIndex, miscvalueId);

            // assign value, TODO move to function
            if (currMiscValueData == ControlType.TextBox || currMiscValueData == ControlType.ListSelector)
            {
                if (DynamicContent.Content is ThreadSafeTextBox textBox)
                    textBox.ThreadSafeText = miscValue.ToString();
            }
            else if (currMiscValueData == ControlType.ComboboxList)
            {
                if (DynamicContent.Content is ThreadSafeComboBox comboBox)
                {
                    // skip checks if resetting
                    if (reset_combobox)
                    {
                        comboBox.ThreadSafeIndex = 0;
                    }
                    else
                    {
                        comboBox.ThreadSafeIndex = miscValue;

                        int text_misc_value = 0;
                        // then verify if id prefix matches
                        if (comboBox.SelectedItem != null)
                        {
                            string selectedText = comboBox.SelectedItem.ToString();
                            text_misc_value = GetNumberPrefixFromText(selectedText);
                        }

                        // Debug.Assert(miscValue == text_misc_value, $"expected Id  {miscValue.ToString()}, got {text_misc_value.ToString()} in miscvalue {GetMiscValueType(effectIndex).ValueA.ToString()} enum.");

                        if (miscValue != text_misc_value)
                        {
                            bool found = false;
                            // mismatch, could simply be because id is offset by 1 or it is a specific value, let's iterate all
                            foreach (var item in comboBox.Items)
                            {
                                string text = "";

                                // for some reason it can be either types
                                if (item is string strItem)
                                    text = strItem;
                                else if (item is FriendlyLabel labelItem)
                                    text = labelItem.Content.ToString();

                                int text_value = GetNumberPrefixFromText(text);
                                if (text_value == miscValue)
                                {
                                    comboBox.SelectedItem = item;

                                    found = true;
                                    break;
                                }
                            }

                            // later, create new entry if doesn't exist
                            if (!found)
                            {
                                Debug.Assert(miscValue == text_misc_value
                                    , $"No enum value with Id  {miscValue.ToString()} found in miscvalue {GetCurrentMiscValueType(effectIndex).ValueA.ToString()} enum.");
                                ComboBoxItem newItem = new ComboBoxItem
                                {
                                    Content = miscValue.ToString() + " - Unknown value, report to devs",
                                    Foreground = Brushes.Red
                                };

                                comboBox.Items.Add(newItem);
                            }

                        }
                    }

                    // comboBox.ToolTip = miscValue;
                }
            }
            else if (currMiscValueData == ControlType.ComboboxMask)
            {
                if (DynamicContent.Content is ThreadSafeComboBox comboBox)
                {
                    comboBox.ThreadSafeText = miscValue.ToString();

                    // reset every box
                    foreach (ThreadSafeCheckBox cb in comboBox.Items)
                    {
                        cb.ThreadSafeChecked = false;
                    }

                    // iterate all active bits to find if there is a missing option
                    // start at 1 to avoid 0 (none)
                    for (int i = 1; i < 33; i++)
                    {
                        // used bit. -1 because we started at 1 instead of 0.
                        if ((miscValue & (1 << ( i - 1) ) ) != 0)
                        {

                            bool found = false;
                            // find the checkbox row with this id
                            foreach (ThreadSafeCheckBox cb in comboBox.Items)
                            {
                                int cb_id = GetNumberPrefixFromText((string)cb.Content);
                                // if ((miscValue & (1 << (cb_id - 1))) != 0)
                                if (cb_id == i)
                                {
                                    cb.ThreadSafeChecked = true;
                                    found = true;
                                    break;
                                }
                            }

                            // Bit not found in our enum, create new row with the value

                            // note that this can happen because sometime they have bit values that include everything like -1
                            // -2 is also common for schools, it's "all except physical"
                            if (miscValue == -1 || miscValue == -2) // in that case ignore "all"
                                continue;

                            Debug.Assert
                                (found, $"No enum value with Id  {i.ToString()} found in miscvalue {GetCurrentMiscValueType(effectIndex).ValueA.ToString()} enum.");
                            if (!found)
                            {
                                // create the new checkbox row
                                Debug.Assert(i != 0, "got 0");

                                string text = $"{i} - UNKNOWN";

                                ThreadSafeCheckBox chk = new ThreadSafeCheckBox
                                {
                                    Content = text,
                                    Tag = (uint)i, // store index

                                    // to make the checkbox fill horizontally
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    HorizontalContentAlignment = HorizontalAlignment.Left,
                                    VerticalAlignment = VerticalAlignment.Center,
                                };
                                chk.IsChecked = true;

                                chk.Checked += HandMaskCombobox_Checked;
                                chk.Unchecked += HandMaskCombobox_Checked;

                                comboBox.Items.Add(chk);
                            }
                        }
                    }

                    // force a checkbox changed event to force update the fancy text even if nothing changed
                    if (comboBox.Items.Count > 0)
                    {
                        // HandMaskCombobox_Checked(comboBox.Items.GetItemAt(0), null);
                        var item = comboBox.Items.GetItemAt(0);
                        ThreadSafeCheckBox chkBox = (ThreadSafeCheckBox)item;
                        // if (item is Grid grid && grid.Children[0] is ThreadSafeCheckBox chkBox)
                            HandMaskCombobox_Checked(chkBox, null);
                    }
                        
                }
            }
        }

        // usable for saving
        private int GetMiscValue(int effectIndex, int miscvalueId /* 1 or 2 (A or B)*/)
        {
            ContentControl DynamicContent = GetMiscValueDynamicContent(effectIndex, miscvalueId);
            ControlType currMiscValueData = GetMiscValueControlType(effectIndex, miscvalueId);

            int misc_value = 0;

            if (currMiscValueData == ControlType.TextBox || currMiscValueData == ControlType.ListSelector)
            {
                if (DynamicContent.Content is ThreadSafeTextBox textBox)
                    misc_value = int.Parse(textBox.Text); // need thread safe text ?
            }
            else if (currMiscValueData == ControlType.ComboboxList)
            {
                if (DynamicContent.Content is ThreadSafeComboBox comboBox)
                {
                    misc_value = GetNumberPrefixFromText(comboBox.Text);
                }
            }
            else if (currMiscValueData == ControlType.ComboboxMask)
            {
                if (DynamicContent.Content is ThreadSafeComboBox comboBox)
                {
                    // temporary
                    misc_value = GetNumberPrefixFromText(comboBox.Text);

                    // TODO : parse boxes instead
                }
            }

            return misc_value;
        }

        private void loadSpell(UpdateTextFunc updateProgress)
        {
            _currentVisualController = null;
            adapter.Updating = true;
            updateProgress("Querying MySQL data...");
            var data = adapter.Query($"SELECT * FROM `spell` WHERE `ID` = '{selectedID}'");
            var rowResult = data.Rows;
            if (rowResult == null || rowResult.Count != 1)
                throw new Exception("An error occurred trying to select spell ID: " + selectedID);
            var row = rowResult[0];
            var numColumns = (int)WoWVersionManager.GetInstance().SelectedVersion().NumLocales;
            var isWotlkOrGreater = WoWVersionManager.IsWotlkOrGreaterSelected;
            var isTbcOrGreater = WoWVersionManager.IsTbcOrGreaterSelected;
            try
            {
                updateProgress("Updating text control's...");
                int i;
                var maxColumns = numColumns > spellDescGenFields.Count ? spellDescGenFields.Count : numColumns;
                for (i = 0; i < maxColumns; ++i)
                {
                    spellDescGenFields[i].ThreadSafeText = SpellStringParser.ParseString(row["SpellDescription" + i].ToString(), row, this);
                    spellTooltipGenFields[i].ThreadSafeText = SpellStringParser.ParseString(row["SpellTooltip" + i].ToString(), row, this);
                }
                for (i = 0; i < maxColumns; ++i)
                {
                    stringObjectMap.TryGetValue(i, out var box);
                    box.ThreadSafeText = row[$"SpellName{i}"];
                }
                for (i = 0; i < maxColumns; ++i)
                {
                    stringObjectMap.TryGetValue(i + 9, out var box);
                    box.ThreadSafeText = row[$"SpellRank{i}"];
                }

                for (i = 0; i < maxColumns; ++i)
                {
                    stringObjectMap.TryGetValue(i + 18, out var box);
                    box.ThreadSafeText = row[$"SpellTooltip{i}"];
                }

                for (i = 0; i < maxColumns; ++i)
                {
                    stringObjectMap.TryGetValue(i + 27, out var box);
                    box.ThreadSafeText = row[$"SpellDescription{i}"];
                }

                updateProgress("Updating category & dispel & mechanic...");
                var loadCategories = (SpellCategory)DBCManager.GetInstance().FindDbcForBinding("SpellCategory");
                Category.ThreadSafeIndex = loadCategories.UpdateCategorySelection(uint.Parse(
                    adapter.Query($"SELECT `Category` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0][0].ToString()));

                var loadDispels = (SpellDispelType)DBCManager.GetInstance().FindDbcForBinding("SpellDispelType");
                DispelType.ThreadSafeIndex = loadDispels.UpdateDispelSelection(uint.Parse(
                    adapter.Query($"SELECT `Dispel` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0][0].ToString()));

                var loadMechanics = (SpellMechanic)DBCManager.GetInstance().FindDbcForBinding("SpellMechanic");
                MechanicType.SelectedIndex = loadMechanics.UpdateMechanicSelection(uint.Parse(
                    adapter.Query($"SELECT `Mechanic` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0][0].ToString()));

                updateProgress("Updating attributes...");
                uint mask = uint.Parse(row["Attributes"].ToString());
                uint flagg = 1;

                foreach (ThreadSafeCheckBox attribute0 in attributes0)
                {
                    attribute0.ThreadSafeChecked = ((mask & flagg) != 0);
                    flagg += flagg;
                }

                mask = uint.Parse(row["AttributesEx"].ToString());
                flagg = 1;

                foreach (ThreadSafeCheckBox attribute1 in attributes1)
                {
                    attribute1.ThreadSafeChecked = ((mask & flagg) != 0);
                    flagg += flagg;
                }

                mask = uint.Parse(row["AttributesEx2"].ToString());
                flagg = 1;

                foreach (ThreadSafeCheckBox attribute2 in attributes2)
                {
                    attribute2.ThreadSafeChecked = ((mask & flagg) != 0);
                    flagg += flagg;
                }

                mask = uint.Parse(row["AttributesEx3"].ToString());
                flagg = 1;

                foreach (ThreadSafeCheckBox attribute3 in attributes3)
                {
                    attribute3.ThreadSafeChecked = ((mask & flagg) != 0);
                    flagg += flagg;
                }

                mask = uint.Parse(row["AttributesEx4"].ToString());
                flagg = 1;

                foreach (ThreadSafeCheckBox attribute4 in attributes4)
                {
                    attribute4.ThreadSafeChecked = ((mask & flagg) != 0);
                    flagg += flagg;
                }

                if (isTbcOrGreater)
                {
                    mask = uint.Parse(row["AttributesEx5"].ToString());
                    flagg = 1;

                    foreach (ThreadSafeCheckBox attribute5 in attributes5)
                    {
                        attribute5.ThreadSafeChecked = ((mask & flagg) != 0);
                        flagg += flagg;
                    }

                    mask = uint.Parse(row["AttributesEx6"].ToString());
                    flagg = 1;

                    foreach (ThreadSafeCheckBox attribute6 in attributes6)
                    {
                        attribute6.ThreadSafeChecked = ((mask & flagg) != 0);
                        flagg += flagg;
                    }

                    updateProgress("Updating stances...");
                    mask = uint.Parse(row["Stances"].ToString());
                    if (mask == 0)
                    {
                        stancesBoxes[0].ThreadSafeChecked = true;
                        for (int f = 1; f < stancesBoxes.Count; ++f) { stancesBoxes[f].ThreadSafeChecked = false; }
                    }
                    else
                    {
                        stancesBoxes[0].ThreadSafeChecked = false;
                        uint flag = 1;
                        for (int f = 1; f < stancesBoxes.Count; ++f)
                        {
                            stancesBoxes[f].ThreadSafeChecked = ((mask & flag) != 0);
                            flag += flag;
                        }
                    }
                }
                if (isWotlkOrGreater)
                {
                    mask = uint.Parse(row["AttributesEx7"].ToString());
                    flagg = 1;

                    foreach (ThreadSafeCheckBox attribute7 in attributes7)
                    {
                        attribute7.ThreadSafeChecked = ((mask & flagg) != 0);
                        flagg += flagg;
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
                    targetBoxes[0].ThreadSafeChecked = true;
                    for (int f = 1; f < targetBoxes.Count; ++f) { targetBoxes[f].ThreadSafeChecked = false; }
                }
                else
                {
                    targetBoxes[0].ThreadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 1; f < targetBoxes.Count; ++f)
                    {
                        targetBoxes[f].ThreadSafeChecked = ((mask & flag) != 0);
                        flag += flag;
                    }
                }

                mask = uint.Parse(row["TargetCreatureType"].ToString());

                if (mask == 0)
                {
                    targetCreatureTypeBoxes[0].ThreadSafeChecked = true;
                    for (int f = 1; f < targetCreatureTypeBoxes.Count; ++f) { targetCreatureTypeBoxes[f].ThreadSafeChecked = false; }
                }
                else
                {
                    targetCreatureTypeBoxes[0].ThreadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 1; f < targetCreatureTypeBoxes.Count; ++f)
                    {
                        targetCreatureTypeBoxes[f].ThreadSafeChecked = ((mask & flag) != 0);
                        flag += flag;
                    }
                }
                updateProgress("Updating spell focus object selection...");
                var loadFocusObjects = (SpellFocusObject)DBCManager.GetInstance().FindDbcForBinding("SpellFocusObject");
                RequiresSpellFocus.ThreadSafeIndex = loadFocusObjects.UpdateSpellFocusObjectSelection(uint.Parse(
                    adapter.Query($"SELECT `RequiresSpellFocus` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0][0].ToString()));

                if (isTbcOrGreater)
                {
                    mask = uint.Parse(row["FacingCasterFlags"].ToString());
                    FacingFrontFlag.ThreadSafeChecked = ((mask & 0x1) != 0);
                }
                FacingFrontFlag.IsEnabled = isTbcOrGreater;

                updateProgress("Updating caster aura state...");
                switch (uint.Parse(row["CasterAuraState"].ToString()))
                {
                    case 0: // None
                        CasterAuraState.ThreadSafeIndex = 0;
                        break;
                    case 1: // Defense
                        CasterAuraState.ThreadSafeIndex = 1;
                        break;
                    case 2: // Healthless 20%
                        CasterAuraState.ThreadSafeIndex = 2;
                        break;
                    case 3: // Berserking
                        CasterAuraState.ThreadSafeIndex = 3;
                        break;
                    case 5: // Judgement
                        CasterAuraState.ThreadSafeIndex = 4;
                        break;
                    case 7: // Hunter Parry
                        CasterAuraState.ThreadSafeIndex = 5;
                        break;
                    case 10: // Victory Rush
                        CasterAuraState.ThreadSafeIndex = 6;
                        break;
                    case 11: // Unknown 1
                        CasterAuraState.ThreadSafeIndex = 7;
                        break;
                    case 13: // Healthless 35%
                        CasterAuraState.ThreadSafeIndex = 8;
                        break;
                    case 17: // Enrage
                        CasterAuraState.ThreadSafeIndex = 9;
                        break;
                    case 22: // Unknown 2
                        CasterAuraState.ThreadSafeIndex = 10;
                        break;
                    case 23: // Health Above 75%
                        CasterAuraState.ThreadSafeIndex = 11;
                        break;
                }

                switch (uint.Parse(row["TargetAuraState"].ToString()))
                {
                    case 0: // None
                        TargetAuraState.ThreadSafeIndex = 0;
                        break;
                    case 2: // Healthless 20%
                        TargetAuraState.ThreadSafeIndex = 1;
                        break;
                    case 3: // Berserking
                        TargetAuraState.ThreadSafeIndex = 2;
                        break;
                    case 13: // Healthless 35%
                        TargetAuraState.ThreadSafeIndex = 3;
                        break;
                    case 14: // Conflagrate
                        TargetAuraState.ThreadSafeIndex = 4;
                        break;
                    case 15: // Swiftmend
                        TargetAuraState.ThreadSafeIndex = 5;
                        break;
                    case 16: // Deadly Poison
                        TargetAuraState.ThreadSafeIndex = 6;
                        break;
                    case 18: // Bleeding
                        TargetAuraState.ThreadSafeIndex = 17;
                        break;
                }
                updateProgress("Updating cast time selection...");
                var loadCastTimes = (SpellCastTimes)DBCManager.GetInstance().FindDbcForBinding("SpellCastTimes");
                CastTime.ThreadSafeIndex = loadCastTimes.UpdateCastTimeSelection(uint.Parse(adapter.Query(
                    $"SELECT `CastingTimeIndex` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0][0].ToString()));
                updateProgress("Updating other stuff...");
                RecoveryTime.ThreadSafeText = uint.Parse(row["RecoveryTime"].ToString());
                CategoryRecoveryTime.ThreadSafeText = uint.Parse(row["CategoryRecoveryTime"].ToString());

                mask = uint.Parse(row["InterruptFlags"].ToString());
                if (mask == 0)
                {
                    interrupts1[0].ThreadSafeChecked = true;
                    for (int f = 1; f < interrupts1.Count; ++f) { interrupts1[f].ThreadSafeChecked = false; }
                }
                else
                {
                    interrupts1[0].ThreadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 1; f < interrupts1.Count; ++f)
                    {
                        interrupts1[f].ThreadSafeChecked = ((mask & flag) != 0);

                        flag += flag;
                    }
                }

                mask = uint.Parse(row["AuraInterruptFlags"].ToString());
                if (mask == 0)
                {
                    interrupts2[0].ThreadSafeChecked = true;
                    for (int f = 1; f < interrupts2.Count; ++f) { interrupts2[f].ThreadSafeChecked = false; }
                }
                else
                {
                    interrupts2[0].ThreadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 1; f < interrupts2.Count; ++f)
                    {
                        interrupts2[f].ThreadSafeChecked = ((mask & flag) != 0);
                        flag += flag;
                    }
                }

                mask = uint.Parse(row["ChannelInterruptFlags"].ToString());
                if (mask == 0)
                {
                    interrupts3[0].ThreadSafeChecked = true;
                    for (int f = 1; f < interrupts3.Count; ++f) { interrupts3[f].ThreadSafeChecked = false; }
                }
                else
                {
                    interrupts3[0].ThreadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 1; f < interrupts3.Count; ++f)
                    {
                        interrupts3[f].ThreadSafeChecked = ((mask & flag) != 0);
                        flag += flag;
                    }
                }

                mask = uint.Parse(row["ProcFlags"].ToString());
                if (mask == 0)
                {
                    procBoxes[0].ThreadSafeChecked = true;
                    for (int f = 1; f < procBoxes.Count; ++f) { procBoxes[f].ThreadSafeChecked = false; }
                }
                else
                {
                    procBoxes[0].ThreadSafeChecked = false;
                    uint flag = 1;
                    for (int f = 1; f < procBoxes.Count; ++f)
                    {
                        procBoxes[f].ThreadSafeChecked = ((mask & flag) != 0);
                        flag += flag;
                    }
                }

                ProcChance.ThreadSafeText = uint.Parse(row["ProcChance"].ToString());
                ProcCharges.ThreadSafeText = uint.Parse(row["ProcCharges"].ToString());
                MaximumLevel.ThreadSafeText = uint.Parse(row["MaximumLevel"].ToString());
                BaseLevel.ThreadSafeText = uint.Parse(row["BaseLevel"].ToString());
                SpellLevel.ThreadSafeText = uint.Parse(row["SpellLevel"].ToString());

                var loadDurations = (SpellDuration)DBCManager.GetInstance().FindDbcForBinding("SpellDuration");
                Duration.ThreadSafeIndex = loadDurations.UpdateDurationIndexes(uint.Parse(adapter.Query(
                    $"SELECT `DurationIndex` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0][0].ToString()));

                uint powerType = uint.Parse(row["PowerType"].ToString());
                // Manually handle 'Health' power type
                if (powerType == (uint.MaxValue - 1))
                    powerType = 13;
                PowerType.ThreadSafeIndex = powerType;
                PowerCost.ThreadSafeText = uint.Parse(row["ManaCost"].ToString());
                ManaCostPerLevel.ThreadSafeText = uint.Parse(row["ManaCostPerLevel"].ToString());
                ManaCostPerSecond.ThreadSafeText = uint.Parse(row["ManaPerSecond"].ToString());
                PerSecondPerLevel.ThreadSafeText = uint.Parse(row["ManaPerSecondPerLevel"].ToString());
                if (data.Columns.Contains("SpellPriority"))
                {
                    SpellPriority.ThreadSafeText = int.Parse(row["SpellPriority"].ToString());
                    SpellPriority.IsEnabled = true;
                }
                else
                {
                    SpellPriority.IsEnabled = false;
                }
                updateProgress("Updating spell range selection...");
                var loadRanges = (SpellRange)DBCManager.GetInstance().FindDbcForBinding("SpellRange");
                Range.ThreadSafeIndex = loadRanges.UpdateSpellRangeSelection(uint.Parse(adapter.Query(
                    $"SELECT `RangeIndex` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0][0].ToString()));

                updateProgress("Updating speed, stacks, totems, reagents...");
                Speed.ThreadSafeText = row["Speed"].ToString();
                Stacks.ThreadSafeText = row["StackAmount"].ToString();
                Totem1.ThreadSafeText = row["Totem1"].ToString();
                Totem2.ThreadSafeText = row["Totem2"].ToString();
                Reagent1.ThreadSafeText = row["Reagent1"].ToString();
                Reagent2.ThreadSafeText = row["Reagent2"].ToString();
                Reagent3.ThreadSafeText = row["Reagent3"].ToString();
                Reagent4.ThreadSafeText = row["Reagent4"].ToString();
                Reagent5.ThreadSafeText = row["Reagent5"].ToString();
                Reagent6.ThreadSafeText = row["Reagent6"].ToString();
                Reagent7.ThreadSafeText = row["Reagent7"].ToString();
                Reagent8.ThreadSafeText = row["Reagent8"].ToString();
                ReagentCount1.ThreadSafeText = row["ReagentCount1"].ToString();
                ReagentCount2.ThreadSafeText = row["ReagentCount2"].ToString();
                ReagentCount3.ThreadSafeText = row["ReagentCount3"].ToString();
                ReagentCount4.ThreadSafeText = row["ReagentCount4"].ToString();
                ReagentCount5.ThreadSafeText = row["ReagentCount5"].ToString();
                ReagentCount6.ThreadSafeText = row["ReagentCount6"].ToString();
                ReagentCount7.ThreadSafeText = row["ReagentCount7"].ToString();
                ReagentCount8.ThreadSafeText = row["ReagentCount8"].ToString();

                updateProgress("Updating item class selection...");
                int ID = int.Parse(adapter.Query(
                    $"SELECT `EquippedItemClass` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0][0].ToString());
                if (ID == -1)
                {
                    EquippedItemClass.ThreadSafeIndex = 0;
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
                        box.ThreadSafeChecked = false;
                    Dispatcher.Invoke(DispatcherPriority.Send, TimeSpan.Zero, new Func<object>(()
                        => EquippedItemInventoryTypeGrid.IsEnabled = false));
                }
                var loadItemClasses = (ItemClass)DBCManager.GetInstance().FindDbcForBinding("ItemClass");
                EquippedItemClass.ThreadSafeIndex = loadItemClasses.UpdateItemClassSelection(ID);

                UpdateItemSubClass(long.Parse(row["EquippedItemClass"].ToString()));

                updateProgress("Updating item subclass mask...");
                int intMask = int.Parse(row["EquippedItemSubClassMask"].ToString());
                if (intMask == 0 || intMask == -1)
                {
                    equippedItemSubClassMaskBoxes[0].ThreadSafeChecked = true;
                    for (int f = 1; f < equippedItemSubClassMaskBoxes.Count; ++f) { equippedItemSubClassMaskBoxes[f].ThreadSafeChecked = false; }
                }
                else
                {
                    equippedItemSubClassMaskBoxes[0].ThreadSafeChecked = false;
                    uint flag = 1;
                    foreach (ThreadSafeCheckBox equippedItemSubClassMaskBox in equippedItemSubClassMaskBoxes)
                    {
                        equippedItemSubClassMaskBox.ThreadSafeChecked = ((intMask & flag) != 0);
                        flag += flag;
                    }
                }

                updateProgress("Updating inventory type...");
                intMask = int.Parse(row["EquippedItemInventoryTypeMask"].ToString());
                if (intMask == 0 || intMask == -1)
                {
                    equippedItemInventoryTypeMaskBoxes[0].ThreadSafeChecked = true;
                    for (int f = 1; f < equippedItemInventoryTypeMaskBoxes.Count; ++f) { equippedItemInventoryTypeMaskBoxes[f].ThreadSafeChecked = false; }
                }
                else
                {
                    equippedItemInventoryTypeMaskBoxes[0].ThreadSafeChecked = false;
                    uint flag = 1;
                    foreach (ThreadSafeCheckBox equippedItemInventoryTypeMaskBox in equippedItemInventoryTypeMaskBoxes)
                    {
                        equippedItemInventoryTypeMaskBox.ThreadSafeChecked = ((intMask & flag) != 0);
                        flag += flag;
                    }
                }
                updateProgress("Updating effects 1-3...");
                SpellEffect1.SetTextFromIndex(uint.Parse(row["Effect1"].ToString()));
                SpellEffect2.SetTextFromIndex(uint.Parse(row["Effect2"].ToString()));
                SpellEffect3.SetTextFromIndex(uint.Parse(row["Effect3"].ToString()));
                GenerateSpellEffectHeader(1);
                GenerateSpellEffectHeader(2);
                GenerateSpellEffectHeader(3);

                DieSides1.ThreadSafeText = row["EffectDieSides1"].ToString();
                DieSides2.ThreadSafeText = row["EffectDieSides2"].ToString();
                DieSides3.ThreadSafeText = row["EffectDieSides3"].ToString();
                BasePointsPerLevel1.ThreadSafeText = row["EffectRealPointsPerLevel1"].ToString();
                BasePointsPerLevel2.ThreadSafeText = row["EffectRealPointsPerLevel2"].ToString();
                BasePointsPerLevel3.ThreadSafeText = row["EffectRealPointsPerLevel3"].ToString();
                BasePoints1.ThreadSafeText = row["EffectBasePoints1"].ToString();
                BasePoints2.ThreadSafeText = row["EffectBasePoints2"].ToString();
                BasePoints3.ThreadSafeText = row["EffectBasePoints3"].ToString();
                Mechanic1.ThreadSafeIndex = int.Parse(row["EffectMechanic1"].ToString());
                Mechanic2.ThreadSafeIndex = int.Parse(row["EffectMechanic2"].ToString());
                Mechanic3.ThreadSafeIndex = int.Parse(row["EffectMechanic3"].ToString());
                TargetA1.ThreadSafeIndex = uint.Parse(row["EffectImplicitTargetA1"].ToString());
                TargetA2.ThreadSafeIndex = uint.Parse(row["EffectImplicitTargetA2"].ToString());
                TargetA3.ThreadSafeIndex = uint.Parse(row["EffectImplicitTargetA3"].ToString());
                TargetB1.ThreadSafeIndex = uint.Parse(row["EffectImplicitTargetB1"].ToString());
                TargetB2.ThreadSafeIndex = uint.Parse(row["EffectImplicitTargetB2"].ToString());
                TargetB3.ThreadSafeIndex = uint.Parse(row["EffectImplicitTargetB3"].ToString());

                updateProgress("Updating radius index...");
                var loadRadiuses = (SpellRadius)DBCManager.GetInstance().FindDbcForBinding("SpellRadius");
                var result = adapter.Query($"SELECT `EffectRadiusIndex1`, `EffectRadiusIndex2`, `EffectRadiusIndex3` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0];
                uint[] IDs = { uint.Parse(result[0].ToString()), uint.Parse(result[1].ToString()), uint.Parse(result[2].ToString()) };
                RadiusIndex1.ThreadSafeIndex = loadRadiuses.UpdateRadiusIndexes(IDs[0]);
                RadiusIndex2.ThreadSafeIndex = loadRadiuses.UpdateRadiusIndexes(IDs[1]);
                RadiusIndex3.ThreadSafeIndex = loadRadiuses.UpdateRadiusIndexes(IDs[2]);

                updateProgress("Updating effect 1-3 data...");
                ApplyAuraName1.SetTextFromIndex(uint.Parse(row["EffectApplyAuraName1"].ToString()));
                ApplyAuraName2.SetTextFromIndex(uint.Parse(row["EffectApplyAuraName2"].ToString()));
                ApplyAuraName3.SetTextFromIndex(uint.Parse(row["EffectApplyAuraName3"].ToString()));
                Amplitude1.ThreadSafeText = row["EffectAmplitude1"].ToString();
                Amplitude2.ThreadSafeText = row["EffectAmplitude2"].ToString();
                Amplitude3.ThreadSafeText = row["EffectAmplitude3"].ToString();
                MultipleValue1.ThreadSafeText = row["EffectMultipleValue1"].ToString();
                MultipleValue2.ThreadSafeText = row["EffectMultipleValue2"].ToString();
                MultipleValue3.ThreadSafeText = row["EffectMultipleValue3"].ToString();
                ChainTarget1.ThreadSafeText = row["EffectChainTarget1"].ToString();
                ChainTarget2.ThreadSafeText = row["EffectChainTarget2"].ToString();
                ChainTarget3.ThreadSafeText = row["EffectChainTarget3"].ToString();
                ItemType1.ThreadSafeText = row["EffectItemType1"].ToString();
                ItemType2.ThreadSafeText = row["EffectItemType2"].ToString();
                ItemType3.ThreadSafeText = row["EffectItemType3"].ToString();
                TriggerSpell1.ThreadSafeText = row["EffectTriggerSpell1"].ToString();
                TriggerSpell2.ThreadSafeText = row["EffectTriggerSpell2"].ToString();
                TriggerSpell3.ThreadSafeText = row["EffectTriggerSpell3"].ToString();
                PointsPerComboPoint1.ThreadSafeText = row["EffectPointsPerComboPoint1"].ToString();
                PointsPerComboPoint2.ThreadSafeText = row["EffectPointsPerComboPoint2"].ToString();
                PointsPerComboPoint3.ThreadSafeText = row["EffectPointsPerComboPoint3"].ToString();

                // handle misc value type UIs
                // MiscValueA1.ThreadSafeText = row["EffectMiscValue1"].ToString();
                // this creates the UI
                SetupMiscValueControl(1, int.Parse(row["Effect1"].ToString()), int.Parse(row["EffectApplyAuraName1"].ToString()));
                SetMiscValue(1, 1, int.Parse(row["EffectMiscValue1"].ToString()));
                SetupMiscValueControl(2, int.Parse(row["Effect2"].ToString()), int.Parse(row["EffectApplyAuraName2"].ToString()));
                SetMiscValue(2, 1, int.Parse(row["EffectMiscValue2"].ToString()));
                SetupMiscValueControl(3, int.Parse(row["Effect3"].ToString()), int.Parse(row["EffectApplyAuraName3"].ToString()));
                SetMiscValue(3, 1, int.Parse(row["EffectMiscValue3"].ToString()));

                if (isTbcOrGreater)
                {
                    // MiscValueB1.ThreadSafeText = row["EffectMiscValueB1"].ToString();
                    // creation is already handled by setupMiscValueControl
                    SetMiscValue(1, 2, int.Parse(row["EffectMiscValueB1"].ToString()));
                    SetMiscValue(2, 2, int.Parse(row["EffectMiscValueB2"].ToString()));
                    SetMiscValue(3, 2, int.Parse(row["EffectMiscValueB3"].ToString()));
                }
                // MiscValueB1.IsEnabled = isTbcOrGreater;

                uint familyName = uint.Parse(row["SpellFamilyName"].ToString());
                AddFamilyIfNotExists(familyName);
                SpellFamilyName.SetTextFromIndex(familyName);
                if (!isWotlkOrGreater)
                {
                    SpellFamilyFlags.ThreadSafeText = row["SpellFamilyFlags1"].ToString();
                    SpellFamilyFlags1.ThreadSafeText = row["SpellFamilyFlags2"].ToString();

                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => 
                        spellFamilyClassMaskParser?.UpdateSpellFamilyClassMask(this, familyName, isWotlkOrGreater, adapter, null)));
                }
                else
                {
                    SpellMask11.ThreadSafeText = row["EffectSpellClassMaskA1"].ToString();
                    SpellMask21.ThreadSafeText = row["EffectSpellClassMaskA2"].ToString();
                    SpellMask31.ThreadSafeText = row["EffectSpellClassMaskA3"].ToString();
                    SpellMask12.ThreadSafeText = row["EffectSpellClassMaskB1"].ToString();
                    SpellMask22.ThreadSafeText = row["EffectSpellClassMaskB2"].ToString();
                    SpellMask32.ThreadSafeText = row["EffectSpellClassMaskB3"].ToString();
                    SpellMask13.ThreadSafeText = row["EffectSpellClassMaskC1"].ToString();
                    SpellMask23.ThreadSafeText = row["EffectSpellClassMaskC2"].ToString();
                    SpellMask33.ThreadSafeText = row["EffectSpellClassMaskC3"].ToString();

                    SpellFamilyFlags.ThreadSafeText = row["SpellFamilyFlags"].ToString();
                    SpellFamilyFlags1.ThreadSafeText = row["SpellFamilyFlags1"].ToString();
                    SpellFamilyFlags2.ThreadSafeText = row["SpellFamilyFlags2"].ToString();

                    var masks = new List<uint>
                    {
                        uint.Parse(row["EffectSpellClassMaskA1"].ToString()),
                        uint.Parse(row["EffectSpellClassMaskA2"].ToString()),
                        uint.Parse(row["EffectSpellClassMaskA3"].ToString()),
                        uint.Parse(row["EffectSpellClassMaskB1"].ToString()),
                        uint.Parse(row["EffectSpellClassMaskB2"].ToString()),
                        uint.Parse(row["EffectSpellClassMaskB3"].ToString()),
                        uint.Parse(row["EffectSpellClassMaskC1"].ToString()),
                        uint.Parse(row["EffectSpellClassMaskC2"].ToString()),
                        uint.Parse(row["EffectSpellClassMaskC3"].ToString())
                    };

                    UpdateSpellMaskCheckBox(masks[0], SpellMask11);
                    UpdateSpellMaskCheckBox(masks[1], SpellMask21);
                    UpdateSpellMaskCheckBox(masks[2], SpellMask31);
                    UpdateSpellMaskCheckBox(masks[3], SpellMask12);
                    UpdateSpellMaskCheckBox(masks[4], SpellMask22);
                    UpdateSpellMaskCheckBox(masks[5], SpellMask32);
                    UpdateSpellMaskCheckBox(masks[6], SpellMask13);
                    UpdateSpellMaskCheckBox(masks[7], SpellMask23);
                    UpdateSpellMaskCheckBox(masks[8], SpellMask33);

                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => 
                        spellFamilyClassMaskParser.UpdateSpellFamilyClassMask(this, familyName, isWotlkOrGreater, GetDBAdapter(), masks)));
                }
                SpellFamilyFlags2.IsEnabled = isWotlkOrGreater;
                ToggleAllSpellMaskCheckBoxes(isWotlkOrGreater);

                SpellVisual1.ThreadSafeText = row["SpellVisual1"].ToString();
                SpellVisual2.ThreadSafeText = row["SpellVisual2"].ToString();

                ManaCostPercent.ThreadSafeText = row["ManaCostPercentage"].ToString();
                StartRecoveryCategory.ThreadSafeText = row["StartRecoveryCategory"].ToString();
                StartRecoveryTime.ThreadSafeText = row["StartRecoveryTime"].ToString();
                MaxTargetsLevel.ThreadSafeText = row["MaximumTargetLevel"].ToString();
                MaxTargets.ThreadSafeText = row["MaximumAffectedTargets"].ToString();
                SpellDamageType.ThreadSafeIndex = int.Parse(row["DamageClass"].ToString());
                PreventionType.ThreadSafeIndex = int.Parse(row["PreventionType"].ToString());
                EffectDamageMultiplier1.ThreadSafeText = row["EffectDamageMultiplier1"].ToString();
                EffectDamageMultiplier2.ThreadSafeText = row["EffectDamageMultiplier2"].ToString();
                EffectDamageMultiplier3.ThreadSafeText = row["EffectDamageMultiplier3"].ToString();

                if (isTbcOrGreater)
                {
                    updateProgress("Updating totem categories & load area groups...");
                    var loadTotemCategories = (TotemCategory)DBCManager.GetInstance().FindDbcForBinding("TotemCategory");
                    result = adapter.Query($"SELECT `TotemCategory1`, `TotemCategory2` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0];
                    IDs = new[] { uint.Parse(result[0].ToString()), uint.Parse(result[1].ToString()) };
                    TotemCategory1.ThreadSafeIndex = loadTotemCategories.UpdateTotemCategoriesSelection(IDs[0]);
                    TotemCategory2.ThreadSafeIndex = loadTotemCategories.UpdateTotemCategoriesSelection(IDs[1]);
                }
                TotemCategory1.IsEnabled = isTbcOrGreater;
                TotemCategory2.IsEnabled = isTbcOrGreater;
                if (isWotlkOrGreater)
                {
                    var loadAreaGroups = (AreaGroup)DBCManager.GetInstance().FindDbcForBinding("AreaGroup");
                    AreaGroup.ThreadSafeIndex = loadAreaGroups.UpdateAreaGroupSelection(uint.Parse(adapter.Query(
                        $"SELECT `AreaGroupID` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0][0].ToString()));
                }
                AreaGroup.IsEnabled = isWotlkOrGreater;

                updateProgress("Updating school mask...");
                mask = uint.Parse(row["SchoolMask"].ToString());
                if (isTbcOrGreater)
                {
                    S1.ThreadSafeChecked = ((mask & 0x01) != 0);
                    S2.ThreadSafeChecked = ((mask & 0x02) != 0);
                    S3.ThreadSafeChecked = ((mask & 0x04) != 0);
                    S4.ThreadSafeChecked = ((mask & 0x08) != 0);
                    S5.ThreadSafeChecked = ((mask & 0x10) != 0);
                    S6.ThreadSafeChecked = ((mask & 0x20) != 0);
                    S7.ThreadSafeChecked = ((mask & 0x40) != 0);
                }
                else
                {
                    S1.ThreadSafeChecked = mask == 1;
                    S2.ThreadSafeChecked = mask == 2;
                    S3.ThreadSafeChecked = mask == 3;
                    S4.ThreadSafeChecked = mask == 4;
                    S5.ThreadSafeChecked = mask == 5;
                    S6.ThreadSafeChecked = mask == 6;
                    S7.ThreadSafeChecked = mask == 7;
                }


                if (isWotlkOrGreater)
                {
                    updateProgress("Updating rune costs...");
                    var loadRuneCosts = (SpellRuneCost)DBCManager.GetInstance().FindDbcForBinding("SpellRuneCost");
                    RuneCost.ThreadSafeIndex = loadRuneCosts.UpdateSpellRuneCostSelection(uint.Parse(adapter.Query(
                        $"SELECT `RuneCostID` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0][0].ToString()));
                }
                RuneCost.IsEnabled = isWotlkOrGreater;

                updateProgress("Updating spell missile & effect bonus multiplier...");
                if (isWotlkOrGreater)
                {
                    EffectBonusMultiplier1.ThreadSafeText = row["EffectBonusMultiplier1"].ToString();
                    EffectBonusMultiplier2.ThreadSafeText = row["EffectBonusMultiplier2"].ToString();
                    EffectBonusMultiplier3.ThreadSafeText = row["EffectBonusMultiplier3"].ToString();
                }
                EffectBonusMultiplier1.IsEnabled = isWotlkOrGreater;
                EffectBonusMultiplier2.IsEnabled = isWotlkOrGreater;
                EffectBonusMultiplier3.IsEnabled = isWotlkOrGreater;
                if (isWotlkOrGreater)
                {
                    SpellMissileID.ThreadSafeText = row["SpellMissileID"].ToString();

                    updateProgress("Updating spell description variables & difficulty selection...");
                    var loadDifficulties = (SpellDifficulty)DBCManager.GetInstance().FindDbcForBinding("SpellDifficulty");
                    var loadDescriptionVariables = (SpellDescriptionVariables)DBCManager.GetInstance().FindDbcForBinding("SpellDescriptionVariables");
                    SpellDescriptionVariables.ThreadSafeIndex = loadDescriptionVariables.UpdateSpellDescriptionVariablesSelection(
                        uint.Parse(adapter.Query($"SELECT `SpellDescriptionVariableID` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0][0].ToString()));

                    Difficulty.ThreadSafeIndex = loadDifficulties.UpdateDifficultySelection(uint.Parse(adapter.Query(
                        $"SELECT `SpellDifficultyID` FROM `{"spell"}` WHERE `ID` = '{selectedID}'").Rows[0][0].ToString()));
                }
                SpellMissileID.IsEnabled = isWotlkOrGreater;
                SpellDescriptionVariables.IsEnabled = isWotlkOrGreater;
                Difficulty.IsEnabled = isWotlkOrGreater;

                FilterClassMaskSpells1.IsEnabled = isWotlkOrGreater;
                FilterClassMaskSpells2.IsEnabled = isWotlkOrGreater;
                FilterClassMaskSpells3.IsEnabled = isWotlkOrGreater;
            }
            catch (Exception e)
            {
                HandleErrorMessage(string.Format("{0}\n\n{1}", "", e, e.InnerException));
            }
            adapter.Updating = false;
        }

        #region VisualTab
        private void UpdateSpellVisualTab(uint selectedId, uint selectedKit = 0, bool forceRefresh = false)
        {
            SelectedVisualPath.Content = selectedId.ToString();
            if (!forceRefresh && 
                _currentVisualController?.VisualId == selectedId && 
                _currentVisualController?.NextLoadAttachmentId == 0)
            {
                return;
            }
            var attachId = _currentVisualController?.NextLoadAttachmentId;
            var controller = selectedId > 0 ? new VisualController(selectedId, adapter) : null;
            if (attachId.HasValue && attachId.Value > 0)
            {
                controller.NextLoadAttachmentId = attachId.Value;
            }
            _currentVisualController = controller;
            // FIXME(Harry): Some of these can probably be loaded in earlier versions
            if (WoWVersionManager.IsWotlkOrGreaterSelected)
            {
                UpdateSpellVisualKitList(controller?.VisualKits, selectedKit);
                UpdatePasteability(controller != null);
                UpdateSpellVisualProperties(controller != null ? controller.VisualId : 0);
                UpdateSpellVisualMotionEditor(controller != null ? controller.VisualId : 0);
                UpdateSpellMissileMotionEditor(controller);
                UpdateSpellMotionEditor(controller != null ? controller.MissileMotion : 0);
            }
            else
            {
                UpdateSpellVisualProperties(0);
                VisualNew.IsEnabled = false;
                UpdateSpellVisualMotionEditor(0);
                MissileEntryNewButton.IsEnabled = false;
                UpdateSpellMissileMotionEditor(null);
                UpdateSpellMotionEditor(0);
                VisualMissileNewButton.IsEnabled = false;
            }
        }

        private void UpdatePasteability(bool enablePaste)
        {
            if (VisualSettingsGrid.Children.Count > 0)
            {
                var kitList = VisualSettingsGrid.Children[0] as ListBox;
                UpdateVisualListPasteEnabled(VisualController.GetCopiedKitEntry() != null, 
                    kitList, 
                    VisualSettingsGrid);
                if (VisualEffectsListGrid.Children.Count > 0)
                {
                    var effectList = VisualEffectsListGrid.Children[0] as ListBox;
                    UpdateVisualListPasteEnabled(enablePaste && VisualController.GetCopiedEffectEntry() != null && kitList.Items.Count > 0,
                        effectList,
                        VisualEffectsListGrid);
                }
            }
        }

        private void UpdateSpellVisualKitList(List<IListEntry> kitEntries, uint selectedKit = 0)
        {
            // Reuse the existing ListBox if it already exists
            ListBox scrollList;
            var exists = VisualSettingsGrid.Children.Count == 1 && VisualSettingsGrid.Children[0] is ListBox;
            if (exists)
            {
                scrollList = VisualSettingsGrid.Children[0] as ListBox;
            }
            else
            {
                scrollList = new ListBox()
                {
                    Margin = new Thickness(5)
                };
                scrollList.SelectionChanged += VisualScrollList_SelectionChanged;
                var animationDbc = DBCManager.GetInstance().FindDbcForBinding("AnimationData") as AnimationData;
                StartAnimIdCombo.ItemsSource = ConvertBoxListToLabels(animationDbc.GetAllBoxes());
                AnimationIdCombo.ItemsSource = ConvertBoxListToLabels(animationDbc.GetAllBoxes());
            }
            kitEntries?.ForEach(entry => entry.SetDeleteClickAction(DeleteVisualKitAction));
            kitEntries?.ForEach(entry => entry.SetCopyClickAction(CopyVisualKitAction));
            kitEntries?.ForEach(entry => entry.SetPasteClickAction(PasteVisualKitAction));
            scrollList.ClearValue(ItemsControl.ItemsSourceProperty);
            scrollList.ItemsSource = kitEntries;
            if (kitEntries != null && kitEntries.Count > 0)
            {
                VisualKitListEntry kitEntry = selectedKit > 0 ?
                    kitEntry = kitEntries.Select(kit => kit as VisualKitListEntry)
                        .FirstOrDefault(kit => uint.Parse(kit.KitRecord[0].ToString()) == selectedKit) :
                    kitEntries[0] as VisualKitListEntry;
                if (kitEntry == null)
                {
                    kitEntry = kitEntries[0] as VisualKitListEntry;
                }
                UpdateSpellVisualEditor(kitEntry);
                scrollList.SelectedItem = kitEntry;
            }
            else
            {
                ClearSpellVisualEfectList();
                ClearStaticSpellVisualElements();
            }
            if (!exists)
            {
                VisualSettingsGrid.Children.Add(scrollList);
            }
        }

        private void CopyVisualKitAction(IListEntry selectedEntry)
        {
            var exists = VisualSettingsGrid.Children.Count == 1 && VisualSettingsGrid.Children[0] is ListBox;
            if (!exists)
            {
                return;
            }
            var scrollList = VisualSettingsGrid.Children[0] as ListBox;
            UpdateVisualListPasteEnabled(true, scrollList, VisualSettingsGrid);
        }

        private void PasteVisualKitAction() => PasteVisualKitAction(VisualController.GetCopiedKitEntry());
        private void PasteVisualEffectAction() => PasteVisualEffectAction(VisualController.GetCopiedEffectEntry());

        private void PasteVisualKitAction(IListEntry selectedEntry)
        {
            var exists = VisualSettingsGrid.Children.Count == 1 && VisualSettingsGrid.Children[0] is ListBox;
            if (!exists || selectedEntry == null)
            {
                return;
            }
            var scrollList = VisualSettingsGrid.Children[0] as ListBox;
            var entries = scrollList.ItemsSource as List<IListEntry>;

            var visualIdStr = SpellVisual1.ThreadSafeText?.ToString();
            if (visualIdStr == null || !uint.TryParse(visualIdStr, out uint visualId))
            {
                return;
            }
            var keyResource = WoWVersionManager.GetInstance().LookupKeyResource();
            var itemToPaste = VisualController.GetCopiedKitEntry();
            var pasteEntry = new VisualPasteListEntry(itemToPaste, 
                _currentVisualController?.GetAvailableFields(itemToPaste) ?? keyResource.KitColumnKeys.ToList());
            pasteEntry.SetDeleteClickAction(entry =>
            {
                entries = scrollList.ItemsSource as List<IListEntry>;
                entries.Remove(pasteEntry);
                scrollList.ClearValue(ItemsControl.ItemsSourceProperty);
                scrollList.ItemsSource = entries;
            });
            pasteEntry.SetPasteClickAction(entry =>
            {
                var key = pasteEntry.SelectedKey();
                if (key == null || key.Length == 0)
                {
                    return;
                }
                var idToCopy = itemToPaste.KitRecord[0].ToString();
                var visualQuery = visualId > 0 ? "SELECT * FROM spellvisual WHERE id = " + visualId : null;
                var visualResults = visualId > 0 ? adapter.Query(visualQuery) : null;
                var kitResults = adapter.Query("SELECT * FROM spellvisualkit WHERE id = " + idToCopy);
                var newKitId = uint.Parse(adapter.Query("SELECT max(id) FROM spellvisualkit").Rows[0][0].ToString()) + 1;
                if (kitResults.Rows.Count == 0 || (visualResults != null && visualResults.Rows.Count == 0))
                {
                    return;
                }
                // Add new spellvisualkit
                var copyRow = kitResults.Rows[0];
                copyRow[0] = newKitId.ToString();
                adapter.Execute($"INSERT INTO spellvisualkit VALUES ({ string.Join(", ", copyRow.ItemArray) })");

                // Update existing spell visual to point to new kit
                if (visualId > 0)
                {
                    var updateRow = visualResults.Rows[0];
                    updateRow.BeginEdit();
                    updateRow[key] = newKitId;
                    updateRow.EndEdit();

                    adapter.CommitChanges(visualQuery, visualResults);
                }
                // Create new spell visual and update kit reference and spell record
                else
                {
                    var parentResults = adapter.Query("SELECT * FROM spellvisual WHERE ID = " + itemToPaste.ParentVisualId);
                    if (parentResults == null || parentResults.Rows.Count == 0)
                    {
                        return;
                    }
                    var newVisualId = uint.Parse(adapter.Query("SELECT max(id) FROM spellvisual").Rows[0][0].ToString()) + 1;
                    var copyParent = parentResults.Rows[0];
                    copyParent[0] = newVisualId;
                    foreach (var _key in keyResource.KitColumnKeys)
                    {
                        copyParent[_key] = 0;
                    }
                    copyParent[key] = newKitId;
                    adapter.Execute($"INSERT INTO spellvisual VALUES ({ string.Join(", ", copyParent.ItemArray) })");
                    SpellVisual1.ThreadSafeText = newVisualId.ToString();
                    visualId = newVisualId;
                    Button_Click(SaveSpellChanges, null);
                }
                _currentVisualController = null;
                UpdateSpellVisualTab(visualId);
                UpdateMainWindow();
            });

            if (entries == null)
            {
                entries = new List<IListEntry>();
            }
            entries.Add(pasteEntry);
            scrollList.ClearValue(ItemsControl.ItemsSourceProperty);
            scrollList.ItemsSource = entries;
        }

        private void PasteVisualEffectAction(IListEntry selectedEntry)
        {
            if (!uint.TryParse(SpellVisual1.ThreadSafeText?.ToString(), out var visualId))
            {
                return;
            }
            var exists = VisualEffectsListGrid.Children.Count == 1 && VisualEffectsListGrid.Children[0] is ListBox;
            exists = exists && VisualSettingsGrid.Children.Count == 1 && VisualSettingsGrid.Children[0] is ListBox;
            if (!exists || selectedEntry == null)
            {
                return;
            }
            var scrollList = VisualEffectsListGrid.Children[0] as ListBox;
            var kitList = VisualSettingsGrid.Children[0] as ListBox;
            uint parentKitId;
            if (kitList.SelectedItem == null)
            {
                parentKitId = uint.Parse((kitList.Items[0] as VisualKitListEntry).KitRecord[0].ToString());
            }
            else
            {
                parentKitId = uint.Parse((kitList.SelectedItem as VisualKitListEntry).KitRecord[0].ToString());
            }
            var entries = scrollList.ItemsSource as List<IListEntry>;

            var effectEntry = VisualController.GetCopiedEffectEntry();
            var pasteEntry = new VisualPasteListEntry(effectEntry,
                _currentVisualController?.GetAvailableFields(effectEntry) ?? 
                WoWVersionManager.GetInstance().LookupKeyResource().EffectColumnKeys.ToList());
            pasteEntry.SetDeleteClickAction(entry =>
            {
                entries = scrollList.ItemsSource as List<IListEntry>;
                entries.Remove(pasteEntry);
                scrollList.ClearValue(ItemsControl.ItemsSourceProperty);
                scrollList.ItemsSource = entries;
            });
            pasteEntry.SetPasteClickAction(entry =>
            {
                var key = pasteEntry.SelectedKey();
                if (key == null || key.Length == 0)
                {
                    return;
                }
                var idToCopy = effectEntry.IsAttachment ? 
                                effectEntry.AttachRecord[0].ToString() :
                                effectEntry.EffectRecord[0].ToString();

                var effectId = idToCopy;
                DataRow copyAttachRow = null;
                // Create new attachment if exists on current effect
                if (effectEntry.IsAttachment)
                {
                    // Create new attachment
                    var attachResults = adapter.Query("SELECT * FROM spellvisualkitmodelattach WHERE id = " + idToCopy);
                    var newAttachId = uint.Parse(adapter.Query("SELECT max(id) FROM spellvisualkitmodelattach").Rows[0][0].ToString()) + 1;
                    copyAttachRow = attachResults.Rows[0];

                    effectId = copyAttachRow["SpellVisualEffectNameId"].ToString();

                    copyAttachRow[0] = newAttachId.ToString();
                }
                // Create and persist new effect
                var effectResults = adapter.Query("SELECT * FROM spellvisualeffectname WHERE id = " + effectId);
                var newEffectId = uint.Parse(adapter.Query("SELECT max(id) FROM spellvisualeffectname").Rows[0][0].ToString()) + 1;
                var copyRow = effectResults.Rows[0];
                copyRow[0] = newEffectId.ToString();
                var escapedItems = copyRow.ItemArray.Select(item => "'" + adapter.EscapeString(item.ToString()) + "'");
                adapter.Execute($"INSERT INTO spellvisualeffectname VALUES ({ string.Join(", ", escapedItems) })");
                // Update kit to point to new effect if no attachment
                if (!effectEntry.IsAttachment)
                {
                    if (kitList.SelectedItem is VisualKitListEntry selectedKitEntry)
                        adapter.Execute($"UPDATE spellvisualkit SET { key } = { newEffectId } WHERE ID = { selectedKitEntry.KitRecord[0] }");
                    else
                        adapter.Execute($"UPDATE spellvisualkit SET { key } = { newEffectId } WHERE ID = { (kitList.Items.CurrentItem as VisualKitListEntry).KitRecord[0] }");
                }
                // If attachment update effect with new id and persist
                else
                {
                    copyAttachRow["ParentSpellVisualKitId"] = parentKitId;
                    copyAttachRow["SpellVisualEffectNameId"] = newEffectId;
                    escapedItems = copyAttachRow.ItemArray.Select(item => "\"" + item + "\"");
                    adapter.Execute($"INSERT INTO spellvisualkitmodelattach VALUES ({ string.Join(", ", escapedItems) })");
                }
                _currentVisualController = null;
                UpdateSpellVisualTab(visualId, effectEntry.ParentKitId);
            });

            if (entries == null)
            {
                entries = new List<IListEntry>();
            }
            entries.Add(pasteEntry);
            scrollList.ClearValue(ItemsControl.ItemsSourceProperty);
            scrollList.ItemsSource = entries;
        }

        private void DeleteVisualKitAction(IListEntry entry)
        {
            var exists = VisualSettingsGrid.Children.Count == 1 && VisualSettingsGrid.Children[0] is ListBox;
            if (!exists)
            {
                return;
            }
            var scrollList = VisualSettingsGrid.Children[0] as ListBox;
            var entries = scrollList.ItemsSource as List<IListEntry>;
            entries.Remove(entry);
            scrollList.ClearValue(ItemsControl.ItemsSourceProperty);
            scrollList.ItemsSource = entries;
            if (entries.Count > 0)
            {
                scrollList.SelectedItem = entries[0];
            }
            else
            {
                ClearStaticSpellVisualElements();
                ClearSpellVisualEfectList();
            }
        }

        private void ClearSpellVisualEfectList()
        {
            if (VisualEffectsListGrid.Children.Count == 1 && VisualEffectsListGrid.Children[0] is ListBox scrollList)
            {
                scrollList.ClearValue(ItemsControl.ItemsSourceProperty);
                scrollList.ItemsSource = null;
            }
        }

        private void InitialiseSpellVisualEffectList()
        {
            // Get scroll list if it exists
            ListBox scrollList = null;
            var exists = VisualEffectsListGrid.Children.Count == 1 && VisualEffectsListGrid.Children[0] is ListBox;
            if (exists)
            {
                scrollList = VisualEffectsListGrid.Children[0] as ListBox;
            }
            if (scrollList != null)
            {
                return;
            }
            // Build ScrollList
            scrollList = new ListBox()
            {
                Margin = new Thickness(5)
            };
            scrollList.SelectionChanged += VisualScrollList_SelectionChanged;
            VisualEffectsListGrid.Children.Add(scrollList);
            // Populate animation combo box
            if (VisualAttachmentIdCombo.Items.Count == 0)
            {
                var names = System.Enum.GetValues(typeof(SpellVisualKitModelAttach.AttachmentPoint))
                    .Cast<SpellVisualKitModelAttach.AttachmentPoint>().ToList();
                var animComboSource = new List<Label>(names.Count);
                names.ForEach((name) => animComboSource.Add(new Label() { Content = $"{ (int)name } - { name.ToString() }" }));
                VisualAttachmentIdCombo.ItemsSource = animComboSource;
            }
        }

        private void VisualScrollList_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            var added_items = args.AddedItems;
            var listBox = sender as ListBox;
            if (added_items.Count == 1)
            {
                if (listBox.SelectedItem is VisualKitListEntry)
                {
                    if (_currentVisualController != null && _currentVisualController.CancelNextLoad)
                    {
                        _currentVisualController.CancelNextLoad = false;
                        return;
                    }
                    UpdateSpellVisualEditor(listBox.SelectedItem as VisualKitListEntry);
                }
                else if (listBox.SelectedItem is VisualEffectListEntry)
                {
                    UpdateSpellEffectEditor(listBox.SelectedItem as VisualEffectListEntry);
                }
            }
            else
            {
                listBox.UnselectAll();
            }
        }

        private void UpdateSpellVisualEditor(VisualKitListEntry entry)
        {
            var animationDbc = DBCManager.GetInstance().FindDbcForBinding("AnimationData") as AnimationData;
            var record = entry.KitRecord;

            var id = uint.Parse(record["StartAnimId"].ToString());
            var matchRecord = animationDbc.Lookups.SingleOrDefault((container) => container.ID == id);
            int index = 0;
            if (matchRecord != null)
            {
                index = matchRecord.ComboBoxIndex;
            }
            StartAnimIdCombo.SelectedIndex = index;
            id = uint.Parse(record["AnimationId"].ToString());
            matchRecord = animationDbc.Lookups.SingleOrDefault((container) => container.ID == id);
            index = 0;
            if (matchRecord != null)
            {
                index = matchRecord.ComboBoxIndex;
            }
            AnimationIdCombo.SelectedIndex = index;
            SpecialEffect1Txt.Text = record["SpecialEffect1"].ToString();
            SpecialEffect2Txt.Text = record["SpecialEffect2"].ToString();
            SpecialEffect3Txt.Text = record["SpecialEffect3"].ToString();
            WorldEffectTxt.Text = record["WorldEffect"].ToString();
            SoundIdTxt.Text = record["SoundID"].ToString();
            ShakeIdTxt.Text = record["ShakeID"].ToString();
            VisualFlagsTxt.Text = record["Flags"].ToString();

            if (VisualEffectsListGrid.Children.Count != 1)
            {
                return;
            }
            var listBox = VisualEffectsListGrid.Children[0] as ListBox;
            if (listBox == null)
            {
                return;
            }
            var effects = entry.GetAllEffectsAndAttachmentsEntries();
            effects?.ForEach((effect) => effect.SetCopyClickAction(CopyVisualEffectAction));
            effects?.ForEach((effect) => effect.SetDeleteClickAction(DeleteVisualEffectAction));
            effects?.ForEach((effect) => effect.SetPasteClickAction(PasteVisualEffectAction));
            listBox.ClearValue(ItemsControl.ItemsSourceProperty);
            listBox.ItemsSource = effects;
            if (listBox.Items.Count > 0)
            {
                var effectIndex = 0;
                var effect = effects[effectIndex] as VisualEffectListEntry;
                var attachmentId = _currentVisualController?.NextLoadAttachmentId;
                if (attachmentId.HasValue && attachmentId.Value > 0)
                {
                    //foreach (var possibleMatch in effects)
                    for (int i = 0; i < effects.Count; ++i)
                    {
                        if (effects[i] is VisualEffectListEntry match && match.IsAttachment)
                        {
                            if (uint.Parse(match.AttachRecord[0].ToString()) == attachmentId)
                            {
                                effect = match;
                                effectIndex = i;
                                _currentVisualController.NextLoadAttachmentId = 0;
                                _currentVisualController.CancelNextLoad = true;
                                break;
                            }
                        }
                    }
                }
                //UpdateSpellEffectEditor(effect);
                listBox.SelectedItem = effects[effectIndex];
            }
            else
            {
                UpdateSpellEffectEditor(null);
            }
        }

        private void CopyVisualEffectAction(IListEntry copiedEntry)
        {
            var exists = VisualEffectsListGrid.Children.Count == 1 && VisualEffectsListGrid.Children[0] is ListBox;
            if (!exists)
            {
                return;
            }
            var scrollList = VisualEffectsListGrid.Children[0] as ListBox;
            UpdateVisualListPasteEnabled(true, scrollList, VisualEffectsListGrid);
        }

        private void UpdateVisualListPasteEnabled(bool enablePaste, ListBox scrollList, Grid parentGrid)
        {
            var entries = scrollList.ItemsSource as List<IListEntry>;
            entries?.Select(entry => entry as StackPanel)
                ?.Select(entry => entry?.ContextMenu as ListContextMenu)
                ?.ToList()
                ?.ForEach(entry => entry?.SetCanPaste(enablePaste));
            if (parentGrid.ContextMenu is ListContextMenu menu)
            {
                menu.SetCanPaste(enablePaste);
            }
        }

        private void DeleteVisualEffectAction(IListEntry entry)
        {
            var selectedKit = (entry as VisualEffectListEntry).ParentKitId;
            var parentVisual = (entry as VisualEffectListEntry).ParentVisualId;
            _currentVisualController = null;
            UpdateSpellVisualTab(parentVisual, selectedKit);
        }

        private void UpdateSpellEffectEditor(VisualEffectListEntry entry)
        {
            var record = entry?.EffectRecord;

            VisualEffectNameTxt.Text = entry != null ? record["Name"].ToString() : string.Empty;
            VisualEffectFilePathTxt.Text = entry != null ? record["FilePath"].ToString() : string.Empty;
            VisualEffectAreaEffectSizeTxt.Text = entry != null ? record["AreaEffectSize"].ToString() : string.Empty;
            VisualEffectScaleTxt.Text = entry != null ? record["Scale"].ToString() : string.Empty;
            if (WoWVersionManager.IsWotlkOrGreaterSelected)
            {
                VisualEffectMinAllowedScaleTxt.Text = entry != null ? record["MinAllowedScale"].ToString() : string.Empty;
                VisualEffectMaxAllowedScaleTxt.Text = entry != null ? record["MaxAllowedScale"].ToString() : string.Empty;
                VisualEffectMinAllowedScaleTxt.IsEnabled = true;
                VisualEffectMaxAllowedScaleTxt.IsEnabled = true;
            }
            else
            {
                VisualEffectMinAllowedScaleTxt.IsEnabled = false;
                VisualEffectMaxAllowedScaleTxt.IsEnabled = false;
            }

            var attachRecord = entry?.AttachRecord;
            ToggleVisualAttachments(attachRecord != null);
            if (attachRecord != null)
            {
                VisualAttachmentIdCombo.SelectedIndex = (int)uint.Parse(attachRecord["AttachmentId"].ToString());
                VisualAttachmentOffsetXTxt.Text = attachRecord["OffsetX"].ToString();
                VisualAttachmentOffsetYTxt.Text = attachRecord["OffsetY"].ToString();
                VisualAttachmentOffsetZTxt.Text = attachRecord["OffsetZ"].ToString();
                VisualAttachmentOffsetYawTxt.Text = attachRecord["Yaw"].ToString();
                VisualAttachmentOffsetPitchTxt.Text = attachRecord["Pitch"].ToString();
                VisualAttachmentOffsetRollTxt.Text = attachRecord["Roll"].ToString();
            }

            NewVisualAttachBtn.IsEnabled = WoWVersionManager.IsWotlkOrGreaterSelected && entry != null && !entry.IsAttachment;
        }

        private void ToggleVisualAttachments(bool enabled)
        {
            VisualAttachmentIdCombo.IsEnabled = enabled;
            VisualAttachmentOffsetXTxt.IsEnabled = enabled;
            VisualAttachmentOffsetYTxt.IsEnabled = enabled;
            VisualAttachmentOffsetZTxt.IsEnabled = enabled;
            VisualAttachmentOffsetYawTxt.IsEnabled = enabled;
            VisualAttachmentOffsetPitchTxt.IsEnabled = enabled;
            VisualAttachmentOffsetRollTxt.IsEnabled = enabled;
            if (!enabled)
            {
                ClearStaticSpellVisualAttachElements();
            }
        }

        private void ClearStaticSpellVisualElements()
        {
            SpecialEffect1Txt.Text = string.Empty;
            SpecialEffect2Txt.Text = string.Empty;
            SpecialEffect3Txt.Text = string.Empty;
            WorldEffectTxt.Text = string.Empty;
            SoundIdTxt.Text = string.Empty;
            ShakeIdTxt.Text = string.Empty;
            VisualFlagsTxt.Text = string.Empty;
            VisualEffectNameTxt.Text = string.Empty;
            VisualEffectFilePathTxt.Text = string.Empty;
            VisualEffectAreaEffectSizeTxt.Text = string.Empty;
            VisualEffectScaleTxt.Text = string.Empty;
            VisualEffectMinAllowedScaleTxt.Text = string.Empty;
            VisualEffectMaxAllowedScaleTxt.Text = string.Empty;
            ClearStaticSpellVisualAttachElements();
        }

        private void ClearStaticSpellVisualAttachElements()
        {
            VisualAttachmentOffsetXTxt.Text = string.Empty;
            VisualAttachmentOffsetYTxt.Text = string.Empty;
            VisualAttachmentOffsetZTxt.Text = string.Empty;
            VisualAttachmentOffsetYawTxt.Text = string.Empty;
            VisualAttachmentOffsetPitchTxt.Text = string.Empty;
            VisualAttachmentOffsetRollTxt.Text = string.Empty;
        }
        
        private void NewVisualAttachBtn_Click(object sender, RoutedEventArgs e)
        {
            if (VisualEffectsListGrid.Children.Count == 0 || !(VisualEffectsListGrid.Children[0] is ListBox scrollList))
            {
                return;
            }
            var effectEntry = scrollList.SelectedItem as VisualEffectListEntry;
            if (effectEntry == null)
            {
                return;
            }
            var key = effectEntry.EffectName;
            var effectId = effectEntry.EffectRecord[0].ToString();
            var kitId = effectEntry.ParentKitId;
            var attachId = uint.Parse(adapter.Query("SELECT MAX(id) FROM spellvisualkitmodelattach").Rows[0][0].ToString()) + 1;
            adapter.Execute($"UPDATE spellvisualkit SET { key } = 0 WHERE id = { kitId }");
            adapter.Execute($"INSERT INTO spellvisualkitmodelattach (id, ParentSpellVisualKitId, SpellVisualEffectNameId) VALUES ({attachId}, {kitId}, {effectId})");
            
            _currentVisualController.NextLoadAttachmentId = attachId;
            UpdateSpellVisualTab(effectEntry.ParentVisualId, effectEntry.ParentKitId);
        }

        private void SaveVisualChangesBtn_Click(object sender, RoutedEventArgs e)
        {
            // Get selected kit
            var exists = VisualSettingsGrid.Children.Count == 1 && VisualSettingsGrid.Children[0] is ListBox;
            if (!exists)
            {
                return;
            }
            var kitScrollList = VisualSettingsGrid.Children[0] as ListBox;
            var selectedKitItem = kitScrollList.SelectedItem;
            if (selectedKitItem == null)
            {
                return;
            }
            var selectedKit = selectedKitItem as VisualKitListEntry;
            if (selectedKit == null)
            {
                return;
            }
            // Get selected effect/attachment
            if (VisualEffectsListGrid.Children.Count == 0 || !(VisualEffectsListGrid.Children[0] is ListBox effectScrolList))
            {
                return;
            }
            var selectedEffect = effectScrolList.SelectedItem as VisualEffectListEntry;
            // Save values to kit
            var kitId = selectedKit.KitRecord[0].ToString();
            var kitQuery = "SELECT * FROM spellvisualkit WHERE id = " + kitId;
            var kitResults = adapter.Query(kitQuery);
            var kitRecord = kitResults.Rows[0];
            kitRecord.BeginEdit();
            var selectedStartAnim = StartAnimIdCombo.SelectedItem.ToString();
            kitRecord["StartAnimId"] = selectedStartAnim.Length > 0 ? selectedStartAnim.Substring(0, selectedStartAnim.IndexOf(' ')) : "0";
            var selectedAnim = AnimationIdCombo.SelectedItem.ToString();
            kitRecord["AnimationId"] = selectedAnim.Length > 0 ? selectedAnim.Substring(0, selectedAnim.IndexOf(' ')) : "0";
            kitRecord["SpecialEffect1"] = SpecialEffect1Txt.Text;
            kitRecord["SpecialEffect2"] = SpecialEffect2Txt.Text;
            kitRecord["SpecialEffect3"] = SpecialEffect3Txt.Text;
            kitRecord["WorldEffect"] = WorldEffectTxt.Text;
            kitRecord["SoundID"] = SoundIdTxt.Text;
            kitRecord["ShakeID"] = ShakeIdTxt.Text;
            kitRecord["Flags"] = VisualFlagsTxt.Text;
            kitRecord.EndEdit();
            adapter.CommitChanges(kitQuery, kitResults);
            var message = "Saved Kit " + kitId;
            // Save values to effect
            if (selectedEffect == null)
            {
                return;
            }
            var effectId = selectedEffect.IsAttachment ? selectedEffect.AttachRecord[0].ToString() : selectedEffect.EffectRecord[0].ToString();
            if (selectedEffect.IsAttachment)
            {
                var attachQuery = "SELECT * FROM spellvisualkitmodelattach WHERE id = " + effectId;
                var attachResults = adapter.Query(attachQuery);
                var attachRecord = attachResults.Rows[0];
                attachRecord.BeginEdit();
                attachRecord["AttachmentId"] = VisualAttachmentIdCombo.SelectedIndex;
                attachRecord["OffsetX"] = VisualAttachmentOffsetXTxt.Text;
                attachRecord["OffsetY"] = VisualAttachmentOffsetYTxt.Text;
                attachRecord["OffsetZ"] = VisualAttachmentOffsetZTxt.Text;
                attachRecord["Yaw"] = VisualAttachmentOffsetYawTxt.Text;
                attachRecord["Pitch"] = VisualAttachmentOffsetPitchTxt.Text;
                attachRecord["Roll"] = VisualAttachmentOffsetRollTxt.Text;
                attachRecord.EndEdit();
                adapter.CommitChanges(attachQuery, attachResults);
                message += ", saved attachment " + effectId;
            }
            else
            {
                var effectQuery = "SELECT * FROM spellvisualeffectname WHERE id = " + effectId;
                var effectResults = adapter.Query(effectQuery);
                var effectRecord = effectResults.Rows[0];
                effectRecord.BeginEdit();
                effectRecord["Name"] = VisualEffectNameTxt.Text;
                effectRecord["FilePath"] = VisualEffectFilePathTxt.Text;
                effectRecord["AreaEffectSize"] = VisualEffectAreaEffectSizeTxt.Text;
                effectRecord["Scale"] = VisualEffectScaleTxt.Text;
                if (WoWVersionManager.IsWotlkOrGreaterSelected)
                {
                    effectRecord["MinAllowedScale"] = VisualEffectMinAllowedScaleTxt.Text;
                    effectRecord["MaxAllowedScale"] = VisualEffectMaxAllowedScaleTxt.Text;
                }
                effectRecord.EndEdit();
                adapter.CommitChanges(effectQuery, effectResults);
                message += ", saved effect " + effectId;
            }
            ShowFlyoutMessage(message);
            _currentVisualController = null;
            UpdateSpellVisualTab(selectedKit.ParentVisualId, uint.Parse(kitId));
        }

        private void UpdateSpellVisualProperties(uint visualId)
        {
            DataRow entry = null;
            var spellSelected = selectedID > 0;
            if (visualId > 0)
            {
                using (var results = adapter.Query("SELECT HasMissile, MissileModel, MissilePathType, " +
                    "MissileDestinationAttachment, MissileSound, AnimEventSoundId, " +
                    "MissileAttachment, MissileFollowGroundHeight, MissileFollowDropSpeed, " +
                    "MissileFollowApproach, MissileFollowGroundFlags, MissileMotion, " +
                    "MissileCastOffsetX, MissileCastOffsetY, MissileCastOffsetZ, " +
                    "MissileImpactOffsetX, MissileImpactOffsetY, MissileImpactOffsetZ " +
                    "FROM spellvisual WHERE ID = " + visualId))
                {
                    if (results.Rows.Count > 0)
                    {
                        entry = results.Rows[0];
                    }
                }
            }
            VisualHasMissileTxt.Text = entry != null ? entry["HasMissile"].ToString() : string.Empty;
            VisualMissileModelTxt.Text = entry != null ? entry["MissileModel"].ToString() : string.Empty;
            VisualMissilePathTypeTxt.Text = entry != null ? entry["MissilePathType"].ToString() : string.Empty;
            VisualMissileDestinationAttachmentTxt.Text = entry != null ? entry["MissileDestinationAttachment"].ToString() : string.Empty;
            VisualMissileSoundTxt.Text = entry != null ? entry["MissileSound"].ToString() : string.Empty;
            VisualAnimEventSoundIdTxt.Text = entry != null ? entry["AnimEventSoundId"].ToString() : string.Empty;
            VisualMissileAttachmentTxt.Text = entry != null ? entry["MissileAttachment"].ToString() : string.Empty;
            VisualMissileFollowGroundHeightTxt.Text = entry != null ? entry["MissileFollowGroundHeight"].ToString() : string.Empty;
            VisualMissileFollowDropSpeedTxt.Text = entry != null ? entry["MissileFollowDropSpeed"].ToString() : string.Empty;
            VisualMissileFollowApproachTxt.Text = entry != null ? entry["MissileFollowApproach"].ToString() : string.Empty;
            VisualMissileFollowGroundFlagsTxt.Text = entry != null ? entry["MissileFollowGroundFlags"].ToString() : string.Empty;
            VisualMissileMotionTxt.Text = entry != null ? entry["MissileMotion"].ToString() : string.Empty;
            VisualMissileCastOffsetXTxt.Text = entry != null ? entry["MissileCastOffsetX"].ToString() : string.Empty;
            VisualMissileCastOffsetYTxt.Text = entry != null ? entry["MissileCastOffsetY"].ToString() : string.Empty;
            VisualMissileCastOffsetZTxt.Text = entry != null ? entry["MissileCastOffsetZ"].ToString() : string.Empty;
            VisualMissileImpactOffsetXTxt.Text = entry != null ? entry["MissileImpactOffsetX"].ToString() : string.Empty;
            VisualMissileImpactOffsetYTxt.Text = entry != null ? entry["MissileImpactOffsetY"].ToString() : string.Empty;
            VisualMissileImpactOffsetZTxt.Text = entry != null ? entry["MissileImpactOffsetZ"].ToString() : string.Empty;
            VisualHasMissileTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileModelTxt.IsEnabled = entry != null && spellSelected;
            VisualMissilePathTypeTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileDestinationAttachmentTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileSoundTxt.IsEnabled = entry != null && spellSelected;
            VisualAnimEventSoundIdTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileAttachmentTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileFollowGroundHeightTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileFollowDropSpeedTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileFollowApproachTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileFollowGroundFlagsTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileMotionTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileCastOffsetXTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileCastOffsetYTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileCastOffsetZTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileImpactOffsetXTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileImpactOffsetYTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileImpactOffsetZTxt.IsEnabled = entry != null && spellSelected;
            VisualNew.IsEnabled = entry == null && spellSelected;
            VisualSave.IsEnabled = entry != null && spellSelected;
        }

        private void UpdateSpellVisualMotionEditor(uint visualId)
        {
            DataRow entry = null;
            var spellSelected = selectedID > 0;
            if (visualId > 0)
            {
                using (var results = adapter.Query("SELECT * FROM spellmissile WHERE ID = " + visualId))
                {
                    if (results.Rows.Count > 0)
                    {
                        entry = results.Rows[0];
                    }
                }
            }
            VisualMissileEntryFlagsTxt.Text = entry != null ? entry["flags"].ToString() : string.Empty;
            VisualMissileEntryDefaultPitchMinTxt.Text = entry != null ? entry["defaultPitchMin"].ToString() : string.Empty;
            VisualMissileEntryDefaultPitchMaxTxt.Text = entry != null ? entry["defaultPitchMax"].ToString() : string.Empty;
            VisualMissileEntryDefaultSpeedMinTxt.Text = entry != null ? entry["defaultSpeedMin"].ToString() : string.Empty;
            VisualMissileEntryDefaultSpeedMaxTxt.Text = entry != null ? entry["defaultSpeedMax"].ToString() : string.Empty;
            VisualMissileEntryRandomiseFacingMinTxt.Text = entry != null ? entry["randomizeFacingMin"].ToString() : string.Empty;
            VisualMissileEntryRandomiseFacingMaxTxt.Text = entry != null ? entry["randomizeFacingMax"].ToString() : string.Empty;
            VisualMissileEntryRandomisePitchMinTxt.Text = entry != null ? entry["randomizePitchMin"].ToString() : string.Empty;
            VisualMissileEntryRandomisePitchMaxTxt.Text = entry != null ? entry["randomizePitchMax"].ToString() : string.Empty;
            VisualMissileEntryRandomiseSpeedMinTxt.Text = entry != null ? entry["randomizeSpeedMin"].ToString() : string.Empty;
            VisualMissileEntryRandomiseSpeedMaxTxt.Text = entry != null ? entry["randomizeSpeedMax"].ToString() : string.Empty;
            VisualMissileEntryGravityTxt.Text = entry != null ? entry["gravity"].ToString() : string.Empty;
            VisualMissileEntryMaxDurationTxt.Text = entry != null ? entry["maxDuration"].ToString() : string.Empty;
            VisualMissileEntryCollisionRadiusTxt.Text = entry != null ? entry["collisionRadius"].ToString() : string.Empty;
            VisualMissileEntryFlagsTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileEntryDefaultPitchMinTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileEntryDefaultPitchMaxTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileEntryDefaultSpeedMinTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileEntryDefaultSpeedMaxTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileEntryRandomiseFacingMinTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileEntryRandomiseFacingMaxTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileEntryRandomisePitchMinTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileEntryRandomisePitchMaxTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileEntryRandomiseSpeedMinTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileEntryRandomiseSpeedMaxTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileEntryGravityTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileEntryMaxDurationTxt.IsEnabled = entry != null && spellSelected;
            VisualMissileEntryCollisionRadiusTxt.IsEnabled = entry != null && spellSelected;
            MissileEntryDeleteButton.IsEnabled = entry != null && spellSelected;
            MissileEntryNewButton.IsEnabled = entry == null && spellSelected;
            MissileEntrySaveButton.IsEnabled = entry != null && spellSelected;
        }

        private void UpdateSpellMissileMotionEditor(VisualController controller)
        {
            var missileModel = controller != null ? controller.MissileModel : 0;
            DataRow missileEntry = null;
            var spellSelected = selectedID > 0;
            if (missileModel > 0)
            {
                using (var results = adapter.Query("SELECT * FROM spellvisualeffectname WHERE ID = " + missileModel))
                {
                    if (results.Rows.Count > 0)
                    {
                        missileEntry = results.Rows[0];
                    }
                }
            }
            VisualMissileEffectIdTxt.Text = missileEntry != null ? missileEntry[0].ToString() : string.Empty;
            VisualMissileEffectNameTxt.Text = missileEntry != null ? missileEntry[1].ToString() : string.Empty;
            VisualMissileEffectFilePathTxt.Text = missileEntry != null ? missileEntry[2].ToString() : string.Empty;
            VisualMissileEffectAreaEffectSizeTxt.Text = missileEntry != null ? missileEntry[3].ToString() : string.Empty;
            VisualMissileEffectScaleTxt.Text = missileEntry != null ? missileEntry[4].ToString() : string.Empty;
            VisualMissileEffectMinAllowedScaleTxt.Text = missileEntry != null ? missileEntry[5].ToString() : string.Empty;
            VisualMissileEffectMaxAllowedScaleTxt.Text = missileEntry != null ? missileEntry[6].ToString() : string.Empty;
            VisualMissileEffectIdTxt.IsEnabled = missileEntry != null && spellSelected;
            VisualMissileEffectNameTxt.IsEnabled = missileEntry != null && spellSelected;
            VisualMissileEffectFilePathTxt.IsEnabled = missileEntry != null && spellSelected;
            VisualMissileEffectAreaEffectSizeTxt.IsEnabled = missileEntry != null && spellSelected;
            VisualMissileEffectScaleTxt.IsEnabled = missileEntry != null && spellSelected;
            VisualMissileEffectMinAllowedScaleTxt.IsEnabled = missileEntry != null && spellSelected;
            VisualMissileEffectMaxAllowedScaleTxt.IsEnabled = missileEntry != null && spellSelected;
            if (controller == null)
            {
                return;
            }
            // Initialise effect list if not already
            if (VisualMissileEffectList.HasItems)
            {
                return;
            }
            // Create all the labels
            var itemSource = new List<Label>();
            using (var results = adapter.Query("SELECT id, `name`, FilePath FROM spellvisualeffectname"))
            {
                foreach (DataRow row in results.Rows)
                {
                    var selectItem = new VisualMissileSelectMenuItem(controller.VisualId.ToString(), row[0].ToString());
                    selectItem.Click += SelectItem_Click;
                    var cancelItem = new MenuItem
                    {
                        Header = TryFindResource("VisualCancelContextMenu") ?? "Cancel"
                    };
                    var contextMenu = new ContextMenu();
                    contextMenu.Items.Add(selectItem);
                    contextMenu.Items.Add(new Separator());
                    contextMenu.Items.Add(cancelItem);
                    Label label = new Label
                    {
                        Content = $"{row[0]} - {row[1]}\r\n\t{row[2]}",
                        ContextMenu = contextMenu
                    };
                    itemSource.Add(label);
                }
            }
            VisualMissileEffectList.ItemsSource = itemSource;
        }

        private void UpdateSpellMotionEditor(uint motionId)
        {
            DataRow motionEntry = null;
            var spellSelected = selectedID > 0;
            if (motionId > 0)
            {
                using (var results = adapter.Query("SELECT * FROM spellmissilemotion WHERE ID = " + motionId))
                {
                    if (results.Rows.Count > 0)
                    {
                        motionEntry = results.Rows[0];
                    }
                }
            }
            VisualMissileMotionIdTxt.Text = motionEntry != null ? motionEntry[0].ToString() : string.Empty;
            VisualMissileMotionNameTxt.Text = motionEntry != null ? motionEntry[1].ToString() : string.Empty;
            VisualMissileMotionFlagsTxt.Text = motionEntry != null ? motionEntry[3].ToString() : string.Empty;
            VisualMissileMotionMissileCountTxt.Text = motionEntry != null ? motionEntry[4].ToString() : string.Empty;
            VisualMissileMotionScriptBox.setScript(motionEntry != null ? motionEntry[2].ToString() : string.Empty);
            VisualMissileMotionIdTxt.IsEnabled = motionEntry != null && spellSelected;
            VisualMissileMotionNameTxt.IsEnabled = motionEntry != null && spellSelected;
            VisualMissileMotionFlagsTxt.IsEnabled = motionEntry != null && spellSelected;
            VisualMissileMotionMissileCountTxt.IsEnabled = motionEntry != null && spellSelected;
            VisualMissileMotionScriptBox.IsEnabled = motionEntry != null && spellSelected;
            VisualMissileDeleteButton.IsEnabled = motionEntry != null && spellSelected;
            VisualMissileNewButton.IsEnabled = motionEntry == null && spellSelected;
            VisualMissileSaveButton.IsEnabled = motionEntry != null && spellSelected;
        }

        private void VisualMissileSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender != VisualMissileSaveButton)
                return;
            if (_currentVisualController == null || _currentVisualController.MissileMotion <= 0)
                return;
            var query = "SELECT * FROM spellmissilemotion WHERE ID = " + _currentVisualController.MissileMotion;
            using (var results = GetDBAdapter().Query(query))
            {
                if (results.Rows.Count > 0)
                {
                    var row = results.Rows[0];
                    row.BeginEdit();
                    row["name"] = VisualMissileMotionNameTxt.Text;
                    row["flags"] = VisualMissileMotionFlagsTxt.Text;
                    row["missilecount"] = VisualMissileMotionMissileCountTxt.Text;
                    VisualMissileMotionScriptBox.SelectAll();
                    row["script"] = VisualMissileMotionScriptBox.Selection.Text;
                    row.EndEdit();
                    adapter.CommitChanges(query, results);
                    ShowFlyoutMessage($"Saved changes to SpellMissileMotion {_currentVisualController.MissileMotion}.");
                }
            }
        }

        private void VisualMissileNewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender != VisualMissileNewButton)
                return;
            if (_currentVisualController == null || _currentVisualController.VisualId <= 0)
            {
                ShowFlyoutMessage("This spell has no spell visual. Cannot create a SpellMissileMotion record.");
                return;
            }
            if (!uint.TryParse(GetDBAdapter().QuerySingleValue("SELECT MAX(ID) FROM spellmissilemotion").ToString(), out uint newId))
            {
                ShowFlyoutMessage("Failed to get a new max ID from SpellMissileMotion.");
                return;
            }
            ++newId;
            // FIXME(Harry): Hardcoded for 3.3.5
            GetDBAdapter().Execute($"INSERT INTO spellmissilemotion VALUES ({newId}, \"New Motion Script\", \"-- Hello world.\", 0, 1)");
            _currentVisualController.MissileMotion = newId;
            UpdateSpellVisualProperties(_currentVisualController.VisualId);
            UpdateSpellMotionEditor(newId);
            ShowFlyoutMessage($"Created new SpellMissileMotion {newId}.");
        }

        private void VisualMissileDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender != VisualMissileDeleteButton)
                return;
            if (_currentVisualController == null || _currentVisualController.VisualId <= 0)
                return;
            // We don't actually want to delete the spellmissilemotion entry here, just unlink it
            GetDBAdapter().Execute($"UPDATE spellvisual SET MissileMotion = 0 WHERE ID = {_currentVisualController.VisualId}");
            _currentVisualController.MissileMotion = 0;
            UpdateSpellVisualProperties(_currentVisualController.VisualId);
            UpdateSpellMotionEditor(_currentVisualController.MissileMotion);
        }

        private void MissileEntrySaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender != MissileEntrySaveButton)
                return;
            if (_currentVisualController == null || _currentVisualController.VisualId <= 0)
                return;
            var query = "SELECT * FROM spellmissile WHERE ID = " + _currentVisualController.VisualId;
            using (var results = GetDBAdapter().Query(query))
            {
                if (results.Rows.Count > 0)
                {
                    var row = results.Rows[0];
                    row.BeginEdit();
                    row["flags"] = VisualMissileEntryFlagsTxt.Text;
                    row["defaultPitchMin"] = VisualMissileEntryDefaultPitchMinTxt.Text;
                    row["defaultPitchMax"] = VisualMissileEntryDefaultPitchMaxTxt.Text;
                    row["defaultSpeedMin"] = VisualMissileEntryDefaultSpeedMinTxt.Text;
                    row["defaultSpeedMax"] = VisualMissileEntryDefaultSpeedMaxTxt.Text;
                    row["randomizeFacingMin"] = VisualMissileEntryRandomiseFacingMinTxt.Text;
                    row["randomizeFacingMax"] = VisualMissileEntryRandomiseFacingMaxTxt.Text;
                    row["randomizePitchMin"] = VisualMissileEntryRandomisePitchMinTxt.Text;
                    row["randomizePitchMax"] = VisualMissileEntryRandomisePitchMaxTxt.Text;
                    row["randomizeSpeedMin"] = VisualMissileEntryRandomiseSpeedMinTxt.Text;
                    row["randomizeSpeedMax"] = VisualMissileEntryRandomiseSpeedMaxTxt.Text;
                    row["gravity"] = VisualMissileEntryGravityTxt.Text;
                    row["maxDuration"] = VisualMissileEntryMaxDurationTxt.Text;
                    row["collisionRadius"] = VisualMissileEntryCollisionRadiusTxt.Text;
                    row.EndEdit();
                    adapter.CommitChanges(query, results);
                    ShowFlyoutMessage($"Saved changes to SpellMissile {_currentVisualController.VisualId}.");
                }
            }
        }

        private void MissileEntryNewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender != MissileEntryNewButton)
                return;
            if (_currentVisualController == null || _currentVisualController.VisualId <= 0)
            {
                ShowFlyoutMessage("This spell has no spell visual. Cannot create a SpellMissile record.");
                return;
            }
            // FIXME(Harry): Hardcoded for 3.3.5
            GetDBAdapter().Execute($"INSERT INTO spellmissile VALUES ({_currentVisualController.VisualId}, 1, 0, 0, 30, 40, 0, 0, 0, 0, 0, 0, 30, 0, 0)");
            UpdateSpellVisualMotionEditor(_currentVisualController.VisualId);
            ShowFlyoutMessage($"Created SpellMissile record {_currentVisualController.VisualId}.");
        }

        private void MissileEntryDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender != MissileEntryDeleteButton)
                return;
            if (_currentVisualController == null || _currentVisualController.VisualId <= 0)
                return;
            GetDBAdapter().Execute($"DELETE FROM spellmissile WHERE ID = {_currentVisualController.VisualId}");
            UpdateSpellVisualMotionEditor(_currentVisualController.VisualId);
        }

        private void VisualSave_Click(object sender, RoutedEventArgs e)
        {
            if (sender != VisualSave)
                return;
            if (_currentVisualController == null || _currentVisualController.VisualId <= 0)
                return;
            var query = "SELECT * FROM spellvisual WHERE ID = " + _currentVisualController.VisualId;
            using (var results = GetDBAdapter().Query(query))
            {
                if (results.Rows.Count > 0)
                {
                    var row = results.Rows[0];
                    row.BeginEdit();
                    row["HasMissile"] = VisualHasMissileTxt.Text;
                    row["MissileModel"] = VisualMissileModelTxt.Text;
                    row["MissilePathType"] = VisualMissilePathTypeTxt.Text;
                    row["MissileDestinationAttachment"] = VisualMissileDestinationAttachmentTxt.Text;
                    row["MissileSound"] = VisualMissileSoundTxt.Text;
                    row["AnimEventSoundId"] = VisualAnimEventSoundIdTxt.Text;
                    row["MissileAttachment"] = VisualMissileAttachmentTxt.Text;
                    row["MissileFollowGroundHeight"] = VisualMissileFollowGroundHeightTxt.Text;
                    row["MissileFollowDropSpeed"] = VisualMissileFollowDropSpeedTxt.Text;
                    row["MissileFollowApproach"] = VisualMissileFollowApproachTxt.Text;
                    row["MissileFollowGroundFlags"] = VisualMissileFollowGroundFlagsTxt.Text;
                    row["MissileMotion"] = VisualMissileMotionTxt.Text;
                    row["MissileCastOffsetX"] = VisualMissileCastOffsetXTxt.Text;
                    row["MissileCastOffsetY"] = VisualMissileCastOffsetYTxt.Text;
                    row["MissileCastOffsetZ"] = VisualMissileCastOffsetZTxt.Text;
                    row["MissileImpactOffsetX"] = VisualMissileImpactOffsetXTxt.Text;
                    row["MissileImpactOffsetY"] = VisualMissileImpactOffsetYTxt.Text;
                    row["MissileImpactOffsetZ"] = VisualMissileImpactOffsetZTxt.Text;
                    row.EndEdit();
                    adapter.CommitChanges(query, results);
                    ShowFlyoutMessage($"Saved changes to SpellVisual {_currentVisualController.VisualId}.");
                }
            }
        }

        private void VisualNew_Click(object sender, RoutedEventArgs e)
        {
            if (sender != VisualNew)
                return;
            var newId = uint.Parse(adapter.QuerySingleValue("SELECT MAX(ID) FROM spellvisual").ToString()) + 1;
            // FIXME(Harry): Hardcoded for 3.3.5
            GetDBAdapter().Execute($"INSERT INTO spellvisual VALUES ({newId}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4294967295, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)");
            // Link up to selected spell
            SpellVisual1.Text = newId.ToString();
            GetDBAdapter().Execute($"UPDATE spell SET SpellVisual1 = {newId} WHERE ID = {selectedID}");
            UpdateSpellVisualTab(newId, 0);
            ShowFlyoutMessage($"Created new SpellVisual record {newId}.");
        }

        private void SelectItem_Click(object sender, RoutedEventArgs e)
        {
            var menu = sender as VisualMissileSelectMenuItem;
            GetDBAdapter().Execute($"UPDATE spellvisual SET MissileModel = {menu.MissileId} WHERE ID = {menu.VisualId}");
            VisualMissileEffectList.ItemsSource = null;
            UpdateSpellVisualTab(uint.Parse(menu.VisualId), 0, true);
            ShowFlyoutMessage($"SpellVisual {menu.VisualId} MissileModel set to {menu.MissileId}.");
        }
        #endregion

        private void AddFamilyIfNotExists(uint familyId)
        {
            // add a family name to the combobox and data if it's missing

            bool exists = SpellFamilyNames.familyNames.ContainsKey((int)familyId);

            bool empty = true;
            if (exists)
                empty = string.IsNullOrEmpty(SpellFamilyNames.familyNames[(int)familyId]);

            if (exists && !empty)
                return;

            if (!exists)
                SpellFamilyNames.familyNames.Add((int)familyId, "Unknown");
            else
                SpellFamilyNames.familyNames[(int)familyId] = "Unknown";

            // refresh item source
            SpellFamilyNames.GenerateItemSource();
            SpellFamilyName.ItemsSource = null;
            SpellFamilyName.ItemsSource = SpellFamilyNames.familyNamesitemSource;


        }

        private void UpdateSpellMaskCheckBox(uint mask, ThreadSafeComboBox comBox)
        {
            for (int i = 0; i < 32; i++)
            {
                uint maskPow = (1u << i);

                ThreadSafeCheckBox safeCheckBox = (ThreadSafeCheckBox)comBox.Items.GetItemAt(i);

                safeCheckBox.ThreadSafeChecked = (mask & maskPow) != 0;
            }
        }

        private void ToggleAllSpellMaskCheckBoxes(bool enabled)
        {
            SpellMask11.IsEnabled = enabled;
            SpellMask12.IsEnabled = enabled;
            SpellMask13.IsEnabled = enabled;
            SpellMask21.IsEnabled = enabled;
            SpellMask22.IsEnabled = enabled;
            SpellMask23.IsEnabled = enabled;
            SpellMask31.IsEnabled = enabled;
            SpellMask32.IsEnabled = enabled;
            SpellMask33.IsEnabled = enabled;
        }

        private void GenerateSpellEffectHeader(int effectIdx /* 1 -3*/)
        {
            // test display spell effect under the tab
            string ressource_name = "SpellEffects" + effectIdx.ToString(); // SpellEffects1
            string text = SafeTryFindResource(ressource_name);

            FilteredComboBox effect_combobox = null;
            System.Windows.Controls.TabItem tab_item = null;
            if (effectIdx == 1)
            {
                effect_combobox = SpellEffect1;
                tab_item = effect1TabItem;
            }
            else if (effectIdx == 2)
            {
                effect_combobox = SpellEffect2;
                tab_item = effect2TabItem;
            }
            else if (effectIdx == 3)
            { 
                effect_combobox = SpellEffect3;
                tab_item = effect3TabItem;
            }

            string spell_effect_name = effect_combobox.Items.GetItemAt(0) as string;
            bool no_effect = true;
            if (effect_combobox.SelectedItem != null && effect_combobox.SelectedIndex != 0)
            {
                no_effect = false;
                spell_effect_name = effect_combobox.SelectedItem as string;
            }
            // remove the Id prefix
            spell_effect_name = Regex.Replace(spell_effect_name, @"^\s*\d+\s*-\s*", "");

            text += "\n" + spell_effect_name;

            tab_item.BorderThickness = new Thickness(1);// (no_effect ? 0 : 1);
            // tab_item.Padding = new Thickness(4);
            tab_item.BorderBrush = Brushes.DarkGray; // no_effect ? Brushes.DarkGray : Brushes.White;

            tab_item.Header = text;
        }

        #endregion

        #region SelectionChanges
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (updating || adapter == null || !Config.IsInit || e.OriginalSource != MainTabControl)
                return;
            var tab = e.AddedItems[0];
            if (tab == IconTab)
            {
                prepareIconEditor();
            }
            else if (tab == VisualTab)
            {
                var idStr = SpellVisual1.Text;
                uint.TryParse(idStr, out var id);
                UpdateSpellVisualTab(id);
            }
        }

        private async void SelectSpell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var added_items = e.AddedItems;
            if (added_items.Count > 1)
            {
                await this.ShowMessageAsync(SafeTryFindResource("SpellEditor"), SafeTryFindResource("String5"));
                ((ListBox)sender).UnselectAll();
                return;
            }

            if (added_items.Count != 1) 
                return;

            ListBox box = (ListBox)sender;

            StackPanel panel = (StackPanel) box.SelectedItem;
            using (var enumerator = panel.GetChildObjects().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current is TextBlock block)
                    {
                        string name = block.Text;
                        selectedID = uint.Parse(name.Substring(1, name.IndexOf(' ', 1)));
                        UpdateMainWindow();
                        return;
                    }
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (adapter == null || updating)
                return;
            if (sender == RequiresSpellFocus)
            {
                var loadFocusObjects = (SpellFocusObject)DBCManager.GetInstance().FindDbcForBinding("SpellFocusObject");
                foreach (var dbcBox in loadFocusObjects.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"RequiresSpellFocus"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == AreaGroup)
            {
                var loadAreaGroups = (AreaGroup)DBCManager.GetInstance().FindDbcForBinding("AreaGroup");
                foreach (var dbcBox in loadAreaGroups.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"AreaGroupID"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == Category)
            {
                var loadCategories = (SpellCategory)DBCManager.GetInstance().FindDbcForBinding("SpellCategory");
                foreach (var dbcBox in loadCategories.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"Category"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == DispelType)
            {
                var loadDispels = (SpellDispelType)DBCManager.GetInstance().FindDbcForBinding("SpellDispelType");
                foreach (var dbcBox in loadDispels.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"Dispel"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == MechanicType)
            {
                var loadMechanics = (SpellMechanic)DBCManager.GetInstance().FindDbcForBinding("SpellMechanic");
                foreach (var dbcBox in loadMechanics.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"Mechanic"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == CastTime)
            {
                var loadCastTimes = (SpellCastTimes)DBCManager.GetInstance().FindDbcForBinding("SpellCastTimes");
                foreach (var dbcBox in loadCastTimes.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"CastingTimeIndex"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == Duration)
            {
                var loadDurations = (SpellDuration)DBCManager.GetInstance().FindDbcForBinding("SpellDuration");
                foreach (var dbcBox in loadDurations.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"DurationIndex"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == Difficulty)
            {
                var loadDifficulties = (SpellDifficulty)DBCManager.GetInstance().FindDbcForBinding("SpellDifficulty");
                foreach (var dbcBox in loadDifficulties.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"SpellDifficultyID"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == Range)
            {
                var loadRanges = (SpellRange)DBCManager.GetInstance().FindDbcForBinding("SpellRange");
                foreach (var dbcBox in loadRanges.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"RangeIndex"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == RadiusIndex1)
            {
                var loadRadiuses = (SpellRadius)DBCManager.GetInstance().FindDbcForBinding("SpellRadius");
                foreach (var dbcBox in loadRadiuses.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"EffectRadiusIndex1"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == RadiusIndex2)
            {
                var loadRadiuses = (SpellRadius)DBCManager.GetInstance().FindDbcForBinding("SpellRadius");
                foreach (var dbcBox in loadRadiuses.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"EffectRadiusIndex2"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == RadiusIndex3)
            {
                var loadRadiuses = (SpellRadius)DBCManager.GetInstance().FindDbcForBinding("SpellRadius");
                foreach (var dbcBox in loadRadiuses.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"EffectRadiusIndex3"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == EquippedItemClass)
            {
                var loadItemClasses = (ItemClass)DBCManager.GetInstance().FindDbcForBinding("ItemClass");
                long itemSubClass = loadItemClasses.Lookups[EquippedItemClass.SelectedIndex].ID;
                UpdateItemSubClass(itemSubClass);
                foreach (var dbcBox in loadItemClasses.Lookups)
                {
                    if (EquippedItemClass.SelectedIndex == 5 || EquippedItemClass.SelectedIndex == 3)
                    {
                        EquippedItemInventoryTypeGrid.IsEnabled = true;
                    } 
                    else
                    {
                        EquippedItemInventoryTypeGrid.IsEnabled = false;
                    }

                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"EquippedItemClass"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == TotemCategory1)
            {
                var loadTotemCategories = (TotemCategory)DBCManager.GetInstance().FindDbcForBinding("TotemCategory");
                foreach (var dbcBox in loadTotemCategories.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"TotemCategory1"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == TotemCategory2)
            {
                var loadTotemCategories = (TotemCategory)DBCManager.GetInstance().FindDbcForBinding("TotemCategory");
                foreach (var dbcBox in loadTotemCategories.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"TotemCategory2"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == RuneCost)
            {
                var loadRuneCosts = (SpellRuneCost)DBCManager.GetInstance().FindDbcForBinding("SpellRuneCost");
                foreach (var dbcBox in loadRuneCosts.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"RuneCostID"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            if (sender == SpellDescriptionVariables)
            {
                var loadDescriptionVariables = (SpellDescriptionVariables)DBCManager.GetInstance().FindDbcForBinding("SpellDescriptionVariables");
                foreach (var dbcBox in loadDescriptionVariables.Lookups)
                {
                    if (dbcBox.ComboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        adapter.Execute($"UPDATE `{"spell"}` SET `{"SpellDescriptionVariableID"}` = '{dbcBox.ID}' WHERE `ID` = '{selectedID}'");
                        break;
                    }
                }
            }

            // optional cleanup : reset fields unsupported by new spell effect
            if (sender == SpellEffect1 || sender == SpellEffect2 || sender == SpellEffect3)
            {
                int effect_id = 0;
                if (sender == SpellEffect2)
                    effect_id = 1;
                else if (sender == SpellEffect3)
                    effect_id = 2;

                string selectedEffectText = SpellEffect1.SelectedItem as string;
                int spelleffect_id = (int)SpellEffect1.GetNumberPrefixFromText(selectedEffectText);

                SpellEffectData spellEffectData = new SpellEffectData();
                bool hasData = MiscValueConstants.spellEffectsData.TryGetValue((spellEffectTypes)spelleffect_id, out spellEffectData);
                if (!hasData && spelleffect_id != (int)spellEffectTypes.NONE)
                    throw new NotImplementedException($"Spell Effect {spelleffect_id} does not have data. Report to devs.");

                if (spelleffect_id == (int)spellEffectTypes.NONE)
                    return;

                if (spellEffectData.usesMultipleValue == false)
                    effectMultipleValueBoxes[effect_id].ThreadSafeText = "0";
                if (spellEffectData.usesAura == false)
                {
                    effectAuraNameBoxes[effect_id].SelectionChanged -= ComboBox_SelectionChanged;
                    effectAuraNameBoxes[effect_id].ThreadSafeIndex = 0;
                    effectAuraNameBoxes[effect_id].SelectionChanged += ComboBox_SelectionChanged;
                }
                if (spellEffectData.UsesChainTarget == false)
                    effectChainTargetBoxes[effect_id].Text = "0";
                if (spellEffectData.UsesItemType == false)
                    effectItemTypeBoxes[effect_id].Text = "0";
                if (spellEffectData.UsesSpell == false)
                    effectTriggerSpellBoxes[effect_id].Text = "0";
                if (spellEffectData.UsesAmplitude == false)
                    effectAmplitudeBoxes[effect_id].Text = "0";
            }

            // can turn into a reusable fucntion eventually
            if (sender == ApplyAuraName1 || sender == SpellEffect1)
            {
                GenerateSpellEffectHeader(1);

                // TODO : store og value and field instead of resetting?

                // need to get from selected item because control main text might not be updated yet
                if (SpellEffect1.SelectedItem != null)
                {
                    string selectedEffectText = SpellEffect1.SelectedItem as string;
                    string selectedAuraText = ApplyAuraName1.SelectedItem as string;

                    int spelleffect = (int)SpellEffect1.GetNumberPrefixFromText(selectedEffectText);
                    int auraindex = (int)ApplyAuraName1.GetNumberPrefixFromText(selectedAuraText);

                    SetupMiscValueControl(1, spelleffect, auraindex);

                    // reset to 0 for now
                    SetMiscValue(1, 1, 0, true);
                    SetMiscValue(1, 2, 0, true);
                }
            }
            // can turn into a reusable fucntion eventually instead of copy pasta
            if (sender == ApplyAuraName2 || sender == SpellEffect2)
            {
                GenerateSpellEffectHeader(2);

                // need to get from selected item because control main text might not be updated yet
                if (SpellEffect2.SelectedItem != null)
                {
                    string selectedEffectText = SpellEffect2.SelectedItem as string ?? string.Empty;
                    string selectedAuraText = ApplyAuraName2.SelectedItem as string ?? string.Empty;

                    int spelleffect = (int)SpellEffect2.GetNumberPrefixFromText(selectedEffectText);
                    int auraindex = (int)ApplyAuraName2.GetNumberPrefixFromText(selectedAuraText);

                    SetupMiscValueControl(2, spelleffect, auraindex);

                    // reset to 0 for now
                    SetMiscValue(2, 1, 0, true);
                    SetMiscValue(2, 2, 0, true);
                }
            }

            if (sender == ApplyAuraName3 || sender == SpellEffect3)
            {
                GenerateSpellEffectHeader(3);

                if (SpellEffect3.SelectedItem != null)
                {
                    string selectedEffectText = SpellEffect3.SelectedItem as string;
                    string selectedAuraText = ApplyAuraName3.SelectedItem as string;

                    int spelleffect = (int)SpellEffect3.GetNumberPrefixFromText(selectedEffectText);
                    int auraindex = (int)ApplyAuraName3.GetNumberPrefixFromText(selectedAuraText);

                    SetupMiscValueControl(3, spelleffect, auraindex);

                    // reset to 0 for now
                    SetMiscValue(3, 1, 0, true);
                    SetMiscValue(3, 2, 0, true);
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
                    box.ThreadSafeContent = SafeTryFindResource("None");
                    box.ThreadSafeVisibility = Visibility.Hidden;
                    //box.threadSafeEnabled = false;
                }
                return;
            }

            Dispatcher.Invoke(DispatcherPriority.Send, TimeSpan.Zero, new Func<object>(() 
                => EquippedItemSubClassGrid.IsEnabled = true));
            uint num = 0;
            var loadItemSubClasses = (ItemSubClass)DBCManager.GetInstance().FindDbcForBinding("ItemSubClass");
            foreach (ThreadSafeCheckBox box in equippedItemSubClassMaskBoxes)
            {
                ItemSubClass.ItemSubClassLookup itemLookup = loadItemSubClasses.LookupClassAndSubclass(classId, num);
                if (itemLookup.Name != null)
                {
                    box.ThreadSafeContent = itemLookup.Name;
                    //box.threadSafeEnabled = true;
                    box.ThreadSafeVisibility = Visibility.Visible;
                }
                else
                {
                    box.ThreadSafeContent = SafeTryFindResource("None"); ;
                    box.ThreadSafeVisibility = Visibility.Hidden;
                    //box.threadSafeEnabled = false;
                }
                box.ThreadSafeChecked = false;
                num++;
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
            foreach (System.Windows.Controls.Image image in IconGrid.Children)
            {
                image.Margin = margin;
                image.Width = e.NewValue;
                image.Height = e.NewValue;
            }
        }

        public DataRow GetSpellRowById(uint spellId) => adapter.Query($"SELECT * FROM `{"spell"}` WHERE `ID` = '{spellId}' LIMIT 1").Rows[0];

        public string GetSpellNameById(uint spellId)
        {
            return SelectSpell.GetSpellNameById(spellId);
        }
        #endregion
        private void MultilingualSwitch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string language = e.AddedItems[0].ToString();
            string path = $"pack://SiteOfOrigin:,,,/Languages/{language}.xaml";
            System.Windows.Application.Current.Resources.MergedDictionaries[0].Source = new Uri(path);
            Config.Language = language;
            RefreshAllUIElements();
        }

        private void MultilingualSwitch_Initialized(object sender, EventArgs e)
        {
            string configLanguage = Config.Language;
            configLanguage = configLanguage == "" ? "enUS" : configLanguage;

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

                if (fileName == configLanguage)
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

        private void FilterSpellEffectCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != FilterSpellEffectCombo)
            {
                return;
            }
            
            var box = sender as ComboBox;
            var selected = box.SelectedItem?.ToString() ?? "0 ";
            var id = int.Parse(selected.Substring(0, selected.IndexOf(' ')));
            var view = CollectionViewSource.GetDefaultView(SelectSpell.Items);
            // Clear filter if id is 0
            if (id == 0)
            {
                view.Filter = obj => true;
                return;
            }
            // Collect all spell ID's with the effect id
            var matchingSpells = adapter.Query($"SELECT id FROM spell WHERE Effect1 = {id} or Effect2 = {id} or Effect3 = {id}").Rows;
            var matchingSpellsSet = new HashSet<string>();
            foreach (DataRow record in matchingSpells)
            {
                matchingSpellsSet.Add(record[0].ToString());
            }
            // Apply filter
            view.Filter = obj =>
            {
                var panel = obj as StackPanel;
                using (var enumerator = panel.GetChildObjects().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (!(enumerator.Current is TextBlock block))
                            continue;
                        var name = block.Text.TrimStart();
                        var blockId = name.Substring(0, name.IndexOf(' '));
                        return matchingSpellsSet.Contains(blockId);
                    }
                }
                return false;
            };
        }

        private void FilterAuraCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != FilterAuraCombo)
            {
                return;
            }
            var box = sender as ComboBox;
            var selected = box.SelectedItem?.ToString() ?? "0 ";
            var id = int.Parse(selected.Substring(0, selected.IndexOf(' ')));
            var view = CollectionViewSource.GetDefaultView(SelectSpell.Items);
            // Clear filter if id is 0
            if (id == 0)
            {
                view.Filter = obj => true;
                return;
            }
            // Collect all spell ID's with the effect id
            var matchingSpells = adapter.Query($"SELECT id FROM spell WHERE EffectApplyAuraName1 = {id} or EffectApplyAuraName2 = {id} or EffectApplyAuraName3 = {id}").Rows;
            var matchingSpellsSet = new HashSet<string>();
            foreach (DataRow record in matchingSpells)
            {
                matchingSpellsSet.Add(record[0].ToString());
            }
            // Apply filter
            view.Filter = obj =>
            {
                var panel = obj as StackPanel;
                using (var enumerator = panel.GetChildObjects().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (!(enumerator.Current is TextBlock block))
                            continue;
                        var name = block.Text.TrimStart();
                        var blockId = name.Substring(0, name.IndexOf(' '));
                        return matchingSpellsSet.Contains(blockId);
                    }
                }
                return false;
            };
        }

        private void FilterSpellTargetA_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != FilterSpellTargetA)
                return;
            var box = sender as ComboBox;
            var selected = box.SelectedItem?.ToString() ?? "0 ";
            var id = int.Parse(selected.Substring(0, selected.IndexOf(' ')));
            var view = CollectionViewSource.GetDefaultView(SelectSpell.Items);
            // Clear filter if id is 0
            if (id == 0)
            {
                view.Filter = obj => true;
                return;
            }
            // Collect all spell ID's with the effect id
            var matchingSpells = adapter.Query($"SELECT id FROM spell WHERE EffectImplicitTargetA1 = {id} or EffectImplicitTargetA2 = {id} or EffectImplicitTargetA3 = {id}").Rows;
            var matchingSpellsSet = new HashSet<string>();
            foreach (DataRow record in matchingSpells)
            {
                matchingSpellsSet.Add(record[0].ToString());
            }
            // Apply filter
            view.Filter = obj =>
            {
                var panel = obj as StackPanel;
                using (var enumerator = panel.GetChildObjects().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (!(enumerator.Current is TextBlock block))
                            continue;
                        var name = block.Text.TrimStart();
                        var blockId = name.Substring(0, name.IndexOf(' '));
                        return matchingSpellsSet.Contains(blockId);
                    }
                }
                return false;
            };
        }

        private void FilterSpellTargetB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != FilterSpellTargetB)
                return;
            var box = sender as ComboBox;
            var selected = box.SelectedItem?.ToString() ?? "0 ";
            var id = int.Parse(selected.Substring(0, selected.IndexOf(' ')));
            var view = CollectionViewSource.GetDefaultView(SelectSpell.Items);
            // Clear filter if id is 0
            if (id == 0)
            {
                view.Filter = obj => true;
                return;
            }
            // Collect all spell ID's with the effect id
            var matchingSpells = adapter.Query($"SELECT id FROM spell WHERE EffectImplicitTargetB1 = {id} or EffectImplicitTargetB2 = {id} or EffectImplicitTargetB3 = {id}").Rows;
            var matchingSpellsSet = new HashSet<string>();
            foreach (DataRow record in matchingSpells)
            {
                matchingSpellsSet.Add(record[0].ToString());
            }
            // Apply filter
            view.Filter = obj =>
            {
                var panel = obj as StackPanel;
                using (var enumerator = panel.GetChildObjects().GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (!(enumerator.Current is TextBlock block))
                            continue;
                        var name = block.Text.TrimStart();
                        var blockId = name.Substring(0, name.IndexOf(' '));
                        return matchingSpellsSet.Contains(blockId);
                    }
                }
                return false;
            };
        }

        private void VisualMissileFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (e.OriginalSource != VisualMissileFilter)
            {
                return;
            }
            var filterBox = sender as TextBox;
            var input = filterBox.Text.ToLower();
            ICollectionView view = CollectionViewSource.GetDefaultView(VisualMissileEffectList.Items);
            view.Filter = o => input.Length == 0 ? true : o.ToString().ToLower().Contains(input);
        }

        private void SpellMask_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!WoWVersionManager.IsWotlkOrGreaterSelected)
                return;

            if (sender is ComboBox comboBox)
            {
                if (!comboBox.Name.Contains("SpellMask"))
                    return;
            }

            var masks = new List<uint>
            {
                uint.Parse(SpellMask11.Text),
                uint.Parse(SpellMask21.Text),
                uint.Parse(SpellMask31.Text),
                uint.Parse(SpellMask21.Text),
                uint.Parse(SpellMask22.Text),
                uint.Parse(SpellMask23.Text),
                uint.Parse(SpellMask31.Text),
                uint.Parse(SpellMask32.Text),
                uint.Parse(SpellMask33.Text)
            };

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                spellFamilyClassMaskParser.UpdateSpellEffectMasksSelected(this, SpellFamilyName.GetNumberPrefixFromText(), GetDBAdapter(), masks)));
        }

        /*
        private void RefreshComboBoxMaskText(ThreadSafeComboBox comboBox)
        {
            uint Mask = 0;
            for (uint i = 0; i < 32; i++)
            {
                ThreadSafeCheckBox cb = (ThreadSafeCheckBox)comboBox.Items.GetItemAt((int)i);
                if (cb.IsChecked == true)
                    Mask |= (1u << (int)i);
            }
            comboBox.Text = Mask.ToString();
        }*/

        private void SpellMask_DropDownClosed(object sender, EventArgs e)
        {
            // recalc to ensure text is right, same code as HandspellFamilyClassMask_Checked for checkboxes
            if (sender is ThreadSafeComboBox comboBox)
            {
                uint Mask = 0;
                for (uint i = 0; i < 32; i++)
                {
                    ThreadSafeCheckBox cb = (ThreadSafeCheckBox)comboBox.Items.GetItemAt((int)i);
                    if (cb.IsChecked == true)
                        Mask |= (1u << (int)i);
                }
                comboBox.Text = Mask.ToString();

                SpellMask_SelectionChanged(sender, null);
            }
        }

        private void MiscValueMask_DropDownClosed(object sender, EventArgs e)
        {
            // recalc to ensure text is right, same code as HandspellFamilyClassMask_Checked for checkboxes
            if (sender is ThreadSafeComboBox comboBox)
            {
                // can't use the same code as spellmask
                RefreshMaskComboboxText(comboBox);
            }
        }

        private void FilterClassMaskSpells_TextChanged(object sender, TextChangedEventArgs e)
        {
            ListBox targetBox;
            if (sender == FilterClassMaskSpells1)
                targetBox = EffectTargetSpellsList1;
            else if (sender == FilterClassMaskSpells2)
                targetBox = EffectTargetSpellsList2;
            else if (sender == FilterClassMaskSpells3)
                targetBox = EffectTargetSpellsList3;
            else
            {
                Logger.Error($"Unable to find target box to Class Mask Filter: {sender}");
                return;
            }
            var filterBox = sender as ThreadSafeTextBox;
            var input = filterBox.Text.ToLower();
            ICollectionView view = CollectionViewSource.GetDefaultView(targetBox.Items);
            view.Filter = o => input.Length == 0 ? true : o.ToString().ToLower().Contains(input);
        }

        private string SafeTryFindResource(object key)
        {
            var resource = TryFindResource(key);
            return resource != null ? resource.ToString() : $"Language files out of date, missing key: {key}";
        }

        private void FilteredComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // reset text of searchable comboboxes when losing focus
            if (sender is FilteredComboBox comboBox)
            {
                if (comboBox.SelectedItem != null && comboBox.Text != comboBox.SelectedItem.ToString())
                    comboBox.Text = comboBox.SelectedItem.ToString();
                else if (comboBox.SelectedItem == null)
                {
                    comboBox.SelectedIndex = 0; // reset it. TODO : reset to last index if possible
                    // comboBox.Text = comboBox.SelectedItem?.ToString();
                    comboBox.SetTextFromIndex(0);
                }

            }
        }
    }
}
