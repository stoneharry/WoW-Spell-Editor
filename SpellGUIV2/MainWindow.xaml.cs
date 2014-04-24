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

        private List<CheckBox> targetBoxes = new List<CheckBox>();
        private List<CheckBox> procBoxes = new List<CheckBox>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void loadAllDBCs()
        {
            string fileName = await this.ShowInputAsync("Load DBC File", "What is the name of your Spell DBC? It must be in the same directory as this program.");
            if (fileName == null || fileName.Length < 1)
            {
                await this.ShowMessageAsync("ERROR", "File name is bad.");
                Environment.Exit(1);
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
                Environment.Exit(1);
                return;
            }

            populateSelectSpell();

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

            string[] effect_strings = { "0 NONE", "1 INSTAKILL", "2 SCHOOL_DAMAGE", "3 DUMMY", "4 PORTAL_TELEPORT unused", "5 TELEPORT_UNITS", 
                                        "6 APPLY_AURA", "7 ENVIRONMENTAL_DAMAGE", "8 POWER_DRAIN", "9 HEALTH_LEECH", "10 HEAL", "11 BIND",
                                        "12 PORTAL", "13 RITUAL_BASE unused", "14 RITUAL_SPECIALIZE unused", "15 RITUAL_ACTIVATE_PORTAL unused",
                                        "16 QUEST_COMPLETE", "17 WEAPON_DAMAGE_NOSCHOOL", "18 RESURRECT", "19 ADD_EXTRA_ATTACKS",
                                        "20 DODGE one spell: Dodge", "21 EVADE one spell: Evade (DND)", "22 PARRY", "23 BLOCK one spell: Block",
                                        "24 CREATE_ITEM", "25 WEAPON", "26 DEFENSE one spell: Defense", "27 PERSISTENT_AREA_AURA", "28 SUMMON",
                                        "29 LEAP", "30 ENERGIZE", "31 WEAPON_PERCENT_DAMAGE", "32 TRIGGER_MISSILE", "33 OPEN_LOCK",
                                        "34 SUMMON_CHANGE_ITEM", "35 APPLY_AREA_AURA_PARTY", "36 LEARN_SPELL", "37 SPELL_DEFENSE one spell: SPELLDEFENSE (DND)",
                                        "38 DISPEL", "39 LANGUAGE", "40 DUAL_WIELD", "41 JUMP", "42 JUMP_DEST", "43 TELEPORT_UNITS_FACE_CASTER", "44 SKILL_STEP",
                                        "45 ADD_HONOR honor/pvp related", "46 SPAWN clientside, unit appears as if it was just spawned", "47 TRADE_SKILL",
                                        "48 STEALTH one spell: Base Stealth", "49 DETECT one spell: Detect", "50 TRANS_DOOR", "51 FORCE_CRITICAL_HIT unused",
                                        "52 GUARANTEE_HIT one spell: zzOLDCritical Shot", "53 ENCHANT_ITEM", "54 ENCHANT_ITEM_TEMPORARY", "55 TAMECREATURE",
                                        "56 SUMMON_PET", "57 LEARN_PET_SPELL", "58 WEAPON_DAMAGE", "59 CREATE_RANDOM_ITEM create item base at spell specific loot",
                                        "60 PROFICIENCY", "61 SEND_EVENT", "62 POWER_BURN", "63 THREAT", "64 TRIGGER_SPELL", "65 APPLY_AREA_AURA_RAID",
                                        "66 CREATE_MANA_GEM (possibly recharge it, misc - is item ID)", "67 HEAL_MAX_HEALTH", "68 INTERRUPT_CAST", "69 DISTRACT",
                                        "70 PULL one spell: Distract Move", "71 PICKPOCKET", "72 ADD_FARSIGHT", "73 UNTRAIN_TALENTS", "74 APPLY_GLYPH",
                                        "75 HEAL_MECHANICAL one spell: Mechanical Patch Kit", "76 SUMMON_OBJECT_WILD", "77 SCRIPT_EFFECT", "78 ATTACK",
                                        "79 SANCTUARY", "80 ADD_COMBO_POINTS", "81 CREATE_HOUSE one spell: Create House (TEST)", "82 BIND_SIGHT", "83 DUEL",
                                        "84 STUCK", "85 SUMMON_PLAYER", "86 ACTIVATE_OBJECT", "87 GAMEOBJECT_DAMAGE", "88 GAMEOBJECT_REPAIR",
                                        "89 GAMEOBJECT_SET_DESTRUCTION_STATE", "90 KILL_CREDIT Kill credit but only for single person",
                                        "91 THREAT_ALL one spell: zzOLDBrainwash", "92 ENCHANT_HELD_ITEM", "93 FORCE_DESELECT", "94 SELF_RESURRECT",
                                        "95 SKINNING", "96 CHARGE", "97 CAST_BUTTON (totem bar since 3.2.2a)", "98 KNOCK_BACK", "99 DISENCHANT", "100 INEBRIATE",
                                        "101 FEED_PET", "102 DISMISS_PET", "103 REPUTATION", "104 SUMMON_OBJECT_SLOT1", "105 SUMMON_OBJECT_SLOT2",
                                        "106 SUMMON_OBJECT_SLOT3", "107 SUMMON_OBJECT_SLOT4", "108 DISPEL_MECHANIC", "109 SUMMON_DEAD_PET", "110 DESTROY_ALL_TOTEMS",
                                        "111 DURABILITY_DAMAGE", "112 112", "113 RESURRECT_NEW", "114 ATTACK_ME", "115 DURABILITY_DAMAGE_PCT",
                                        "116 SKIN_PLAYER_CORPSE one spell: Remove Insignia, bg usage, required special corpse flags...",
                                        "117 SPIRIT_HEAL one spell: Spirit Heal", "118 SKILL professions and more", "119 APPLY_AREA_AURA_PET",
                                        "120 TELEPORT_GRAVEYARD one spell: Graveyard Teleport Test", "121 NORMALIZED_WEAPON_DMG", "122 122 unused",
                                        "123 SEND_TAXI taxi/flight related (misc value is taxi path id)", "124 PULL_TOWARDS", "125 MODIFY_THREAT_PERCENT",
                                        "126 STEAL_BENEFICIAL_BUFF spell steal effect?", "127 PROSPECTING Prospecting spell", "128 APPLY_AREA_AURA_FRIEND",
                                        "129 APPLY_AREA_AURA_ENEMY", "130 REDIRECT_THREAT", "131 PLAYER_NOTIFICATION sound id in misc value (SoundEntries.dbc)",
                                        "132 PLAY_MUSIC sound id in misc value (SoundEntries.dbc)", "133 UNLEARN_SPECIALIZATION unlearn profession specialization",
                                        "134 KILL_CREDIT misc value is creature entry", "135 CALL_PET", "136 HEAL_PCT", "137 ENERGIZE_PCT", "138 LEAP_BACK Leap back",
                                        "139 CLEAR_QUEST Reset quest status (miscValue - quest ID)", "140 FORCE_CAST", "141 FORCE_CAST_WITH_VALUE",
                                        "142 TRIGGER_SPELL_WITH_VALUE", "143 APPLY_AREA_AURA_OWNER", "144 KNOCK_BACK_DEST", "145 PULL_TOWARDS_DEST Black Hole Effect",
                                        "146 ACTIVATE_RUNE", "147 QUEST_FAIL quest fail", "148 TRIGGER_MISSILE_SPELL_WITH_VALUE", "149 CHARGE_DEST", "150 QUEST_START",
                                        "151 TRIGGER_SPELL_2", "152 SUMMON_RAF_FRIEND summon Refer-a-Friend", "153 CREATE_TAMED_PET misc value is creature entry",
                                        "154 DISCOVER_TAXI",
                                        "155 TITAN_GRIP Allows you to equip two-handed axes, maces and swords in one hand, but you attack $49152s1% slower than normal.",
                                        "156 ENCHANT_ITEM_PRISMATIC", "157 CREATE_ITEM_2 create item or create item template and replace by some randon spell loot item",
                                        "158 MILLING milling", "159 ALLOW_RENAME_PET allow rename pet once again", "160 160 1 spell - 45534",
                                        "161 TALENT_SPEC_COUNT second talent spec (learn/revert)", "162 TALENT_SPEC_SELECT activate primary/secondary spec",
                                        "163 unused", "164 REMOVE_AURA" };

            for (int i = 0; i < effect_strings.Length; ++i)
            {
                Effect1.Items.Add(effect_strings[i]);
                Effect2.Items.Add(effect_strings[i]);
                Effect3.Items.Add(effect_strings[i]);
            }

            string[] damage_prevention_types = { "0 - SPELL_DAMAGE_CLASS_NONE", "1 - SPELL_DAMAGE_CLASS_MAGIC", "2 - SPELL_DAMAGE_CLASS_MELEE", "3 - SPELL_DAMAGE_CLASS_RANGED",
                                               "0 - SPELL_PREVENTION_TYPE_NONE", "1 - SPELL_PREVENTION_TYPE_SILENCE", "2 - SPELL_PREVENTION_TYPE_PACIFY"};
            for (int i = 0; i < damage_prevention_types.Length; ++i)
            {
                if (i < 4)
                    SpellDamageType.Items.Add(damage_prevention_types[i]);
                else
                    PreventionType.Items.Add(damage_prevention_types[i]);
            }

            string[] target_strings = { "NONE            ", "UNUSED_1        ", "UNIT\t\tsomeone", "UNIT_RAID\tsomeone in raid", "UNIT_PARTY\tsomeone in party", "ITEM\t\titem enchantment",
                                        "SOURCE_LOCATION point blank AoE", "DEST_LOCATION\ttarget AoE", "UNIT_ENEMY\ttarget dead players", "UNIT_ALLY\ttarget allies",
                                        "CORPSE_ENEMY\ttarget dead enemies   ", "UNIT_DEAD\ttarget dead", "GAMEOBJECT\tspawn game object", "TRADE_ITEM      ", "STRING          ",
                                        "GAMEOBJECT_ITEM ", "CORPSE_ALLY     ", "UNIT_MINIPET    ", "GLYPH_SLOT      ", "DEST_TARGET     ", "UNUSED20        ", "UNIT_PASSENGER" };

            for (int i = 0; i < target_strings.Length; ++i)
            {
                CheckBox box = new CheckBox();
                box.Content = target_strings[i];
                box.Margin = new Thickness(5, (-10.5 + i) * 45, 0, 0);
                TargetEditorGrid.Children.Add(box);
                targetBoxes.Add(box);
            }

            string[] proc_strings = { "NONE", "ON_ANY_HOSTILE_ACTION", "ON_GAIN_EXPIERIENCE", "ON_MELEE_ATTACK", "ON_CRIT_HIT_VICTIM", "ON_CAST_SPELL", "ON_PHYSICAL_ATTACK_VICTIM",
                                        "ON_RANGED_ATTACK", "ON_RANGED_CRIT_ATTACK", "ON_PHYSICAL_ATTACK", "ON_MELEE_ATTACK_VICTIM", "ON_SPELL_HIT", "ON_RANGED_CRIT_ATTACK_VICTIM",
                                        "ON_CRIT_ATTACK", "ON_RANGED_ATTACK_VICTIM", "ON_PRE_DISPELL_AURA_VICTIM", "ON_SPELL_LAND_VICTIM", "ON_CAST_SPECIFIC_SPELL", "ON_SPELL_HIT_VICTIM",
                                        "ON_SPELL_CRIT_HIT_VICTIM", "ON_TARGET_DIE", "ON_ANY_DAMAGE_VICTIM", "ON_TRAP_TRIGGER", "ON_AUTO_SHOT_HIT", "ON_ABSORB", "ON_RESIST_VICTIM",
                                        "ON_DODGE_VICTIM", "ON_DIE", "REMOVEONUSE", "MISC", "ON_BLOCK_VICTIM", "ON_SPELL_CRIT_HIT" };
            for (int i = 0; i < proc_strings.Length; ++i)
            {
                CheckBox box = new CheckBox();
                box.Content = proc_strings[i];
                box.Margin = new Thickness(5, (-15.5 + i) * 45, 0, 0);
                ProcEditorGrid.Children.Add(box);
                procBoxes.Add(box);
            }

            loadedDispelDBC = new SpellDispelType(this, loadedDBC);
            if (ERROR_STR.Length != 0)
            {
                await this.ShowMessageAsync("ERROR", ERROR_STR);
                ERROR_STR = "";
                Environment.Exit(1);
            }
            loadedMechanic = new SpellMechanic(this, loadedDBC);
            if (ERROR_STR.Length != 0)
            {
                await this.ShowMessageAsync("ERROR", ERROR_STR);
                ERROR_STR = "";
                Environment.Exit(1);
                return;
            }
            loadedCastTime = new SpellCastTimes(this, loadedDBC);
            if (ERROR_STR.Length != 0)
            {
                await this.ShowMessageAsync("ERROR", ERROR_STR);
                ERROR_STR = "";
                Environment.Exit(1);
                return;
            }
            loadedDuration = new SpellDuration(this, loadedDBC);
            if (ERROR_STR.Length != 0)
            {
                await this.ShowMessageAsync("ERROR", ERROR_STR);
                ERROR_STR = "";
                Environment.Exit(1);
                return;
            }
            loadedRange = new SpellRange(this, loadedDBC);
            if (ERROR_STR.Length != 0)
            {
                await this.ShowMessageAsync("ERROR", ERROR_STR);
                Environment.Exit(1);
                return;
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

        private void updateMainWindow()
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

            Effect1.SelectedIndex = (int)loadedDBC.body.records[selectedID].record.Effect1;
            Effect2.SelectedIndex = (int)loadedDBC.body.records[selectedID].record.Effect2;
            Effect3.SelectedIndex = (int)loadedDBC.body.records[selectedID].record.Effect3;
            EffectBase1.Text = loadedDBC.body.records[selectedID].record.EffectBasePoints1.ToString();
            EffectMod1.Text = loadedDBC.body.records[selectedID].record.EffectDieSides1.ToString();
            EffectBase2.Text = loadedDBC.body.records[selectedID].record.EffectBasePoints2.ToString();
            EffectMod2.Text = loadedDBC.body.records[selectedID].record.EffectDieSides2.ToString();
            EffectBase3.Text = loadedDBC.body.records[selectedID].record.EffectBasePoints3.ToString();
            EffectMod3.Text = loadedDBC.body.records[selectedID].record.EffectDieSides3.ToString();

            PreventionType.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.PreventionType;
            SpellDamageType.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.DmgClass;
            SpellMissileID.Text = loadedDBC.body.records[selectedID].record.spellMissileID.ToString();

            mask = loadedDBC.body.records[selectedID].record.Targets;
            if (mask == 0)
            {
                targetBoxes[0].IsChecked = true;
                for (int f = 1; f < targetBoxes.Count; ++f)
                    targetBoxes[f].IsChecked = false;
            }
            else
            {
                targetBoxes[0].IsChecked = false;
                UInt32 flag = 1;
                for (int f = 1; f < targetBoxes.Count; ++f)
                {
                    targetBoxes[f].IsChecked = ((mask & flag) != 0) ? true : false;
                    flag = flag + flag;
                }
            }
            // I don't trust a UInt32 will be big enough 2^32 possible values and UInt32 only fits (2^32)-1 I believe?
            // Still the Blizzard DBC stores it as a UInt32 so lets try this...
            mask = loadedDBC.body.records[selectedID].record.procFlags;
            if (mask == 0)
            {
                procBoxes[0].IsChecked = true;
                for (int f = 1; f < procBoxes.Count; ++f)
                    procBoxes[f].IsChecked = false;
            }
            else
            {
                procBoxes[0].IsChecked = false;
                UInt32 flag = 1;
                for (int f = 1; f < procBoxes.Count; ++f)
                {
                    procBoxes[f].IsChecked = ((mask & flag) != 0) ? true : false;
                    flag = flag + flag;
                }
            }

            ProcChance.Text = loadedDBC.body.records[selectedID].record.procChance.ToString();
            ProcCharges.Text = loadedDBC.body.records[selectedID].record.procCharges.ToString();

            DieSides1.Text = loadedDBC.body.records[selectedID].record.EffectDieSides1.ToString();
            DieSides2.Text = loadedDBC.body.records[selectedID].record.EffectDieSides2.ToString();
            DieSides3.Text = loadedDBC.body.records[selectedID].record.EffectDieSides3.ToString();
            BasePointsPerLevel1.Text = loadedDBC.body.records[selectedID].record.EffectRealPointsPerLevel1.ToString();
            BasePointsPerLevel2.Text = loadedDBC.body.records[selectedID].record.EffectRealPointsPerLevel2.ToString();
            BasePointsPerLevel3.Text = loadedDBC.body.records[selectedID].record.EffectRealPointsPerLevel3.ToString();
            BasePoints1.Text = loadedDBC.body.records[selectedID].record.EffectBasePoints1.ToString();
            BasePoints2.Text = loadedDBC.body.records[selectedID].record.EffectBasePoints2.ToString();
            BasePoints3.Text = loadedDBC.body.records[selectedID].record.EffectBasePoints3.ToString();
            Amplitude1.Text = loadedDBC.body.records[selectedID].record.EffectAmplitude1.ToString();
            Amplitude2.Text = loadedDBC.body.records[selectedID].record.EffectAmplitude2.ToString();
            Amplitude3.Text = loadedDBC.body.records[selectedID].record.EffectAmplitude3.ToString();
            MultipleValue1.Text = loadedDBC.body.records[selectedID].record.EffectMultipleValue1.ToString();
            MultipleValue2.Text = loadedDBC.body.records[selectedID].record.EffectMultipleValue2.ToString();
            MultipleValue3.Text = loadedDBC.body.records[selectedID].record.EffectMultipleValue3.ToString();
            ItemType1.Text = loadedDBC.body.records[selectedID].record.EffectItemType1.ToString();
            ItemType2.Text = loadedDBC.body.records[selectedID].record.EffectItemType2.ToString();
            ItemType3.Text = loadedDBC.body.records[selectedID].record.EffectItemType3.ToString();
            TriggerSpell1.Text = loadedDBC.body.records[selectedID].record.EffectTriggerSpell1.ToString();
            TriggerSpell2.Text = loadedDBC.body.records[selectedID].record.EffectTriggerSpell2.ToString();
            TriggerSpell3.Text = loadedDBC.body.records[selectedID].record.EffectTriggerSpell3.ToString();
            PointsPerComboPoint1.Text = loadedDBC.body.records[selectedID].record.EffectPointsPerComboPoint1.ToString();
            PointsPerComboPoint2.Text = loadedDBC.body.records[selectedID].record.EffectPointsPerComboPoint2.ToString();
            PointsPerComboPoint3.Text = loadedDBC.body.records[selectedID].record.EffectPointsPerComboPoint3.ToString();

            loadedDispelDBC.UpdateDispelSelection();
            loadedMechanic.updateMechanicSelection();
            loadedCastTime.updateCastTimeSelection();
            loadedDuration.updateDurationIndexes();
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
            loadAllDBCs();
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
                loadedDBC.body.records[selectedID].record.Effect1 = (UInt32)Effect1.SelectedIndex;
                loadedDBC.body.records[selectedID].record.Effect2 = (UInt32)Effect2.SelectedIndex;
                loadedDBC.body.records[selectedID].record.Effect3 = (UInt32)Effect3.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectBasePoints1 = Int32.Parse(EffectBase1.Text);
                loadedDBC.body.records[selectedID].record.EffectDieSides1 = Int32.Parse(EffectMod1.Text);
                loadedDBC.body.records[selectedID].record.EffectBasePoints2 = Int32.Parse(EffectBase2.Text);
                loadedDBC.body.records[selectedID].record.EffectDieSides2 = Int32.Parse(EffectMod2.Text);
                loadedDBC.body.records[selectedID].record.EffectBasePoints3 = Int32.Parse(EffectBase3.Text);
                loadedDBC.body.records[selectedID].record.EffectDieSides3 = Int32.Parse(EffectMod3.Text);
                loadedDBC.body.records[selectedID].record.PreventionType = (UInt32)PreventionType.SelectedIndex;
                loadedDBC.body.records[selectedID].record.DmgClass = (UInt32)SpellDamageType.SelectedIndex;
                loadedDBC.body.records[selectedID].record.spellMissileID = UInt32.Parse(SpellMissileID.Text);
                if (targetBoxes[0].IsChecked.Value)
                    loadedDBC.body.records[selectedID].record.Targets = 0;
                else
                {
                    UInt32 mask = 0;
                    UInt32 flag = 1;
                    for (int f = 1; f < targetBoxes.Count; ++f)
                    {
                        if (targetBoxes[f].IsChecked.Value)
                            mask = mask + flag;
                        flag = flag + flag;
                    }
                    loadedDBC.body.records[selectedID].record.Targets = mask;
                }
                if (procBoxes[0].IsChecked.Value)
                    loadedDBC.body.records[selectedID].record.procFlags = 0;
                else
                {
                    UInt32 mask = 0;
                    UInt32 flag = 1;
                    for (int f = 1; f < procBoxes.Count; ++f)
                    {
                        if (procBoxes[f].IsChecked.Value)
                            mask = mask + flag;
                        flag = flag + flag;
                    }
                    loadedDBC.body.records[selectedID].record.procFlags = mask;
                }
                loadedDBC.body.records[selectedID].record.procChance = UInt32.Parse(ProcChance.Text);
                loadedDBC.body.records[selectedID].record.procCharges = UInt32.Parse(ProcCharges.Text);

                loadedDBC.body.records[selectedID].record.EffectDieSides1 = Int32.Parse(DieSides1.Text);
                loadedDBC.body.records[selectedID].record.EffectDieSides2 = Int32.Parse(DieSides2.Text);
                loadedDBC.body.records[selectedID].record.EffectDieSides3 = Int32.Parse(DieSides3.Text);
                loadedDBC.body.records[selectedID].record.EffectRealPointsPerLevel1 = Int32.Parse(BasePointsPerLevel1.Text);
                loadedDBC.body.records[selectedID].record.EffectRealPointsPerLevel2 = Int32.Parse(BasePointsPerLevel2.Text);
                loadedDBC.body.records[selectedID].record.EffectRealPointsPerLevel3 = Int32.Parse(BasePointsPerLevel3.Text);
                loadedDBC.body.records[selectedID].record.EffectBasePoints1 = Int32.Parse(BasePoints1.Text);
                loadedDBC.body.records[selectedID].record.EffectBasePoints2 = Int32.Parse(BasePoints2.Text);
                loadedDBC.body.records[selectedID].record.EffectBasePoints3 = Int32.Parse(BasePoints3.Text);
                loadedDBC.body.records[selectedID].record.EffectAmplitude1 = UInt32.Parse(Amplitude1.Text);
                loadedDBC.body.records[selectedID].record.EffectAmplitude2 = UInt32.Parse(Amplitude2.Text);
                loadedDBC.body.records[selectedID].record.EffectAmplitude3 = UInt32.Parse(Amplitude3.Text);
                loadedDBC.body.records[selectedID].record.EffectMultipleValue1 = float.Parse(MultipleValue1.Text);
                loadedDBC.body.records[selectedID].record.EffectMultipleValue2 = float.Parse(MultipleValue2.Text);
                loadedDBC.body.records[selectedID].record.EffectMultipleValue3 = float.Parse(MultipleValue3.Text);
                loadedDBC.body.records[selectedID].record.EffectItemType1 = UInt32.Parse(ItemType1.Text);
                loadedDBC.body.records[selectedID].record.EffectItemType2 = UInt32.Parse(ItemType2.Text);
                loadedDBC.body.records[selectedID].record.EffectItemType3 = UInt32.Parse(ItemType3.Text);
                loadedDBC.body.records[selectedID].record.EffectTriggerSpell1 = UInt32.Parse(TriggerSpell1.Text);
                loadedDBC.body.records[selectedID].record.EffectTriggerSpell2 = UInt32.Parse(TriggerSpell2.Text);
                loadedDBC.body.records[selectedID].record.EffectTriggerSpell3 = UInt32.Parse(TriggerSpell3.Text);
                loadedDBC.body.records[selectedID].record.EffectPointsPerComboPoint1 = float.Parse(PointsPerComboPoint1.Text);
                loadedDBC.body.records[selectedID].record.EffectPointsPerComboPoint2 = float.Parse(PointsPerComboPoint2.Text);
                loadedDBC.body.records[selectedID].record.EffectPointsPerComboPoint3 = float.Parse(PointsPerComboPoint3.Text);
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
