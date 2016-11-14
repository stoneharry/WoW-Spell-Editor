using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using SpellEditor.Sources.Constants;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.Controls;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Threading;
using SpellEditor.Sources.Config;
using SpellEditor.Sources.MySQL;
using System.Data;
using MySql.Data.MySqlClient;
using System.Windows.Media.Animation;
using System.ComponentModel;

// Public use of a DBC Header file
public struct DBC_Header
{
    public UInt32 Magic;
    public UInt32 RecordCount;
    public UInt32 FieldCount;
    public UInt32 RecordSize;
    public Int32 StringBlockSize;
};

public class VirtualStrTableEntry
{
    public string Value;
    public UInt32 NewValue;
};

enum LocaleConstant
{
	LOCALE_enUS = 0,
	LOCALE_koKR = 1,
	LOCALE_frFR = 2,
	LOCALE_deDE = 3,
	LOCALE_zhCN = 4,
	LOCALE_zhTW = 5,
	LOCALE_esES = 6,
	LOCALE_esMX = 7,
	LOCALE_ruRU = 8
};


namespace SpellEditor
{
    partial class MainWindow
    {
		//todo:multilingual.Temporary define zhCN
		private LocaleConstant Locale_language = LocaleConstant.LOCALE_zhCN;

		private AreaTable loadAreaTable = null;
        // Begin DBCs
        private SpellCategory loadCategories = null;
        private SpellDispelType loadDispels = null;
        private SpellMechanic loadMechanics = null;
        private SpellFocusObject loadFocusObjects = null;
        private AreaGroup loadAreaGroups = null;
        private SpellCastTimes loadCastTimes = null;
        private SpellDuration loadDurations = null;
        private SpellDifficulty loadDifficulties = null;
        private SpellIconDBC loadIcons = null;
        private SpellRange loadRanges = null;
        private SpellRadius loadRadiuses = null;
        private ItemClass loadItemClasses = null;
		private ItemSubClass loadItemSubClasses = null;
        private TotemCategory loadTotemCategories = null;
        private SpellRuneCost loadRuneCosts = null;
        private SpellDescriptionVariables loadDescriptionVariables = null;
        // End DBCs

        // Begin Boxes
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
        // End Boxes

        // Begin Other
        private MySQL mySQL;
        private Config config;
        public UInt32 selectedID = 0;
        public UInt32 newIconID = 1;
        private Boolean updating;
        public TaskScheduler UIScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        private DataTable spellTable = new DataTable();
        private int storedLocale = -1;
        // End Other

        public MainWindow() { InitializeComponent(); }

        public async void HandleErrorMessage(string msg) { await this.ShowMessageAsync("Spell Editor", msg); }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            File.WriteAllText("Error.txt", e.Exception.Message, UTF8Encoding.GetEncoding(0));
            HandleErrorMessage(e.Exception.Message);
            e.Handled = true;
        }

		public int GetLanguage(){return (int)Locale_language;}

		public String GetAreaTableName(UInt32 id) {return loadAreaTable.body.lookup.ContainsKey(id) ? loadAreaTable.body.lookup[id].AreaName : ""; }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);

            try
            {
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

                string[] att_flags = { "Unknown 0", "On Next Ranged", "On Next Swing (Player)", "Is Replenishment", "Ability", "Trade Spell", "Passive Spell", "Hidden Client-Side", "Hide in Combat Log", "Target Main-Hand Item", "On Next Swing (NPCs)", "Unknown 11", "Daytime Only", "Night Only", "Indoors Only", "Outdoors Only", "No Shapeshift", "Requires Stealth", "Don't Affect Sheath State", "Spell Damage depends on Caster Level", "Stops Auto-Attack", "Impossible to Dodge, Parry or Block", "Track Target while Casting", "Castable While Dead", "Castable While Mounted", "Start Cooldown after Aura Fades", "Negative", "Castable While Sitting", "Cannot be used in Combat", "Unaffected by Invulnerability", "Breakable by Damage", "Aura Cannot be Cancelled" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = att_flags[i];
					box.Margin = new Thickness(0, 5, 0, 0);

                    Attributes1.Children.Add(box);
                    attributes0.Add(box);
                }

                att_flags = new string[] { "Dismiss Pet", "Drains All Power", "Channeled 1", "Cannot be Redirected", "Unknown 4", "Does not Break Stealth", "Channeled 2", "Cannot be Reflected", "Cannot Target in Combat", "Melee Combat Start", "Generates No Threat", "Unknown 11", "Pickpocket", "Far Sight", "Track Target while Channeling", "Remove Auras on Immunity", "Unaffected by School Immune", "Unautoscalable by Pet", "Stun, Polymorph, Daze, Hex", "Cannot Target Self", "Requires Combo Points on Target 1", "Unknown 21", "Required Combo Points on Target 2", "Unknown 23", "Fishing", "Unknown 25", "Focus Targeting Macro", "Unknown 27", "Hidden in Aura Bar", "Channel Display Name", "Enable Spell when Dodged", "Unknown 31" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = att_flags[i];
					box.Margin = new Thickness(0, 5, 0, 0);

                    Attributes2.Children.Add(box);
                    attributes1.Add(box);
                }

                att_flags = new string[] { "Can Target Dead Unit or Corpse", "Vanish, Shadowform, Ghost", "Can Target not in Line of Sight", "Unknown 3", "Display in Stance Bar", "Autorepeat Flag", "Requires Untapped Target", "Unknown 7", "Unknown 8", "Unknown 9", "Unknown 10 (Tame)", "Health Funnel", "Cleave, Heart Strike, Maul, Sunder Armor, Swipe", "Preserve Enchant in Arena", "Unknown 14", "Unknown 15", "Tame Beast", "Don't Reset Auto Actions", "Requires Dead Pet", "Don't Need Shapeshift", "Unknown 20", "Damage Reduced Shield", "Ambush, Backstab, Cheap Shot, Death Grip, Garrote, Judgements, Mutilate, Pounce, Ravage, Shiv, Shred", "Arcane Concentration", "Unknown 24", "Unknown 25", "Unaffected by School Immunity", "Requires Fishing Pole", "Unknown 28", "Cannot Crit", "Triggered can Trigger Proc", "Food Buff" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = att_flags[i];
					box.Margin = new Thickness(0, 5, 0, 0);

                    Attributes3.Children.Add(box);
                    attributes2.Add(box);
                }

                att_flags = new string[] { "Unknown 0", "Unknown 1", "Unknown 2", "Blockable Spell", "Ignore Resurrection Timer", "Unknown 5", "Unknown 6", "Stack for Different Casters", "Only Target Players", "Triggered can Trigger Proc 2", "Requires Main-Hand", "Battleground Only", "Only Target Ghosts", "Hide Channel Bar", "Honorless Target", "Auto-Shoot", "Cannot Trigger Proc", "No Initial Aggro", "Cannot Miss", "Disable Procs", "Death Persistent", "Unknown 21", "Requires Wands", "Unknown 23", "Requires Off-Hand", "Can Proc with Triggered", "Drain Soul", "Unknown 28", "No Done Bonus", "Do not Display Range", "Unknown 31" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = att_flags[i];
					box.Margin = new Thickness(0, 5, 0, 0);

                    Attributes4.Children.Add(box);
                    attributes3.Add(box);
                }

                att_flags = new string[] { "Ignore All Resistances", "Proc Only on Caster", "Continue to Tick while Offline", "Unknown 3", "Unknown 4", "Unknown 5", "Not Stealable", "Triggered", "Fixed Damage", "Activate from Event", "Spell vs Extended Cost", "Unknown 11", "Unknown 12", "Unknown 13", "Damage doesn't Break Auras", "Unknown 15", "Not Usable in Arena", "Usable in Arena", "Area Target Chain", "Unknown 19", "Don't Check Selfcast Power", "Unknown 21", "Unknown 22", "Unknown 23", "Unknown 24", "Pet Scaling", "Can Only be Casted in Outland", "Unknown 27", "Aimed Shot", "Unknown 29", "Unknown 30", "Polymorph" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = att_flags[i];
					box.Margin = new Thickness(0, 5, 0, 0);

                    Attributes5.Children.Add(box);
                    attributes4.Add(box);
                }

                att_flags = new string[] { "Unknown 0", "No Reagent While Preparation", "Unknown 2", "Usable while Stunned", "Unknown 4", "Single-Target Spell", "Unknown 6", "Unknown 7", "Unknown 8", "Start Periodic at Aura Apply", "Hide Duration", "Allow Target of Target as Target", "Cleave", "Haste Affect Duration", "Unknown 14", "Inflict on Multiple Targets", "Special Item Class Check", "Usable while Feared", "Usable feared Confused", "Don't Turn during Casting", "Unknown 20", "Unknown 21", "Unknown 22", "Unknown 23", "Unknown 24", "Unknown 25", "Unknown 26", "Don't Show Aura if Self-Cast", "Don't Show Aura if Not Self-Cast", "Unknown 29", "Unknown 30", "AoE Taunt" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = att_flags[i];
					box.Margin = new Thickness(0, 5, 0, 0);

                    Attributes6.Children.Add(box);
                    attributes5.Add(box);
                }

                att_flags = new string[] { "Don't Display Cooldown", "Only in Arena", "Ignore Caster Auras", "Assist Ignore Immune Flag", "Unknown 4", "Unknown 5", "Spell Cast Event", "Unknown 7", "Can't Target Crowd-Controlled", "Unknown 9", "Can Target Possessed Friends", "Not in Raid Instance", "Castable while on Vehicle", "Can Target Invisible", "Unknown 14", "Unknown 15", "Unknown 16", "Mount", "Cast by Charmer", "Unknown 19", "Only Visible to Caster", "Client UI Target Effects", "Unknown 22", "Unknown 23", "Can Target Untargetable", "Exorcism, Flash of Light", "Unknown 26", "Unknown 27", "Death Grip", "Not Done Percent Damage Mods", "Unknown 30", "Ignore Category Cooldown Mods" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = att_flags[i];
					box.Margin = new Thickness(0, 5, 0, 0);

                    Attributes7.Children.Add(box);
                    attributes6.Add(box);
                }

                att_flags = new string[] { "Feign Death", "Unknown 1", "Re-Activate at Resurrect", "Cheat Spell", "Soulstone Resurrection", "Totem", "No Pushback on Damage", "Unknown 7", "Horde Only", "Alliance Only", "Dispel Charges", "Interrupt only Non-Player", "Unknown 12", "Unknown 13", "Raise Dead", "Unknown 15", "Restore Secondary Power", "Unknown 17", "Charge", "Zone Teleport", "Blink, Divine Shield, Ice Block", "Unknown 21", "Unknown 22", "Unknown 23", "Unknown 24", "Unknown 25", "Unknown 26", "Unknown 27", "Consolidated Raid Buff", "Unknown 29", "Unknown 30", "Client Indicator" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = att_flags[i];
					box.Margin = new Thickness(0, 5, 0, 0);

                    Attributes8.Children.Add(box);
                    attributes7.Add(box);
                }

                string[] stances_strings = { "None", "Cat", "Tree", "Travel", "Aqua", "Bear", "Ambient", "Ghoul", "Dire Bear", "Steves Ghoul", "Tharonja Skeleton", "Test of Strength", "BLB Player", "Shadow Dance", "Creature Bear", "Creature Cat", "Ghost Wolf", "Battle Stance", "Defensive Stance", "Berserker Stance", "Test", "Zombie", "Metamorphosis", "Undead", "Master Angler", "Flight (Epic)", "Shadow", "Flight (Normal)", "Stealth", "Moonkin", "Spirit of Redemption" };

                for (int i = 0; i < stances_strings.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = stances_strings[i];
					box.Margin = new Thickness(0, 5, 0, 0);

                    StancesGrid.Children.Add(box);
                    stancesBoxes.Add(box);
                }

                string[] creature_type_strings = { "None", "Beast", "Dragonkin", "Demon", "Elemental", "Giant", "Undead", "Humanoid", "Critter", "Mechanical", "Not specified", "Totem", "Non-combat Pet", "Gas Cloud" };

                for (int i = 0; i < creature_type_strings.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = creature_type_strings[i];
					box.Margin = new Thickness(0, 5, 0, 0);

                    TargetCreatureType.Children.Add(box);
                    targetCreatureTypeBoxes.Add(box);
                }

                string[] caster_aura_state_strings = { "None", "Defense", "Healthless 20%", "Berserking", "Judgement", "Hunter Parry", "Victory Rush", "Unknown 1", "Healthless 35%", "Enrage", "Unknown 2", "Health Above 75%" };

                for (int i = 0; i < caster_aura_state_strings.Length; ++i) { CasterAuraState.Items.Add(caster_aura_state_strings[i]); }

                string[] target_aura_state_strings = { "None", "Healthless 20%", "Berserking", "Healthless 35%", "Conflagrate", "Swiftmend", "Deadly Poison", "Bleeding" };

                for (int i = 0; i < target_aura_state_strings.Length; ++i) { TargetAuraState.Items.Add(target_aura_state_strings[i]); }

                string[] equipped_item_inventory_type_mask_strings = { "Non-Equip", "Head", "Necklace", "Shoulders", "Body", "Chest", "Waist", "Legs", "Feet", "Wrists", "Hands", "Finger", "Trinket", "Weapon", "Shield", "Ranged", "Cloak", "Two-Handed Weapon", "Bag", "Tabard", "Robe", "Main-Hand", "Off-Hand", "Holdable", "Ammo", "Thrown", "Ranged Right", "Quiver", "Relic" };

                for (int i = 0; i < equipped_item_inventory_type_mask_strings.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = equipped_item_inventory_type_mask_strings[i];
					box.Margin = new Thickness(0, 5, 0, 0);

                    EquippedItemInventoryTypeGrid.Children.Add(box);
                    equippedItemInventoryTypeMaskBoxes.Add(box);
                }
				
				for (int i = 0; i < 29; ++i)
				{
					ThreadSafeCheckBox box = new ThreadSafeCheckBox();

					box.Content = "None";
					box.Margin = new Thickness(0, 5, 0, 0);
					box.Visibility = System.Windows.Visibility.Hidden;
					EquippedItemSubClassGrid.Children.Add(box);
					equippedItemSubClassMaskBoxes.Add(box);
				}


                string[] school_strings = { "Mana", "Rage", "Focus", "Energy", "Happiness", "Runes", "Runic Power", "Steam", "Pyrite", "Heat", "Ooze", "Blood", "Wrath", "Health" };

                for (int i = 0; i < school_strings.Length; ++i) { PowerType.Items.Add(school_strings[i]); }

                string[] effect_strings = { "0 NONE", "1 INSTAKILL", "2 SCHOOL_DAMAGE", "3 DUMMY", "4 PORTAL_TELEPORT unused", "5 TELEPORT_UNITS", "6 APPLY_AURA", "7 ENVIRONMENTAL_DAMAGE", "8 POWER_DRAIN", "9 HEALTH_LEECH", "10 HEAL", "11 BIND", "12 PORTAL", "13 RITUAL_BASE unused", "14 RITUAL_SPECIALIZE unused", "15 RITUAL_ACTIVATE_PORTAL unused", "16 QUEST_COMPLETE", "17 WEAPON_DAMAGE_NOSCHOOL", "18 RESURRECT", "19 ADD_EXTRA_ATTACKS", "20 DODGE one spell: Dodge", "21 EVADE one spell: Evade (DND)", "22 PARRY", "23 BLOCK one spell: Block", "24 CREATE_ITEM", "25 WEAPON", "26 DEFENSE one spell: Defense", "27 PERSISTENT_AREA_AURA", "28 SUMMON", "29 LEAP", "30 ENERGIZE", "31 WEAPON_PERCENT_DAMAGE", "32 TRIGGER_MISSILE", "33 OPEN_LOCK", "34 SUMMON_CHANGE_ITEM", "35 APPLY_AREA_AURA_PARTY", "36 LEARN_SPELL", "37 SPELL_DEFENSE one spell: SPELLDEFENSE (DND)", "38 DISPEL", "39 LANGUAGE", "40 DUAL_WIELD", "41 JUMP", "42 JUMP_DEST", "43 TELEPORT_UNITS_FACE_CASTER", "44 SKILL_STEP", "45 ADD_HONOR honor/pvp related", "46 SPAWN clientside, unit appears as if it was just spawned", "47 TRADE_SKILL", "48 STEALTH one spell: Base Stealth", "49 DETECT one spell: Detect", "50 TRANS_DOOR", "51 FORCE_CRITICAL_HIT unused", "52 GUARANTEE_HIT one spell: zzOLDCritical Shot", "53 ENCHANT_ITEM", "54 ENCHANT_ITEM_TEMPORARY", "55 TAMECREATURE", "56 SUMMON_PET", "57 LEARN_PET_SPELL", "58 WEAPON_DAMAGE", "59 CREATE_RANDOM_ITEM create item base at spell specific loot", "60 PROFICIENCY", "61 SEND_EVENT", "62 POWER_BURN", "63 THREAT", "64 TRIGGER_SPELL", "65 APPLY_AREA_AURA_RAID", "66 CREATE_MANA_GEM (possibly recharge it, misc - is item ID)", "67 HEAL_MAX_HEALTH", "68 INTERRUPT_CAST", "69 DISTRACT", "70 PULL one spell: Distract Move", "71 PICKPOCKET", "72 ADD_FARSIGHT", "73 UNTRAIN_TALENTS", "74 APPLY_GLYPH", "75 HEAL_MECHANICAL one spell: Mechanical Patch Kit", "76 SUMMON_OBJECT_WILD", "77 SCRIPT_EFFECT", "78 ATTACK", "79 SANCTUARY", "80 ADD_COMBO_POINTS", "81 CREATE_HOUSE one spell: Create House (TEST)", "82 BIND_SIGHT", "83 DUEL", "84 STUCK", "85 SUMMON_PLAYER", "86 ACTIVATE_OBJECT", "87 GAMEOBJECT_DAMAGE", "88 GAMEOBJECT_REPAIR", "89 GAMEOBJECT_SET_DESTRUCTION_STATE", "90 KILL_CREDIT Kill credit but only for single person", "91 THREAT_ALL one spell: zzOLDBrainwash", "92 ENCHANT_HELD_ITEM", "93 FORCE_DESELECT", "94 SELF_RESURRECT", "95 SKINNING", "96 CHARGE", "97 CAST_BUTTON (totem bar since 3.2.2a)", "98 KNOCK_BACK", "99 DISENCHANT", "100 INEBRIATE", "101 FEED_PET", "102 DISMISS_PET", "103 REPUTATION", "104 SUMMON_OBJECT_SLOT1", "105 SUMMON_OBJECT_SLOT2", "106 SUMMON_OBJECT_SLOT3", "107 SUMMON_OBJECT_SLOT4", "108 DISPEL_MECHANIC", "109 SUMMON_DEAD_PET", "110 DESTROY_ALL_TOTEMS", "111 DURABILITY_DAMAGE", "112 112", "113 RESURRECT_NEW", "114 ATTACK_ME", "115 DURABILITY_DAMAGE_PCT", "116 SKIN_PLAYER_CORPSE one spell: Remove Insignia, bg usage, required special corpse flags...", "117 SPIRIT_HEAL one spell: Spirit Heal", "118 SKILL professions and more", "119 APPLY_AREA_AURA_PET", "120 TELEPORT_GRAVEYARD one spell: Graveyard Teleport Test", "121 NORMALIZED_WEAPON_DMG", "122 122 unused", "123 SEND_TAXI taxi/flight related (misc value is taxi path id)", "124 PULL_TOWARDS", "125 MODIFY_THREAT_PERCENT", "126 STEAL_BENEFICIAL_BUFF spell steal effect?", "127 PROSPECTING Prospecting spell", "128 APPLY_AREA_AURA_FRIEND", "129 APPLY_AREA_AURA_ENEMY", "130 REDIRECT_THREAT", "131 PLAYER_NOTIFICATION sound id in misc value (SoundEntries.dbc)", "132 PLAY_MUSIC sound id in misc value (SoundEntries.dbc)", "133 UNLEARN_SPECIALIZATION unlearn profession specialization", "134 KILL_CREDIT misc value is creature entry", "135 CALL_PET", "136 HEAL_PCT", "137 ENERGIZE_PCT", "138 LEAP_BACK Leap back", "139 CLEAR_QUEST Reset quest status (miscValue - quest ID)", "140 FORCE_CAST", "141 FORCE_CAST_WITH_VALUE", "142 TRIGGER_SPELL_WITH_VALUE", "143 APPLY_AREA_AURA_OWNER", "144 KNOCK_BACK_DEST", "145 PULL_TOWARDS_DEST Black Hole Effect", "146 ACTIVATE_RUNE", "147 QUEST_FAIL quest fail", "148 TRIGGER_MISSILE_SPELL_WITH_VALUE", "149 CHARGE_DEST", "150 QUEST_START", "151 TRIGGER_SPELL_2", "152 SUMMON_RAF_FRIEND summon Refer-a-Friend", "153 CREATE_TAMED_PET misc value is creature entry", "154 DISCOVER_TAXI", "155 TITAN_GRIP Allows you to equip two-handed axes, maces and swords in one hand, but you attack $49152s1% slower than normal.", "156 ENCHANT_ITEM_PRISMATIC", "157 CREATE_ITEM_2 create item or create item template and replace by some randon spell loot item", "158 MILLING milling", "159 ALLOW_RENAME_PET allow rename pet once again", "160 160 1 spell - 45534", "161 TALENT_SPEC_COUNT second talent spec (learn/revert)", "162 TALENT_SPEC_SELECT activate primary/secondary spec", "163 unused", "164 REMOVE_AURA" };

                for (int i = 0; i < effect_strings.Length; ++i)
                {
                    Effect1.Items.Add(effect_strings[i]);
                    Effect2.Items.Add(effect_strings[i]);
                    Effect3.Items.Add(effect_strings[i]);
                }

                string[] damage_prevention_types = { "None", "Magic", "Melee", "Ranged", "None", "Silence", "Pacify" };

                for (int i = 0; i < damage_prevention_types.Length; ++i)
                {
                    if (i < 4) { SpellDamageType.Items.Add(damage_prevention_types[i]); }
                    else { PreventionType.Items.Add(damage_prevention_types[i]); }
                }

                string[] target_strings = { "None", "Unused 1", "Unit", "Unit in Raid", "Unit in Party", "Item Enchantment", "Blank AoE Source Location", "Target AoE Destination Location", "Enemy", "Ally", "Corpse of an Enemy", "Dead Unit", "Gameobject", "Trade Item", "String", "Gameobject Item", "Corpse of an Ally", "Mini Pet", "Glyph", "Destination Target", "Unused 20", "Passenger" };

                for (int i = 0; i < target_strings.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = target_strings[i];
					box.Margin = new Thickness(0, 5, 0, 0);

                    TargetEditorGrid.Children.Add(box);
                    targetBoxes.Add(box);
                }

                string[] proc_strings = { "None", "Any Hostile Action", "On Gain Experience", "On Melee Attack", "On Crit Hit Victim", "On Cast Spell", "On Physical Attack Victim", "On Ranged Attack", "On Ranged Crit Attack", "On Physical Attack", "On Melee Attack Victim", "On Spell Hit", "On Ranged Crit Attack Victim", "On Crit Attack", "On Ranged Attack Victim", "On Pre Dispell Aura Victim", "On Spell Land Victim", "On Cast Specific Spell", "On Spell Hit Victim", "On Spell Crit Hit Victim", "On Target Death", "On Any Damage Victim", "On Trap Trigger", "On Auto Shot Hit", "On Absorb", "On Resist Victim", "On Dodge Victim", "On Death", "Remove On Use", "Misc", "On Block Victim", "On Spell Crit Hit" };

                for (int i = 0; i < proc_strings.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = proc_strings[i];
					box.Margin = new Thickness(0, 5, 0, 0);


                    ProcEditorGrid.Children.Add(box);
                    procBoxes.Add(box);
                }

                string[] spell_aura_effect_names = { "SPELL_AURA_NONE", "SPELL_AURA_BIND_SIGHT", "SPELL_AURA_MOD_POSSESS", "SPELL_AURA_PERIODIC_DAMAGE", "SPELL_AURA_DUMMY", "SPELL_AURA_MOD_CONFUSE", "SPELL_AURA_MOD_CHARM", "SPELL_AURA_MOD_FEAR", "SPELL_AURA_PERIODIC_HEAL", "SPELL_AURA_MOD_ATTACKSPEED", "SPELL_AURA_MOD_THREAT", "SPELL_AURA_MOD_TAUNT", "SPELL_AURA_MOD_STUN", "SPELL_AURA_MOD_DAMAGE_DONE", "SPELL_AURA_MOD_DAMAGE_TAKEN", "SPELL_AURA_DAMAGE_SHIELD", "SPELL_AURA_MOD_STEALTH", "SPELL_AURA_MOD_STEALTH_DETECT", "SPELL_AURA_MOD_INVISIBILITY", "SPELL_AURA_MOD_INVISIBILITY_DETECT", "SPELL_AURA_OBS_MOD_HEALTH", "SPELL_AURA_OBS_MOD_POWER", "SPELL_AURA_MOD_RESISTANCE", "SPELL_AURA_PERIODIC_TRIGGER_SPELL", "SPELL_AURA_PERIODIC_ENERGIZE", "SPELL_AURA_MOD_PACIFY", "SPELL_AURA_MOD_ROOT", "SPELL_AURA_MOD_SILENCE", "SPELL_AURA_REFLECT_SPELLS", "SPELL_AURA_MOD_STAT", "SPELL_AURA_MOD_SKILL", "SPELL_AURA_MOD_INCREASE_SPEED", "SPELL_AURA_MOD_INCREASE_MOUNTED_SPEED", "SPELL_AURA_MOD_DECREASE_SPEED", "SPELL_AURA_MOD_INCREASE_HEALTH", "SPELL_AURA_MOD_INCREASE_ENERGY", "SPELL_AURA_MOD_SHAPESHIFT", "SPELL_AURA_EFFECT_IMMUNITY", "SPELL_AURA_STATE_IMMUNITY", "SPELL_AURA_SCHOOL_IMMUNITY", "SPELL_AURA_DAMAGE_IMMUNITY", "SPELL_AURA_DISPEL_IMMUNITY", "SPELL_AURA_PROC_TRIGGER_SPELL", "SPELL_AURA_PROC_TRIGGER_DAMAGE", "SPELL_AURA_TRACK_CREATURES", "SPELL_AURA_TRACK_RESOURCES", "SPELL_AURA_46", "SPELL_AURA_MOD_PARRY_PERCENT", "SPELL_AURA_48", "SPELL_AURA_MOD_DODGE_PERCENT", "SPELL_AURA_MOD_CRITICAL_HEALING_AMOUNT", "SPELL_AURA_MOD_BLOCK_PERCENT", "SPELL_AURA_MOD_WEAPON_CRIT_PERCENT", "SPELL_AURA_PERIODIC_LEECH", "SPELL_AURA_MOD_HIT_CHANCE", "SPELL_AURA_MOD_SPELL_HIT_CHANCE", "SPELL_AURA_TRANSFORM", "SPELL_AURA_MOD_SPELL_CRIT_CHANCE", "SPELL_AURA_MOD_INCREASE_SWIM_SPEED", "SPELL_AURA_MOD_DAMAGE_DONE_CREATURE", "SPELL_AURA_MOD_PACIFY_SILENCE", "SPELL_AURA_MOD_SCALE", "SPELL_AURA_PERIODIC_HEALTH_FUNNEL", "SPELL_AURA_63", "SPELL_AURA_PERIODIC_MANA_LEECH", "SPELL_AURA_MOD_CASTING_SPEED_NOT_STACK", "SPELL_AURA_FEIGN_DEATH", "SPELL_AURA_MOD_DISARM", "SPELL_AURA_MOD_STALKED", "SPELL_AURA_SCHOOL_ABSORB", "SPELL_AURA_EXTRA_ATTACKS", "SPELL_AURA_MOD_SPELL_CRIT_CHANCE_SCHOOL", "SPELL_AURA_MOD_POWER_COST_SCHOOL_PCT", "SPELL_AURA_MOD_POWER_COST_SCHOOL", "SPELL_AURA_REFLECT_SPELLS_SCHOOL", "SPELL_AURA_MOD_LANGUAGE", "SPELL_AURA_FAR_SIGHT", "SPELL_AURA_MECHANIC_IMMUNITY", "SPELL_AURA_MOUNTED", "SPELL_AURA_MOD_DAMAGE_PERCENT_DONE", "SPELL_AURA_MOD_PERCENT_STAT", "SPELL_AURA_SPLIT_DAMAGE_PCT", "SPELL_AURA_WATER_BREATHING", "SPELL_AURA_MOD_BASE_RESISTANCE", "SPELL_AURA_MOD_REGEN", "SPELL_AURA_MOD_POWER_REGEN", "SPELL_AURA_CHANNEL_DEATH_ITEM", "SPELL_AURA_MOD_DAMAGE_PERCENT_TAKEN", "SPELL_AURA_MOD_HEALTH_REGEN_PERCENT", "SPELL_AURA_PERIODIC_DAMAGE_PERCENT", "SPELL_AURA_90", "SPELL_AURA_MOD_DETECT_RANGE", "SPELL_AURA_PREVENTS_FLEEING", "SPELL_AURA_MOD_UNATTACKABLE", "SPELL_AURA_INTERRUPT_REGEN", "SPELL_AURA_GHOST", "SPELL_AURA_SPELL_MAGNET", "SPELL_AURA_MANA_SHIELD", "SPELL_AURA_MOD_SKILL_TALENT", "SPELL_AURA_MOD_ATTACK_POWER", "SPELL_AURA_AURAS_VISIBLE", "SPELL_AURA_MOD_RESISTANCE_PCT", "SPELL_AURA_MOD_MELEE_ATTACK_POWER_VERSUS", "SPELL_AURA_MOD_TOTAL_THREAT", "SPELL_AURA_WATER_WALK", "SPELL_AURA_FEATHER_FALL", "SPELL_AURA_HOVER", "SPELL_AURA_ADD_FLAT_MODIFIER", "SPELL_AURA_ADD_PCT_MODIFIER", "SPELL_AURA_ADD_TARGET_TRIGGER", "SPELL_AURA_MOD_POWER_REGEN_PERCENT", "SPELL_AURA_ADD_CASTER_HIT_TRIGGER", "SPELL_AURA_OVERRIDE_CLASS_SCRIPTS", "SPELL_AURA_MOD_RANGED_DAMAGE_TAKEN", "SPELL_AURA_MOD_RANGED_DAMAGE_TAKEN_PCT", "SPELL_AURA_MOD_HEALING", "SPELL_AURA_MOD_REGEN_DURING_COMBAT", "SPELL_AURA_MOD_MECHANIC_RESISTANCE", "SPELL_AURA_MOD_HEALING_PCT", "SPELL_AURA_119", "SPELL_AURA_UNTRACKABLE", "SPELL_AURA_EMPATHY", "SPELL_AURA_MOD_OFFHAND_DAMAGE_PCT", "SPELL_AURA_MOD_TARGET_RESISTANCE", "SPELL_AURA_MOD_RANGED_ATTACK_POWER", "SPELL_AURA_MOD_MELEE_DAMAGE_TAKEN", "SPELL_AURA_MOD_MELEE_DAMAGE_TAKEN_PCT", "SPELL_AURA_RANGED_ATTACK_POWER_ATTACKER_BONUS", "SPELL_AURA_MOD_POSSESS_PET", "SPELL_AURA_MOD_SPEED_ALWAYS", "SPELL_AURA_MOD_MOUNTED_SPEED_ALWAYS", "SPELL_AURA_MOD_RANGED_ATTACK_POWER_VERSUS", "SPELL_AURA_MOD_INCREASE_ENERGY_PERCENT", "SPELL_AURA_MOD_INCREASE_HEALTH_PERCENT", "SPELL_AURA_MOD_MANA_REGEN_INTERRUPT", "SPELL_AURA_MOD_HEALING_DONE", "SPELL_AURA_MOD_HEALING_DONE_PERCENT", "SPELL_AURA_MOD_TOTAL_STAT_PERCENTAGE", "SPELL_AURA_MOD_MELEE_HASTE", "SPELL_AURA_FORCE_REACTION", "SPELL_AURA_MOD_RANGED_HASTE", "SPELL_AURA_MOD_RANGED_AMMO_HASTE", "SPELL_AURA_MOD_BASE_RESISTANCE_PCT", "SPELL_AURA_MOD_RESISTANCE_EXCLUSIVE", "SPELL_AURA_SAFE_FALL", "SPELL_AURA_MOD_PET_TALENT_POINTS", "SPELL_AURA_ALLOW_TAME_PET_TYPE", "SPELL_AURA_MECHANIC_IMMUNITY_MASK", "SPELL_AURA_RETAIN_COMBO_POINTS", "SPELL_AURA_REDUCE_PUSHBACK", "SPELL_AURA_MOD_SHIELD_BLOCKVALUE_PCT", "SPELL_AURA_TRACK_STEALTHED", "SPELL_AURA_MOD_DETECTED_RANGE", "SPELL_AURA_SPLIT_DAMAGE_FLAT", "SPELL_AURA_MOD_STEALTH_LEVEL", "SPELL_AURA_MOD_WATER_BREATHING", "SPELL_AURA_MOD_REPUTATION_GAIN", "SPELL_AURA_PET_DAMAGE_MULTI", "SPELL_AURA_MOD_SHIELD_BLOCKVALUE", "SPELL_AURA_NO_PVP_CREDIT", "SPELL_AURA_MOD_AOE_AVOIDANCE", "SPELL_AURA_MOD_HEALTH_REGEN_IN_COMBAT", "SPELL_AURA_POWER_BURN", "SPELL_AURA_MOD_CRIT_DAMAGE_BONUS", "SPELL_AURA_164", "SPELL_AURA_MELEE_ATTACK_POWER_ATTACKER_BONUS", "SPELL_AURA_MOD_ATTACK_POWER_PCT", "SPELL_AURA_MOD_RANGED_ATTACK_POWER_PCT", "SPELL_AURA_MOD_DAMAGE_DONE_VERSUS", "SPELL_AURA_MOD_CRIT_PERCENT_VERSUS", "SPELL_AURA_DETECT_AMORE", "SPELL_AURA_MOD_SPEED_NOT_STACK", "SPELL_AURA_MOD_MOUNTED_SPEED_NOT_STACK", "SPELL_AURA_173", "SPELL_AURA_MOD_SPELL_DAMAGE_OF_STAT_PERCENT", "SPELL_AURA_MOD_SPELL_HEALING_OF_STAT_PERCENT", "SPELL_AURA_SPIRIT_OF_REDEMPTION", "SPELL_AURA_AOE_CHARM", "SPELL_AURA_MOD_DEBUFF_RESISTANCE", "SPELL_AURA_MOD_ATTACKER_SPELL_CRIT_CHANCE", "SPELL_AURA_MOD_FLAT_SPELL_DAMAGE_VERSUS", "SPELL_AURA_181", "SPELL_AURA_MOD_RESISTANCE_OF_STAT_PERCENT", "SPELL_AURA_MOD_CRITICAL_THREAT", "SPELL_AURA_MOD_ATTACKER_MELEE_HIT_CHANCE", "SPELL_AURA_MOD_ATTACKER_RANGED_HIT_CHANCE", "SPELL_AURA_MOD_ATTACKER_SPELL_HIT_CHANCE", "SPELL_AURA_MOD_ATTACKER_MELEE_CRIT_CHANCE", "SPELL_AURA_MOD_ATTACKER_RANGED_CRIT_CHANCE", "SPELL_AURA_MOD_RATING", "SPELL_AURA_MOD_FACTION_REPUTATION_GAIN", "SPELL_AURA_USE_NORMAL_MOVEMENT_SPEED", "SPELL_AURA_MOD_MELEE_RANGED_HASTE", "SPELL_AURA_MELEE_SLOW", "SPELL_AURA_MOD_TARGET_ABSORB_SCHOOL", "SPELL_AURA_MOD_TARGET_ABILITY_ABSORB_SCHOOL", "SPELL_AURA_MOD_COOLDOWN", "SPELL_AURA_MOD_ATTACKER_SPELL_AND_WEAPON_CRIT_CHANCE", "SPELL_AURA_198", "SPELL_AURA_MOD_INCREASES_SPELL_PCT_TO_HIT", "SPELL_AURA_MOD_XP_PCT", "SPELL_AURA_FLY", "SPELL_AURA_IGNORE_COMBAT_RESULT", "SPELL_AURA_MOD_ATTACKER_MELEE_CRIT_DAMAGE", "SPELL_AURA_MOD_ATTACKER_RANGED_CRIT_DAMAGE", "SPELL_AURA_MOD_SCHOOL_CRIT_DMG_TAKEN", "SPELL_AURA_MOD_INCREASE_VEHICLE_FLIGHT_SPEED", "SPELL_AURA_MOD_INCREASE_MOUNTED_FLIGHT_SPEED", "SPELL_AURA_MOD_INCREASE_FLIGHT_SPEED", "SPELL_AURA_MOD_MOUNTED_FLIGHT_SPEED_ALWAYS", "SPELL_AURA_MOD_VEHICLE_SPEED_ALWAYS", "SPELL_AURA_MOD_FLIGHT_SPEED_NOT_STACK", "SPELL_AURA_MOD_RANGED_ATTACK_POWER_OF_STAT_PERCENT", "SPELL_AURA_MOD_RAGE_FROM_DAMAGE_DEALT", "SPELL_AURA_214", "SPELL_AURA_ARENA_PREPARATION", "SPELL_AURA_HASTE_SPELLS", "SPELL_AURA_MOD_MELEE_HASTE_2", "SPELL_AURA_HASTE_RANGED", "SPELL_AURA_MOD_MANA_REGEN_FROM_STAT", "SPELL_AURA_MOD_RATING_FROM_STAT", "SPELL_AURA_MOD_DETAUNT", "SPELL_AURA_222", "SPELL_AURA_RAID_PROC_FROM_CHARGE", "SPELL_AURA_224", "SPELL_AURA_RAID_PROC_FROM_CHARGE_WITH_VALUE", "SPELL_AURA_PERIODIC_DUMMY", "SPELL_AURA_PERIODIC_TRIGGER_SPELL_WITH_VALUE", "SPELL_AURA_DETECT_STEALTH", "SPELL_AURA_MOD_AOE_DAMAGE_AVOIDANCE", "SPELL_AURA_230", "SPELL_AURA_PROC_TRIGGER_SPELL_WITH_VALUE", "SPELL_AURA_MECHANIC_DURATION_MOD", "SPELL_AURA_CHANGE_MODEL_FOR_ALL_HUMANOIDS", "SPELL_AURA_MECHANIC_DURATION_MOD_NOT_STACK", "SPELL_AURA_MOD_DISPEL_RESIST", "SPELL_AURA_CONTROL_VEHICLE", "SPELL_AURA_MOD_SPELL_DAMAGE_OF_ATTACK_POWER", "SPELL_AURA_MOD_SPELL_HEALING_OF_ATTACK_POWER", "SPELL_AURA_MOD_SCALE_2", "SPELL_AURA_MOD_EXPERTISE", "SPELL_AURA_FORCE_MOVE_FORWARD", "SPELL_AURA_MOD_SPELL_DAMAGE_FROM_HEALING", "SPELL_AURA_MOD_FACTION", "SPELL_AURA_COMPREHEND_LANGUAGE", "SPELL_AURA_MOD_AURA_DURATION_BY_DISPEL", "SPELL_AURA_MOD_AURA_DURATION_BY_DISPEL_NOT_STACK", "SPELL_AURA_CLONE_CASTER", "SPELL_AURA_MOD_COMBAT_RESULT_CHANCE", "SPELL_AURA_CONVERT_RUNE", "SPELL_AURA_MOD_INCREASE_HEALTH_2", "SPELL_AURA_MOD_ENEMY_DODGE", "SPELL_AURA_MOD_SPEED_SLOW_ALL", "SPELL_AURA_MOD_BLOCK_CRIT_CHANCE", "SPELL_AURA_MOD_DISARM_OFFHAND", "SPELL_AURA_MOD_MECHANIC_DAMAGE_TAKEN_PERCENT", "SPELL_AURA_NO_REAGENT_USE", "SPELL_AURA_MOD_TARGET_RESIST_BY_SPELL_CLASS", "SPELL_AURA_258", "SPELL_AURA_MOD_HOT_PCT", "SPELL_AURA_SCREEN_EFFECT", "SPELL_AURA_PHASE", "SPELL_AURA_ABILITY_IGNORE_AURASTATE", "SPELL_AURA_ALLOW_ONLY_ABILITY", "SPELL_AURA_264", "SPELL_AURA_265", "SPELL_AURA_266", "SPELL_AURA_MOD_IMMUNE_AURA_APPLY_SCHOOL", "SPELL_AURA_MOD_ATTACK_POWER_OF_STAT_PERCENT", "SPELL_AURA_MOD_IGNORE_TARGET_RESIST", "SPELL_AURA_MOD_ABILITY_IGNORE_TARGET_RESIST", "SPELL_AURA_MOD_DAMAGE_FROM_CASTER", "SPELL_AURA_IGNORE_MELEE_RESET", "SPELL_AURA_X_RAY", "SPELL_AURA_ABILITY_CONSUME_NO_AMMO", "SPELL_AURA_MOD_IGNORE_SHAPESHIFT", "SPELL_AURA_MOD_DAMAGE_DONE_FOR_MECHANIC", "SPELL_AURA_MOD_MAX_AFFECTED_TARGETS", "SPELL_AURA_MOD_DISARM_RANGED", "SPELL_AURA_INITIALIZE_IMAGES", "SPELL_AURA_MOD_ARMOR_PENETRATION_PCT", "SPELL_AURA_MOD_HONOR_GAIN_PCT", "SPELL_AURA_MOD_BASE_HEALTH_PCT", "SPELL_AURA_MOD_HEALING_RECEIVED", "SPELL_AURA_LINKED", "SPELL_AURA_MOD_ATTACK_POWER_OF_ARMOR", "SPELL_AURA_ABILITY_PERIODIC_CRIT", "SPELL_AURA_DEFLECT_SPELLS", "SPELL_AURA_IGNORE_HIT_DIRECTION", "SPELL_AURA_289", "SPELL_AURA_MOD_CRIT_PCT", "SPELL_AURA_MOD_XP_QUEST_PCT", "SPELL_AURA_OPEN_STABLE", "SPELL_AURA_OVERRIDE_SPELLS", "SPELL_AURA_PREVENT_REGENERATE_POWER", "SPELL_AURA_295", "SPELL_AURA_SET_VEHICLE_ID", "SPELL_AURA_BLOCK_SPELL_FAMILY", "SPELL_AURA_STRANGULATE", "SPELL_AURA_299", "SPELL_AURA_SHARE_DAMAGE_PCT", "SPELL_AURA_SCHOOL_HEAL_ABSORB", "SPELL_AURA_302", "SPELL_AURA_MOD_DAMAGE_DONE_VERSUS_AURASTATE", "SPELL_AURA_MOD_FAKE_INEBRIATE", "SPELL_AURA_MOD_MINIMUM_SPEED", "SPELL_AURA_306", "SPELL_AURA_HEAL_ABSORB_TEST", "SPELL_AURA_MOD_CRIT_CHANCE_FOR_CASTER", "SPELL_AURA_309", "SPELL_AURA_MOD_CREATURE_AOE_DAMAGE_AVOIDANCE", "SPELL_AURA_311", "SPELL_AURA_312", "SPELL_AURA_313", "SPELL_AURA_PREVENT_RESURRECTION", "SPELL_AURA_UNDERWATER_WALKING", "SPELL_AURA_PERIODIC_HASTE" };

                for (int i = 0; i < spell_aura_effect_names.Length; ++i)
                {
                    ApplyAuraName1.Items.Add(i + "- " + spell_aura_effect_names[i]);
                    ApplyAuraName2.Items.Add(i + "- " + spell_aura_effect_names[i]);
                    ApplyAuraName3.Items.Add(i + "- " + spell_aura_effect_names[i]);
                }

                string[] spell_effect_names = { "NULL", "INSTANT_KILL", "SCHOOL_DAMAGE", "DUMMY", "PORTAL_TELEPORT", "TELEPORT_UNITS", "APPLY_AURA", "ENVIRONMENTAL_DAMAGE", "POWER_DRAIN", "HEALTH_LEECH", "HEAL", "BIND", "PORTAL", "RITUAL_BASE", "RITUAL_SPECIALIZE", "RITUAL_ACTIVATE_PORTAL", "QUEST_COMPLETE", "WEAPON_DAMAGE_NOSCHOOL", "RESURRECT", "ADD_EXTRA_ATTACKS", "DODGE", "EVADE", "PARRY", "BLOCK", "CREATE_ITEM", "WEAPON", "DEFENSE", "PERSISTENT_AREA_AURA", "SUMMON", "LEAP", "ENERGIZE", "WEAPON_PERCENT_DAMAGE", "TRIGGER_MISSILE", "OPEN_LOCK", "TRANSFORM_ITEM", "APPLY_GROUP_AREA_AURA", "LEARN_SPELL", "SPELL_DEFENSE", "DISPEL", "LANGUAGE", "DUAL_WIELD", "LEAP_41", "SUMMON_GUARDIAN", "TELEPORT_UNITS_FACE_CASTER", "SKILL_STEP", "UNDEFINED_45", "SPAWN", "TRADE_SKILL", "STEALTH", "DETECT", "SUMMON_OBJECT", "FORCE_CRITICAL_HIT", "GUARANTEE_HIT", "ENCHANT_ITEM", "ENCHANT_ITEM_TEMPORARY", "TAMECREATURE", "SUMMON_PET", "LEARN_PET_SPELL", "WEAPON_DAMAGE", "OPEN_LOCK_ITEM", "PROFICIENCY", "SEND_EVENT", "POWER_BURN", "THREAT", "TRIGGER_SPELL", "APPLY_RAID_AREA_AURA", "POWER_FUNNEL", "HEAL_MAX_HEALTH", "INTERRUPT_CAST", "DISTRACT", "PULL", "PICKPOCKET", "ADD_FARSIGHT", "UNTRAIN_TALENTS", "USE_GLYPH", "HEAL_MECHANICAL", "SUMMON_OBJECT_WILD", "SCRIPT_EFFECT", "ATTACK", "SANCTUARY", "ADD_COMBO_POINTS", "CREATE_HOUSE", "BIND_SIGHT", "DUEL", "STUCK", "SUMMON_PLAYER", "ACTIVATE_OBJECT", "BUILDING_DAMAGE", "BUILDING_REPAIR", "BUILDING_SWITCH_STATE", "KILL_CREDIT_90", "THREAT_ALL", "ENCHANT_HELD_ITEM", "SUMMON_PHANTASM", "SELF_RESURRECT", "SKINNING", "CHARGE", "SUMMON_MULTIPLE_TOTEMS", "KNOCK_BACK", "DISENCHANT", "INEBRIATE", "FEED_PET", "DISMISS_PET", "REPUTATION", "SUMMON_OBJECT_SLOT1", "SUMMON_OBJECT_SLOT2", "SUMMON_OBJECT_SLOT3", "SUMMON_OBJECT_SLOT4", "DISPEL_MECHANIC", "SUMMON_DEAD_PET", "DESTROY_ALL_TOTEMS", "DURABILITY_DAMAGE", "NONE_112", "RESURRECT_FLAT", "ATTACK_ME", "DURABILITY_DAMAGE_PCT", "SKIN_PLAYER_CORPSE", "SPIRIT_HEAL", "SKILL", "APPLY_PET_AREA_AURA", "TELEPORT_GRAVEYARD", "DUMMYMELEE", "UNKNOWN1", "START_TAXI", "PLAYER_PULL", "UNKNOWN4", "UNKNOWN5", "PROSPECTING", "APPLY_FRIEND_AREA_AURA", "APPLY_ENEMY_AREA_AURA", "UNKNOWN10", "UNKNOWN11", "PLAY_MUSIC", "FORGET_SPECIALIZATION", "KILL_CREDIT", "UNKNOWN15", "UNKNOWN16", "UNKNOWN17", "UNKNOWN18", "CLEAR_QUEST", "UNKNOWN20", "UNKNOWN21", "TRIGGER_SPELL_WITH_VALUE", "APPLY_OWNER_AREA_AURA", "UNKNOWN23", "UNKNOWN24", "ACTIVATE_RUNES", "UNKNOWN26", "UNKNOWN27", "QUEST_FAIL", "UNKNOWN28", "UNKNOWN29", "UNKNOWN30", "SUMMON_TARGET", "SUMMON_REFER_A_FRIEND", "TAME_CREATURE", "ADD_SOCKET", "CREATE_ITEM2", "MILLING", "UNKNOWN37", "UNKNOWN38", "LEARN_SPEC", "ACTIVATE_SPEC", "UNKNOWN" };

                for (int i = 0; i < spell_effect_names.Length; ++i)
                {
                    SpellEffect1.Items.Add(i + "- " + spell_effect_names[i]);
                    SpellEffect2.Items.Add(i + "- " + spell_effect_names[i]);
                    SpellEffect3.Items.Add(i + "- " + spell_effect_names[i]);
                }

                string[] mechanic_names = { "None", "Charmed", "Disoriented", "Disarmed", "Distracted", "Fleeing", "Clumsy", "Rooted", "Pacified", "Silenced", "Asleep", "Ensnared", "Stunned", "Frozen", "Incapacipated", "Bleeding", "Healing", "Polymorphed", "Banished", "Shielded", "Shackled", "Mounted", "Seduced", "Turned", "Horrified", "Invulnarable", "Interrupted", "Dazed", "Discovery", "Invulnerable", "Sapped", "Enraged" };

                for (int i = 0; i < mechanic_names.Length; ++i)
                {
                    Mechanic1.Items.Add(mechanic_names[i]);
                    Mechanic2.Items.Add(mechanic_names[i]);
                    Mechanic3.Items.Add(mechanic_names[i]);
                }

                int number = 0;
                foreach (Targets t in Enum.GetValues(typeof(Targets)))
                {
                    String toDisplay = number + " - " + t;
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

                string[] interrupt_strings = { "None", "On Movement", "On Knockback", "On Interrupt Casting", "On Interrupt School", "On Damage Taken", "On Interrupt All" };

                for (int i = 0; i < interrupt_strings.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = interrupt_strings[i];
					box.Margin = new Thickness(0, 5, 0, 0);


                    InterruptFlagsGrid.Children.Add(box);
                    interrupts1.Add(box);
                }

                string[] aura_interrupt_strings = { "None", "On Hit By Spell", "On Take Damage", "On Casting", "On Moving", "On Turning", "On Jumping", "Not Mounted", "Not Above Water", "Not Underwater", "Not Sheathed", "On Talk", "On Use", "On Melee Attack", "On Spell Attack", "Unknown 14", "On Transform", "Unknown 16", "On Mount", "Not Seated", "On Change Map", "Immune or Lost Selection", "Unknown 21", "On Teleport", "On Enter PvP Combat", "On Direct Damage", "Landing" };

                for (int i = 0; i < aura_interrupt_strings.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = aura_interrupt_strings[i];
					box.Margin = new Thickness(0, 5, 0, 0);
					
                    AuraInterruptFlagsGrid.Children.Add(box);
                    interrupts3.Add(box);
                }

                string[] channel_interrupt_strings = { "None", "On 1", "On 2", "On 3", "On 4", "On 5", "On 6", "On 7", "On 8", "On 9", "On 10", "On 11", "On 12", "On 13", "On 14", "On 15", "On 16", "On 17", "On 18" };

                for (int i = 0; i < channel_interrupt_strings.Length; ++i)
                {
                    ThreadSafeCheckBox box = new ThreadSafeCheckBox();

                    box.Content = channel_interrupt_strings[i];
					box.Margin = new Thickness(0, 5, 0, 0);


                    ChannelInterruptFlagsGrid.Children.Add(box);
                    interrupts2.Add(box);
                }

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

                loadAllData();
            }

            catch (Exception ex) { HandleErrorMessage(ex.Message); }
        }

        public delegate void UpdateProgressFunc(double value);
        public delegate void UpdateTextFunc(string value);

        private async void ImportSpellDbcButton(object sender, RoutedEventArgs e)
        {
            MetroDialogSettings settings = new MetroDialogSettings();
            settings.AffirmativeButtonText = "YES";
            settings.NegativeButtonText = "NO";
            MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
            var res = await this.ShowMessageAsync("Import Spell.dbc?",
                "It appears the table in the database is empty. Would you like to import a Spell.dbc now?", style, settings);
            if (res == MessageDialogResult.Affirmative)
            {
                var controller = await this.ShowProgressAsync("Please wait...", "Importing the Spell.dbc. Cancelling this task will corrupt the table.");
                await Task.Delay(1000);
                controller.SetCancelable(false);

                SpellDBC dbc = new SpellDBC();
                dbc.LoadDBCFile(this);
                await dbc.import(mySQL, new UpdateProgressFunc(controller.SetProgress));
                await controller.CloseAsync();
                PopulateSelectSpell();

                if (SpellDBC.ErrorMessage.Length > 0)
                {
                    HandleErrorMessage(SpellDBC.ErrorMessage);
                    SpellDBC.ErrorMessage = "";
                }
            }
        }

        private async void loadAllData()
        {
            config = await getConfig();
            if (config == null)
                return;
            String errorMsg = "";
            try
            {
                mySQL = new MySQL(config);
            }
            catch (Exception e)
            {
                errorMsg = e.Message;
            }
            if (errorMsg.Length > 0)
            {
                await this.ShowMessageAsync("ERROR", "An error occured setting up the MySQL connection:\n" + errorMsg);
                return;
            }

            spellTable.Columns.Add("id", typeof(System.UInt32));
            spellTable.Columns.Add("SpellName" + GetLocale(), typeof(System.String));
            spellTable.Columns.Add("Icon", typeof(System.UInt32));

            PrepareIconEditor();
            PopulateSelectSpell();
            // Load other DBC's
			loadAreaTable = new AreaTable(this, mySQL);
            loadCategories = new SpellCategory(this, mySQL);
            loadDispels = new SpellDispelType(this, mySQL);
            loadMechanics = new SpellMechanic(this, mySQL);
            loadFocusObjects = new SpellFocusObject(this, mySQL);
            loadAreaGroups = new AreaGroup(this, mySQL);
            loadDifficulties = new SpellDifficulty(this, mySQL);
            loadCastTimes = new SpellCastTimes(this, mySQL);
            loadDurations = new SpellDuration(this, mySQL);
            loadRanges = new SpellRange(this, mySQL);
            loadRadiuses = new SpellRadius(this, mySQL);
            loadItemClasses = new ItemClass(this, mySQL);
			loadItemSubClasses = new ItemSubClass(this, mySQL);
            loadTotemCategories = new TotemCategory(this, mySQL);
            loadRuneCosts = new SpellRuneCost(this, mySQL);
            loadDescriptionVariables = new SpellDescriptionVariables(this, mySQL);
        }

        private async Task<Config> getConfig()
        {
            String errorMsg = "";
            try
            {
                Config config = new Config();
                if (!File.Exists("config.xml"))
                {

                    String host = await this.ShowInputAsync("Input MySQL Details", "Input your MySQL host:");
                    String user = await this.ShowInputAsync("Input MySQL Details", "Input your MySQL username:");
                    String pass = await this.ShowInputAsync("Input MySQL Details", "Input your MySQL password:");
                    String port = await this.ShowInputAsync("Input MySQL Details", "Input your MySQL port:");
                    String db = await this.ShowInputAsync("Input MySQL Details", "Input which MySQL database to create/use:");
                    String tb = await this.ShowInputAsync("Input MySQL Details", "Input which MySQL table to create/use:");

                    UInt32 result = 0;
                    if (host == null || user == null || pass == null || port == null || db == null || tb == null ||
                        host.Length == 0 || user.Length == 0 || port.Length == 0 || db.Length == 0 || tb.Length == 0 ||
                            !UInt32.TryParse(port, out result))
                        throw new Exception("The MySQL details input are not valid.");

                    config.createFile(host, user, pass, port, db, tb);
                }
                config.loadFile();
                return config;
            }
            catch (Exception e)
            {
                errorMsg = e.Message;
            }
            await this.ShowMessageAsync("ERROR", "An exception was thrown creating the config file.\n" + errorMsg);
            return null;
        }

        private void _KeyUp(object sender, KeyEventArgs e)
        {
            if (sender == FilterSpellNames && e.Key == Key.Back)
            {
                _KeyDown(sender, new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Space));
            }
        }

        private volatile Boolean imageLoadEventRunning = false;

        private async void _KeyDown(object sender, KeyEventArgs e)
        {
            if (sender == this)
            {
                if (e.Key == Key.Escape)
                {
                    MetroDialogSettings settings = new MetroDialogSettings();

                    settings.AffirmativeButtonText = "YES";
                    settings.NegativeButtonText = "NO";

                    MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
                    MessageDialogResult exitCode = await this.ShowMessageAsync("Spell Editor", "Are you sure you want to exit?\n\nMake sure you have saved before doing this action or all progress will be lost!", style, settings);

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

                    int ID = Int32.Parse(box.Text);

					Int32 count = 0;
					foreach (StackPanel obj in SelectSpell.Items)
					{
						foreach (var item in obj.Children)
							if (item is TextBlock)
							{
								TextBlock tb = (TextBlock)item;

								if (Int32.Parse(tb.Text.Split(' ')[1]) == ID)
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
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (mySQL == null) { return; }

            if (sender == ExportDBC)
            {
                MetroDialogSettings settings = new MetroDialogSettings();
                settings.AffirmativeButtonText = "YES";
                settings.NegativeButtonText = "NO";
                MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
                var res = await this.ShowMessageAsync("Export Spell.dbc?",
                    "Exporting to a new Spell.dbc can be very slow depending on your connection to the MySQL server."
                    + " It will be exported to a 'Export' folder. Are you sure you wish to continue?", style, settings);
                if (res == MessageDialogResult.Affirmative)
                {
                    var controller = await this.ShowProgressAsync("Please wait...", "Exporting to a new Spell.dbc.");
                    //await Task.Delay(1000);
                    controller.SetCancelable(false);

                    SpellDBC dbc = new SpellDBC();
                    await dbc.export(mySQL, new UpdateProgressFunc(controller.SetProgress));
                    await controller.CloseAsync();
                }
                return;
            }
            
            if (sender == TruncateTable)
            {
                MetroDialogSettings settings = new MetroDialogSettings();
                settings.AffirmativeButtonText = "YES";
                settings.NegativeButtonText = "NO";
                MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
                var res = await this.ShowMessageAsync("ARE YOU SURE?", "Truncating the table will remove ALL data in the MySQL table.\n\n" +
                    "This feature should only be used when you want to reset the table and import a new Spell.dbc.", style, settings);
                if (res == MessageDialogResult.Affirmative)
                {
                    mySQL.execute(String.Format("TRUNCATE TABLE `{0}`", mySQL.Table));
                    PopulateSelectSpell();
                    if (SelectSpell.Items.Count == 0)
                    {
                        res = await this.ShowMessageAsync("Import Spell.dbc?",
                            "It appears the table in the database is empty. Would you like to import a Spell.dbc now?", style, settings);
                        if (res == MessageDialogResult.Affirmative)
                        {
                            var controller = await this.ShowProgressAsync("Please wait...", "Importing the Spell.dbc. Cancelling this task will corrupt the table.");
                            await Task.Delay(1000);
                            controller.SetCancelable(false);

                            SpellDBC dbc = new SpellDBC();
                            dbc.LoadDBCFile(this);
                            await dbc.import(mySQL, new UpdateProgressFunc(controller.SetProgress));
                            await controller.CloseAsync();
                            PopulateSelectSpell();

                            if (SpellDBC.ErrorMessage.Length > 0)
                            {
                                HandleErrorMessage(SpellDBC.ErrorMessage);
                                SpellDBC.ErrorMessage = "";
                            }
                        }
                    }
                }
            }

            if (sender == InsertANewRecord)
            {
                MetroDialogSettings settings = new MetroDialogSettings();

                settings.AffirmativeButtonText = "YES";
                settings.NegativeButtonText = "NO";

                MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
                MessageDialogResult copySpell = await this.ShowMessageAsync("Spell Editor", "Copy an existing record?", style, settings);

                UInt32 oldIDIndex = UInt32.MaxValue;

                if (copySpell == MessageDialogResult.Affirmative)
                {
                    UInt32 oldID = 0;

                    string inputCopySpell = await this.ShowInputAsync("Spell Editor", "Input the spell ID to copy from.");
                    if (inputCopySpell == null) { return; }

                    if (!UInt32.TryParse(inputCopySpell, out oldID))
                    {
                        HandleErrorMessage("ERROR: Input spell ID was not an integer.");
                        return;
                    }
                    oldIDIndex = oldID;
                }

                string inputNewRecord = await this.ShowInputAsync("Spell Editor", "Input the new spell ID.");
                if (inputNewRecord == null) { return; }

                UInt32 newID = 0;
                if (!UInt32.TryParse(inputNewRecord, out newID))
                {
                    HandleErrorMessage("ERROR: Input spell ID was not an integer.");
                    return;
                }

                if (UInt32.Parse(mySQL.query(String.Format("SELECT COUNT(*) FROM `{0}` WHERE `ID` = '{1}'", mySQL.Table, newID)).Rows[0][0].ToString()) > 0)
                {
                    HandleErrorMessage("ERROR: That spell ID is already taken.");
                    return;
                }

                if (oldIDIndex != UInt32.MaxValue)
                {
                    // Copy old spell to new spell
                    var row = mySQL.query(String.Format("SELECT * FROM `{0}` WHERE `ID` = '{1}' LIMIT 1", mySQL.Table, oldIDIndex)).Rows[0];
                    StringBuilder str = new StringBuilder();
                    str.Append(String.Format("INSERT INTO `{0}` VALUES ('{1}'", mySQL.Table, newID));
                    for (int i = 1; i < row.Table.Columns.Count; ++i)
                        str.Append(String.Format(", \"{0}\"", MySqlHelper.EscapeString(row[i].ToString())));
                    str.Append(")");
                    mySQL.execute(str.ToString());
                }
                else
                {
                    // Create new spell
                    HandleErrorMessage("Creating a new spell from scratch is currently not implemented. Please copy an existing spell.");
                    return;
                }

                PopulateSelectSpell();

                await this.ShowMessageAsync("Spell Editor", "Created new record with ID " + inputNewRecord + " sucessfully.");
            }

            if (sender == DeleteARecord)
            {
                string input = await this.ShowInputAsync("Spell Editor", "Input the spell ID to delete.");

                if (input == null) { return; }

                UInt32 spellID = 0;
                if (!UInt32.TryParse(input, out spellID))
                {
                    HandleErrorMessage("ERROR: Input spell ID was not an integer.");
                    return;
                }
                
                mySQL.execute(String.Format("DELETE FROM `{0}` WHERE `ID` = '{1}'", mySQL.Table, spellID));
                
                selectedID = 0;

                PopulateSelectSpell();

                await this.ShowMessageAsync("Spell Editor", "Deleted record successfully.");
            }

            if (sender == SaveSpellChanges)
            {
                String query = String.Format("SELECT * FROM `{0}` WHERE `ID` = '{1}' LIMIT 1", mySQL.Table, selectedID);
                var q = mySQL.query(query);
                if (q.Rows.Count == 0)
                    return;
                var row = q.Rows[0];
                row.BeginEdit();
                try
                {

                    UInt32 maskk = 0;
                    UInt32 flagg = 1;

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

                    maskk = 0;
                    flagg = 1;

                    for (int f = 0; f < attributes7.Count; ++f)
                    {
                        if (attributes7[f].IsChecked.Value == true) { maskk = maskk + flagg; }

                        flagg = flagg + flagg;
                    }

                    row["AttributesEx7"] = maskk;

                    if (stancesBoxes[0].IsChecked.Value == true) { row["Stances"] = 0; }
                    else
                    {
                        UInt32 mask = 0;
                        UInt32 flag = 1;

                        for (int f = 1; f < stancesBoxes.Count; ++f)
                        {
                            if (stancesBoxes[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        row["Stances"] = mask;
                    }

                    if (targetBoxes[0].IsChecked.Value == true) { row["Targets"] = 0; }
                    else
                    {
                        UInt32 mask = 0;
                        UInt32 flag = 1;

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
                        UInt32 mask = 0;
                        UInt32 flag = 1;

                        for (int f = 1; f < targetCreatureTypeBoxes.Count; ++f)
                        {
                            if (targetCreatureTypeBoxes[f].IsChecked.Value == true) { mask = mask + flag; }
                            flag = flag + flag;
                        }

                        row["TargetCreatureType"] = mask;
                    }

                    row["FacingCasterFlags"] = FacingFrontFlag.IsChecked.Value ? (UInt32)0x1 : (UInt32)0x0;
                    
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

                    row["RecoveryTime"] = UInt32.Parse(RecoveryTime.Text);
                    row["CategoryRecoveryTime"] = UInt32.Parse(CategoryRecoveryTime.Text);

                    if (interrupts1[0].IsChecked.Value == true) { row["InterruptFlags"] = 0; }
                    else
                    {
                        UInt32 mask = 0;
                        UInt32 flag = 1;

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
                        UInt32 mask = 0;
                        UInt32 flag = 1;

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
                        UInt32 mask = 0;
                        UInt32 flag = 1;

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
                        UInt32 mask = 0;
                        UInt32 flag = 1;

                        for (int f = 1; f < procBoxes.Count; ++f)
                        {
                            if (procBoxes[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        row["ProcFlags"] = mask;
                    }

                    row["ProcChance"] = UInt32.Parse(ProcChance.Text);
                    row["ProcCharges"] = UInt32.Parse(ProcCharges.Text);
                    row["MaximumLevel"] = UInt32.Parse(MaximumLevel.Text);
                    row["BaseLevel"] = UInt32.Parse(BaseLevel.Text);
                    row["SpellLevel"] = UInt32.Parse(SpellLevel.Text);
                    row["PowerType"] = PowerType.SelectedIndex == 13 ? 4294967294 : (UInt32)PowerType.SelectedIndex;
                    row["ManaCost"] = UInt32.Parse(PowerCost.Text);
                    row["ManaCostPerLevel"] = UInt32.Parse(ManaCostPerLevel.Text);
                    row["ManaPerSecond"] = UInt32.Parse(ManaCostPerSecond.Text);
                    row["ManaPerSecondPerLevel"] = UInt32.Parse(PerSecondPerLevel.Text);
                    row["Speed"] = float.Parse(Speed.Text);
                    row["StackAmount"] = UInt32.Parse(Stacks.Text);
                    row["Totem1"] = UInt32.Parse(Totem1.Text);
                    row["Totem2"] = UInt32.Parse(Totem2.Text);
                    row["Reagent1"] = Int32.Parse(Reagent1.Text);
                    row["Reagent2"] = Int32.Parse(Reagent2.Text);
                    row["Reagent3"] = Int32.Parse(Reagent3.Text);
                    row["Reagent4"] = Int32.Parse(Reagent4.Text);
                    row["Reagent5"] = Int32.Parse(Reagent5.Text);
                    row["Reagent6"] = Int32.Parse(Reagent6.Text);
                    row["Reagent7"] = Int32.Parse(Reagent7.Text);
                    row["Reagent8"] = Int32.Parse(Reagent8.Text);
                    row["ReagentCount1"] = UInt32.Parse(ReagentCount1.Text);
                    row["ReagentCount2"] = UInt32.Parse(ReagentCount2.Text);
                    row["ReagentCount3"] = UInt32.Parse(ReagentCount3.Text);
                    row["ReagentCount4"] = UInt32.Parse(ReagentCount4.Text);
                    row["ReagentCount5"] = UInt32.Parse(ReagentCount5.Text);
                    row["ReagentCount6"] = UInt32.Parse(ReagentCount6.Text);
                    row["ReagentCount7"] = UInt32.Parse(ReagentCount7.Text);
                    row["ReagentCount8"] = UInt32.Parse(ReagentCount8.Text);

                    if (equippedItemInventoryTypeMaskBoxes[0].IsChecked.Value == true) { row["EquippedItemInventoryTypeMask"] = 0; }
                    else
                    {
                        UInt32 mask = 0;
                        UInt32 flag = 1;

                        for (int f = 0; f < equippedItemInventoryTypeMaskBoxes.Count; ++f)
                        {
                            if (equippedItemInventoryTypeMaskBoxes[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        row["EquippedItemInventoryTypeMask"] = (Int32)mask;
                    }

					if (equippedItemSubClassMaskBoxes[0].IsChecked.Value == true) { row["EquippedItemSubClassMask"] = 0; }
					else
					{
						UInt32 mask = 0;
						UInt32 flag = 1;

						for (int f = 0; f < equippedItemSubClassMaskBoxes.Count; ++f)
						{
							if (equippedItemSubClassMaskBoxes[f].IsChecked.Value == true) { mask = mask + flag; }

							flag = flag + flag;
						}

						row["EquippedItemSubClassMask"] = (Int32)mask;
					}

                    row["Effect1"] = (UInt32)SpellEffect1.SelectedIndex;
                    row["Effect2"] = (UInt32)SpellEffect2.SelectedIndex;
                    row["Effect3"] = (UInt32)SpellEffect3.SelectedIndex;
                    row["EffectDieSides1"] = Int32.Parse(DieSides1.Text);
                    row["EffectDieSides2"] = Int32.Parse(DieSides2.Text);
                    row["EffectDieSides3"] = Int32.Parse(DieSides3.Text);
                    row["EffectRealPointsPerLevel1"] = float.Parse(BasePointsPerLevel1.Text);
                    row["EffectRealPointsPerLevel2"] = float.Parse(BasePointsPerLevel2.Text);
                    row["EffectRealPointsPerLevel3"] = float.Parse(BasePointsPerLevel3.Text);
                    row["EffectBasePoints1"] = Int32.Parse(BasePoints1.Text);
                    row["EffectBasePoints2"] = Int32.Parse(BasePoints2.Text);
                    row["EffectBasePoints3"] = Int32.Parse(BasePoints3.Text);
                    row["EffectMechanic1"] = (UInt32)Mechanic1.SelectedIndex;
                    row["EffectMechanic2"] = (UInt32)Mechanic2.SelectedIndex;
                    row["EffectMechanic3"] = (UInt32)Mechanic3.SelectedIndex;
                    row["EffectImplicitTargetA1"] = (UInt32)TargetA1.SelectedIndex;
                    row["EffectImplicitTargetA2"] = (UInt32)TargetA2.SelectedIndex;
                    row["EffectImplicitTargetA3"] = (UInt32)TargetA3.SelectedIndex;
                    row["EffectImplicitTargetB1"] = (UInt32)TargetB1.SelectedIndex;
                    row["EffectImplicitTargetB2"] = (UInt32)TargetB2.SelectedIndex;
                    row["EffectImplicitTargetB3"] = (UInt32)TargetB3.SelectedIndex;
                    row["EffectApplyAuraName1"] = (UInt32)ApplyAuraName1.SelectedIndex;
                    row["EffectApplyAuraName2"] = (UInt32)ApplyAuraName2.SelectedIndex;
                    row["EffectApplyAuraName3"] = (UInt32)ApplyAuraName3.SelectedIndex;
                    row["EffectAmplitude1"] = UInt32.Parse(Amplitude1.Text);
                    row["EffectAmplitude2"] = UInt32.Parse(Amplitude2.Text);
                    row["EffectAmplitude3"] = UInt32.Parse(Amplitude3.Text);
                    row["EffectMultipleValue1"] = float.Parse(MultipleValue1.Text);
                    row["EffectMultipleValue2"] = float.Parse(MultipleValue1.Text);
                    row["EffectMultipleValue3"] = float.Parse(MultipleValue1.Text);
					row["EffectChainTarget1"] = (UInt32)ChainTarget1.threadSafeText;
					row["EffectChainTarget2"] = (UInt32)ChainTarget2.threadSafeText;
					row["EffectChainTarget3"] = (UInt32)ChainTarget3.threadSafeText;
                    row["EffectItemType1"] = UInt32.Parse(ItemType1.Text);
                    row["EffectItemType2"] = UInt32.Parse(ItemType2.Text);
                    row["EffectItemType3"] = UInt32.Parse(ItemType3.Text);
                    row["EffectMiscValue1"] = Int32.Parse(MiscValueA1.Text);
                    row["EffectMiscValue2"] = Int32.Parse(MiscValueA2.Text);
                    row["EffectMiscValue3"] = Int32.Parse(MiscValueA3.Text);
                    row["EffectMiscValueB1"] = Int32.Parse(MiscValueB1.Text);
                    row["EffectMiscValueB2"] = Int32.Parse(MiscValueB2.Text);
                    row["EffectMiscValueB3"] = Int32.Parse(MiscValueB3.Text);
                    row["EffectTriggerSpell1"] = UInt32.Parse(TriggerSpell1.Text);
                    row["EffectTriggerSpell2"] = UInt32.Parse(TriggerSpell2.Text);
                    row["EffectTriggerSpell3"] = UInt32.Parse(TriggerSpell3.Text);
                    row["EffectPointsPerComboPoint1"] = float.Parse(PointsPerComboPoint1.Text);
                    row["EffectPointsPerComboPoint2"] = float.Parse(PointsPerComboPoint2.Text);
                    row["EffectPointsPerComboPoint3"] = float.Parse(PointsPerComboPoint3.Text);
                    row["EffectSpellClassMaskA1"] = UInt32.Parse(SpellMask11.Text);
                    row["EffectSpellClassMaskA2"] = UInt32.Parse(SpellMask21.Text);
                    row["EffectSpellClassMaskA3"] = UInt32.Parse(SpellMask31.Text);
                    row["EffectSpellClassMaskB1"] = UInt32.Parse(SpellMask12.Text);
                    row["EffectSpellClassMaskB2"] = UInt32.Parse(SpellMask22.Text);
                    row["EffectSpellClassMaskB3"] = UInt32.Parse(SpellMask32.Text);
                    row["EffectSpellClassMaskC1"] = UInt32.Parse(SpellMask13.Text);
                    row["EffectSpellClassMaskC2"] = UInt32.Parse(SpellMask23.Text);
                    row["EffectSpellClassMaskC3"] = UInt32.Parse(SpellMask33.Text);
                    row["SpellVisual1"] = UInt32.Parse(SpellVisual1.Text);
                    row["SpellVisual2"] = UInt32.Parse(SpellVisual2.Text);
                    row["ManaCostPercentage"] = UInt32.Parse(ManaCostPercent.Text);
                    row["StartRecoveryCategory"] = UInt32.Parse(StartRecoveryCategory.Text);
                    row["StartRecoveryTime"] = UInt32.Parse(StartRecoveryTime.Text);
                    row["MaximumTargetLevel"] = UInt32.Parse(MaxTargetsLevel.Text);
                    row["SpellFamilyName"] = UInt32.Parse(SpellFamilyName.Text);
                    row["SpellFamilyFlags"] = UInt32.Parse(SpellFamilyFlags.Text);
                    row["SpellFamilyFlags1"] = UInt32.Parse(SpellFamilyFlags1.Text);
                    row["SpellFamilyFlags2"] = UInt32.Parse(SpellFamilyFlags2.Text);
                    row["MaximumAffectedTargets"] = UInt32.Parse(MaxTargets.Text);
                    row["DamageClass"] = (UInt32)SpellDamageType.SelectedIndex;
                    row["PreventionType"] = (UInt32)PreventionType.SelectedIndex;
                    row["EffectDamageMultiplier1"] = float.Parse(EffectDamageMultiplier1.Text);
                    row["EffectDamageMultiplier2"] = float.Parse(EffectDamageMultiplier2.Text);
                    row["EffectDamageMultiplier3"] = float.Parse(EffectDamageMultiplier3.Text);
                    row["SchoolMask"] = (S1.IsChecked.Value ? (UInt32)0x01 : (UInt32)0x00) + (S2.IsChecked.Value ? (UInt32)0x02 : (UInt32)0x00) + (S3.IsChecked.Value ? (UInt32)0x04 : (UInt32)0x00) + (S4.IsChecked.Value ? (UInt32)0x08 : (UInt32)0x00) + (S5.IsChecked.Value ? (UInt32)0x10 : (UInt32)0x00) + (S6.IsChecked.Value ? (UInt32)0x20 : (UInt32)0x00) + (S7.IsChecked.Value ? (UInt32)0x40 : (UInt32)0x00);
                    row["SpellMissileID"] = UInt32.Parse(SpellMissileID.Text);
                    row["EffectBonusMultiplier1"] = float.Parse(EffectBonusMultiplier1.Text);
                    row["EffectBonusMultiplier2"] = float.Parse(EffectBonusMultiplier2.Text);
                    row["EffectBonusMultiplier3"] = float.Parse(EffectBonusMultiplier3.Text);

                    TextBox[] boxes = stringObjectMap.Values.ToArray();
                    for (int i = 0; i < 9; ++i)
                        row["SpellName" + i] = boxes[i].Text;
                    for (int i = 0; i < 9; ++i)
                        row["SpellRank" + i] = boxes[i + 9].Text;
                    for (int i = 0; i < 9; ++i)
                        row["SpellTooltip" + i] = boxes[i + 18].Text;
                    for (int i = 0; i < 9; ++i)
                        row["SpellDescription" + i] = boxes[i + 27].Text;
                    // This seems to mimic Blizzlike values correctly, though I don't understand it at all.
                    // Discussed on modcraft IRC - these fields are not even read by the client.
                    // The structure used in this program is actually incorrect. All the string columns are
                    //   for different locales apart from the last one which is the flag column. So there are
                    //   not multiple flag columns, hence why we only write to the last one here. The current
                    //   released clients only use 9 locales hence the confusion with the other columns.
                    row["SpellNameFlag7"] = (uint)(TextFlags.NOT_EMPTY);
                    row["SpellRankFlags7"] = (uint)(TextFlags.NOT_EMPTY);
                    row["SpellToolTipFlags7"] = (uint)(TextFlags.NOT_EMPTY);
                    row["SpellDescriptionFlags7"] = (uint)(TextFlags.NOT_EMPTY);

                    row.EndEdit();
                    mySQL.commitChanges(query, q.GetChanges());

                    PopulateSelectSpell();
                }

                catch (Exception ex)
                {
                    row.CancelEdit();
                    HandleErrorMessage(ex.Message);
                    return;
                }
            }

            if (sender == SaveIcon)
            {
                MetroDialogSettings settings = new MetroDialogSettings();

                settings.AffirmativeButtonText = "YES";
                settings.NegativeButtonText = "NO";

                MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
                MessageDialogResult spellOrActive = await this.ShowMessageAsync("Spell Editor", "Yes for Spell Icon ID.\nNo for Active Icon ID.", style, settings);

                String column = null;
                if (spellOrActive == MessageDialogResult.Affirmative)
                    column = "SpellIconID";
                else if (spellOrActive == MessageDialogResult.Negative)
                    column = "ActiveIconID";
                mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'", mySQL.Table, column, newIconID, selectedID));
            }

            if (sender == ResetSpellIconID)
                mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'", mySQL.Table, "SpellIconID", 1, selectedID));
            if (sender == ResetActiveIconID)
                mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'", mySQL.Table, "ActiveIconID", 0, selectedID)); 
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

        private async void PrepareIconEditor()
        {
            loadIcons = new SpellIconDBC(this, mySQL);

            await loadIcons.LoadImages();
        }

        private class Worker : BackgroundWorker
        {
            public MySQL __mySQL;
            public Config __config;

            public Worker(MySQL _mySQL, Config _config)
            {
                __mySQL = _mySQL;
                __config = _config;
                
            }
        }

        public int GetLocale()
        {
            if (storedLocale != -1)
                return storedLocale;

            // Attempt localisation on Death Touch, HACKY
            DataRowCollection res = mySQL.query(String.Format("SELECT `id`,`SpellName0`,`SpellName1`,`SpellName2`,`SpellName3`,`SpellName4`," +
                "`SpellName5`,`SpellName6`,`SpellName7`,`SpellName8` FROM `{0}` WHERE `ID` = '5'", config.Table)).Rows;
            if (res == null || res.Count == 0)
                return -1;
            int locale = 0;
            if (res[0] != null)
            {
                for (int i = 0; i < 9; ++i)
                {
                    if (res[0][i + 1].ToString().Length > 3)
                    {
                        locale = i;
                        break;
                    }
                }
            }
            storedLocale = locale;
            return locale;
        }

        private void PopulateSelectSpell()
        {
            SelectSpell.Items.Clear();
            SpellsLoadedLabel.Content = "No spells loaded.";
            Worker _worker = new Worker(mySQL, config);
            _worker.WorkerReportsProgress = true;
            _worker.ProgressChanged += new ProgressChangedEventHandler(_worker_ProgressChanged);

            FilterSpellNames.IsEnabled = false;

            _worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                if (_worker.__mySQL == null || _worker.__config == null)
                    return;

                // Attempt localisation on Death Touch, HACKY
                DataRowCollection res = mySQL.query(String.Format("SELECT `id`,`SpellName0`,`SpellName1`,`SpellName2`,`SpellName3`,`SpellName4`," +
                    "`SpellName5`,`SpellName6`,`SpellName7`,`SpellName8` FROM `{0}` WHERE `ID` = '5'", config.Table)).Rows;
                if (res == null || res.Count == 0)
                    return;
                int locale = 0;
                if (res[0] != null)
                {
                    for (int i = 0; i < 9; ++i)
                    {
                        if (res[0][i + 1].ToString().Length > 3)
                        {
                            locale = i;
                            break;
                        }
                    }
                }

                spellTable.Rows.Clear();

                UInt32 lowerBounds = 0;
                UInt32 pageSize = 5000;
                UInt32 targetSize = pageSize;
                DataRowCollection results = GetSpellNames(lowerBounds, 100, locale);
                lowerBounds += 100;
                while (results != null && results.Count != 0)
                {
                    _worker.ReportProgress(0, results);
                    results = GetSpellNames(lowerBounds, pageSize, locale);
                    lowerBounds += pageSize;
                }

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => FilterSpellNames.IsEnabled = true));
            };
            _worker.RunWorkerAsync();
        }

        private void _worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            DataRowCollection collection = (DataRowCollection) e.UserState;
            SpellsLoadedLabel.Content = "Highest Spell ID Loaded: " + collection[collection.Count - 1][0].ToString();
            int locale = GetLocale();
            foreach (DataRow row in collection)
            {
                var spellName = row["SpellName" + locale].ToString();
                var textBlock = new TextBlock();
                textBlock.Text = String.Format(" {0} - {1}", row["id"], row["SpellName" + locale].ToString());
                var image = new System.Windows.Controls.Image();
                var iconId = Int32.Parse(row["SpellIconID"].ToString());
                if (iconId > 0)
                {
                    image.IsVisibleChanged += (o, args) =>
                    {
                        if (!((bool)args.NewValue))
                        {
                            return;
                        }
                        FileStream fileStream = null;
                        try
                        {
                            fileStream = new FileStream(loadIcons.getIconPath(iconId) + ".blp", FileMode.Open);
                            var blpImage = new SereniaBLPLib.BlpFile(fileStream);
                            var bit = blpImage.getBitmap(0);
                            image.Width = 32;
                            image.Height = 32;
                            image.Margin = new System.Windows.Thickness(1, 1, 1, 1);
                            image.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                bit.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty,
                                BitmapSizeOptions.FromWidthAndHeight(bit.Width, bit.Height));
                            //image.UpdateLayout();
                        }
                        catch (Exception ex)
                        {
                            // These are only really thrown if the image could not be loaded
                        }
                        finally
                        {
                            if (fileStream != null)
                                fileStream.Close();
                        }
                    };
                    var stackPanel = new StackPanel() { Orientation = Orientation.Horizontal };
                    stackPanel.Children.Add(image);
                    stackPanel.Children.Add(textBlock);
                    SelectSpell.Items.Add(stackPanel);
                }
            }
        }

        private DataRowCollection GetSpellNames(UInt32 lowerBound, UInt32 pageSize, int locale)
        {
            DataTable newSpellNames = mySQL.query(String.Format(@"SELECT `id`,`SpellName{1}`,`SpellIconID` FROM `{0}` LIMIT {2}, {3}",
                 config.Table, locale, lowerBound, pageSize));

            spellTable.Merge(newSpellNames, false, MissingSchemaAction.Add);

            return newSpellNames.Rows;
        }

        private async void NewIconClick(object sender, RoutedEventArgs e)
        {
            if (mySQL == null) { return; }

            MetroDialogSettings settings = new MetroDialogSettings();
            settings.AffirmativeButtonText = "Spell Icon ID";
            settings.NegativeButtonText = "Active Icon ID";

            MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
            MessageDialogResult spellOrActive = await this.ShowMessageAsync("Spell Editor", "Select whether to change the Spell Icon ID or the Active Icon ID.", style, settings);

            String column = null;
            if (spellOrActive == MessageDialogResult.Affirmative)
                column = "SpellIconID";
            else if (spellOrActive == MessageDialogResult.Negative)
                column = "ActiveIconID";
            mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'", mySQL.Table, column, newIconID, selectedID));
        }



        private async void UpdateMainWindow()
        {
            try
            {
                updating = true;

                var controller = await this.ShowProgressAsync("Please wait...", "Loading Spell: " + selectedID +
                    ".\n\nThe first spell to be loaded will always take a while but afterwards it should be quite fast.");
                controller.SetCancelable(false);

               /* Timeline.DesiredFrameRateProperty.OverrideMetadata(
                    typeof(Timeline),
                    new FrameworkPropertyMetadata { DefaultValue = 30 }
                );*/

                await loadSpell(new UpdateTextFunc(controller.SetMessage));

                await controller.CloseAsync();

                updating = false;
            }

            catch (Exception ex)
            {
                HandleErrorMessage(ex.Message);

                updating = false;
            }
        }

        private Task loadSpell(UpdateTextFunc updateProgress)
        {
            return Task.Run(() =>
            {
                mySQL.setUpdating(true);
                updateProgress("Querying MySQL data...");
                DataRowCollection rowResult = mySQL.query(String.Format("SELECT * FROM `{0}` WHERE `ID` = '{1}'", config.Table, selectedID)).Rows;
                if (rowResult == null || rowResult.Count != 1)
                    throw new Exception("An error occurred trying to select spell ID: " + selectedID.ToString());
                var row = rowResult[0];
                updateProgress("Updating text control's...");
                int i;
                for (i = 0; i < 9; ++i)
                {
                    ThreadSafeTextBox box;
                    stringObjectMap.TryGetValue(i, out box);
                    box.threadSafeText = row[String.Format("SpellName{0}", i)];
                }
                for (i = 0; i < 9; ++i)
                {
                    ThreadSafeTextBox box;
                    stringObjectMap.TryGetValue(i + 9, out box);
                    box.threadSafeText = row[String.Format("SpellRank{0}", i)];
                }

                for (i = 0; i < 9; ++i)
                {
                    ThreadSafeTextBox box;
                    stringObjectMap.TryGetValue(i + 18, out box);
                    box.threadSafeText = row[String.Format("SpellTooltip{0}", i)];
                }

                for (i = 0; i < 9; ++i)
                {
                    ThreadSafeTextBox box;
                    stringObjectMap.TryGetValue(i + 27, out box);
                    box.threadSafeText = row[String.Format("SpellDescription{0}", i)];
                }

                updateProgress("Updating category & dispel & mechanic...");
                loadCategories.UpdateCategorySelection();
                loadDispels.UpdateDispelSelection();
                loadMechanics.UpdateMechanicSelection();

                updateProgress("Updating attributes...");
                UInt32 mask = UInt32.Parse(row["Attributes"].ToString());
                UInt32 flagg = 1;

                for (int f = 0; f < attributes0.Count; ++f)
                {
                    attributes0[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                    flagg = flagg + flagg;
                }

                mask = UInt32.Parse(row["AttributesEx"].ToString());
                flagg = 1;

                for (int f = 0; f < attributes1.Count; ++f)
                {
                    attributes1[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                    flagg = flagg + flagg;
                }

                mask = UInt32.Parse(row["AttributesEx2"].ToString());
                flagg = 1;

                for (int f = 0; f < attributes2.Count; ++f)
                {
                    attributes2[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                    flagg = flagg + flagg;
                }

                mask = UInt32.Parse(row["AttributesEx3"].ToString());
                flagg = 1;

                for (int f = 0; f < attributes3.Count; ++f)
                {
                    attributes3[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                    flagg = flagg + flagg;
                }

                mask = UInt32.Parse(row["AttributesEx4"].ToString());
                flagg = 1;

                for (int f = 0; f < attributes4.Count; ++f)
                {
                    attributes4[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                    flagg = flagg + flagg;
                }

                mask = UInt32.Parse(row["AttributesEx5"].ToString());
                flagg = 1;

                for (int f = 0; f < attributes5.Count; ++f)
                {
                    attributes5[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                    flagg = flagg + flagg;
                }

                mask = UInt32.Parse(row["AttributesEx6"].ToString());
                flagg = 1;

                for (int f = 0; f < attributes6.Count; ++f)
                {
                    attributes6[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                    flagg = flagg + flagg;
                }

                mask = UInt32.Parse(row["AttributesEx7"].ToString());
                flagg = 1;

                for (int f = 0; f < attributes7.Count; ++f)
                {
                    attributes7[f].threadSafeChecked = ((mask & flagg) != 0) ? true : false;
                    flagg = flagg + flagg;
                }
                updateProgress("Updating stances...");
                mask = UInt32.Parse(row["Stances"].ToString());
                if (mask == 0)
                {
                    stancesBoxes[0].threadSafeChecked = true;
                    for (int f = 1; f < stancesBoxes.Count; ++f) { stancesBoxes[f].threadSafeChecked = false; }
                }
                else
                {
                    stancesBoxes[0].threadSafeChecked = false;
                    UInt32 flag = 1;
                    for (int f = 1; f < stancesBoxes.Count; ++f)
                    {
                        stancesBoxes[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }

                updateProgress("Updating targets...");
                mask = UInt32.Parse(row["Targets"].ToString());
                if (mask == 0)
                {
                    targetBoxes[0].threadSafeChecked = true;
                    for (int f = 1; f < targetBoxes.Count; ++f) { targetBoxes[f].threadSafeChecked = false; }
                }
                else
                {
                    targetBoxes[0].threadSafeChecked = false;
                    UInt32 flag = 1;
                    for (int f = 1; f < targetBoxes.Count; ++f)
                    {
                        targetBoxes[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }

                mask = UInt32.Parse(row["TargetCreatureType"].ToString());

                if (mask == 0)
                {
                    targetCreatureTypeBoxes[0].threadSafeChecked = true;
                    for (int f = 1; f < targetCreatureTypeBoxes.Count; ++f) { targetCreatureTypeBoxes[f].threadSafeChecked = false; }
                }
                else
                {
                    targetCreatureTypeBoxes[0].threadSafeChecked = false;
                    UInt32 flag = 1;
                    for (int f = 1; f < targetCreatureTypeBoxes.Count; ++f)
                    {
                        targetCreatureTypeBoxes[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }
                updateProgress("Updating spell focus object selection...");
                loadFocusObjects.UpdateSpellFocusObjectSelection();

                mask = UInt32.Parse(row["FacingCasterFlags"].ToString());

                FacingFrontFlag.threadSafeChecked = ((mask & 0x1) != 0) ? true : false;
                updateProgress("Updating caster aura state...");
                switch (UInt32.Parse(row["CasterAuraState"].ToString()))
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

                switch (UInt32.Parse(row["TargetAuraState"].ToString()))
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
                loadCastTimes.UpdateCastTimeSelection();
                updateProgress("Updating other stuff...");
                RecoveryTime.threadSafeText = UInt32.Parse(row["RecoveryTime"].ToString());
                CategoryRecoveryTime.threadSafeText = UInt32.Parse(row["CategoryRecoveryTime"].ToString());

                mask = UInt32.Parse(row["InterruptFlags"].ToString());
                if (mask == 0)
                {
                    interrupts1[0].threadSafeChecked = true;
                    for (int f = 1; f < interrupts1.Count; ++f) { interrupts1[f].threadSafeChecked = false; }
                }
                else
                {
                    interrupts1[0].threadSafeChecked = false;
                    UInt32 flag = 1;
                    for (int f = 1; f < interrupts1.Count; ++f)
                    {
                        interrupts1[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;

                        flag = flag + flag;
                    }
                }

                mask = UInt32.Parse(row["AuraInterruptFlags"].ToString());
                if (mask == 0)
                {
                    interrupts2[0].threadSafeChecked = true;
                    for (int f = 1; f < interrupts2.Count; ++f) { interrupts2[f].threadSafeChecked = false; }
                }
                else
                {
                    interrupts2[0].threadSafeChecked = false;
                    UInt32 flag = 1;
                    for (int f = 1; f < interrupts2.Count; ++f)
                    {
                        interrupts2[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }

                mask = UInt32.Parse(row["ChannelInterruptFlags"].ToString());
                if (mask == 0)
                {
                    interrupts3[0].threadSafeChecked = true;
                    for (int f = 1; f < interrupts3.Count; ++f) { interrupts3[f].threadSafeChecked = false; }
                }
                else
                {
                    interrupts3[0].threadSafeChecked = false;
                    UInt32 flag = 1;
                    for (int f = 1; f < interrupts3.Count; ++f)
                    {
                        interrupts3[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }

                mask = UInt32.Parse(row["ProcFlags"].ToString());
                if (mask == 0)
                {
                    procBoxes[0].threadSafeChecked = true;
                    for (int f = 1; f < procBoxes.Count; ++f) { procBoxes[f].threadSafeChecked = false; }
                }
                else
                {
                    procBoxes[0].threadSafeChecked = false;
                    UInt32 flag = 1;
                    for (int f = 1; f < procBoxes.Count; ++f)
                    {
                        procBoxes[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }

                ProcChance.threadSafeText = UInt32.Parse(row["ProcChance"].ToString());
                ProcCharges.threadSafeText = UInt32.Parse(row["ProcCharges"].ToString());
                MaximumLevel.threadSafeText = UInt32.Parse(row["MaximumLevel"].ToString());
                BaseLevel.threadSafeText = UInt32.Parse(row["BaseLevel"].ToString());
                SpellLevel.threadSafeText = UInt32.Parse(row["SpellLevel"].ToString());

                loadDurations.UpdateDurationIndexes();

                Int32 powerType = Int32.Parse(row["PowerType"].ToString());
                // Dirty hack fix
                if (powerType < 0)
                    powerType = 13;
                PowerType.threadSafeIndex = powerType;
                PowerCost.threadSafeText = UInt32.Parse(row["ManaCost"].ToString());
                ManaCostPerLevel.threadSafeText = UInt32.Parse(row["ManaCostPerLevel"].ToString());
                ManaCostPerSecond.threadSafeText = UInt32.Parse(row["ManaPerSecond"].ToString());
                PerSecondPerLevel.threadSafeText = UInt32.Parse(row["ManaPerSecondPerLevel"].ToString());
                updateProgress("Updating spell range selection...");
                loadRanges.UpdateSpellRangeSelection();

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
                loadItemClasses.UpdateItemClassSelection();

				UpdateItemSubClass(int.Parse(row["EquippedItemClass"].ToString()));

				updateProgress("Updating item subclass mask...");
				mask = UInt32.Parse(row["EquippedItemSubClassMask"].ToString());
				if (mask == 0)
				{
					equippedItemSubClassMaskBoxes[0].threadSafeChecked = true;
					for (int f = 1; f < equippedItemSubClassMaskBoxes.Count; ++f) { equippedItemSubClassMaskBoxes[f].threadSafeChecked = false; }
				}
				else
				{
					equippedItemSubClassMaskBoxes[0].threadSafeChecked = false;
					UInt32 flag = 1;
					for (int f = 0; f < equippedItemSubClassMaskBoxes.Count; ++f)
					{
						equippedItemSubClassMaskBoxes[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
						flag = flag + flag;
					}
				}

                updateProgress("Updating inventory type...");
                mask = UInt32.Parse(row["EquippedItemInventoryTypeMask"].ToString());
                if (mask == 0)
                {
                    equippedItemInventoryTypeMaskBoxes[0].threadSafeChecked = true;
                    for (int f = 1; f < equippedItemInventoryTypeMaskBoxes.Count; ++f) { equippedItemInventoryTypeMaskBoxes[f].threadSafeChecked = false; }
                }
                else
                {
                    equippedItemInventoryTypeMaskBoxes[0].threadSafeChecked = false;
                    UInt32 flag = 1;
                    for (int f = 0; f < equippedItemInventoryTypeMaskBoxes.Count; ++f)
                    {
                        equippedItemInventoryTypeMaskBoxes[f].threadSafeChecked = ((mask & flag) != 0) ? true : false;
                        flag = flag + flag;
                    }
                }
                updateProgress("Updating effects 1-3...");
                // Some duplication of fields here
                Effect1.threadSafeIndex = Int32.Parse(row["Effect1"].ToString());
                Effect2.threadSafeIndex = Int32.Parse(row["Effect2"].ToString());
                Effect3.threadSafeIndex = Int32.Parse(row["Effect3"].ToString());
                SpellEffect1.threadSafeIndex = Int32.Parse(row["Effect1"].ToString());
                SpellEffect2.threadSafeIndex = Int32.Parse(row["Effect2"].ToString());
                SpellEffect3.threadSafeIndex = Int32.Parse(row["Effect3"].ToString());
                EffectDieSides1.threadSafeText = row["EffectDieSides1"].ToString();
                EffectDieSides2.threadSafeText = row["EffectDieSides2"].ToString();
                EffectDieSides3.threadSafeText = row["EffectDieSides3"].ToString();
                DieSides1.threadSafeText = row["EffectDieSides1"].ToString();
                DieSides2.threadSafeText = row["EffectDieSides2"].ToString();
                DieSides3.threadSafeText = row["EffectDieSides3"].ToString();
                BasePointsPerLevel1.threadSafeText = row["EffectRealPointsPerLevel1"].ToString();
                BasePointsPerLevel2.threadSafeText = row["EffectRealPointsPerLevel2"].ToString();
                BasePointsPerLevel3.threadSafeText = row["EffectRealPointsPerLevel3"].ToString();
                EffectBaseValue1.threadSafeText = row["EffectBasePoints1"].ToString();
                EffectBaseValue2.threadSafeText = row["EffectBasePoints2"].ToString();
                EffectBaseValue3.threadSafeText = row["EffectBasePoints3"].ToString();
                BasePoints1.threadSafeText = row["EffectBasePoints1"].ToString();
                BasePoints2.threadSafeText = row["EffectBasePoints2"].ToString();
                BasePoints3.threadSafeText = row["EffectBasePoints3"].ToString();
                Mechanic1.threadSafeIndex = Int32.Parse(row["EffectMechanic1"].ToString());
                Mechanic2.threadSafeIndex = Int32.Parse(row["EffectMechanic2"].ToString());
                Mechanic3.threadSafeIndex = Int32.Parse(row["EffectMechanic3"].ToString());
                TargetA1.threadSafeIndex = UInt32.Parse(row["EffectImplicitTargetA1"].ToString());
                TargetA2.threadSafeIndex = UInt32.Parse(row["EffectImplicitTargetA2"].ToString());
                TargetA3.threadSafeIndex = UInt32.Parse(row["EffectImplicitTargetA3"].ToString());
                TargetB1.threadSafeIndex = UInt32.Parse(row["EffectImplicitTargetB1"].ToString());
                TargetB2.threadSafeIndex = UInt32.Parse(row["EffectImplicitTargetB2"].ToString());
                TargetB3.threadSafeIndex = UInt32.Parse(row["EffectImplicitTargetB3"].ToString());

                updateProgress("Updating radius index...");
                loadRadiuses.UpdateRadiusIndexes();

                updateProgress("Updating effect 1-3 data...");
                ApplyAuraName1.threadSafeIndex = Int32.Parse(row["EffectApplyAuraName1"].ToString());
                ApplyAuraName2.threadSafeIndex = Int32.Parse(row["EffectApplyAuraName2"].ToString());
                ApplyAuraName3.threadSafeIndex = Int32.Parse(row["EffectApplyAuraName3"].ToString());
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
                MiscValueB1.threadSafeText = row["EffectMiscValueB1"].ToString();
                MiscValueB2.threadSafeText = row["EffectMiscValueB2"].ToString();
                MiscValueB3.threadSafeText = row["EffectMiscValueB3"].ToString();
                TriggerSpell1.threadSafeText = row["EffectTriggerSpell1"].ToString();
                TriggerSpell2.threadSafeText = row["EffectTriggerSpell2"].ToString();
                TriggerSpell3.threadSafeText = row["EffectTriggerSpell3"].ToString();
                PointsPerComboPoint1.threadSafeText = row["EffectPointsPerComboPoint1"].ToString();
                PointsPerComboPoint2.threadSafeText = row["EffectPointsPerComboPoint2"].ToString();
                PointsPerComboPoint3.threadSafeText = row["EffectPointsPerComboPoint3"].ToString();
                SpellMask11.threadSafeText = row["EffectSpellClassMaskA1"].ToString();
                SpellMask21.threadSafeText = row["EffectSpellClassMaskA2"].ToString();
                SpellMask31.threadSafeText = row["EffectSpellClassMaskA3"].ToString();
                SpellMask12.threadSafeText = row["EffectSpellClassMaskB1"].ToString();
                SpellMask22.threadSafeText = row["EffectSpellClassMaskB2"].ToString();
                SpellMask32.threadSafeText = row["EffectSpellClassMaskB3"].ToString();
                SpellMask13.threadSafeText = row["EffectSpellClassMaskC1"].ToString();
                SpellMask23.threadSafeText = row["EffectSpellClassMaskC2"].ToString();
                SpellMask33.threadSafeText = row["EffectSpellClassMaskC3"].ToString();
                SpellVisual1.threadSafeText = row["SpellVisual1"].ToString();
                SpellVisual2.threadSafeText = row["SpellVisual2"].ToString();
                ManaCostPercent.threadSafeText = row["ManaCostPercentage"].ToString();
                StartRecoveryCategory.threadSafeText = row["StartRecoveryCategory"].ToString();
                StartRecoveryTime.threadSafeText = row["StartRecoveryTime"].ToString();
                MaxTargetsLevel.threadSafeText = row["MaximumTargetLevel"].ToString();
                SpellFamilyName.threadSafeText = row["SpellFamilyName"].ToString();
                SpellFamilyFlags.threadSafeText = row["SpellFamilyFlags"].ToString();
                SpellFamilyFlags1.threadSafeText = row["SpellFamilyFlags1"].ToString();
                SpellFamilyFlags2.threadSafeText = row["SpellFamilyFlags2"].ToString();
                MaxTargets.threadSafeText = row["MaximumAffectedTargets"].ToString();
                SpellDamageType.threadSafeIndex = Int32.Parse(row["DamageClass"].ToString());
                PreventionType.threadSafeIndex = Int32.Parse(row["PreventionType"].ToString());
                EffectDamageMultiplier1.threadSafeText = row["EffectDamageMultiplier1"].ToString();
                EffectDamageMultiplier2.threadSafeText = row["EffectDamageMultiplier2"].ToString();
                EffectDamageMultiplier3.threadSafeText = row["EffectDamageMultiplier3"].ToString();

                updateProgress("Updating totem categories & load area groups...");
                loadTotemCategories.UpdateTotemCategoriesSelection();
                loadAreaGroups.UpdateAreaGroupSelection();

                updateProgress("Updating school mask...");
                mask = UInt32.Parse(row["SchoolMask"].ToString());
                S1.threadSafeChecked = ((mask & 0x01) != 0) ? true : false;
                S2.threadSafeChecked = ((mask & 0x02) != 0) ? true : false;
                S3.threadSafeChecked = ((mask & 0x04) != 0) ? true : false;
                S4.threadSafeChecked = ((mask & 0x08) != 0) ? true : false;
                S5.threadSafeChecked = ((mask & 0x10) != 0) ? true : false;
                S6.threadSafeChecked = ((mask & 0x20) != 0) ? true : false;
                S7.threadSafeChecked = ((mask & 0x40) != 0) ? true : false;

                updateProgress("Updating rune costs...");
                loadRuneCosts.UpdateSpellRuneCostSelection();

                updateProgress("Updating spell missile & effect bonus multiplier...");
                SpellMissileID.threadSafeText = row["SpellMissileID"].ToString();
                EffectBonusMultiplier1.threadSafeText = row["EffectBonusMultiplier1"].ToString();
                EffectBonusMultiplier2.threadSafeText = row["EffectBonusMultiplier2"].ToString();
                EffectBonusMultiplier3.threadSafeText = row["EffectBonusMultiplier3"].ToString();

                updateProgress("Updating spell description variables & difficulty selection...");
                loadDescriptionVariables.UpdateSpellDescriptionVariablesSelection();
                loadDifficulties.UpdateDifficultySelection();
                mySQL.setUpdating(false);
            });
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (updating || mySQL == null || config == null)
                return;
            var item = sender as TabControl;

            if (item.SelectedIndex == item.Items.Count - 1) { PrepareIconEditor(); }
        }

        private async void SelectSpell_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var added_items = e.AddedItems;
            if (added_items.Count > 1)
            {
                await this.ShowMessageAsync("Spell Editor", "Only one spell can be selected at a time.");
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
                            selectedID = UInt32.Parse(name.Substring(1, name.IndexOf(' ', 1)));
                            UpdateMainWindow();
                            enumerator.Dispose();
                            return;
                        }
                    }
                    enumerator.Dispose();
                }
            }
        }

        static object Lock = new object();

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (mySQL == null || updating) { return; }
            if (sender == EffectBaseValue1) { BasePoints1.Text = EffectBaseValue1.Text; }
            if (sender == EffectBaseValue2) { BasePoints2.Text = EffectBaseValue2.Text; }
            if (sender == EffectBaseValue3) { BasePoints3.Text = EffectBaseValue3.Text; }
            if (sender == BasePoints1) { EffectBaseValue1.Text = BasePoints1.Text; }
            if (sender == BasePoints2) { EffectBaseValue2.Text = BasePoints2.Text; }
            if (sender == BasePoints3) { EffectBaseValue3.Text = BasePoints3.Text; }
            if (sender == EffectDieSides1) { DieSides1.Text = EffectDieSides1.Text; }
            if (sender == EffectDieSides2) { DieSides2.Text = EffectDieSides2.Text; }
            if (sender == EffectDieSides3) { DieSides3.Text = EffectDieSides3.Text; }
            if (sender == DieSides1) { EffectDieSides1.Text = DieSides1.Text; }
            if (sender == DieSides2) { EffectDieSides2.Text = DieSides2.Text; }
            if (sender == DieSides3) { EffectDieSides3.Text = DieSides3.Text; }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mySQL == null || updating)
                return;
            if (sender == RequiresSpellFocus)
            {
                for (int i = 0; i < loadFocusObjects.body.lookup.Count; ++i)
                {
                    if (loadFocusObjects.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "RequiresSpellFocus", (UInt32)loadFocusObjects.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == AreaGroup)
            {
                for (int i = 0; i < loadAreaGroups.body.lookup.Count; ++i)
                {
                    if (loadAreaGroups.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "AreaGroupID", (UInt32)loadAreaGroups.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == Category)
            {
                for (int i = 0; i < loadCategories.body.lookup.Count; ++i)
                {
                    if (loadCategories.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "Category", (UInt32)loadCategories.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == DispelType)
            {
                for (int i = 0; i < loadDispels.body.lookup.Count; ++i)
                {
                    if (loadDispels.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "Dispel", (UInt32)loadDispels.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == MechanicType)
            {
                for (int i = 0; i < loadMechanics.body.lookup.Count; ++i)
                {
                    if (loadMechanics.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "Mechanic", (UInt32)loadMechanics.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == CastTime)
            {
                for (int i = 0; i < loadCastTimes.body.lookup.Count; ++i)
                {
                    if (loadCastTimes.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "CastingTimeIndex", (UInt32)loadCastTimes.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == Duration)
            {
                for (int i = 0; i < loadDurations.body.lookup.Count; ++i)
                {
                    if (loadDurations.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "DurationIndex", (UInt32)loadDurations.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == Difficulty)
            {
                for (int i = 0; i < loadDifficulties.body.lookup.Count; ++i)
                {
                    if (loadDifficulties.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "SpellDifficultyID", (UInt32)loadDifficulties.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == Range)
            {
                for (int i = 0; i < loadRanges.body.lookup.Count; ++i)
                {
                    if (loadRanges.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "RangeIndex", (UInt32)loadRanges.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == RadiusIndex1)
            {
                for (int i = 0; i < loadRadiuses.body.lookup.Count; ++i)
                {
                    if (loadRadiuses.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "EffectRadiusIndex1", (UInt32)loadRadiuses.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == RadiusIndex2)
            {
                for (int i = 0; i < loadRadiuses.body.lookup.Count; ++i)
                {
                    if (loadRadiuses.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "EffectRadiusIndex2", (UInt32)loadRadiuses.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == RadiusIndex3)
            {
                for (int i = 0; i < loadRadiuses.body.lookup.Count; ++i)
                {
                    if (loadRadiuses.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "EffectRadiusIndex3", (UInt32)loadRadiuses.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == EquippedItemClass)
            {
				UpdateItemSubClass(loadItemClasses.body.lookup[EquippedItemClass.SelectedIndex].ID);
                for (int i = 0; i < loadItemClasses.body.lookup.Count; ++i)
                {
					if (EquippedItemClass.SelectedIndex == 5 || EquippedItemClass.SelectedIndex == 3)
					{
						EquippedItemInventoryTypeGrid.IsEnabled = true;
					} 
                    else
                    {
                        EquippedItemInventoryTypeGrid.IsEnabled = false;
                    }

                    if (loadItemClasses.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "EquippedItemClass", (UInt32)loadItemClasses.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == TotemCategory1)
            {
                for (int i = 0; i < loadTotemCategories.body.lookup.Count; ++i)
                {
                    if (loadTotemCategories.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "TotemCategory1", (UInt32)loadTotemCategories.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == TotemCategory2)
            {
                for (int i = 0; i < loadTotemCategories.body.lookup.Count; ++i)
                {
                    if (loadTotemCategories.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "TotemCategory2", (UInt32)loadTotemCategories.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == RuneCost)
            {
                for (int i = 0; i < loadRuneCosts.body.lookup.Count; ++i)
                {
                    if (loadRuneCosts.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "RuneCostID", (UInt32)loadRuneCosts.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }

            if (sender == SpellDescriptionVariables)
            {
                for (int i = 0; i < loadDescriptionVariables.body.lookup.Count; ++i)
                {
                    if (loadDescriptionVariables.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        mySQL.execute(String.Format("UPDATE `{0}` SET `{1}` = '{2}' WHERE `ID` = '{3}'",
                            mySQL.Table, "SpellDescriptionVariableID", (UInt32)loadDescriptionVariables.body.lookup[i].ID, selectedID));
                        break;
                    }
                }
            }
            if (sender == Effect1) { SpellEffect1.SelectedIndex = Effect1.SelectedIndex; }
            if (sender == Effect2) { SpellEffect2.SelectedIndex = Effect2.SelectedIndex; }
            if (sender == Effect3) { SpellEffect3.SelectedIndex = Effect3.SelectedIndex; }
            if (sender == SpellEffect1) { Effect1.SelectedIndex = SpellEffect1.SelectedIndex; }
            if (sender == SpellEffect2) { Effect2.SelectedIndex = SpellEffect2.SelectedIndex; }
            if (sender == SpellEffect3) { Effect3.SelectedIndex = SpellEffect3.SelectedIndex; }
        }

		public void UpdateItemSubClass(int classId)
		{
			if (classId == -1)
			{
				Dispatcher.Invoke(DispatcherPriority.Send, TimeSpan.Zero, new Func<object>(() => EquippedItemInventoryTypeGrid.IsEnabled = false));

				foreach (ThreadSafeCheckBox box in equippedItemSubClassMaskBoxes)
				{
					box.threadSafeContent = "None";
					box.threadSafeVisibility = System.Windows.Visibility.Hidden;
					//box.threadSafeEnabled = false;
				}
				return;
			}
			else
			{
				Dispatcher.Invoke(DispatcherPriority.Send, TimeSpan.Zero, new Func<object>(() => EquippedItemSubClassGrid.IsEnabled = true));

			}
			UInt32 num = 0;
			foreach (ThreadSafeCheckBox box in equippedItemSubClassMaskBoxes)
			{
				SpellEditor.Sources.DBC.ItemSubClass.ItemSubClassLookup itemLookup = (SpellEditor.Sources.DBC.ItemSubClass.ItemSubClassLookup)loadItemSubClasses.body.lookup.GetValue(classId, num);

				if (itemLookup.Name != null)
				{
					box.threadSafeContent = itemLookup.Name;
					//box.threadSafeEnabled = true;
					box.threadSafeVisibility = System.Windows.Visibility.Visible;
				}
				else
				{
					box.threadSafeContent = "None";
					box.threadSafeVisibility = System.Windows.Visibility.Hidden;
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
    };
};
