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
        private SpellRadius loadedRadius = null;

        private Dictionary<int, TextBox> stringObjectMap = new Dictionary<int, TextBox>();
        public UInt32 selectedID = 1;
        private bool Updating_Strings = false;
        public TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        public UInt32 NewIconID = 1;

        public string ERROR_STR = "";

        private List<CheckBox> targetBoxes = new List<CheckBox>();
        private List<CheckBox> procBoxes = new List<CheckBox>();
        private List<CheckBox> interrupt1 = new List<CheckBox>();
        private List<CheckBox> interrupt2 = new List<CheckBox>();
        private List<CheckBox> interrupt3 = new List<CheckBox>();

        public MainWindow()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(CrashHandler);

            InitializeComponent();
        }

        static void CrashHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("ERROR: " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);

            string[] errorMsg = { "[" + DateTime.Now.ToShortTimeString().ToString() + "] ERROR: ", e.ToString() + "\n" };

            System.IO.File.WriteAllLines(@"ERRORS.txt", errorMsg);
        }

        private async void loadAllDBCs()
        {
            //// Load DBC's
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

            //// Update spell list
            populateSelectSpell();

            ///// Update map
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

            //// Set up form
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

            string[] spell_aura_effect_names = { "SPELL_AURA_NONE", "SPELL_AURA_BIND_SIGHT", "SPELL_AURA_MOD_POSSESS", "SPELL_AURA_PERIODIC_DAMAGE", "SPELL_AURA_DUMMY",
                                                   "SPELL_AURA_MOD_CONFUSE", "SPELL_AURA_MOD_CHARM", "SPELL_AURA_MOD_FEAR", "SPELL_AURA_PERIODIC_HEAL", "SPELL_AURA_MOD_ATTACKSPEED", 
                                                   "SPELL_AURA_MOD_THREAT", "SPELL_AURA_MOD_TAUNT", "SPELL_AURA_MOD_STUN", "SPELL_AURA_MOD_DAMAGE_DONE", "SPELL_AURA_MOD_DAMAGE_TAKEN",
                                                   "SPELL_AURA_DAMAGE_SHIELD", "SPELL_AURA_MOD_STEALTH", "SPELL_AURA_MOD_STEALTH_DETECT", "SPELL_AURA_MOD_INVISIBILITY", 
                                                   "SPELL_AURA_MOD_INVISIBILITY_DETECT", "SPELL_AURA_OBS_MOD_HEALTH", "SPELL_AURA_OBS_MOD_POWER", "SPELL_AURA_MOD_RESISTANCE", 
                                                   "SPELL_AURA_PERIODIC_TRIGGER_SPELL", "SPELL_AURA_PERIODIC_ENERGIZE", "SPELL_AURA_MOD_PACIFY", "SPELL_AURA_MOD_ROOT", 
                                                   "SPELL_AURA_MOD_SILENCE", "SPELL_AURA_REFLECT_SPELLS", "SPELL_AURA_MOD_STAT", "SPELL_AURA_MOD_SKILL", "SPELL_AURA_MOD_INCREASE_SPEED", 
                                                   "SPELL_AURA_MOD_INCREASE_MOUNTED_SPEED", "SPELL_AURA_MOD_DECREASE_SPEED", "SPELL_AURA_MOD_INCREASE_HEALTH", 
                                                   "SPELL_AURA_MOD_INCREASE_ENERGY", "SPELL_AURA_MOD_SHAPESHIFT", "SPELL_AURA_EFFECT_IMMUNITY", "SPELL_AURA_STATE_IMMUNITY",
                                                   "SPELL_AURA_SCHOOL_IMMUNITY", "SPELL_AURA_DAMAGE_IMMUNITY", "SPELL_AURA_DISPEL_IMMUNITY", "SPELL_AURA_PROC_TRIGGER_SPELL",
                                                   "SPELL_AURA_PROC_TRIGGER_DAMAGE", "SPELL_AURA_TRACK_CREATURES", "SPELL_AURA_TRACK_RESOURCES", "SPELL_AURA_46", 
                                                   "SPELL_AURA_MOD_PARRY_PERCENT", "SPELL_AURA_48", "SPELL_AURA_MOD_DODGE_PERCENT", "SPELL_AURA_MOD_CRITICAL_HEALING_AMOUNT", 
                                                   "SPELL_AURA_MOD_BLOCK_PERCENT", "SPELL_AURA_MOD_WEAPON_CRIT_PERCENT", "SPELL_AURA_PERIODIC_LEECH", "SPELL_AURA_MOD_HIT_CHANCE",
                                                   "SPELL_AURA_MOD_SPELL_HIT_CHANCE", "SPELL_AURA_TRANSFORM", "SPELL_AURA_MOD_SPELL_CRIT_CHANCE", "SPELL_AURA_MOD_INCREASE_SWIM_SPEED",
                                                   "SPELL_AURA_MOD_DAMAGE_DONE_CREATURE", "SPELL_AURA_MOD_PACIFY_SILENCE", "SPELL_AURA_MOD_SCALE", "SPELL_AURA_PERIODIC_HEALTH_FUNNEL",
                                                   "SPELL_AURA_63", "SPELL_AURA_PERIODIC_MANA_LEECH", "SPELL_AURA_MOD_CASTING_SPEED_NOT_STACK", "SPELL_AURA_FEIGN_DEATH", 
                                                   "SPELL_AURA_MOD_DISARM", "SPELL_AURA_MOD_STALKED", "SPELL_AURA_SCHOOL_ABSORB", "SPELL_AURA_EXTRA_ATTACKS", 
                                                   "SPELL_AURA_MOD_SPELL_CRIT_CHANCE_SCHOOL", "SPELL_AURA_MOD_POWER_COST_SCHOOL_PCT", "SPELL_AURA_MOD_POWER_COST_SCHOOL",
                                                   "SPELL_AURA_REFLECT_SPELLS_SCHOOL", "SPELL_AURA_MOD_LANGUAGE", "SPELL_AURA_FAR_SIGHT", "SPELL_AURA_MECHANIC_IMMUNITY",
                                                   "SPELL_AURA_MOUNTED", "SPELL_AURA_MOD_DAMAGE_PERCENT_DONE", "SPELL_AURA_MOD_PERCENT_STAT", "SPELL_AURA_SPLIT_DAMAGE_PCT", 
                                                   "SPELL_AURA_WATER_BREATHING", "SPELL_AURA_MOD_BASE_RESISTANCE", "SPELL_AURA_MOD_REGEN", "SPELL_AURA_MOD_POWER_REGEN", 
                                                   "SPELL_AURA_CHANNEL_DEATH_ITEM", "SPELL_AURA_MOD_DAMAGE_PERCENT_TAKEN", "SPELL_AURA_MOD_HEALTH_REGEN_PERCENT",
                                                   "SPELL_AURA_PERIODIC_DAMAGE_PERCENT", "SPELL_AURA_90", "SPELL_AURA_MOD_DETECT_RANGE", "SPELL_AURA_PREVENTS_FLEEING",
                                                   "SPELL_AURA_MOD_UNATTACKABLE", "SPELL_AURA_INTERRUPT_REGEN", "SPELL_AURA_GHOST", "SPELL_AURA_SPELL_MAGNET",
                                                   "SPELL_AURA_MANA_SHIELD", "SPELL_AURA_MOD_SKILL_TALENT", "SPELL_AURA_MOD_ATTACK_POWER", "SPELL_AURA_AURAS_VISIBLE",
                                                   "SPELL_AURA_MOD_RESISTANCE_PCT", "SPELL_AURA_MOD_MELEE_ATTACK_POWER_VERSUS", "SPELL_AURA_MOD_TOTAL_THREAT", "SPELL_AURA_WATER_WALK",
                                                   "SPELL_AURA_FEATHER_FALL", "SPELL_AURA_HOVER", "SPELL_AURA_ADD_FLAT_MODIFIER", "SPELL_AURA_ADD_PCT_MODIFIER",
                                                   "SPELL_AURA_ADD_TARGET_TRIGGER", "SPELL_AURA_MOD_POWER_REGEN_PERCENT", "SPELL_AURA_ADD_CASTER_HIT_TRIGGER", 
                                                   "SPELL_AURA_OVERRIDE_CLASS_SCRIPTS", "SPELL_AURA_MOD_RANGED_DAMAGE_TAKEN", "SPELL_AURA_MOD_RANGED_DAMAGE_TAKEN_PCT", 
                                                   "SPELL_AURA_MOD_HEALING", "SPELL_AURA_MOD_REGEN_DURING_COMBAT", "SPELL_AURA_MOD_MECHANIC_RESISTANCE", "SPELL_AURA_MOD_HEALING_PCT", 
                                                   "SPELL_AURA_119", "SPELL_AURA_UNTRACKABLE", "SPELL_AURA_EMPATHY", "SPELL_AURA_MOD_OFFHAND_DAMAGE_PCT", 
                                                   "SPELL_AURA_MOD_TARGET_RESISTANCE", "SPELL_AURA_MOD_RANGED_ATTACK_POWER", "SPELL_AURA_MOD_MELEE_DAMAGE_TAKEN",
                                                   "SPELL_AURA_MOD_MELEE_DAMAGE_TAKEN_PCT", "SPELL_AURA_RANGED_ATTACK_POWER_ATTACKER_BONUS", "SPELL_AURA_MOD_POSSESS_PET",
                                                   "SPELL_AURA_MOD_SPEED_ALWAYS", "SPELL_AURA_MOD_MOUNTED_SPEED_ALWAYS", "SPELL_AURA_MOD_RANGED_ATTACK_POWER_VERSUS",
                                                   "SPELL_AURA_MOD_INCREASE_ENERGY_PERCENT", "SPELL_AURA_MOD_INCREASE_HEALTH_PERCENT", "SPELL_AURA_MOD_MANA_REGEN_INTERRUPT", 
                                                   "SPELL_AURA_MOD_HEALING_DONE", "SPELL_AURA_MOD_HEALING_DONE_PERCENT", "SPELL_AURA_MOD_TOTAL_STAT_PERCENTAGE",
                                                   "SPELL_AURA_MOD_MELEE_HASTE", "SPELL_AURA_FORCE_REACTION", "SPELL_AURA_MOD_RANGED_HASTE", "SPELL_AURA_MOD_RANGED_AMMO_HASTE", 
                                                   "SPELL_AURA_MOD_BASE_RESISTANCE_PCT", "SPELL_AURA_MOD_RESISTANCE_EXCLUSIVE", "SPELL_AURA_SAFE_FALL",
                                                   "SPELL_AURA_MOD_PET_TALENT_POINTS", "SPELL_AURA_ALLOW_TAME_PET_TYPE", "SPELL_AURA_MECHANIC_IMMUNITY_MASK",
                                                   "SPELL_AURA_RETAIN_COMBO_POINTS", "SPELL_AURA_REDUCE_PUSHBACK", "SPELL_AURA_MOD_SHIELD_BLOCKVALUE_PCT", "SPELL_AURA_TRACK_STEALTHED",
                                                   "SPELL_AURA_MOD_DETECTED_RANGE", "SPELL_AURA_SPLIT_DAMAGE_FLAT", "SPELL_AURA_MOD_STEALTH_LEVEL", "SPELL_AURA_MOD_WATER_BREATHING",
                                                   "SPELL_AURA_MOD_REPUTATION_GAIN", "SPELL_AURA_PET_DAMAGE_MULTI", "SPELL_AURA_MOD_SHIELD_BLOCKVALUE", "SPELL_AURA_NO_PVP_CREDIT",
                                                   "SPELL_AURA_MOD_AOE_AVOIDANCE", "SPELL_AURA_MOD_HEALTH_REGEN_IN_COMBAT", "SPELL_AURA_POWER_BURN", "SPELL_AURA_MOD_CRIT_DAMAGE_BONUS",
                                                   "SPELL_AURA_164", "SPELL_AURA_MELEE_ATTACK_POWER_ATTACKER_BONUS", "SPELL_AURA_MOD_ATTACK_POWER_PCT",
                                                   "SPELL_AURA_MOD_RANGED_ATTACK_POWER_PCT", "SPELL_AURA_MOD_DAMAGE_DONE_VERSUS", "SPELL_AURA_MOD_CRIT_PERCENT_VERSUS",
                                                   "SPELL_AURA_DETECT_AMORE", "SPELL_AURA_MOD_SPEED_NOT_STACK", "SPELL_AURA_MOD_MOUNTED_SPEED_NOT_STACK", "SPELL_AURA_173",
                                                   "SPELL_AURA_MOD_SPELL_DAMAGE_OF_STAT_PERCENT", "SPELL_AURA_MOD_SPELL_HEALING_OF_STAT_PERCENT", "SPELL_AURA_SPIRIT_OF_REDEMPTION",
                                                   "SPELL_AURA_AOE_CHARM", "SPELL_AURA_MOD_DEBUFF_RESISTANCE", "SPELL_AURA_MOD_ATTACKER_SPELL_CRIT_CHANCE", 
                                                   "SPELL_AURA_MOD_FLAT_SPELL_DAMAGE_VERSUS", "SPELL_AURA_181", "SPELL_AURA_MOD_RESISTANCE_OF_STAT_PERCENT", "SPELL_AURA_MOD_CRITICAL_THREAT",
                                                   "SPELL_AURA_MOD_ATTACKER_MELEE_HIT_CHANCE", "SPELL_AURA_MOD_ATTACKER_RANGED_HIT_CHANCE", "SPELL_AURA_MOD_ATTACKER_SPELL_HIT_CHANCE", 
                                                   "SPELL_AURA_MOD_ATTACKER_MELEE_CRIT_CHANCE", "SPELL_AURA_MOD_ATTACKER_RANGED_CRIT_CHANCE", "SPELL_AURA_MOD_RATING", 
                                                   "SPELL_AURA_MOD_FACTION_REPUTATION_GAIN", "SPELL_AURA_USE_NORMAL_MOVEMENT_SPEED", "SPELL_AURA_MOD_MELEE_RANGED_HASTE",
                                                   "SPELL_AURA_MELEE_SLOW", "SPELL_AURA_MOD_TARGET_ABSORB_SCHOOL", "SPELL_AURA_MOD_TARGET_ABILITY_ABSORB_SCHOOL", "SPELL_AURA_MOD_COOLDOWN",
                                                   "SPELL_AURA_MOD_ATTACKER_SPELL_AND_WEAPON_CRIT_CHANCE", "SPELL_AURA_198", "SPELL_AURA_MOD_INCREASES_SPELL_PCT_TO_HIT",
                                                   "SPELL_AURA_MOD_XP_PCT", "SPELL_AURA_FLY", "SPELL_AURA_IGNORE_COMBAT_RESULT", "SPELL_AURA_MOD_ATTACKER_MELEE_CRIT_DAMAGE", 
                                                   "SPELL_AURA_MOD_ATTACKER_RANGED_CRIT_DAMAGE", "SPELL_AURA_MOD_SCHOOL_CRIT_DMG_TAKEN", "SPELL_AURA_MOD_INCREASE_VEHICLE_FLIGHT_SPEED",
                                                   "SPELL_AURA_MOD_INCREASE_MOUNTED_FLIGHT_SPEED", "SPELL_AURA_MOD_INCREASE_FLIGHT_SPEED", "SPELL_AURA_MOD_MOUNTED_FLIGHT_SPEED_ALWAYS",
                                                   "SPELL_AURA_MOD_VEHICLE_SPEED_ALWAYS", "SPELL_AURA_MOD_FLIGHT_SPEED_NOT_STACK", "SPELL_AURA_MOD_RANGED_ATTACK_POWER_OF_STAT_PERCENT",
                                                   "SPELL_AURA_MOD_RAGE_FROM_DAMAGE_DEALT", "SPELL_AURA_214", "SPELL_AURA_ARENA_PREPARATION", "SPELL_AURA_HASTE_SPELLS",
                                                   "SPELL_AURA_MOD_MELEE_HASTE_2", "SPELL_AURA_HASTE_RANGED", "SPELL_AURA_MOD_MANA_REGEN_FROM_STAT", "SPELL_AURA_MOD_RATING_FROM_STAT",
                                                   "SPELL_AURA_MOD_DETAUNT", "SPELL_AURA_222", "SPELL_AURA_RAID_PROC_FROM_CHARGE", "SPELL_AURA_224", 
                                                   "SPELL_AURA_RAID_PROC_FROM_CHARGE_WITH_VALUE", "SPELL_AURA_PERIODIC_DUMMY", "SPELL_AURA_PERIODIC_TRIGGER_SPELL_WITH_VALUE",
                                                   "SPELL_AURA_DETECT_STEALTH", "SPELL_AURA_MOD_AOE_DAMAGE_AVOIDANCE", "SPELL_AURA_230", "SPELL_AURA_PROC_TRIGGER_SPELL_WITH_VALUE",
                                                   "SPELL_AURA_MECHANIC_DURATION_MOD", "SPELL_AURA_CHANGE_MODEL_FOR_ALL_HUMANOIDS", "SPELL_AURA_MECHANIC_DURATION_MOD_NOT_STACK",
                                                   "SPELL_AURA_MOD_DISPEL_RESIST", "SPELL_AURA_CONTROL_VEHICLE", "SPELL_AURA_MOD_SPELL_DAMAGE_OF_ATTACK_POWER",
                                                   "SPELL_AURA_MOD_SPELL_HEALING_OF_ATTACK_POWER", "SPELL_AURA_MOD_SCALE_2", "SPELL_AURA_MOD_EXPERTISE", "SPELL_AURA_FORCE_MOVE_FORWARD",
                                                   "SPELL_AURA_MOD_SPELL_DAMAGE_FROM_HEALING", "SPELL_AURA_MOD_FACTION", "SPELL_AURA_COMPREHEND_LANGUAGE", "SPELL_AURA_MOD_AURA_DURATION_BY_DISPEL",
                                                   "SPELL_AURA_MOD_AURA_DURATION_BY_DISPEL_NOT_STACK", "SPELL_AURA_CLONE_CASTER", "SPELL_AURA_MOD_COMBAT_RESULT_CHANCE", "SPELL_AURA_CONVERT_RUNE",
                                                   "SPELL_AURA_MOD_INCREASE_HEALTH_2", "SPELL_AURA_MOD_ENEMY_DODGE", "SPELL_AURA_MOD_SPEED_SLOW_ALL", "SPELL_AURA_MOD_BLOCK_CRIT_CHANCE",
                                                   "SPELL_AURA_MOD_DISARM_OFFHAND", "SPELL_AURA_MOD_MECHANIC_DAMAGE_TAKEN_PERCENT", "SPELL_AURA_NO_REAGENT_USE", 
                                                   "SPELL_AURA_MOD_TARGET_RESIST_BY_SPELL_CLASS", "SPELL_AURA_258", "SPELL_AURA_MOD_HOT_PCT", "SPELL_AURA_SCREEN_EFFECT", "SPELL_AURA_PHASE",
                                                   "SPELL_AURA_ABILITY_IGNORE_AURASTATE", "SPELL_AURA_ALLOW_ONLY_ABILITY", "SPELL_AURA_264", "SPELL_AURA_265", "SPELL_AURA_266",
                                                   "SPELL_AURA_MOD_IMMUNE_AURA_APPLY_SCHOOL", "SPELL_AURA_MOD_ATTACK_POWER_OF_STAT_PERCENT", "SPELL_AURA_MOD_IGNORE_TARGET_RESIST", 
                                                   "SPELL_AURA_MOD_ABILITY_IGNORE_TARGET_RESIST", "SPELL_AURA_MOD_DAMAGE_FROM_CASTER", "SPELL_AURA_IGNORE_MELEE_RESET", "SPELL_AURA_X_RAY",
                                                   "SPELL_AURA_ABILITY_CONSUME_NO_AMMO", "SPELL_AURA_MOD_IGNORE_SHAPESHIFT", "SPELL_AURA_MOD_DAMAGE_DONE_FOR_MECHANIC", 
                                                   "SPELL_AURA_MOD_MAX_AFFECTED_TARGETS", "SPELL_AURA_MOD_DISARM_RANGED", "SPELL_AURA_INITIALIZE_IMAGES", "SPELL_AURA_MOD_ARMOR_PENETRATION_PCT",
                                                   "SPELL_AURA_MOD_HONOR_GAIN_PCT", "SPELL_AURA_MOD_BASE_HEALTH_PCT", "SPELL_AURA_MOD_HEALING_RECEIVED", "SPELL_AURA_LINKED",
                                                   "SPELL_AURA_MOD_ATTACK_POWER_OF_ARMOR", "SPELL_AURA_ABILITY_PERIODIC_CRIT", "SPELL_AURA_DEFLECT_SPELLS", "SPELL_AURA_IGNORE_HIT_DIRECTION",
                                                   "SPELL_AURA_289", "SPELL_AURA_MOD_CRIT_PCT", "SPELL_AURA_MOD_XP_QUEST_PCT", "SPELL_AURA_OPEN_STABLE", "SPELL_AURA_OVERRIDE_SPELLS",
                                                   "SPELL_AURA_PREVENT_REGENERATE_POWER", "SPELL_AURA_295", "SPELL_AURA_SET_VEHICLE_ID", "SPELL_AURA_BLOCK_SPELL_FAMILY", "SPELL_AURA_STRANGULATE", 
                                                   "SPELL_AURA_299", "SPELL_AURA_SHARE_DAMAGE_PCT", "SPELL_AURA_SCHOOL_HEAL_ABSORB", "SPELL_AURA_302", "SPELL_AURA_MOD_DAMAGE_DONE_VERSUS_AURASTATE", 
                                                   "SPELL_AURA_MOD_FAKE_INEBRIATE", "SPELL_AURA_MOD_MINIMUM_SPEED", "SPELL_AURA_306", "SPELL_AURA_HEAL_ABSORB_TEST",
                                                   "SPELL_AURA_MOD_CRIT_CHANCE_FOR_CASTER", "SPELL_AURA_309", "SPELL_AURA_MOD_CREATURE_AOE_DAMAGE_AVOIDANCE", "SPELL_AURA_311", "SPELL_AURA_312",
                                                   "SPELL_AURA_313", "SPELL_AURA_PREVENT_RESURRECTION", "SPELL_AURA_UNDERWATER_WALKING", "SPELL_AURA_PERIODIC_HASTE" };
            for (int i = 0; i < spell_aura_effect_names.Length; ++i)
            {
                ApplyAuraName1.Items.Add(spell_aura_effect_names[i]);
                ApplyAuraName2.Items.Add(spell_aura_effect_names[i]);
                ApplyAuraName3.Items.Add(spell_aura_effect_names[i]);
            }

            string[] spell_effect_names = { "NULL", "INSTANT_KILL", "SCHOOL_DAMAGE", "DUMMY", "PORTAL_TELEPORT", "TELEPORT_UNITS", "APPLY_AURA", "ENVIRONMENTAL_DAMAGE", "POWER_DRAIN", "HEALTH_LEECH",
                                              "HEAL", "BIND", "PORTAL", "RITUAL_BASE", "RITUAL_SPECIALIZE", "RITUAL_ACTIVATE_PORTAL", "QUEST_COMPLETE", "WEAPON_DAMAGE_NOSCHOOL", "RESURRECT",
                                              "ADD_EXTRA_ATTACKS", "DODGE", "EVADE", "PARRY", "BLOCK", "CREATE_ITEM", "WEAPON", "DEFENSE", "PERSISTENT_AREA_AURA", "SUMMON", "LEAP", "ENERGIZE",
                                              "WEAPON_PERCENT_DAMAGE", "TRIGGER_MISSILE", "OPEN_LOCK", "TRANSFORM_ITEM", "APPLY_GROUP_AREA_AURA", "LEARN_SPELL", "SPELL_DEFENSE", "DISPEL", "LANGUAGE",
                                              "DUAL_WIELD", "LEAP_41", "SUMMON_GUARDIAN", "TELEPORT_UNITS_FACE_CASTER", "SKILL_STEP", "UNDEFINED_45", "SPAWN", "TRADE_SKILL", "STEALTH", "DETECT",
                                              "SUMMON_OBJECT", "FORCE_CRITICAL_HIT", "GUARANTEE_HIT", "ENCHANT_ITEM", "ENCHANT_ITEM_TEMPORARY", "TAMECREATURE", "SUMMON_PET", "LEARN_PET_SPELL",
                                              "WEAPON_DAMAGE", "OPEN_LOCK_ITEM", "PROFICIENCY", "SEND_EVENT", "POWER_BURN", "THREAT", "TRIGGER_SPELL", "APPLY_RAID_AREA_AURA", "POWER_FUNNEL",
                                              "HEAL_MAX_HEALTH", "INTERRUPT_CAST", "DISTRACT", "PULL", "PICKPOCKET", "ADD_FARSIGHT", "UNTRAIN_TALENTS", "USE_GLYPH", "HEAL_MECHANICAL",
                                              "SUMMON_OBJECT_WILD", "SCRIPT_EFFECT", "ATTACK", "SANCTUARY", "ADD_COMBO_POINTS", "CREATE_HOUSE", "BIND_SIGHT", "DUEL", "STUCK", "SUMMON_PLAYER", 
                                              "ACTIVATE_OBJECT", "BUILDING_DAMAGE", "BUILDING_REPAIR", "BUILDING_SWITCH_STATE", "KILL_CREDIT_90", "THREAT_ALL", "ENCHANT_HELD_ITEM", "SUMMON_PHANTASM",
                                              "SELF_RESURRECT", "SKINNING", "CHARGE", "SUMMON_MULTIPLE_TOTEMS", "KNOCK_BACK", "DISENCHANT", "INEBRIATE", "FEED_PET", "DISMISS_PET", "REPUTATION",
                                              "SUMMON_OBJECT_SLOT1", "SUMMON_OBJECT_SLOT2", "SUMMON_OBJECT_SLOT3", "SUMMON_OBJECT_SLOT4", "DISPEL_MECHANIC", "SUMMON_DEAD_PET", "DESTROY_ALL_TOTEMS", 
                                              "DURABILITY_DAMAGE", "NONE_112", "RESURRECT_FLAT", "ATTACK_ME", "DURABILITY_DAMAGE_PCT", "SKIN_PLAYER_CORPSE", "SPIRIT_HEAL", "SKILL", "APPLY_PET_AREA_AURA",
                                              "TELEPORT_GRAVEYARD", "DUMMYMELEE", "UNKNOWN1", "START_TAXI", "PLAYER_PULL", "UNKNOWN4", "UNKNOWN5", "PROSPECTING", "APPLY_FRIEND_AREA_AURA", 
                                              "APPLY_ENEMY_AREA_AURA", "UNKNOWN10", "UNKNOWN11", "PLAY_MUSIC", "FORGET_SPECIALIZATION", "KILL_CREDIT", "UNKNOWN15", "UNKNOWN16", "UNKNOWN17",
                                              "UNKNOWN18", "CLEAR_QUEST", "UNKNOWN20", "UNKNOWN21", "TRIGGER_SPELL_WITH_VALUE", "APPLY_OWNER_AREA_AURA", "UNKNOWN23", "UNKNOWN24", "ACTIVATE_RUNES", 
                                              "UNKNOWN26", "UNKNOWN27", "QUEST_FAIL", "UNKNOWN28", "UNKNOWN29", "UNKNOWN30", "SUMMON_TARGET", "SUMMON_REFER_A_FRIEND", "TAME_CREATURE", "ADD_SOCKET", 
                                              "CREATE_ITEM2", "MILLING", "UNKNOWN37", "UNKNOWN38", "LEARN_SPEC", "ACTIVATE_SPEC", "UNKNOWN" };
            for (int i = 0; i < spell_effect_names.Length; ++i)
            {
                SpellEffect1.Items.Add(spell_effect_names[i]);
                SpellEffect2.Items.Add(spell_effect_names[i]);
                SpellEffect3.Items.Add(spell_effect_names[i]);
            }

            string[] mechanic_names = { "MECHANIC_NONE", "MECHANIC_CHARMED", "MECHANIC_DISORIENTED", "MECHANIC_DISARMED", "MECHANIC_DISTRACED", "MECHANIC_FLEEING", "MECHANIC_CLUMSY", "MECHANIC_ROOTED", 
                                          "MECHANIC_PACIFIED", "MECHANIC_SILENCED", "MECHANIC_ASLEEP", "MECHANIC_ENSNARED", "MECHANIC_STUNNED", "MECHANIC_FROZEN", "MECHANIC_INCAPACIPATED", 
                                          "MECHANIC_BLEEDING", "MECHANIC_HEALING", "MECHANIC_POLYMORPHED", "MECHANIC_BANISHED", "MECHANIC_SHIELDED", "MECHANIC_SHACKLED", "MECHANIC_MOUNTED",
                                          "MECHANIC_SEDUCED", "MECHANIC_TURNED", "MECHANIC_HORRIFIED", "MECHANIC_INVULNARABLE", "MECHANIC_INTERRUPTED", "MECHANIC_DAZED", "MECHANIC_DISCOVERY",
                                          "MECHANIC_INVULNERABLE", "MECHANIC_SAPPED", "MECHANIC_ENRAGED" };
            for (int i = 0; i < mechanic_names.Length; ++i)
            {
                Mechanic1.Items.Add(mechanic_names[i]);
                Mechanic2.Items.Add(mechanic_names[i]);
                Mechanic3.Items.Add(mechanic_names[i]);
            }

            string[] implicit_target_names = { "NONE", "SELF", "INVISIBLE_OR_HIDDEN_ENEMIES_AT_LOCATION_RADIUS", "PET", "SINGLE_ENEMY", "SCRIPTED_TARGET", "ALL_TARGETABLE_AROUND_LOCATION_IN_RADIUS",
                                                 "HEARTSTONE_LOCATION", "ALL_ENEMY_IN_AREA", "ALL_ENEMY_IN_AREA_INSTANT", "TELEPORT_LOCATION", "LOCATION_TO_SUMMON", "ALL_PARTY_AROUND_CASTER",
                                                 "SINGLE_FRIEND", "ALL_ENEMIES_AROUND_CASTER", "GAMEOBJECT", "IN_FRONT_OF_CASTER", "DUEL", "GAMEOBJECT_ITEM", "PET_MASTER", "ALL_ENEMY_IN_AREA_CHANNELED",
                                                 "ALL_PARTY_IN_AREA_CHANNELED", "ALL_FRIENDLY_IN_AREA", "ALL_TARGETABLE_AROUND_LOCATION_IN_RADIUS_OVER_TIME", "MINION", "ALL_PARTY_IN_AREA", "SINGLE_PARTY",
                                                 "PET_SUMMON_LOCATION", "ALL_PARTY", "SCRIPTED_OR_SINGLE_TARGET", "SELF_FISHING", "SCRIPTED_GAMEOBJECT", "TOTEM_EARTH", "TOTEM_WATER", "TOTEM_AIR",
                                                 "TOTEM_FIRE", "CHAIN", "SCIPTED_OBJECT_LOCATION", "DYNAMIC_OBJECT", "MULTIPLE_SUMMON_LOCATION", "MULTIPLE_SUMMON_PET_LOCATION", "SUMMON_LOCATION", 
                                                 "CALIRI_EGS", "LOCATION_NEAR_CASTER", "CURRENT_SELECTION", "TARGET_AT_ORIENTATION_TO_CASTER", "LOCATION_INFRONT_CASTER", "ALL_RAID", "PARTY_MEMBER",
                                                 "TARGET_FOR_VISUAL_EFFECT", "SCRIPTED_TARGET2", "AREAEFFECT_PARTY_AND_CLASS", "PRIEST_CHAMPION", "NATURE_SUMMON_LOCATION", "BEHIND_TARGET_LOCATION", 
                                                 "MULTIPLE_GUARDIAN_SUMMON_LOCATION", "NETHETDRAKE_SUMMON_LOCATION", "SCRIPTED_LOCATION", "LOCATION_INFRONT_CASTER_AT_RANGE",
                                                 "ENEMIES_IN_AREA_CHANNELED_WITH_EXCEPTIONS", "SELECTED_ENEMY_CHANNELED", "SELECTED_ENEMY_DEADLY_POISON", "NON_COMBAT_PET" };
            for (int i = 0; i < implicit_target_names.Length; ++i)
            {
                TargetA1.Items.Add(implicit_target_names[i]);
                TargetB1.Items.Add(implicit_target_names[i]);
                TargetA2.Items.Add(implicit_target_names[i]);
                TargetB2.Items.Add(implicit_target_names[i]);
                TargetA3.Items.Add(implicit_target_names[i]);
                TargetB3.Items.Add(implicit_target_names[i]);
                ChainTarget1.Items.Add(implicit_target_names[i]);
                ChainTarget2.Items.Add(implicit_target_names[i]);
                ChainTarget3.Items.Add(implicit_target_names[i]);
            }

            string[] interrupt_strings = { "NULL", "ON_MOVEMENT", "PUSHBACK", "ON_INTERRUPT_CAS", "ON_INTERRUPT_SCHOOL", "ON_DAMAGE_TAKEN", "ON_INTERRUPT_ALL" };
            for (int i = 0; i < interrupt_strings.Length; ++i)
            {
                CheckBox box = new CheckBox();
                box.Content = interrupt_strings[i];
                box.Margin = new Thickness(5, (-9 + i) * 45, 0, 0);
                IntGrid.Children.Add(box);
                interrupt1.Add(box);
            }
            string[] aura_interrupt_strings = { "NULL", "HITBYSPELL", "TAKE_DAMAGE", "CAST", "MOVE", "TURNING", "JUMP", "NOT_MOUNTED", "NOT_ABOVEWATER", "NOT_UNDERWATER", "NOT_SHEATHED", "TALK", "USE", "MELEE_ATTACK", "SPELL_ATTACK", "UNK14", "TRANSFORM", "UNK16", "MOUNT", "NOT_SEATED", "CHANGE_MAP", "IMMUNE_OR_LOST_SELECTION", "UNK21", "TELEPORTED", "ENTER_PVP_COMBAT", "DIRECT_DAMAGE", "LANDING" };
            for (int i = 0; i < aura_interrupt_strings.Length; ++i)
            {
                CheckBox box = new CheckBox();
                box.Content = aura_interrupt_strings[i];
                box.Margin = new Thickness(5, (-13 + i) * 45, 0, 0);
                AuraIntGrid.Children.Add(box);
                interrupt3.Add(box);
            }
            string[] channel_interrupt_strings = { "NULL", "ON_1", "ON_2", "ON_3", "ON_4", "ON_5", "ON_6", "ON_7", "ON_8", "ON_9", "ON_10", "ON_11", "ON_12", "ON_13", "ON_14", "ON_15", "ON_16", "ON_17", "ON_18" };
            for (int i = 0; i < channel_interrupt_strings.Length; ++i)
            {
                CheckBox box = new CheckBox();
                box.Content = channel_interrupt_strings[i];
                box.Margin = new Thickness(5, (-9 + i) * 45, 0, 0);
                ChannelIntGrid.Children.Add(box);
                interrupt2.Add(box);
            }

            //// Load DBC's
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
            loadedRadius = new SpellRadius(this, loadedDBC);
            if (ERROR_STR.Length != 0)
            {
                await this.ShowMessageAsync("ERROR", ERROR_STR);
                Environment.Exit(1);
                return;
            }
        }

        private async void InsertNewRecord(object sender, RoutedEventArgs e)
        {
            if (loadedDBC == null)
                return;
            string input = await this.ShowInputAsync("New Record", "Input the new spell ID.");
            if (input == null)
                return;
            string errorMsg = "";
            UInt32 newID = 0;
            try
            {
                newID = UInt32.Parse(input);
                for (int i = 0; i < loadedDBC.body.records.Length; ++i)
                {
                    if (loadedDBC.body.records[i].record.Id == newID)
                        throw new Exception("The spell ID is already taken!");
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }
            if (errorMsg.Length != 0)
            {
                await this.ShowMessageAsync("ERROR", errorMsg);
                return;
            }

            Int32 newRecord = (Int32)loadedDBC.header.record_count++;
            Array.Resize(ref loadedDBC.body.records, (Int32)loadedDBC.header.record_count);

            loadedDBC.body.records[newRecord].record.SpellName = new UInt32[9];
            loadedDBC.body.records[newRecord].record.Description = new UInt32[9];
            loadedDBC.body.records[newRecord].record.ToolTip = new UInt32[9];
            loadedDBC.body.records[newRecord].record.Rank = new UInt32[9];
            loadedDBC.body.records[newRecord].spellDesc = new String[9];
            loadedDBC.body.records[newRecord].spellName = new String[9];
            loadedDBC.body.records[newRecord].spellRank = new String[9];
            loadedDBC.body.records[newRecord].spellTool = new String[9];
            loadedDBC.body.records[newRecord].record.SpellNameFlag = new UInt32[8];
            loadedDBC.body.records[newRecord].record.DescriptionFlags = new UInt32[8];
            loadedDBC.body.records[newRecord].record.ToolTipFlags = new UInt32[8];
            loadedDBC.body.records[newRecord].record.RankFlags = new UInt32[8];
            for (int i = 0; i < 9; ++i)
            {
                loadedDBC.body.records[newRecord].record.SpellName[i] = 0;
                loadedDBC.body.records[newRecord].record.Description[i] = 0;
                loadedDBC.body.records[newRecord].record.ToolTip[i] = 0;
                loadedDBC.body.records[newRecord].record.Rank[i] = 0;
                loadedDBC.body.records[newRecord].spellDesc[i] = "";
                loadedDBC.body.records[newRecord].spellName[i] = "";
                loadedDBC.body.records[newRecord].spellRank[i] = "";
                loadedDBC.body.records[newRecord].spellTool[i] = "";
                if (i < 8)
                {
                    loadedDBC.body.records[newRecord].record.SpellNameFlag[i] = 0;
                    loadedDBC.body.records[newRecord].record.DescriptionFlags[i] = 0;
                    loadedDBC.body.records[newRecord].record.ToolTipFlags[i] = 0;
                    loadedDBC.body.records[newRecord].record.RankFlags[i] = 0;
                }
            }
            loadedDBC.body.records[newRecord].record.rangeIndex = 1;
            loadedDBC.body.records[newRecord].record.SpellIconID = 1;
            loadedDBC.body.records[newRecord].spellName[0] = "New Spell";
            loadedDBC.body.records[newRecord].record.Id = newID;
            loadedDBC.body.records[newRecord].record.EquippedItemClass = -1;

            // Sort by ID
            loadedDBC.body.records = loadedDBC.body.records.OrderBy(SpellDBC_RecordMap => SpellDBC_RecordMap.record.Id).ToArray<SpellDBC_RecordMap>();

            if (MainTabControl.SelectedIndex != 0)
                MainTabControl.SelectedIndex = 0;
            else
                populateSelectSpell();

            await this.ShowMessageAsync("Success", "Created new record with ID " + input + " sucessfully.");
        }

        private async void DeleteRecord(object sender, RoutedEventArgs e)
        {
            if (loadedDBC == null)
                return;
            string input = await this.ShowInputAsync("Delete Record", "Input the spell ID to delete.");
            if (input == null)
                return;
            string errorMsg = "";
            Int32 newID = 0;
            try
            {
                // Make sure the record ID is a UInt, but parse to a Int afterwards for later functions
                newID = (Int32)UInt32.Parse(input);
                bool found = false;
                for (Int32 i = 0; i < loadedDBC.body.records.Length; ++i)
                {
                    if (loadedDBC.body.records[i].record.Id == newID)
                    {
                        newID = i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                    throw new Exception("The spell ID was not found!");
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }
            if (errorMsg.Length != 0)
            {
                await this.ShowMessageAsync("ERROR", errorMsg);
                return;
            }

            List<SpellDBC_RecordMap> records = loadedDBC.body.records.ToList<SpellDBC_RecordMap>();
            records.RemoveAt(newID);
            loadedDBC.body.records = records.ToArray<SpellDBC_RecordMap>();
            --loadedDBC.header.record_count;

            if (MainTabControl.SelectedIndex != 0)
                MainTabControl.SelectedIndex = 0;
            else
                populateSelectSpell();

            await this.ShowMessageAsync("Success", "Deleted record successfully.");
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
            else if (item.SelectedIndex == item.Items.Count - 1)
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
            int locale = 0;
            // Attempt to get the nearest locality
            for (int i = 0; i < 9; ++i)
            {
                if (loadedDBC.body.records.Length < 3) // unlikely
                    break;
                if (loadedDBC.body.records[2].spellName[i].Length > 0) // 2 = death touch normally
                {
                    locale = i;
                    break;
                }
            }
            // Render spell ID's and name
            for (UInt32 i = 0; i < loadedDBC.body.records.Length; ++i)
            {
                SelectSpell.Items.Add(loadedDBC.body.records[i].record.Id.ToString() + " - " +
                    loadedDBC.body.records[i].spellName[locale]);
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
            mask = loadedDBC.body.records[selectedID].record.InterruptFlags;
            if (mask == 0)
            {
                interrupt1[0].IsChecked = true;
                for (int f = 1; f < interrupt1.Count; ++f)
                    interrupt1[f].IsChecked = false;
            }
            else
            {
                interrupt1[0].IsChecked = false;
                UInt32 flag = 1;
                for (int f = 1; f < interrupt1.Count; ++f)
                {
                    interrupt1[f].IsChecked = ((mask & flag) != 0) ? true : false;
                    flag = flag + flag;
                }
            }
            mask = loadedDBC.body.records[selectedID].record.AuraInterruptFlags;
            if (mask == 0)
            {
                interrupt2[0].IsChecked = true;
                for (int f = 1; f < interrupt2.Count; ++f)
                    interrupt2[f].IsChecked = false;
            }
            else
            {
                interrupt2[0].IsChecked = false;
                UInt32 flag = 1;
                for (int f = 1; f < interrupt2.Count; ++f)
                {
                    interrupt2[f].IsChecked = ((mask & flag) != 0) ? true : false;
                    flag = flag + flag;
                }
            }
            mask = loadedDBC.body.records[selectedID].record.ChannelInterruptFlags;
            if (mask == 0)
            {
                interrupt3[0].IsChecked = true;
                for (int f = 1; f < interrupt3.Count; ++f)
                    interrupt3[f].IsChecked = false;
            }
            else
            {
                interrupt3[0].IsChecked = false;
                UInt32 flag = 1;
                for (int f = 1; f < interrupt3.Count; ++f)
                {
                    interrupt3[f].IsChecked = ((mask & flag) != 0) ? true : false;
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
            SpellMask11.Text = loadedDBC.body.records[selectedID].record.EffectSpellClassMaskA1.ToString();
            SpellMask21.Text = loadedDBC.body.records[selectedID].record.EffectSpellClassMaskA2.ToString();
            SpellMask31.Text = loadedDBC.body.records[selectedID].record.EffectSpellClassMaskA3.ToString();
            SpellMask12.Text = loadedDBC.body.records[selectedID].record.EffectSpellClassMaskB1.ToString();
            SpellMask22.Text = loadedDBC.body.records[selectedID].record.EffectSpellClassMaskB2.ToString();
            SpellMask32.Text = loadedDBC.body.records[selectedID].record.EffectSpellClassMaskB3.ToString();
            SpellMask13.Text = loadedDBC.body.records[selectedID].record.EffectSpellClassMaskC1.ToString();
            SpellMask23.Text = loadedDBC.body.records[selectedID].record.EffectSpellClassMaskC2.ToString();
            SpellMask33.Text = loadedDBC.body.records[selectedID].record.EffectSpellClassMaskC3.ToString();
            MiscValueA1.Text = loadedDBC.body.records[selectedID].record.EffectMiscValue1.ToString();
            MiscValueA2.Text = loadedDBC.body.records[selectedID].record.EffectMiscValue2.ToString();
            MiscValueA3.Text = loadedDBC.body.records[selectedID].record.EffectMiscValue3.ToString();
            MiscValueB1.Text = loadedDBC.body.records[selectedID].record.EffectMiscValueB1.ToString();
            MiscValueB2.Text = loadedDBC.body.records[selectedID].record.EffectMiscValueB2.ToString();
            MiscValueB3.Text = loadedDBC.body.records[selectedID].record.EffectMiscValueB3.ToString();

            ApplyAuraName1.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectApplyAuraName1;
            ApplyAuraName2.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectApplyAuraName2;
            ApplyAuraName3.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectApplyAuraName3;
            SpellEffect1.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.Effect1;
            SpellEffect2.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.Effect2;
            SpellEffect3.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.Effect3;
            Mechanic1.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectMechanic1;
            Mechanic2.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectMechanic2;
            Mechanic3.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectMechanic3;
            TargetA1.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectImplicitTargetA1;
            TargetB1.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectImplicitTargetB1;
            TargetA2.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectImplicitTargetA2;
            TargetB2.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectImplicitTargetB2;
            TargetA3.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectImplicitTargetA3;
            TargetB3.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectImplicitTargetB3;
            ChainTarget1.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectChainTarget1;
            ChainTarget2.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectChainTarget2;
            ChainTarget3.SelectedIndex = (Int32)loadedDBC.body.records[selectedID].record.EffectChainTarget3;

            loadedDispelDBC.UpdateDispelSelection();
            loadedMechanic.updateMechanicSelection();
            loadedCastTime.updateCastTimeSelection();
            loadedDuration.updateDurationIndexes();
            loadedRange.updateSpellRangeSelection();
            loadedRadius.updateRadiusIndexes();
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
                if (selectedID >= loadedDBC.body.records.Length || loadedDBC.body.records[selectedID].spellName == null
                    || loadedDBC.body.records[selectedID].spellName.Length == 0 || loadedDBC.body.records[selectedID].spellName[0] == null)
                {
                    await this.ShowMessageAsync("ERROR", "Something went wrong trying to select this spell.");
                    populateSelectSpell();
                }
                else
                {
                    updateMainWindow();
                    MainTabControl.SelectedIndex = 1;
                }
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
                if (interrupt1[0].IsChecked.Value)
                    loadedDBC.body.records[selectedID].record.InterruptFlags = 0;
                else
                {
                    UInt32 mask = 0;
                    UInt32 flag = 1;
                    for (int f = 1; f < interrupt1.Count; ++f)
                    {
                        if (interrupt1[f].IsChecked.Value)
                            mask = mask + flag;
                        flag = flag + flag;
                    }
                    loadedDBC.body.records[selectedID].record.InterruptFlags = mask;
                }
                if (interrupt2[0].IsChecked.Value)
                    loadedDBC.body.records[selectedID].record.AuraInterruptFlags = 0;
                else
                {
                    UInt32 mask = 0;
                    UInt32 flag = 1;
                    for (int f = 1; f < interrupt2.Count; ++f)
                    {
                        if (interrupt2[f].IsChecked.Value)
                            mask = mask + flag;
                        flag = flag + flag;
                    }
                    loadedDBC.body.records[selectedID].record.AuraInterruptFlags = mask;
                }
                if (interrupt3[0].IsChecked.Value)
                    loadedDBC.body.records[selectedID].record.ChannelInterruptFlags = 0;
                else
                {
                    UInt32 mask = 0;
                    UInt32 flag = 1;
                    for (int f = 1; f < interrupt3.Count; ++f)
                    {
                        if (interrupt3[f].IsChecked.Value)
                            mask = mask + flag;
                        flag = flag + flag;
                    }
                    loadedDBC.body.records[selectedID].record.ChannelInterruptFlags = mask;
                }

                loadedDBC.body.records[selectedID].record.procChance = UInt32.Parse(ProcChance.Text);
                loadedDBC.body.records[selectedID].record.procCharges = UInt32.Parse(ProcCharges.Text);

                loadedDBC.body.records[selectedID].record.EffectDieSides1 = Int32.Parse(DieSides1.Text);
                loadedDBC.body.records[selectedID].record.EffectDieSides2 = Int32.Parse(DieSides2.Text);
                loadedDBC.body.records[selectedID].record.EffectDieSides3 = Int32.Parse(DieSides3.Text);
                loadedDBC.body.records[selectedID].record.EffectRealPointsPerLevel1 = float.Parse(BasePointsPerLevel1.Text);
                loadedDBC.body.records[selectedID].record.EffectRealPointsPerLevel2 = float.Parse(BasePointsPerLevel2.Text);
                loadedDBC.body.records[selectedID].record.EffectRealPointsPerLevel3 = float.Parse(BasePointsPerLevel3.Text);
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
                loadedDBC.body.records[selectedID].record.EffectSpellClassMaskA1 = UInt32.Parse(SpellMask11.Text);
                loadedDBC.body.records[selectedID].record.EffectSpellClassMaskA2 = UInt32.Parse(SpellMask12.Text);
                loadedDBC.body.records[selectedID].record.EffectSpellClassMaskA3 = UInt32.Parse(SpellMask13.Text);
                loadedDBC.body.records[selectedID].record.EffectSpellClassMaskB1 = UInt32.Parse(SpellMask21.Text);
                loadedDBC.body.records[selectedID].record.EffectSpellClassMaskB2 = UInt32.Parse(SpellMask22.Text);
                loadedDBC.body.records[selectedID].record.EffectSpellClassMaskB3 = UInt32.Parse(SpellMask23.Text);
                loadedDBC.body.records[selectedID].record.EffectSpellClassMaskC1 = UInt32.Parse(SpellMask31.Text);
                loadedDBC.body.records[selectedID].record.EffectSpellClassMaskC2 = UInt32.Parse(SpellMask32.Text);
                loadedDBC.body.records[selectedID].record.EffectSpellClassMaskC3 = UInt32.Parse(SpellMask33.Text);
                loadedDBC.body.records[selectedID].record.EffectMiscValue1 = Int32.Parse(MiscValueA1.Text);
                loadedDBC.body.records[selectedID].record.EffectMiscValue2 = Int32.Parse(MiscValueA2.Text);
                loadedDBC.body.records[selectedID].record.EffectMiscValue3 = Int32.Parse(MiscValueA3.Text);
                loadedDBC.body.records[selectedID].record.EffectMiscValueB1 = Int32.Parse(MiscValueB1.Text);
                loadedDBC.body.records[selectedID].record.EffectMiscValueB2 = Int32.Parse(MiscValueB2.Text);
                loadedDBC.body.records[selectedID].record.EffectMiscValueB3 = Int32.Parse(MiscValueB3.Text);

                loadedDBC.body.records[selectedID].record.EffectApplyAuraName1 = (UInt32)ApplyAuraName1.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectApplyAuraName2 = (UInt32)ApplyAuraName2.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectApplyAuraName3 = (UInt32)ApplyAuraName3.SelectedIndex;
                loadedDBC.body.records[selectedID].record.Effect1 = (UInt32)SpellEffect1.SelectedIndex;
                loadedDBC.body.records[selectedID].record.Effect2 = (UInt32)SpellEffect2.SelectedIndex;
                loadedDBC.body.records[selectedID].record.Effect3 = (UInt32)SpellEffect3.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectMechanic1 = (UInt32)Mechanic1.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectMechanic2 = (UInt32)Mechanic2.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectMechanic3 = (UInt32)Mechanic3.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectImplicitTargetA1 = (UInt32)TargetA1.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectImplicitTargetB1 = (UInt32)TargetB1.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectImplicitTargetA2 = (UInt32)TargetA2.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectImplicitTargetB2 = (UInt32)TargetB2.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectImplicitTargetA3 = (UInt32)TargetA3.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectImplicitTargetB3 = (UInt32)TargetB3.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectChainTarget1 = (UInt32)ChainTarget1.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectChainTarget2 = (UInt32)ChainTarget2.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectChainTarget3 = (UInt32)ChainTarget3.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectRadiusIndex1 = (UInt32)RadiusIndex1.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectRadiusIndex2 = (UInt32)RadiusIndex2.SelectedIndex;
                loadedDBC.body.records[selectedID].record.EffectRadiusIndex3 = (UInt32)RadiusIndex3.SelectedIndex;
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
