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

namespace SpellEditor
{
    partial class MainWindow
    {
        // Begin DBCs
        private SpellDBC loadDBC = null;
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
        private TotemCategory loadTotemCategories = null;
        private SpellRuneCost loadRuneCosts = null;
        private SpellDescriptionVariables loadDescriptionVariables = null;
        // End DBCs
        
        // Begin Arrays
        private byte[] stances_values = { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20 };
        private byte[] creature_type_values = { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0x0A, 0x0B, 0x0C, 0x0D };
        // End Arrays

        // Begin Boxes
        private Dictionary<int, TextBox> stringObjectMap = new Dictionary<int, TextBox>();
        private List<CheckBox> attributes0 = new List<CheckBox>();
        private List<CheckBox> attributes1 = new List<CheckBox>();
        private List<CheckBox> attributes2 = new List<CheckBox>();
        private List<CheckBox> attributes3 = new List<CheckBox>();
        private List<CheckBox> attributes4 = new List<CheckBox>();
        private List<CheckBox> attributes5 = new List<CheckBox>();
        private List<CheckBox> attributes6 = new List<CheckBox>();
        private List<CheckBox> attributes7 = new List<CheckBox>();
        private List<CheckBox> stancesBoxes = new List<CheckBox>();
        private List<CheckBox> targetCreatureTypeBoxes = new List<CheckBox>();
        private List<CheckBox> targetBoxes = new List<CheckBox>();
        private List<CheckBox> procBoxes = new List<CheckBox>();
        private List<CheckBox> interrupts1 = new List<CheckBox>();
        private List<CheckBox> interrupts2 = new List<CheckBox>();
        private List<CheckBox> interrupts3 = new List<CheckBox>();
        public List<CheckBox> equippedItemInventoryTypeMaskBoxes = new List<CheckBox>();
        // End Boxes

        // Begin Other
        public UInt32 selectedID = 1;
        public UInt32 newIconID = 1;
        private Boolean updating;
        public TaskScheduler UIScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        // End Other

        public MainWindow() { InitializeComponent(); }

        public async void HandleErrorMessage(string msg) { await this.ShowMessageAsync("Spell Editor", msg); }

        private void _Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists("DBC/Spell.dbc"))
                {
                    HandleErrorMessage("Failed to load Spell.dbc!");

                    Environment.Exit(0x1);

                    return;
                }

                loadDBC = new SpellDBC();

                if (!loadDBC.LoadDBCFile(this))
                {
                    HandleErrorMessage("Failed to load Spell.dbc!");

                    Environment.Exit(0x1);

                    return;
                }

                PopulateSelectSpell();

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
                    CheckBox box = new CheckBox();

                    box.Content = att_flags[i];
                    box.Margin = new Thickness(5, (-15.5 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    Attributes1.Children.Add(box);
                    attributes0.Add(box);
                }

                att_flags = new string[] { "Dismiss Pet", "Drains All Power", "Channeled 1", "Cannot be Redirected", "Unknown 4", "Does not Break Stealth", "Channeled 2", "Cannot be Reflected", "Cannot Target in Combat", "Melee Combat Start", "Generates No Threat", "Unknown 11", "Pickpocket", "Far Sight", "Track Target while Channeling", "Remove Auras on Immunity", "Unaffected by School Immune", "Unautoscalable by Pet", "Stun, Polymorph, Daze, Hex", "Cannot Target Self", "Requires Combo Points on Target 1", "Unknown 21", "Required Combo Points on Target 2", "Unknown 23", "Fishing", "Unknown 25", "Focus Targeting Macro", "Unknown 27", "Hidden in Aura Bar", "Channel Display Name", "Enable Spell when Dodged", "Unknown 31" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    CheckBox box = new CheckBox();

                    box.Content = att_flags[i];
                    box.Margin = new Thickness(5, (-15.5 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    Attributes2.Children.Add(box);
                    attributes1.Add(box);
                }

                att_flags = new string[] { "Can Target Dead Unit or Corpse", "Vanish, Shadowform, Ghost", "Can Target not in Line of Sight", "Unknown 3", "Display in Stance Bar", "Autorepeat Flag", "Requires Untapped Target", "Unknown 7", "Unknown 8", "Unknown 9", "Unknown 10 (Tame)", "Health Funnel", "Cleave, Heart Strike, Maul, Sunder Armor, Swipe", "Preserve Enchant in Arena", "Unknown 14", "Unknown 15", "Tame Beast", "Don't Reset Auto Actions", "Requires Dead Pet", "Don't Need Shapeshift", "Unknown 20", "Damage Reduced Shield", "Ambush, Backstab, Cheap Shot, Death Grip, Garrote, Judgements, Mutilate, Pounce, Ravage, Shiv, Shred", "Arcane Concentration", "Unknown 24", "Unknown 25", "Unaffected by School Immunity", "Requires Fishing Pole", "Unknown 28", "Cannot Crit", "Triggered can Trigger Proc", "Food Buff" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    CheckBox box = new CheckBox();

                    box.Content = att_flags[i];
                    box.Margin = new Thickness(5, (-15.5 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    Attributes3.Children.Add(box);
                    attributes2.Add(box);
                }

                att_flags = new string[] { "Unknown 0", "Unknown 1", "Unknown 2", "Blockable Spell", "Ignore Resurrection Timer", "Unknown 5", "Unknown 6", "Stack for Different Casters", "Only Target Players", "Triggered can Trigger Proc 2", "Requires Main-Hand", "Battleground Only", "Only Target Ghosts", "Hide Channel Bar", "Honorless Target", "Auto-Shoot", "Cannot Trigger Proc", "No Initial Aggro", "Cannot Miss", "Disable Procs", "Death Persistent", "Unknown 21", "Requires Wands", "Unknown 23", "Requires Off-Hand", "Can Proc with Triggered", "Drain Soul", "Unknown 28", "No Done Bonus", "Do not Display Range", "Unknown 31" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    CheckBox box = new CheckBox();

                    box.Content = att_flags[i];
                    box.Margin = new Thickness(5, (-15.5 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    Attributes4.Children.Add(box);
                    attributes3.Add(box);
                }

                att_flags = new string[] { "Ignore All Reistances", "Proc Only on Caster", "Continue to Tick while Offline", "Unknown 3", "Unknown 4", "Unknown 5", "Not Stealable", "Triggered", "Fixed Damage", "Activate from Event", "Spell vs Extended Cost", "Unknown 11", "Unknown 12", "Unknown 13", "Damage doesn't Break Auras", "Unknown 15", "Not Usable in Arena", "Usable in Arena", "Area Target Chain", "Unknown 19", "Don't Check Selfcast Power", "Unknown 21", "Unknown 22", "Unknown 23", "Unknown 24", "Pet Scaling", "Can Only be Casted in Outland", "Unknown 27", "Aimed Shot", "Unknown 29", "Unknown 30", "Polymorph" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    CheckBox box = new CheckBox();

                    box.Content = att_flags[i];
                    box.Margin = new Thickness(5, (-15.5 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    Attributes5.Children.Add(box);
                    attributes4.Add(box);
                }

                att_flags = new string[] { "Unknown 0", "No Reagent While Preparation", "Unknown 2", "Usable while Stunned", "Unknown 4", "Single-Target Spell", "Unknown 6", "Unknown 7", "Unknown 8", "Start Periodic at Aura Apply", "Hide Duration", "Allow Target of Target as Target", "Cleave", "Haste Affect Duration", "Unknown 14", "Inflict on Multiple Targets", "Special Item Class Check", "Usable while Feared", "Usable feared Confused", "Don't Turn during Casting", "Unknown 20", "Unknown 21", "Unknown 22", "Unknown 23", "Unknown 24", "Unknown 25", "Unknown 26", "Don't Show Aura if Self-Cast", "Don't Show Aura if Not Self-Cast", "Unknown 29", "Unknown 30", "AoE Taunt" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    CheckBox box = new CheckBox();

                    box.Content = att_flags[i];
                    box.Margin = new Thickness(5, (-15.5 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    Attributes6.Children.Add(box);
                    attributes5.Add(box);
                }

                att_flags = new string[] { "Don't Display Cooldown", "Only in Arena", "Ignore Caster Auras", "Assist Ignore Immune Flag", "Unknown 4", "Unknown 5", "Spell Cast Event", "Unknown 7", "Can't Target Crowd-Controlled", "Unknown 9", "Can Target Possessed Friends", "Not in Raid Instance", "Castable while on Vehicle", "Can Target Invisible", "Unknown 14", "Unknown 15", "Unknown 16", "Mount", "Cast by Charmer", "Unknown 19", "Only Visible to Caster", "Client UI Target Effects", "Unknown 22", "Unknown 23", "Can Target Untargetable", "Exorcism, Flash of Light", "Unknown 26", "Unknown 27", "Death Grip", "Not Done Percent Damage Mods", "Unknown 30", "Ignore Category Cooldown Mods" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    CheckBox box = new CheckBox();

                    box.Content = att_flags[i];
                    box.Margin = new Thickness(5, (-15.5 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    Attributes7.Children.Add(box);
                    attributes6.Add(box);
                }

                att_flags = new string[] { "Feign Death", "Unknown 1", "Re-Activate at Resurrect", "Cheat Spell", "Soulstone Resurrection", "Totem", "No Pushback on Damage", "Unknown 7", "Horde Only", "Alliance Only", "Dispel Charges", "Interrupt only Non-Player", "Unknown 12", "Unknown 13", "Raise Dead", "Unknown 15", "Restore Secondary Power", "Unknown 17", "Charge", "Zone Teleport", "Blink, Divine Shield, Ice Block", "Unknown 21", "Unknown 22", "Unknown 23", "Unknown 24", "Unknown 25", "Unknown 26", "Unknown 27", "Consolidated Raid Buff", "Unknown 29", "Unknown 30", "Client Indicator" };

                for (int i = 0; i < att_flags.Length; ++i)
                {
                    CheckBox box = new CheckBox();

                    box.Content = att_flags[i];
                    box.Margin = new Thickness(5, (-15.5 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    Attributes8.Children.Add(box);
                    attributes7.Add(box);
                }

                string[] stances_strings = { "None", "Cat", "Tree", "Travel", "Aqua", "Bear", "Ambient", "Ghoul", "Dire Bear", "Steves Ghoul", "Tharonja Skeleton", "Test of Strength", "BLB Player", "Shadow Dance", "Creature Bear", "Creature Cat", "Ghost Wolf", "Battle Stance", "Defensive Stance", "Berserker Stance", "Test", "Zombie", "Metamorphosis", "Undead", "Master Angler", "Flight (Epic)", "Shadow", "Flight (Normal)", "Stealth", "Moonkin", "Spirit of Redemption" };

                for (int i = 0; i < stances_strings.Length; ++i)
                {
                    CheckBox box = new CheckBox();

                    box.Content = stances_strings[i];
                    box.Margin = new Thickness(5, (-15 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    StancesGrid.Children.Add(box);
                    stancesBoxes.Add(box);
                }

                string[] creature_type_strings = { "None", "Beast", "Dragonkin", "Demon", "Elemental", "Giant", "Undead", "Humanoid", "Critter", "Mechanical", "Not specified", "Totem", "Non-combat Pet", "Gas Cloud" };

                for (int i = 0; i < creature_type_strings.Length; ++i)
                {
                    CheckBox box = new CheckBox();

                    box.Content = creature_type_strings[i];
                    box.Margin = new Thickness(5, (-6.5 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

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
                    CheckBox box = new CheckBox();

                    box.Content = equipped_item_inventory_type_mask_strings[i];
                    box.Margin = new Thickness(5, (-13.9 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    EquippedItemInventoryTypeGrid.Children.Add(box);
                    equippedItemInventoryTypeMaskBoxes.Add(box);
                }

                string[] school_strings = { "Mana", "Rage", "Focus", "Energy", "Happiness", "Runes", "Runic Power", "Steam", "Pyrite", "Heat", "Ooze", "Blood", "Wrath" };

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
                    CheckBox box = new CheckBox();

                    box.Content = target_strings[i];
                    box.Margin = new Thickness(5, (-10.5 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    TargetEditorGrid.Children.Add(box);
                    targetBoxes.Add(box);
                }

                string[] proc_strings = { "None", "Any Hostile Action", "On Gain Experience", "On Melee Attack", "On Crit Hit Victim", "On Cast Spell", "On Physical Attack Victim", "On Ranged Attack", "On Ranged Crit Attack", "On Physical Attack", "On Melee Attack Victim", "On Spell Hit", "On Ranged Crit Attack Victim", "On Crit Attack", "On Ranged Attack Victim", "On Pre Dispell Aura Victim", "On Spell Land Victim", "On Cast Specific Spell", "On Spell Hit Victim", "On Spell Crit Hit Victim", "On Target Death", "On Any Damage Victim", "On Trap Trigger", "On Auto Shot Hit", "On Absorb", "On Resist Victim", "On Dodge Victim", "On Death", "Remove On Use", "Misc", "On Block Victim", "On Spell Crit Hit" };

                for (int i = 0; i < proc_strings.Length; ++i)
                {
                    CheckBox box = new CheckBox();

                    box.Content = proc_strings[i];
                    box.Margin = new Thickness(5, (-15.5 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    ProcEditorGrid.Children.Add(box);
                    procBoxes.Add(box);
                }

                string[] spell_aura_effect_names = { "SPELL_AURA_NONE", "SPELL_AURA_BIND_SIGHT", "SPELL_AURA_MOD_POSSESS", "SPELL_AURA_PERIODIC_DAMAGE", "SPELL_AURA_DUMMY", "SPELL_AURA_MOD_CONFUSE", "SPELL_AURA_MOD_CHARM", "SPELL_AURA_MOD_FEAR", "SPELL_AURA_PERIODIC_HEAL", "SPELL_AURA_MOD_ATTACKSPEED", "SPELL_AURA_MOD_THREAT", "SPELL_AURA_MOD_TAUNT", "SPELL_AURA_MOD_STUN", "SPELL_AURA_MOD_DAMAGE_DONE", "SPELL_AURA_MOD_DAMAGE_TAKEN", "SPELL_AURA_DAMAGE_SHIELD", "SPELL_AURA_MOD_STEALTH", "SPELL_AURA_MOD_STEALTH_DETECT", "SPELL_AURA_MOD_INVISIBILITY", "SPELL_AURA_MOD_INVISIBILITY_DETECT", "SPELL_AURA_OBS_MOD_HEALTH", "SPELL_AURA_OBS_MOD_POWER", "SPELL_AURA_MOD_RESISTANCE", "SPELL_AURA_PERIODIC_TRIGGER_SPELL", "SPELL_AURA_PERIODIC_ENERGIZE", "SPELL_AURA_MOD_PACIFY", "SPELL_AURA_MOD_ROOT", "SPELL_AURA_MOD_SILENCE", "SPELL_AURA_REFLECT_SPELLS", "SPELL_AURA_MOD_STAT", "SPELL_AURA_MOD_SKILL", "SPELL_AURA_MOD_INCREASE_SPEED", "SPELL_AURA_MOD_INCREASE_MOUNTED_SPEED", "SPELL_AURA_MOD_DECREASE_SPEED", "SPELL_AURA_MOD_INCREASE_HEALTH", "SPELL_AURA_MOD_INCREASE_ENERGY", "SPELL_AURA_MOD_SHAPESHIFT", "SPELL_AURA_EFFECT_IMMUNITY", "SPELL_AURA_STATE_IMMUNITY", "SPELL_AURA_SCHOOL_IMMUNITY", "SPELL_AURA_DAMAGE_IMMUNITY", "SPELL_AURA_DISPEL_IMMUNITY", "SPELL_AURA_PROC_TRIGGER_SPELL", "SPELL_AURA_PROC_TRIGGER_DAMAGE", "SPELL_AURA_TRACK_CREATURES", "SPELL_AURA_TRACK_RESOURCES", "SPELL_AURA_46", "SPELL_AURA_MOD_PARRY_PERCENT", "SPELL_AURA_48", "SPELL_AURA_MOD_DODGE_PERCENT", "SPELL_AURA_MOD_CRITICAL_HEALING_AMOUNT", "SPELL_AURA_MOD_BLOCK_PERCENT", "SPELL_AURA_MOD_WEAPON_CRIT_PERCENT", "SPELL_AURA_PERIODIC_LEECH", "SPELL_AURA_MOD_HIT_CHANCE", "SPELL_AURA_MOD_SPELL_HIT_CHANCE", "SPELL_AURA_TRANSFORM", "SPELL_AURA_MOD_SPELL_CRIT_CHANCE", "SPELL_AURA_MOD_INCREASE_SWIM_SPEED", "SPELL_AURA_MOD_DAMAGE_DONE_CREATURE", "SPELL_AURA_MOD_PACIFY_SILENCE", "SPELL_AURA_MOD_SCALE", "SPELL_AURA_PERIODIC_HEALTH_FUNNEL", "SPELL_AURA_63", "SPELL_AURA_PERIODIC_MANA_LEECH", "SPELL_AURA_MOD_CASTING_SPEED_NOT_STACK", "SPELL_AURA_FEIGN_DEATH", "SPELL_AURA_MOD_DISARM", "SPELL_AURA_MOD_STALKED", "SPELL_AURA_SCHOOL_ABSORB", "SPELL_AURA_EXTRA_ATTACKS", "SPELL_AURA_MOD_SPELL_CRIT_CHANCE_SCHOOL", "SPELL_AURA_MOD_POWER_COST_SCHOOL_PCT", "SPELL_AURA_MOD_POWER_COST_SCHOOL", "SPELL_AURA_REFLECT_SPELLS_SCHOOL", "SPELL_AURA_MOD_LANGUAGE", "SPELL_AURA_FAR_SIGHT", "SPELL_AURA_MECHANIC_IMMUNITY", "SPELL_AURA_MOUNTED", "SPELL_AURA_MOD_DAMAGE_PERCENT_DONE", "SPELL_AURA_MOD_PERCENT_STAT", "SPELL_AURA_SPLIT_DAMAGE_PCT", "SPELL_AURA_WATER_BREATHING", "SPELL_AURA_MOD_BASE_RESISTANCE", "SPELL_AURA_MOD_REGEN", "SPELL_AURA_MOD_POWER_REGEN", "SPELL_AURA_CHANNEL_DEATH_ITEM", "SPELL_AURA_MOD_DAMAGE_PERCENT_TAKEN", "SPELL_AURA_MOD_HEALTH_REGEN_PERCENT", "SPELL_AURA_PERIODIC_DAMAGE_PERCENT", "SPELL_AURA_90", "SPELL_AURA_MOD_DETECT_RANGE", "SPELL_AURA_PREVENTS_FLEEING", "SPELL_AURA_MOD_UNATTACKABLE", "SPELL_AURA_INTERRUPT_REGEN", "SPELL_AURA_GHOST", "SPELL_AURA_SPELL_MAGNET", "SPELL_AURA_MANA_SHIELD", "SPELL_AURA_MOD_SKILL_TALENT", "SPELL_AURA_MOD_ATTACK_POWER", "SPELL_AURA_AURAS_VISIBLE", "SPELL_AURA_MOD_RESISTANCE_PCT", "SPELL_AURA_MOD_MELEE_ATTACK_POWER_VERSUS", "SPELL_AURA_MOD_TOTAL_THREAT", "SPELL_AURA_WATER_WALK", "SPELL_AURA_FEATHER_FALL", "SPELL_AURA_HOVER", "SPELL_AURA_ADD_FLAT_MODIFIER", "SPELL_AURA_ADD_PCT_MODIFIER", "SPELL_AURA_ADD_TARGET_TRIGGER", "SPELL_AURA_MOD_POWER_REGEN_PERCENT", "SPELL_AURA_ADD_CASTER_HIT_TRIGGER", "SPELL_AURA_OVERRIDE_CLASS_SCRIPTS", "SPELL_AURA_MOD_RANGED_DAMAGE_TAKEN", "SPELL_AURA_MOD_RANGED_DAMAGE_TAKEN_PCT", "SPELL_AURA_MOD_HEALING", "SPELL_AURA_MOD_REGEN_DURING_COMBAT", "SPELL_AURA_MOD_MECHANIC_RESISTANCE", "SPELL_AURA_MOD_HEALING_PCT", "SPELL_AURA_119", "SPELL_AURA_UNTRACKABLE", "SPELL_AURA_EMPATHY", "SPELL_AURA_MOD_OFFHAND_DAMAGE_PCT", "SPELL_AURA_MOD_TARGET_RESISTANCE", "SPELL_AURA_MOD_RANGED_ATTACK_POWER", "SPELL_AURA_MOD_MELEE_DAMAGE_TAKEN", "SPELL_AURA_MOD_MELEE_DAMAGE_TAKEN_PCT", "SPELL_AURA_RANGED_ATTACK_POWER_ATTACKER_BONUS", "SPELL_AURA_MOD_POSSESS_PET", "SPELL_AURA_MOD_SPEED_ALWAYS", "SPELL_AURA_MOD_MOUNTED_SPEED_ALWAYS", "SPELL_AURA_MOD_RANGED_ATTACK_POWER_VERSUS", "SPELL_AURA_MOD_INCREASE_ENERGY_PERCENT", "SPELL_AURA_MOD_INCREASE_HEALTH_PERCENT", "SPELL_AURA_MOD_MANA_REGEN_INTERRUPT", "SPELL_AURA_MOD_HEALING_DONE", "SPELL_AURA_MOD_HEALING_DONE_PERCENT", "SPELL_AURA_MOD_TOTAL_STAT_PERCENTAGE", "SPELL_AURA_MOD_MELEE_HASTE", "SPELL_AURA_FORCE_REACTION", "SPELL_AURA_MOD_RANGED_HASTE", "SPELL_AURA_MOD_RANGED_AMMO_HASTE", "SPELL_AURA_MOD_BASE_RESISTANCE_PCT", "SPELL_AURA_MOD_RESISTANCE_EXCLUSIVE", "SPELL_AURA_SAFE_FALL", "SPELL_AURA_MOD_PET_TALENT_POINTS", "SPELL_AURA_ALLOW_TAME_PET_TYPE", "SPELL_AURA_MECHANIC_IMMUNITY_MASK", "SPELL_AURA_RETAIN_COMBO_POINTS", "SPELL_AURA_REDUCE_PUSHBACK", "SPELL_AURA_MOD_SHIELD_BLOCKVALUE_PCT", "SPELL_AURA_TRACK_STEALTHED", "SPELL_AURA_MOD_DETECTED_RANGE", "SPELL_AURA_SPLIT_DAMAGE_FLAT", "SPELL_AURA_MOD_STEALTH_LEVEL", "SPELL_AURA_MOD_WATER_BREATHING", "SPELL_AURA_MOD_REPUTATION_GAIN", "SPELL_AURA_PET_DAMAGE_MULTI", "SPELL_AURA_MOD_SHIELD_BLOCKVALUE", "SPELL_AURA_NO_PVP_CREDIT", "SPELL_AURA_MOD_AOE_AVOIDANCE", "SPELL_AURA_MOD_HEALTH_REGEN_IN_COMBAT", "SPELL_AURA_POWER_BURN", "SPELL_AURA_MOD_CRIT_DAMAGE_BONUS", "SPELL_AURA_164", "SPELL_AURA_MELEE_ATTACK_POWER_ATTACKER_BONUS", "SPELL_AURA_MOD_ATTACK_POWER_PCT", "SPELL_AURA_MOD_RANGED_ATTACK_POWER_PCT", "SPELL_AURA_MOD_DAMAGE_DONE_VERSUS", "SPELL_AURA_MOD_CRIT_PERCENT_VERSUS", "SPELL_AURA_DETECT_AMORE", "SPELL_AURA_MOD_SPEED_NOT_STACK", "SPELL_AURA_MOD_MOUNTED_SPEED_NOT_STACK", "SPELL_AURA_173", "SPELL_AURA_MOD_SPELL_DAMAGE_OF_STAT_PERCENT", "SPELL_AURA_MOD_SPELL_HEALING_OF_STAT_PERCENT", "SPELL_AURA_SPIRIT_OF_REDEMPTION", "SPELL_AURA_AOE_CHARM", "SPELL_AURA_MOD_DEBUFF_RESISTANCE", "SPELL_AURA_MOD_ATTACKER_SPELL_CRIT_CHANCE", "SPELL_AURA_MOD_FLAT_SPELL_DAMAGE_VERSUS", "SPELL_AURA_181", "SPELL_AURA_MOD_RESISTANCE_OF_STAT_PERCENT", "SPELL_AURA_MOD_CRITICAL_THREAT", "SPELL_AURA_MOD_ATTACKER_MELEE_HIT_CHANCE", "SPELL_AURA_MOD_ATTACKER_RANGED_HIT_CHANCE", "SPELL_AURA_MOD_ATTACKER_SPELL_HIT_CHANCE", "SPELL_AURA_MOD_ATTACKER_MELEE_CRIT_CHANCE", "SPELL_AURA_MOD_ATTACKER_RANGED_CRIT_CHANCE", "SPELL_AURA_MOD_RATING", "SPELL_AURA_MOD_FACTION_REPUTATION_GAIN", "SPELL_AURA_USE_NORMAL_MOVEMENT_SPEED", "SPELL_AURA_MOD_MELEE_RANGED_HASTE", "SPELL_AURA_MELEE_SLOW", "SPELL_AURA_MOD_TARGET_ABSORB_SCHOOL", "SPELL_AURA_MOD_TARGET_ABILITY_ABSORB_SCHOOL", "SPELL_AURA_MOD_COOLDOWN", "SPELL_AURA_MOD_ATTACKER_SPELL_AND_WEAPON_CRIT_CHANCE", "SPELL_AURA_198", "SPELL_AURA_MOD_INCREASES_SPELL_PCT_TO_HIT", "SPELL_AURA_MOD_XP_PCT", "SPELL_AURA_FLY", "SPELL_AURA_IGNORE_COMBAT_RESULT", "SPELL_AURA_MOD_ATTACKER_MELEE_CRIT_DAMAGE", "SPELL_AURA_MOD_ATTACKER_RANGED_CRIT_DAMAGE", "SPELL_AURA_MOD_SCHOOL_CRIT_DMG_TAKEN", "SPELL_AURA_MOD_INCREASE_VEHICLE_FLIGHT_SPEED", "SPELL_AURA_MOD_INCREASE_MOUNTED_FLIGHT_SPEED", "SPELL_AURA_MOD_INCREASE_FLIGHT_SPEED", "SPELL_AURA_MOD_MOUNTED_FLIGHT_SPEED_ALWAYS", "SPELL_AURA_MOD_VEHICLE_SPEED_ALWAYS", "SPELL_AURA_MOD_FLIGHT_SPEED_NOT_STACK", "SPELL_AURA_MOD_RANGED_ATTACK_POWER_OF_STAT_PERCENT", "SPELL_AURA_MOD_RAGE_FROM_DAMAGE_DEALT", "SPELL_AURA_214", "SPELL_AURA_ARENA_PREPARATION", "SPELL_AURA_HASTE_SPELLS", "SPELL_AURA_MOD_MELEE_HASTE_2", "SPELL_AURA_HASTE_RANGED", "SPELL_AURA_MOD_MANA_REGEN_FROM_STAT", "SPELL_AURA_MOD_RATING_FROM_STAT", "SPELL_AURA_MOD_DETAUNT", "SPELL_AURA_222", "SPELL_AURA_RAID_PROC_FROM_CHARGE", "SPELL_AURA_224", "SPELL_AURA_RAID_PROC_FROM_CHARGE_WITH_VALUE", "SPELL_AURA_PERIODIC_DUMMY", "SPELL_AURA_PERIODIC_TRIGGER_SPELL_WITH_VALUE", "SPELL_AURA_DETECT_STEALTH", "SPELL_AURA_MOD_AOE_DAMAGE_AVOIDANCE", "SPELL_AURA_230", "SPELL_AURA_PROC_TRIGGER_SPELL_WITH_VALUE", "SPELL_AURA_MECHANIC_DURATION_MOD", "SPELL_AURA_CHANGE_MODEL_FOR_ALL_HUMANOIDS", "SPELL_AURA_MECHANIC_DURATION_MOD_NOT_STACK", "SPELL_AURA_MOD_DISPEL_RESIST", "SPELL_AURA_CONTROL_VEHICLE", "SPELL_AURA_MOD_SPELL_DAMAGE_OF_ATTACK_POWER", "SPELL_AURA_MOD_SPELL_HEALING_OF_ATTACK_POWER", "SPELL_AURA_MOD_SCALE_2", "SPELL_AURA_MOD_EXPERTISE", "SPELL_AURA_FORCE_MOVE_FORWARD", "SPELL_AURA_MOD_SPELL_DAMAGE_FROM_HEALING", "SPELL_AURA_MOD_FACTION", "SPELL_AURA_COMPREHEND_LANGUAGE", "SPELL_AURA_MOD_AURA_DURATION_BY_DISPEL", "SPELL_AURA_MOD_AURA_DURATION_BY_DISPEL_NOT_STACK", "SPELL_AURA_CLONE_CASTER", "SPELL_AURA_MOD_COMBAT_RESULT_CHANCE", "SPELL_AURA_CONVERT_RUNE", "SPELL_AURA_MOD_INCREASE_HEALTH_2", "SPELL_AURA_MOD_ENEMY_DODGE", "SPELL_AURA_MOD_SPEED_SLOW_ALL", "SPELL_AURA_MOD_BLOCK_CRIT_CHANCE", "SPELL_AURA_MOD_DISARM_OFFHAND", "SPELL_AURA_MOD_MECHANIC_DAMAGE_TAKEN_PERCENT", "SPELL_AURA_NO_REAGENT_USE", "SPELL_AURA_MOD_TARGET_RESIST_BY_SPELL_CLASS", "SPELL_AURA_258", "SPELL_AURA_MOD_HOT_PCT", "SPELL_AURA_SCREEN_EFFECT", "SPELL_AURA_PHASE", "SPELL_AURA_ABILITY_IGNORE_AURASTATE", "SPELL_AURA_ALLOW_ONLY_ABILITY", "SPELL_AURA_264", "SPELL_AURA_265", "SPELL_AURA_266", "SPELL_AURA_MOD_IMMUNE_AURA_APPLY_SCHOOL", "SPELL_AURA_MOD_ATTACK_POWER_OF_STAT_PERCENT", "SPELL_AURA_MOD_IGNORE_TARGET_RESIST", "SPELL_AURA_MOD_ABILITY_IGNORE_TARGET_RESIST", "SPELL_AURA_MOD_DAMAGE_FROM_CASTER", "SPELL_AURA_IGNORE_MELEE_RESET", "SPELL_AURA_X_RAY", "SPELL_AURA_ABILITY_CONSUME_NO_AMMO", "SPELL_AURA_MOD_IGNORE_SHAPESHIFT", "SPELL_AURA_MOD_DAMAGE_DONE_FOR_MECHANIC", "SPELL_AURA_MOD_MAX_AFFECTED_TARGETS", "SPELL_AURA_MOD_DISARM_RANGED", "SPELL_AURA_INITIALIZE_IMAGES", "SPELL_AURA_MOD_ARMOR_PENETRATION_PCT", "SPELL_AURA_MOD_HONOR_GAIN_PCT", "SPELL_AURA_MOD_BASE_HEALTH_PCT", "SPELL_AURA_MOD_HEALING_RECEIVED", "SPELL_AURA_LINKED", "SPELL_AURA_MOD_ATTACK_POWER_OF_ARMOR", "SPELL_AURA_ABILITY_PERIODIC_CRIT", "SPELL_AURA_DEFLECT_SPELLS", "SPELL_AURA_IGNORE_HIT_DIRECTION", "SPELL_AURA_289", "SPELL_AURA_MOD_CRIT_PCT", "SPELL_AURA_MOD_XP_QUEST_PCT", "SPELL_AURA_OPEN_STABLE", "SPELL_AURA_OVERRIDE_SPELLS", "SPELL_AURA_PREVENT_REGENERATE_POWER", "SPELL_AURA_295", "SPELL_AURA_SET_VEHICLE_ID", "SPELL_AURA_BLOCK_SPELL_FAMILY", "SPELL_AURA_STRANGULATE", "SPELL_AURA_299", "SPELL_AURA_SHARE_DAMAGE_PCT", "SPELL_AURA_SCHOOL_HEAL_ABSORB", "SPELL_AURA_302", "SPELL_AURA_MOD_DAMAGE_DONE_VERSUS_AURASTATE", "SPELL_AURA_MOD_FAKE_INEBRIATE", "SPELL_AURA_MOD_MINIMUM_SPEED", "SPELL_AURA_306", "SPELL_AURA_HEAL_ABSORB_TEST", "SPELL_AURA_MOD_CRIT_CHANCE_FOR_CASTER", "SPELL_AURA_309", "SPELL_AURA_MOD_CREATURE_AOE_DAMAGE_AVOIDANCE", "SPELL_AURA_311", "SPELL_AURA_312", "SPELL_AURA_313", "SPELL_AURA_PREVENT_RESURRECTION", "SPELL_AURA_UNDERWATER_WALKING", "SPELL_AURA_PERIODIC_HASTE" };

                for (int i = 0; i < spell_aura_effect_names.Length; ++i)
                {
                    ApplyAuraName1.Items.Add(spell_aura_effect_names[i]);
                    ApplyAuraName2.Items.Add(spell_aura_effect_names[i]);
                    ApplyAuraName3.Items.Add(spell_aura_effect_names[i]);
                }

                string[] spell_effect_names = { "NULL", "INSTANT_KILL", "SCHOOL_DAMAGE", "DUMMY", "PORTAL_TELEPORT", "TELEPORT_UNITS", "APPLY_AURA", "ENVIRONMENTAL_DAMAGE", "POWER_DRAIN", "HEALTH_LEECH", "HEAL", "BIND", "PORTAL", "RITUAL_BASE", "RITUAL_SPECIALIZE", "RITUAL_ACTIVATE_PORTAL", "QUEST_COMPLETE", "WEAPON_DAMAGE_NOSCHOOL", "RESURRECT", "ADD_EXTRA_ATTACKS", "DODGE", "EVADE", "PARRY", "BLOCK", "CREATE_ITEM", "WEAPON", "DEFENSE", "PERSISTENT_AREA_AURA", "SUMMON", "LEAP", "ENERGIZE", "WEAPON_PERCENT_DAMAGE", "TRIGGER_MISSILE", "OPEN_LOCK", "TRANSFORM_ITEM", "APPLY_GROUP_AREA_AURA", "LEARN_SPELL", "SPELL_DEFENSE", "DISPEL", "LANGUAGE", "DUAL_WIELD", "LEAP_41", "SUMMON_GUARDIAN", "TELEPORT_UNITS_FACE_CASTER", "SKILL_STEP", "UNDEFINED_45", "SPAWN", "TRADE_SKILL", "STEALTH", "DETECT", "SUMMON_OBJECT", "FORCE_CRITICAL_HIT", "GUARANTEE_HIT", "ENCHANT_ITEM", "ENCHANT_ITEM_TEMPORARY", "TAMECREATURE", "SUMMON_PET", "LEARN_PET_SPELL", "WEAPON_DAMAGE", "OPEN_LOCK_ITEM", "PROFICIENCY", "SEND_EVENT", "POWER_BURN", "THREAT", "TRIGGER_SPELL", "APPLY_RAID_AREA_AURA", "POWER_FUNNEL", "HEAL_MAX_HEALTH", "INTERRUPT_CAST", "DISTRACT", "PULL", "PICKPOCKET", "ADD_FARSIGHT", "UNTRAIN_TALENTS", "USE_GLYPH", "HEAL_MECHANICAL", "SUMMON_OBJECT_WILD", "SCRIPT_EFFECT", "ATTACK", "SANCTUARY", "ADD_COMBO_POINTS", "CREATE_HOUSE", "BIND_SIGHT", "DUEL", "STUCK", "SUMMON_PLAYER", "ACTIVATE_OBJECT", "BUILDING_DAMAGE", "BUILDING_REPAIR", "BUILDING_SWITCH_STATE", "KILL_CREDIT_90", "THREAT_ALL", "ENCHANT_HELD_ITEM", "SUMMON_PHANTASM", "SELF_RESURRECT", "SKINNING", "CHARGE", "SUMMON_MULTIPLE_TOTEMS", "KNOCK_BACK", "DISENCHANT", "INEBRIATE", "FEED_PET", "DISMISS_PET", "REPUTATION", "SUMMON_OBJECT_SLOT1", "SUMMON_OBJECT_SLOT2", "SUMMON_OBJECT_SLOT3", "SUMMON_OBJECT_SLOT4", "DISPEL_MECHANIC", "SUMMON_DEAD_PET", "DESTROY_ALL_TOTEMS", "DURABILITY_DAMAGE", "NONE_112", "RESURRECT_FLAT", "ATTACK_ME", "DURABILITY_DAMAGE_PCT", "SKIN_PLAYER_CORPSE", "SPIRIT_HEAL", "SKILL", "APPLY_PET_AREA_AURA", "TELEPORT_GRAVEYARD", "DUMMYMELEE", "UNKNOWN1", "START_TAXI", "PLAYER_PULL", "UNKNOWN4", "UNKNOWN5", "PROSPECTING", "APPLY_FRIEND_AREA_AURA", "APPLY_ENEMY_AREA_AURA", "UNKNOWN10", "UNKNOWN11", "PLAY_MUSIC", "FORGET_SPECIALIZATION", "KILL_CREDIT", "UNKNOWN15", "UNKNOWN16", "UNKNOWN17", "UNKNOWN18", "CLEAR_QUEST", "UNKNOWN20", "UNKNOWN21", "TRIGGER_SPELL_WITH_VALUE", "APPLY_OWNER_AREA_AURA", "UNKNOWN23", "UNKNOWN24", "ACTIVATE_RUNES", "UNKNOWN26", "UNKNOWN27", "QUEST_FAIL", "UNKNOWN28", "UNKNOWN29", "UNKNOWN30", "SUMMON_TARGET", "SUMMON_REFER_A_FRIEND", "TAME_CREATURE", "ADD_SOCKET", "CREATE_ITEM2", "MILLING", "UNKNOWN37", "UNKNOWN38", "LEARN_SPEC", "ACTIVATE_SPEC", "UNKNOWN" };

                for (int i = 0; i < spell_effect_names.Length; ++i)
                {
                    SpellEffect1.Items.Add(spell_effect_names[i]);
                    SpellEffect2.Items.Add(spell_effect_names[i]);
                    SpellEffect3.Items.Add(spell_effect_names[i]);
                }

                string[] mechanic_names = { "None", "Charmed", "Disoriented", "Disarmed", "Distracted", "Fleeing", "Clumsy", "Rooted", "Pacified", "Silenced", "Asleep", "Ensnared", "Stunned", "Frozen", "Incapacipated", "Bleeding", "Healing", "Polymorphed", "Banished", "Shielded", "Shackled", "Mounted", "Seduced", "Turned", "Horrified", "Invulnarable", "Interrupted", "Dazed", "Discovery", "Invulnerable", "Sapped", "Enraged" };

                for (int i = 0; i < mechanic_names.Length; ++i)
                {
                    Mechanic1.Items.Add(mechanic_names[i]);
                    Mechanic2.Items.Add(mechanic_names[i]);
                    Mechanic3.Items.Add(mechanic_names[i]);
                }

                foreach (Targets t in Enum.GetValues(typeof(Targets)))
                {
                    TargetA1.Items.Add(t);
                    TargetB1.Items.Add(t);
                    TargetA2.Items.Add(t);
                    TargetB2.Items.Add(t);
                    TargetA3.Items.Add(t);
                    TargetB3.Items.Add(t);

                    ChainTarget1.Items.Add(t);
                    ChainTarget2.Items.Add(t);
                    ChainTarget3.Items.Add(t);
                }

                string[] interrupt_strings = { "None", "On Movement", "On Knockback", "On Interrupt Casting", "On Interrupt School", "On Damage Taken", "On Interrupt All" };

                for (int i = 0; i < interrupt_strings.Length; ++i)
                {
                    CheckBox box = new CheckBox();

                    box.Content = interrupt_strings[i];
                    box.Margin = new Thickness(5, (-2.2 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    InterruptFlagsGrid.Children.Add(box);
                    interrupts1.Add(box);
                }

                string[] aura_interrupt_strings = { "None", "On Hit By Spell", "On Take Damage", "On Casting", "On Moving", "On Turning", "On Jumping", "Not Mounted", "Not Above Water", "Not Underwater", "Not Sheathed", "On Talk", "On Use", "On Melee Attack", "On Spell Attack", "Unknown 14", "On Transform", "Unknown 16", "On Mount", "Not Seated", "On Change Map", "Immune or Lost Selection", "Unknown 21", "On Teleport", "On Enter PvP Combat", "On Direct Damage", "Landing" };

                for (int i = 0; i < aura_interrupt_strings.Length; ++i)
                {
                    CheckBox box = new CheckBox();

                    box.Content = aura_interrupt_strings[i];
                    box.Margin = new Thickness(5, (-13 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    AuraInterruptFlagsGrid.Children.Add(box);
                    interrupts3.Add(box);
                }

                string[] channel_interrupt_strings = { "None", "On 1", "On 2", "On 3", "On 4", "On 5", "On 6", "On 7", "On 8", "On 9", "On 10", "On 11", "On 12", "On 13", "On 14", "On 15", "On 16", "On 17", "On 18" };

                for (int i = 0; i < channel_interrupt_strings.Length; ++i)
                {
                    CheckBox box = new CheckBox();

                    box.Content = channel_interrupt_strings[i];
                    box.Margin = new Thickness(5, (-9.7 + i) * 45, 0, 0);
                    box.Foreground = new SolidColorBrush(Colors.Red);

                    ChannelInterruptFlagsGrid.Children.Add(box);
                    interrupts2.Add(box);
                }

                loadCategories = new SpellCategory(this, loadDBC);
                loadDispels = new SpellDispelType(this, loadDBC);
                loadMechanics = new SpellMechanic(this, loadDBC);
                loadFocusObjects = new SpellFocusObject(this, loadDBC);
                loadAreaGroups = new AreaGroup(this, loadDBC);
                loadDifficulties = new SpellDifficulty(this, loadDBC);
                loadCastTimes = new SpellCastTimes(this, loadDBC);
                loadDurations = new SpellDuration(this, loadDBC);
                loadRanges = new SpellRange(this, loadDBC);
                loadRadiuses = new SpellRadius(this, loadDBC);
                loadItemClasses = new ItemClass(this, loadDBC);
                loadTotemCategories = new TotemCategory(this, loadDBC);
                loadRuneCosts = new SpellRuneCost(this, loadDBC);
                loadDescriptionVariables = new SpellDescriptionVariables(this, loadDBC);
            }

            catch (Exception ex) { HandleErrorMessage(ex.Message); }
        }

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

                    if (exitCode == MessageDialogResult.Affirmative) { Environment.Exit(0x1); }
                    else if (exitCode == MessageDialogResult.Negative) { return; }
                }

                if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.S)) { Button_Click(SaveSpellChanges, e); }
            }

            if (sender == NavigateToSpell)
            {
                if (e.Key != Key.Enter) { return; }

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

                catch (Exception ex) { HandleErrorMessage(ex.Message); }
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (loadDBC == null) { return; }

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

                    try
                    {
                        oldID = UInt32.Parse(inputCopySpell);

                        for (UInt32 i = 0; i < loadDBC.body.records.Length; ++i)
                        {
                            if (loadDBC.body.records[i].record.ID == oldID)
                            {
                                oldIDIndex = i;

                                break;
                            }
                        }

                        if (oldIDIndex == UInt32.MaxValue) { throw new Exception("Input spell ID does not exist!"); }
                    }

                    catch (Exception ex) { HandleErrorMessage(ex.Message); }
                }

                string inputNewRecord = await this.ShowInputAsync("Spell Editor", "Input the new spell ID.");

                if (inputNewRecord == null) { return; }

                UInt32 newID = 0;

                try
                {
                    newID = UInt32.Parse(inputNewRecord);

                    for (int i = 0; i < loadDBC.body.records.Length; ++i)
                    {
                        if (loadDBC.body.records[i].record.ID == newID) { throw new Exception("The spell ID is already taken!"); }
                    }
                }

                catch (Exception ex) { HandleErrorMessage(ex.Message); }

                Int32 newRecord = (Int32)loadDBC.header.RecordCount++;
                Array.Resize(ref loadDBC.body.records, (Int32)loadDBC.header.RecordCount);

                if (oldIDIndex != UInt32.MaxValue)
                {
                    loadDBC.body.records[newRecord] = loadDBC.body.records[oldIDIndex];
                    loadDBC.body.records[newRecord].record.ID = newID;
                    //loadDBC.body.records[newRecord].spellName = new String[10];
                    //loadDBC.body.records[newRecord].spellDesc = new String[10];
                    //loadDBC.body.records[newRecord].spellRank = new String[10];
                    //loadDBC.body.records[newRecord].spellTool = new String[10];
                }

                else
                {
                    loadDBC.body.records[newRecord].record.SpellName = new UInt32[9];
                    loadDBC.body.records[newRecord].record.SpellDescription = new UInt32[9];
                    loadDBC.body.records[newRecord].record.SpellToolTip = new UInt32[9];
                    loadDBC.body.records[newRecord].record.SpellRank = new UInt32[9];
                    loadDBC.body.records[newRecord].spellDesc = new String[9];
                    loadDBC.body.records[newRecord].spellName = new String[9];
                    loadDBC.body.records[newRecord].spellRank = new String[9];
                    loadDBC.body.records[newRecord].spellTool = new String[9];
                    loadDBC.body.records[newRecord].record.SpellNameFlag = new UInt32[8];
                    loadDBC.body.records[newRecord].record.SpellDescriptionFlags = new UInt32[8];
                    loadDBC.body.records[newRecord].record.SpellToolTipFlags = new UInt32[8];
                    loadDBC.body.records[newRecord].record.SpellRankFlags = new UInt32[8];

                    for (int i = 0; i < 9; ++i)
                    {
                        loadDBC.body.records[newRecord].record.SpellName[i] = 0;
                        loadDBC.body.records[newRecord].record.SpellDescription[i] = 0;
                        loadDBC.body.records[newRecord].record.SpellToolTip[i] = 0;
                        loadDBC.body.records[newRecord].record.SpellRank[i] = 0;
                        loadDBC.body.records[newRecord].spellDesc[i] = "";
                        loadDBC.body.records[newRecord].spellName[i] = "";
                        loadDBC.body.records[newRecord].spellRank[i] = "";
                        loadDBC.body.records[newRecord].spellTool[i] = "";

                        if (i < 8)
                        {
                            loadDBC.body.records[newRecord].record.SpellNameFlag[i] = 0;
                            loadDBC.body.records[newRecord].record.SpellDescriptionFlags[i] = 0;
                            loadDBC.body.records[newRecord].record.SpellToolTipFlags[i] = 0;
                            loadDBC.body.records[newRecord].record.SpellRankFlags[i] = 0;
                        }
                    }

                    loadDBC.body.records[newRecord].record.ID = newID;
                    loadDBC.body.records[newRecord].record.SpellIconID = 1;
                    loadDBC.body.records[newRecord].record.ActiveIconID = 0;
                }

                loadDBC.body.records = loadDBC.body.records.OrderBy(Spell_DBC_RecordMap => Spell_DBC_RecordMap.record.ID).ToArray<Spell_DBC_RecordMap>();

                if (MainTabControl.SelectedIndex != 0) { MainTabControl.SelectedIndex = 0; }
                else { PopulateSelectSpell(); }

                await this.ShowMessageAsync("Spell Editor", "Created new record with ID " + inputNewRecord + " sucessfully.");
            }

            if (sender == DeleteARecord)
            {
                string input = await this.ShowInputAsync("Spell Editor", "Input the spell ID to delete.");

                if (input == null) { return; }

                Int32 newID = 0;

                try
                {
                    newID = (Int32)UInt32.Parse(input);

                    bool found = false;

                    for (Int32 i = 0; i < loadDBC.body.records.Length; ++i)
                    {
                        if (loadDBC.body.records[i].record.ID == newID)
                        {
                            newID = i;

                            found = true;

                            break;
                        }
                    }

                    if (!found) { throw new Exception("The spell ID was not found!"); }
                }

                catch (Exception ex) { HandleErrorMessage(ex.Message); }

                List<Spell_DBC_RecordMap> records = loadDBC.body.records.ToList<Spell_DBC_RecordMap>();

                records.RemoveAt(newID);

                loadDBC.body.records = records.ToArray<Spell_DBC_RecordMap>();

                --loadDBC.header.RecordCount;

                if (MainTabControl.SelectedIndex != 0) { MainTabControl.SelectedIndex = 0; }
                else { PopulateSelectSpell(); }

                await this.ShowMessageAsync("Spell Editor", "Deleted record successfully.");
            }

            if (sender == SaveSpellChanges)
            {
                try
                {
                    UInt32 maskk = 0;
                    UInt32 flagg = 1;

                    for (int f = 0; f < attributes0.Count; ++f)
                    {
                        if (attributes0[f].IsChecked.Value == true) { maskk = maskk + flagg; }

                        flagg = flagg + flagg;
                    }

                    loadDBC.body.records[selectedID].record.Attributes = maskk;

                    maskk = 0;
                    flagg = 1;

                    for (int f = 0; f < attributes1.Count; ++f)
                    {
                        if (attributes1[f].IsChecked.Value == true) { maskk = maskk + flagg; }

                        flagg = flagg + flagg;
                    }

                    loadDBC.body.records[selectedID].record.AttributesEx = maskk;

                    maskk = 0;
                    flagg = 1;

                    for (int f = 0; f < attributes2.Count; ++f)
                    {
                        if (attributes2[f].IsChecked.Value == true) { maskk = maskk + flagg; }

                        flagg = flagg + flagg;
                    }

                    loadDBC.body.records[selectedID].record.AttributesEx2 = maskk;

                    maskk = 0;
                    flagg = 1;

                    for (int f = 0; f < attributes3.Count; ++f)
                    {
                        if (attributes3[f].IsChecked.Value == true) { maskk = maskk + flagg; }

                        flagg = flagg + flagg;
                    }

                    loadDBC.body.records[selectedID].record.AttributesEx3 = maskk;

                    maskk = 0;
                    flagg = 1;

                    for (int f = 0; f < attributes4.Count; ++f)
                    {
                        if (attributes4[f].IsChecked.Value == true) { maskk = maskk + flagg; }

                        flagg = flagg + flagg;
                    }

                    loadDBC.body.records[selectedID].record.AttributesEx4 = maskk;

                    maskk = 0;
                    flagg = 1;

                    for (int f = 0; f < attributes5.Count; ++f)
                    {
                        if (attributes5[f].IsChecked.Value == true) { maskk = maskk + flagg; }

                        flagg = flagg + flagg;
                    }

                    loadDBC.body.records[selectedID].record.AttributesEx5 = maskk;

                    maskk = 0;
                    flagg = 1;

                    for (int f = 0; f < attributes6.Count; ++f)
                    {
                        if (attributes6[f].IsChecked.Value == true) { maskk = maskk + flagg; }

                        flagg = flagg + flagg;
                    }

                    loadDBC.body.records[selectedID].record.AttributesEx6 = maskk;

                    maskk = 0;
                    flagg = 1;

                    for (int f = 0; f < attributes7.Count; ++f)
                    {
                        if (attributes7[f].IsChecked.Value == true) { maskk = maskk + flagg; }

                        flagg = flagg + flagg;
                    }

                    loadDBC.body.records[selectedID].record.AttributesEx7 = maskk;

                    UInt32 stances_mask = 0;

                    for (int f = 0; f < stancesBoxes.Count; ++f)
                    {
                        if (stancesBoxes[f].IsChecked.Value == true) { stances_mask = stances_mask + stances_values[f]; }
                    }

                    loadDBC.body.records[selectedID].record.Stances = stances_mask;

                    if (targetBoxes[0].IsChecked.Value == true) { loadDBC.body.records[selectedID].record.Targets = 0; }
                    else
                    {
                        UInt32 mask = 0;
                        UInt32 flag = 1;

                        for (int f = 1; f < targetBoxes.Count; ++f)
                        {
                            if (targetBoxes[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        loadDBC.body.records[selectedID].record.Targets = mask;
                    }

                    UInt32 creature_type_mask = 0;

                    for (int f = 1; f < targetCreatureTypeBoxes.Count; ++f)
                    {
                        if (targetCreatureTypeBoxes[f].IsChecked.Value == true) { creature_type_mask = creature_type_mask + creature_type_values[f]; }
                    }

                    loadDBC.body.records[selectedID].record.TargetCreatureType = creature_type_mask;

                    loadDBC.body.records[selectedID].record.FacingCasterFlags = FacingFrontFlag.IsChecked.Value ? (UInt32)0x1 : (UInt32)0x0;
                    
                    switch (CasterAuraState.SelectedIndex)
                    {
                        case 0: // None
                        {
                            loadDBC.body.records[selectedID].record.CasterAuraState = 0;

                            break;
                        }

                        case 1: // Defense
                        {
                            loadDBC.body.records[selectedID].record.CasterAuraState = 1;

                            break;
                        }

                        case 2: // Healthless 20%
                        {
                            loadDBC.body.records[selectedID].record.CasterAuraState = 2;

                            break;
                        }

                        case 3: // Berserking
                        {
                            loadDBC.body.records[selectedID].record.CasterAuraState = 3;

                            break;
                        }

                        case 4: // Judgement
                        {
                            loadDBC.body.records[selectedID].record.CasterAuraState = 5;

                            break;
                        }

                        case 5: // Hunter Parry
                        {
                            loadDBC.body.records[selectedID].record.CasterAuraState = 7;

                            break;
                        }

                        case 6: // Victory Rush
                        {
                            loadDBC.body.records[selectedID].record.CasterAuraState = 10;

                            break;
                        }

                        case 7: // Unknown 1
                        {
                            loadDBC.body.records[selectedID].record.CasterAuraState = 11;

                            break;
                        }

                        case 8: // Healthless 35%
                        {
                            loadDBC.body.records[selectedID].record.CasterAuraState = 13;

                            break;
                        }

                        case 9: // Enrage
                        {
                            loadDBC.body.records[selectedID].record.CasterAuraState = 17;

                            break;
                        }

                        case 10: // Unknown 2
                        {
                            loadDBC.body.records[selectedID].record.CasterAuraState = 22;

                            break;
                        }

                        case 11: // Health Above 75%
                        {
                            loadDBC.body.records[selectedID].record.CasterAuraState = 23;

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
                            loadDBC.body.records[selectedID].record.TargetAuraState = 0;

                            break;
                        }

                        case 1: // Healthless 20%
                        {
                            loadDBC.body.records[selectedID].record.TargetAuraState = 2;

                            break;
                        }

                        case 2: // Berserking
                        {
                            loadDBC.body.records[selectedID].record.TargetAuraState = 3;

                            break;
                        }

                        case 3: // Healthless 35%
                        {
                            loadDBC.body.records[selectedID].record.TargetAuraState = 13;

                            break;
                        }

                        case 4: // Conflagrate
                        {
                            loadDBC.body.records[selectedID].record.TargetAuraState = 14;

                            break;
                        }

                        case 5: // Swiftmend
                        {
                            loadDBC.body.records[selectedID].record.TargetAuraState = 15;

                            break;
                        }

                        case 6: // Deadly Poison
                        {
                            loadDBC.body.records[selectedID].record.TargetAuraState = 16;

                            break;
                        }

                        case 7: // Bleeding
                        {
                            loadDBC.body.records[selectedID].record.TargetAuraState = 18;

                            break;
                        }

                        default:
                        {
                            break;
                        }
                    }

                    loadDBC.body.records[selectedID].record.RecoveryTime = UInt32.Parse(RecoveryTime.Text);
                    loadDBC.body.records[selectedID].record.CategoryRecoveryTime = UInt32.Parse(CategoryRecoveryTime.Text);

                    if (interrupts1[0].IsChecked.Value == true) { loadDBC.body.records[selectedID].record.InterruptFlags = 0; }
                    else
                    {
                        UInt32 mask = 0;
                        UInt32 flag = 1;

                        for (int f = 1; f < interrupts1.Count; ++f)
                        {
                            if (interrupts1[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        loadDBC.body.records[selectedID].record.InterruptFlags = mask;
                    }

                    if (interrupts2[0].IsChecked.Value == true) { loadDBC.body.records[selectedID].record.AuraInterruptFlags = 0; }
                    else
                    {
                        UInt32 mask = 0;
                        UInt32 flag = 1;

                        for (int f = 1; f < interrupts2.Count; ++f)
                        {
                            if (interrupts2[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        loadDBC.body.records[selectedID].record.AuraInterruptFlags = mask;
                    }

                    if (interrupts3[0].IsChecked.Value == true) { loadDBC.body.records[selectedID].record.ChannelInterruptFlags = 0; }
                    else
                    {
                        UInt32 mask = 0;
                        UInt32 flag = 1;

                        for (int f = 1; f < interrupts3.Count; ++f)
                        {
                            if (interrupts3[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        loadDBC.body.records[selectedID].record.ChannelInterruptFlags = mask;
                    }

                    if (procBoxes[0].IsChecked.Value == true) { loadDBC.body.records[selectedID].record.ProcFlags = 0; }
                    else
                    {
                        UInt32 mask = 0;
                        UInt32 flag = 1;

                        for (int f = 1; f < procBoxes.Count; ++f)
                        {
                            if (procBoxes[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        loadDBC.body.records[selectedID].record.ProcFlags = mask;
                    }

                    loadDBC.body.records[selectedID].record.ProcChance = UInt32.Parse(ProcChance.Text);
                    loadDBC.body.records[selectedID].record.ProcCharges = UInt32.Parse(ProcCharges.Text);
                    loadDBC.body.records[selectedID].record.MaximumLevel = UInt32.Parse(MaximumLevel.Text);
                    loadDBC.body.records[selectedID].record.BaseLevel = UInt32.Parse(BaseLevel.Text);
                    loadDBC.body.records[selectedID].record.SpellLevel = UInt32.Parse(SpellLevel.Text);
                    loadDBC.body.records[selectedID].record.PowerType = (UInt32)PowerType.SelectedIndex;
                    loadDBC.body.records[selectedID].record.ManaCost = UInt32.Parse(PowerCost.Text);
                    loadDBC.body.records[selectedID].record.ManaCostPerLevel = UInt32.Parse(ManaCostPerLevel.Text);
                    loadDBC.body.records[selectedID].record.ManaPerSecond = UInt32.Parse(ManaCostPerSecond.Text);
                    loadDBC.body.records[selectedID].record.ManaPerSecondPerLevel = UInt32.Parse(PerSecondPerLevel.Text);
                    loadDBC.body.records[selectedID].record.Speed = float.Parse(Speed.Text);
                    loadDBC.body.records[selectedID].record.StackAmount = UInt32.Parse(Stacks.Text);
                    loadDBC.body.records[selectedID].record.Totem1 = UInt32.Parse(Totem1.Text);
                    loadDBC.body.records[selectedID].record.Totem2 = UInt32.Parse(Totem2.Text);
                    loadDBC.body.records[selectedID].record.Reagent1 = Int32.Parse(Reagent1.Text);
                    loadDBC.body.records[selectedID].record.Reagent2 = Int32.Parse(Reagent2.Text);
                    loadDBC.body.records[selectedID].record.Reagent3 = Int32.Parse(Reagent3.Text);
                    loadDBC.body.records[selectedID].record.Reagent4 = Int32.Parse(Reagent4.Text);
                    loadDBC.body.records[selectedID].record.Reagent5 = Int32.Parse(Reagent5.Text);
                    loadDBC.body.records[selectedID].record.Reagent6 = Int32.Parse(Reagent6.Text);
                    loadDBC.body.records[selectedID].record.Reagent7 = Int32.Parse(Reagent7.Text);
                    loadDBC.body.records[selectedID].record.Reagent8 = Int32.Parse(Reagent8.Text);
                    loadDBC.body.records[selectedID].record.ReagentCount1 = UInt32.Parse(ReagentCount1.Text);
                    loadDBC.body.records[selectedID].record.ReagentCount2 = UInt32.Parse(ReagentCount2.Text);
                    loadDBC.body.records[selectedID].record.ReagentCount3 = UInt32.Parse(ReagentCount3.Text);
                    loadDBC.body.records[selectedID].record.ReagentCount4 = UInt32.Parse(ReagentCount4.Text);
                    loadDBC.body.records[selectedID].record.ReagentCount5 = UInt32.Parse(ReagentCount5.Text);
                    loadDBC.body.records[selectedID].record.ReagentCount6 = UInt32.Parse(ReagentCount6.Text);
                    loadDBC.body.records[selectedID].record.ReagentCount7 = UInt32.Parse(ReagentCount7.Text);
                    loadDBC.body.records[selectedID].record.ReagentCount8 = UInt32.Parse(ReagentCount8.Text);

                    if (equippedItemInventoryTypeMaskBoxes[0].IsChecked.Value == true) { loadDBC.body.records[selectedID].record.EquippedItemInventoryTypeMask = 0; }
                    else
                    {
                        UInt32 mask = 0;
                        UInt32 flag = 1;

                        for (int f = 0; f < equippedItemInventoryTypeMaskBoxes.Count; ++f)
                        {
                            if (equippedItemInventoryTypeMaskBoxes[f].IsChecked.Value == true) { mask = mask + flag; }

                            flag = flag + flag;
                        }

                        loadDBC.body.records[selectedID].record.EquippedItemInventoryTypeMask = (Int32)mask;
                    }

                    loadDBC.body.records[selectedID].record.Effect1 = (UInt32)SpellEffect1.SelectedIndex;
                    loadDBC.body.records[selectedID].record.Effect2 = (UInt32)SpellEffect2.SelectedIndex;
                    loadDBC.body.records[selectedID].record.Effect3 = (UInt32)SpellEffect3.SelectedIndex;
                    loadDBC.body.records[selectedID].record.EffectDieSides1 = Int32.Parse(DieSides1.Text);
                    loadDBC.body.records[selectedID].record.EffectDieSides2 = Int32.Parse(DieSides2.Text);
                    loadDBC.body.records[selectedID].record.EffectDieSides3 = Int32.Parse(DieSides3.Text);
                    loadDBC.body.records[selectedID].record.EffectRealPointsPerLevel1 = float.Parse(BasePointsPerLevel1.Text);
                    loadDBC.body.records[selectedID].record.EffectRealPointsPerLevel2 = float.Parse(BasePointsPerLevel2.Text);
                    loadDBC.body.records[selectedID].record.EffectRealPointsPerLevel3 = float.Parse(BasePointsPerLevel3.Text);
                    loadDBC.body.records[selectedID].record.EffectBasePoints1 = Int32.Parse(BasePoints1.Text);
                    loadDBC.body.records[selectedID].record.EffectBasePoints2 = Int32.Parse(BasePoints2.Text);
                    loadDBC.body.records[selectedID].record.EffectBasePoints3 = Int32.Parse(BasePoints3.Text);
                    loadDBC.body.records[selectedID].record.EffectMechanic1 = (UInt32)Mechanic1.SelectedIndex;
                    loadDBC.body.records[selectedID].record.EffectMechanic2 = (UInt32)Mechanic2.SelectedIndex;
                    loadDBC.body.records[selectedID].record.EffectMechanic3 = (UInt32)Mechanic3.SelectedIndex;
                    loadDBC.body.records[selectedID].record.EffectImplicitTargetA1 = (UInt32)(Targets)TargetA1.SelectedItem;
                    loadDBC.body.records[selectedID].record.EffectImplicitTargetA2 = (UInt32)(Targets)TargetA2.SelectedItem;
                    loadDBC.body.records[selectedID].record.EffectImplicitTargetA3 = (UInt32)(Targets)TargetA3.SelectedItem;
                    loadDBC.body.records[selectedID].record.EffectImplicitTargetB1 = (UInt32)(Targets)TargetB1.SelectedItem;
                    loadDBC.body.records[selectedID].record.EffectImplicitTargetB2 = (UInt32)(Targets)TargetB2.SelectedItem;
                    loadDBC.body.records[selectedID].record.EffectImplicitTargetB3 = (UInt32)(Targets)TargetB3.SelectedItem;
                    loadDBC.body.records[selectedID].record.EffectApplyAuraName1 = (UInt32)ApplyAuraName1.SelectedIndex;
                    loadDBC.body.records[selectedID].record.EffectApplyAuraName2 = (UInt32)ApplyAuraName2.SelectedIndex;
                    loadDBC.body.records[selectedID].record.EffectApplyAuraName3 = (UInt32)ApplyAuraName3.SelectedIndex;
                    loadDBC.body.records[selectedID].record.EffectAmplitude1 = UInt32.Parse(Amplitude1.Text);
                    loadDBC.body.records[selectedID].record.EffectAmplitude2 = UInt32.Parse(Amplitude2.Text);
                    loadDBC.body.records[selectedID].record.EffectAmplitude3 = UInt32.Parse(Amplitude3.Text);
                    loadDBC.body.records[selectedID].record.EffectMultipleValue1 = float.Parse(MultipleValue1.Text);
                    loadDBC.body.records[selectedID].record.EffectMultipleValue2 = float.Parse(MultipleValue1.Text);
                    loadDBC.body.records[selectedID].record.EffectMultipleValue3 = float.Parse(MultipleValue1.Text);
                    loadDBC.body.records[selectedID].record.EffectChainTarget1 = (UInt32)ChainTarget1.SelectedIndex;
                    loadDBC.body.records[selectedID].record.EffectChainTarget2 = (UInt32)ChainTarget2.SelectedIndex;
                    loadDBC.body.records[selectedID].record.EffectChainTarget3 = (UInt32)ChainTarget3.SelectedIndex;
                    loadDBC.body.records[selectedID].record.EffectItemType1 = UInt32.Parse(ItemType1.Text);
                    loadDBC.body.records[selectedID].record.EffectItemType2 = UInt32.Parse(ItemType2.Text);
                    loadDBC.body.records[selectedID].record.EffectItemType3 = UInt32.Parse(ItemType3.Text);
                    loadDBC.body.records[selectedID].record.EffectMiscValue1 = Int32.Parse(MiscValueA1.Text);
                    loadDBC.body.records[selectedID].record.EffectMiscValue2 = Int32.Parse(MiscValueA2.Text);
                    loadDBC.body.records[selectedID].record.EffectMiscValue3 = Int32.Parse(MiscValueA3.Text);
                    loadDBC.body.records[selectedID].record.EffectMiscValueB1 = Int32.Parse(MiscValueB1.Text);
                    loadDBC.body.records[selectedID].record.EffectMiscValueB2 = Int32.Parse(MiscValueB2.Text);
                    loadDBC.body.records[selectedID].record.EffectMiscValueB3 = Int32.Parse(MiscValueB3.Text);
                    loadDBC.body.records[selectedID].record.EffectTriggerSpell1 = UInt32.Parse(TriggerSpell1.Text);
                    loadDBC.body.records[selectedID].record.EffectTriggerSpell2 = UInt32.Parse(TriggerSpell2.Text);
                    loadDBC.body.records[selectedID].record.EffectTriggerSpell3 = UInt32.Parse(TriggerSpell3.Text);
                    loadDBC.body.records[selectedID].record.EffectPointsPerComboPoint1 = float.Parse(PointsPerComboPoint1.Text);
                    loadDBC.body.records[selectedID].record.EffectPointsPerComboPoint2 = float.Parse(PointsPerComboPoint2.Text);
                    loadDBC.body.records[selectedID].record.EffectPointsPerComboPoint3 = float.Parse(PointsPerComboPoint3.Text);
                    loadDBC.body.records[selectedID].record.EffectSpellClassMaskA1 = UInt32.Parse(SpellMask11.Text);
                    loadDBC.body.records[selectedID].record.EffectSpellClassMaskA2 = UInt32.Parse(SpellMask21.Text);
                    loadDBC.body.records[selectedID].record.EffectSpellClassMaskA3 = UInt32.Parse(SpellMask31.Text);
                    loadDBC.body.records[selectedID].record.EffectSpellClassMaskB1 = UInt32.Parse(SpellMask12.Text);
                    loadDBC.body.records[selectedID].record.EffectSpellClassMaskB2 = UInt32.Parse(SpellMask22.Text);
                    loadDBC.body.records[selectedID].record.EffectSpellClassMaskB3 = UInt32.Parse(SpellMask32.Text);
                    loadDBC.body.records[selectedID].record.EffectSpellClassMaskC1 = UInt32.Parse(SpellMask13.Text);
                    loadDBC.body.records[selectedID].record.EffectSpellClassMaskC2 = UInt32.Parse(SpellMask23.Text);
                    loadDBC.body.records[selectedID].record.EffectSpellClassMaskC3 = UInt32.Parse(SpellMask33.Text);
                    loadDBC.body.records[selectedID].record.SpellVisual1 = UInt32.Parse(SpellVisual1.Text);
                    loadDBC.body.records[selectedID].record.SpellVisual2 = UInt32.Parse(SpellVisual2.Text);
                    loadDBC.body.records[selectedID].record.ManaCostPercentage = UInt32.Parse(ManaCostPercent.Text);
                    loadDBC.body.records[selectedID].record.StartRecoveryCategory = UInt32.Parse(StartRecoveryCategory.Text);
                    loadDBC.body.records[selectedID].record.StartRecoveryTime = UInt32.Parse(StartRecoveryTime.Text);
                    loadDBC.body.records[selectedID].record.MaximumTargetLevel = UInt32.Parse(MaxTargetsLevel.Text);
                    loadDBC.body.records[selectedID].record.SpellFamilyName = UInt32.Parse(SpellFamilyName.Text);
                    loadDBC.body.records[selectedID].record.MaximumAffectedTargets = UInt32.Parse(MaxTargets.Text);
                    loadDBC.body.records[selectedID].record.DamageClass = (UInt32)SpellDamageType.SelectedIndex;
                    loadDBC.body.records[selectedID].record.PreventionType = (UInt32)PreventionType.SelectedIndex;
                    loadDBC.body.records[selectedID].record.EffectDamageMultiplier1 = float.Parse(EffectDamageMultiplier1.Text);
                    loadDBC.body.records[selectedID].record.EffectDamageMultiplier2 = float.Parse(EffectDamageMultiplier2.Text);
                    loadDBC.body.records[selectedID].record.EffectDamageMultiplier3 = float.Parse(EffectDamageMultiplier3.Text);
                    loadDBC.body.records[selectedID].record.SchoolMask = (S1.IsChecked.Value ? (UInt32)0x01 : (UInt32)0x00) + (S2.IsChecked.Value ? (UInt32)0x02 : (UInt32)0x00) + (S3.IsChecked.Value ? (UInt32)0x04 : (UInt32)0x00) + (S4.IsChecked.Value ? (UInt32)0x08 : (UInt32)0x00) + (S5.IsChecked.Value ? (UInt32)0x10 : (UInt32)0x00) + (S6.IsChecked.Value ? (UInt32)0x20 : (UInt32)0x00) + (S7.IsChecked.Value ? (UInt32)0x40 : (UInt32)0x00);
                    loadDBC.body.records[selectedID].record.SpellMissileID = UInt32.Parse(SpellMissileID.Text);
                    loadDBC.body.records[selectedID].record.EffectBonusMultiplier1 = float.Parse(EffectBonusMultiplier1.Text);
                    loadDBC.body.records[selectedID].record.EffectBonusMultiplier2 = float.Parse(EffectBonusMultiplier2.Text);
                    loadDBC.body.records[selectedID].record.EffectBonusMultiplier3 = float.Parse(EffectBonusMultiplier3.Text);

                    loadDBC.body.records[selectedID].spellName[0] = SpellName0.Text;
                    loadDBC.body.records[selectedID].spellRank[0] = SpellRank0.Text;
                    loadDBC.body.records[selectedID].spellTool[0] = SpellTooltip0.Text;
                    loadDBC.body.records[selectedID].spellDesc[0] = SpellDescription0.Text;

                    // TODO - this should be implemented somewhere -- if non empty strings to set appropriate flag, probably important client-wise!
                    //loadDBC.body.records[selectedID].record.SpellNameFlag[0] = (uint)(SpellName0.Text.Length > 0 ? TextFlags.NOT_EMPTY : TextFlags.EMPTY);
                    //loadDBC.body.records[selectedID].record.SpellRankFlags[0] = (uint)(SpellRank0.Text.Length > 0 ? TextFlags.NOT_EMPTY : TextFlags.EMPTY);
                    //loadDBC.body.records[selectedID].record.SpellToolTipFlags[0] = (uint)(SpellTooltip0.Text.Length > 0 ? TextFlags.NOT_EMPTY : TextFlags.EMPTY);
                    //loadDBC.body.records[selectedID].record.SpellDescriptionFlags[0] = (uint)(SpellDescription0.Text.Length > 0 ? TextFlags.NOT_EMPTY : TextFlags.EMPTY);
                    
                    loadDBC.SaveDBCFile();
                }

                catch (Exception ex)
                {
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

                if (spellOrActive == MessageDialogResult.Affirmative) { loadDBC.body.records[selectedID].record.SpellIconID = newIconID; }
                else if (spellOrActive == MessageDialogResult.Negative) { loadDBC.body.records[selectedID].record.ActiveIconID = newIconID; }
            }

            if (sender == ResetSpellIconID) { loadDBC.body.records[selectedID].record.SpellIconID = 1; }
            if (sender == ResetActiveIconID) { loadDBC.body.records[selectedID].record.ActiveIconID = 0; }
        }

        private async void PrepareIconEditor()
        {
            if (loadDBC == null) { return; }

            loadIcons = new SpellIconDBC(this, loadDBC);

            await loadIcons.LoadImages();
        }

        private void PopulateSelectSpell()
        {
            if (loadDBC == null) { return; }

            SelectSpell.Items.Clear();

            int locales = 0;

            for (int i = 0; i < 9; ++i)
            {
                if (loadDBC.body.records.Length < 3) { break; }
                if (loadDBC.body.records[2].spellName[i].Length > 0)
                {
                    locales = i;

                    break;
                }
            }

            for (UInt32 i = 0; i < loadDBC.body.records.Length; ++i) { SelectSpell.Items.Add(loadDBC.body.records[i].record.ID.ToString() + " - " + loadDBC.body.records[i].spellName[locales]); }
        }

        private async void NewIconClick(object sender, RoutedEventArgs e)
        {
            if (loadDBC == null) { return; }

            MetroDialogSettings settings = new MetroDialogSettings();

            settings.AffirmativeButtonText = "YES";
            settings.NegativeButtonText = "NO";

            MessageDialogStyle style = MessageDialogStyle.AffirmativeAndNegative;
            MessageDialogResult spellOrActive = await this.ShowMessageAsync("Spell Editor", "Yes for Spell Icon ID.\nNo for Active Icon ID.", style, settings);

            if (spellOrActive == MessageDialogResult.Affirmative) { loadDBC.body.records[selectedID].record.SpellIconID = newIconID; }
            else if (spellOrActive == MessageDialogResult.Negative) { loadDBC.body.records[selectedID].record.ActiveIconID = newIconID; }
        }

        private void UpdateMainWindow()
        {
            if (loadDBC == null) { return; }

            try
            {
                updating = true;

                int i;

                for (i = 0; i < 9; ++i)
                {
                    TextBox box;

                    stringObjectMap.TryGetValue(i, out box);

                    box.Text = loadDBC.body.records[selectedID].spellName[i];
                }

                for (i = 0; i < 9; ++i)
                {
                    TextBox box;

                    stringObjectMap.TryGetValue(i + 9, out box);

                    box.Text = loadDBC.body.records[selectedID].spellRank[i];
                }

                for (i = 0; i < 9; ++i)
                {
                    TextBox box;

                    stringObjectMap.TryGetValue(i + 18, out box);

                    box.Text = loadDBC.body.records[selectedID].spellTool[i];
                }

                for (i = 0; i < 9; ++i)
                {
                    TextBox box;

                    stringObjectMap.TryGetValue(i + 27, out box);

                    box.Text = loadDBC.body.records[selectedID].spellDesc[i];
                }

                loadCategories.UpdateCategorySelection();
                loadDispels.UpdateDispelSelection();
                loadMechanics.UpdateMechanicSelection();

                UInt32 mask = loadDBC.body.records[selectedID].record.Attributes;
                UInt32 flagg = 1;

                for (int f = 0; f < attributes0.Count; ++f)
                {
                    attributes0[f].IsChecked = ((mask & flagg) != 0) ? true : false;

                    flagg = flagg + flagg;
                }

                mask = loadDBC.body.records[selectedID].record.AttributesEx;
                flagg = 1;

                for (int f = 0; f < attributes1.Count; ++f)
                {
                    attributes1[f].IsChecked = ((mask & flagg) != 0) ? true : false;

                    flagg = flagg + flagg;
                }

                mask = loadDBC.body.records[selectedID].record.AttributesEx2;
                flagg = 1;

                for (int f = 0; f < attributes2.Count; ++f)
                {
                    attributes2[f].IsChecked = ((mask & flagg) != 0) ? true : false;

                    flagg = flagg + flagg;
                }

                mask = loadDBC.body.records[selectedID].record.AttributesEx3;
                flagg = 1;

                for (int f = 0; f < attributes3.Count; ++f)
                {
                    attributes3[f].IsChecked = ((mask & flagg) != 0) ? true : false;

                    flagg = flagg + flagg;
                }

                mask = loadDBC.body.records[selectedID].record.AttributesEx4;
                flagg = 1;

                for (int f = 0; f < attributes4.Count; ++f)
                {
                    attributes4[f].IsChecked = ((mask & flagg) != 0) ? true : false;

                    flagg = flagg + flagg;
                }

                mask = loadDBC.body.records[selectedID].record.AttributesEx5;
                flagg = 1;

                for (int f = 0; f < attributes5.Count; ++f)
                {
                    attributes5[f].IsChecked = ((mask & flagg) != 0) ? true : false;

                    flagg = flagg + flagg;
                }

                mask = loadDBC.body.records[selectedID].record.AttributesEx6;
                flagg = 1;

                for (int f = 0; f < attributes6.Count; ++f)
                {
                    attributes6[f].IsChecked = ((mask & flagg) != 0) ? true : false;

                    flagg = flagg + flagg;
                }

                mask = loadDBC.body.records[selectedID].record.AttributesEx7;
                flagg = 1;

                for (int f = 0; f < attributes7.Count; ++f)
                {
                    attributes7[f].IsChecked = ((mask & flagg) != 0) ? true : false;

                    flagg = flagg + flagg;
                }

                mask = loadDBC.body.records[selectedID].record.Stances;

                if (mask == 0)
                {
                    stancesBoxes[0].IsChecked = true;

                    for (int f = 1; f < stancesBoxes.Count; ++f) { stancesBoxes[f].IsChecked = false; }
                }

                else
                {
                    stancesBoxes[0].IsChecked = false;

                    UInt32 flag = 1;

                    for (int f = 1; f < stancesBoxes.Count; ++f)
                    {
                        stancesBoxes[f].IsChecked = ((mask & flag) != 0) ? true : false;

                        flag = flag + flag;
                    }
                }

                mask = loadDBC.body.records[selectedID].record.Targets;

                if (mask == 0)
                {
                    targetBoxes[0].IsChecked = true;

                    for (int f = 1; f < targetBoxes.Count; ++f) { targetBoxes[f].IsChecked = false; }
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

                mask = loadDBC.body.records[selectedID].record.TargetCreatureType;

                if (mask == 0)
                {
                    targetCreatureTypeBoxes[0].IsChecked = true;

                    for (int f = 1; f < targetCreatureTypeBoxes.Count; ++f) { targetCreatureTypeBoxes[f].IsChecked = false; }
                }

                else
                {
                    targetCreatureTypeBoxes[0].IsChecked = false;

                    UInt32 flag = 1;

                    for (int f = 1; f < targetCreatureTypeBoxes.Count; ++f)
                    {
                        targetCreatureTypeBoxes[f].IsChecked = ((mask & flag) != 0) ? true : false;

                        flag = flag + flag;
                    }
                }

                loadFocusObjects.UpdateSpellFocusObjectSelection();

                mask = loadDBC.body.records[selectedID].record.FacingCasterFlags;

                FacingFrontFlag.IsChecked = ((mask & 0x1) != 0) ? true : false;

                switch (loadDBC.body.records[selectedID].record.CasterAuraState)
                {
                    case 0: // None
                    {
                        CasterAuraState.SelectedIndex = 0;

                        break;
                    }

                    case 1: // Defense
                    {
                        CasterAuraState.SelectedIndex = 1;

                        break;
                    }

                    case 2: // Healthless 20%
                    {
                        CasterAuraState.SelectedIndex = 2;

                        break;
                    }

                    case 3: // Berserking
                    {
                        CasterAuraState.SelectedIndex = 3;

                        break;
                    }

                    case 5: // Judgement
                    {
                        CasterAuraState.SelectedIndex = 4;

                        break;
                    }

                    case 7: // Hunter Parry
                    {
                        CasterAuraState.SelectedIndex = 5;

                        break;
                    }

                    case 10: // Victory Rush
                    {
                        CasterAuraState.SelectedIndex = 6;

                        break;
                    }

                    case 11: // Unknown 1
                    {
                        CasterAuraState.SelectedIndex = 7;

                        break;
                    }

                    case 13: // Healthless 35%
                    {
                        CasterAuraState.SelectedIndex = 8;

                        break;
                    }

                    case 17: // Enrage
                    {
                        CasterAuraState.SelectedIndex = 9;

                        break;
                    }

                    case 22: // Unknown 2
                    {
                        CasterAuraState.SelectedIndex = 10;

                        break;
                    }

                    case 23: // Health Above 75%
                    {
                        CasterAuraState.SelectedIndex = 11;

                        break;
                    }

                    default: { break; }
                }

                switch (loadDBC.body.records[selectedID].record.TargetAuraState)
                {
                    case 0: // None
                    {
                        TargetAuraState.SelectedIndex = 0;

                        break;
                    }

                    case 2: // Healthless 20%
                    {
                        TargetAuraState.SelectedIndex = 1;

                        break;
                    }

                    case 3: // Berserking
                    {
                        TargetAuraState.SelectedIndex = 2;

                        break;
                    }

                    case 13: // Healthless 35%
                    {
                        TargetAuraState.SelectedIndex = 3;

                        break;
                    }

                    case 14: // Conflagrate
                    {
                        TargetAuraState.SelectedIndex = 4;

                        break;
                    }

                    case 15: // Swiftmend
                    {
                        TargetAuraState.SelectedIndex = 5;

                        break;
                    }

                    case 16: // Deadly Poison
                    {
                        TargetAuraState.SelectedIndex = 6;

                        break;
                    }

                    case 18: // Bleeding
                    {
                        TargetAuraState.SelectedIndex = 17;

                        break;
                    }

                    default: { break; }
                }

                loadCastTimes.UpdateCastTimeSelection();

                RecoveryTime.Text = loadDBC.body.records[selectedID].record.RecoveryTime.ToString();
                CategoryRecoveryTime.Text = loadDBC.body.records[selectedID].record.CategoryRecoveryTime.ToString();

                mask = loadDBC.body.records[selectedID].record.InterruptFlags;

                if (mask == 0)
                {
                    interrupts1[0].IsChecked = true;

                    for (int f = 1; f < interrupts1.Count; ++f) { interrupts1[f].IsChecked = false; }
                }

                else
                {
                    interrupts1[0].IsChecked = false;

                    UInt32 flag = 1;

                    for (int f = 1; f < interrupts1.Count; ++f)
                    {
                        interrupts1[f].IsChecked = ((mask & flag) != 0) ? true : false;

                        flag = flag + flag;
                    }
                }

                mask = loadDBC.body.records[selectedID].record.AuraInterruptFlags;

                if (mask == 0)
                {
                    interrupts2[0].IsChecked = true;

                    for (int f = 1; f < interrupts2.Count; ++f) { interrupts2[f].IsChecked = false; }
                }

                else
                {
                    interrupts2[0].IsChecked = false;

                    UInt32 flag = 1;

                    for (int f = 1; f < interrupts2.Count; ++f)
                    {
                        interrupts2[f].IsChecked = ((mask & flag) != 0) ? true : false;

                        flag = flag + flag;
                    }
                }

                mask = loadDBC.body.records[selectedID].record.ChannelInterruptFlags;

                if (mask == 0)
                {
                    interrupts3[0].IsChecked = true;

                    for (int f = 1; f < interrupts3.Count; ++f) { interrupts3[f].IsChecked = false; }
                }

                else
                {
                    interrupts3[0].IsChecked = false;

                    UInt32 flag = 1;

                    for (int f = 1; f < interrupts3.Count; ++f)
                    {
                        interrupts3[f].IsChecked = ((mask & flag) != 0) ? true : false;

                        flag = flag + flag;
                    }
                }

                mask = loadDBC.body.records[selectedID].record.ProcFlags;

                if (mask == 0)
                {
                    procBoxes[0].IsChecked = true;

                    for (int f = 1; f < procBoxes.Count; ++f) { procBoxes[f].IsChecked = false; }
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

                ProcChance.Text = loadDBC.body.records[selectedID].record.ProcChance.ToString();
                ProcCharges.Text = loadDBC.body.records[selectedID].record.ProcCharges.ToString();
                MaximumLevel.Text = loadDBC.body.records[selectedID].record.MaximumLevel.ToString();
                BaseLevel.Text = loadDBC.body.records[selectedID].record.BaseLevel.ToString();
                SpellLevel.Text = loadDBC.body.records[selectedID].record.SpellLevel.ToString();

                loadDurations.UpdateDurationIndexes();

                PowerType.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.PowerType;
                PowerCost.Text = loadDBC.body.records[selectedID].record.ManaCost.ToString();
                ManaCostPerLevel.Text = loadDBC.body.records[selectedID].record.ManaCostPerLevel.ToString();
                ManaCostPerSecond.Text = loadDBC.body.records[selectedID].record.ManaPerSecond.ToString();
                PerSecondPerLevel.Text = loadDBC.body.records[selectedID].record.ManaPerSecondPerLevel.ToString();

                loadRanges.UpdateSpellRangeSelection();

                Speed.Text = loadDBC.body.records[selectedID].record.Speed.ToString();
                Stacks.Text = loadDBC.body.records[selectedID].record.StackAmount.ToString();
                Totem1.Text = loadDBC.body.records[selectedID].record.Totem1.ToString();
                Totem2.Text = loadDBC.body.records[selectedID].record.Totem2.ToString();
                Reagent1.Text = loadDBC.body.records[selectedID].record.Reagent1.ToString();
                Reagent2.Text = loadDBC.body.records[selectedID].record.Reagent2.ToString();
                Reagent3.Text = loadDBC.body.records[selectedID].record.Reagent3.ToString();
                Reagent4.Text = loadDBC.body.records[selectedID].record.Reagent4.ToString();
                Reagent5.Text = loadDBC.body.records[selectedID].record.Reagent5.ToString();
                Reagent6.Text = loadDBC.body.records[selectedID].record.Reagent6.ToString();
                Reagent7.Text = loadDBC.body.records[selectedID].record.Reagent7.ToString();
                Reagent8.Text = loadDBC.body.records[selectedID].record.Reagent8.ToString();
                ReagentCount1.Text = loadDBC.body.records[selectedID].record.ReagentCount1.ToString();
                ReagentCount2.Text = loadDBC.body.records[selectedID].record.ReagentCount2.ToString();
                ReagentCount3.Text = loadDBC.body.records[selectedID].record.ReagentCount3.ToString();
                ReagentCount4.Text = loadDBC.body.records[selectedID].record.ReagentCount4.ToString();
                ReagentCount5.Text = loadDBC.body.records[selectedID].record.ReagentCount5.ToString();
                ReagentCount6.Text = loadDBC.body.records[selectedID].record.ReagentCount6.ToString();
                ReagentCount7.Text = loadDBC.body.records[selectedID].record.ReagentCount7.ToString();
                ReagentCount8.Text = loadDBC.body.records[selectedID].record.ReagentCount8.ToString();

                loadItemClasses.UpdateItemClassSelection();

                mask = (UInt32)loadDBC.body.records[selectedID].record.EquippedItemInventoryTypeMask;

                if (mask == 0)
                {
                    equippedItemInventoryTypeMaskBoxes[0].IsChecked = true;

                    for (int f = 1; f < equippedItemInventoryTypeMaskBoxes.Count; ++f) { equippedItemInventoryTypeMaskBoxes[f].IsChecked = false; }
                }

                else
                {
                    equippedItemInventoryTypeMaskBoxes[0].IsChecked = false;

                    UInt32 flag = 1;

                    for (int f = 0; f < equippedItemInventoryTypeMaskBoxes.Count; ++f)
                    {
                        equippedItemInventoryTypeMaskBoxes[f].IsChecked = ((mask & flag) != 0) ? true : false;

                        flag = flag + flag;
                    }
                }

                Effect1.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.Effect1;
                Effect2.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.Effect2;
                Effect3.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.Effect3;
                SpellEffect1.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.Effect1;
                SpellEffect2.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.Effect2;
                SpellEffect3.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.Effect3;
                EffectDieSides1.Text = loadDBC.body.records[selectedID].record.EffectDieSides1.ToString();
                EffectDieSides2.Text = loadDBC.body.records[selectedID].record.EffectDieSides2.ToString();
                EffectDieSides3.Text = loadDBC.body.records[selectedID].record.EffectDieSides3.ToString();
                DieSides1.Text = loadDBC.body.records[selectedID].record.EffectDieSides1.ToString();
                DieSides2.Text = loadDBC.body.records[selectedID].record.EffectDieSides2.ToString();
                DieSides3.Text = loadDBC.body.records[selectedID].record.EffectDieSides3.ToString();
                BasePointsPerLevel1.Text = loadDBC.body.records[selectedID].record.EffectRealPointsPerLevel1.ToString();
                BasePointsPerLevel2.Text = loadDBC.body.records[selectedID].record.EffectRealPointsPerLevel1.ToString();
                BasePointsPerLevel3.Text = loadDBC.body.records[selectedID].record.EffectRealPointsPerLevel1.ToString();
                EffectBaseValue1.Text = loadDBC.body.records[selectedID].record.EffectBasePoints1.ToString();
                EffectBaseValue2.Text = loadDBC.body.records[selectedID].record.EffectBasePoints2.ToString();
                EffectBaseValue3.Text = loadDBC.body.records[selectedID].record.EffectBasePoints3.ToString();
                BasePoints1.Text = loadDBC.body.records[selectedID].record.EffectBasePoints1.ToString();
                BasePoints2.Text = loadDBC.body.records[selectedID].record.EffectBasePoints2.ToString();
                BasePoints3.Text = loadDBC.body.records[selectedID].record.EffectBasePoints3.ToString();
                Mechanic1.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.EffectMechanic1;
                Mechanic2.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.EffectMechanic2;
                Mechanic3.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.EffectMechanic3;
                TargetA1.SelectedItem = (Targets)loadDBC.body.records[selectedID].record.EffectImplicitTargetA1;
                TargetA2.SelectedItem = (Targets)loadDBC.body.records[selectedID].record.EffectImplicitTargetA2;
                TargetA3.SelectedItem = (Targets)loadDBC.body.records[selectedID].record.EffectImplicitTargetA3;
                TargetB1.SelectedItem = (Targets)loadDBC.body.records[selectedID].record.EffectImplicitTargetB1;
                TargetB2.SelectedItem = (Targets)loadDBC.body.records[selectedID].record.EffectImplicitTargetB2;
                TargetB3.SelectedItem = (Targets)loadDBC.body.records[selectedID].record.EffectImplicitTargetB3;

                loadRadiuses.UpdateRadiusIndexes();

                ApplyAuraName1.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.EffectApplyAuraName1;
                ApplyAuraName2.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.EffectApplyAuraName2;
                ApplyAuraName3.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.EffectApplyAuraName3;
                Amplitude1.Text = loadDBC.body.records[selectedID].record.EffectAmplitude1.ToString();
                Amplitude2.Text = loadDBC.body.records[selectedID].record.EffectAmplitude2.ToString();
                Amplitude3.Text = loadDBC.body.records[selectedID].record.EffectAmplitude3.ToString();
                MultipleValue1.Text = loadDBC.body.records[selectedID].record.EffectMultipleValue1.ToString();
                MultipleValue2.Text = loadDBC.body.records[selectedID].record.EffectMultipleValue2.ToString();
                MultipleValue3.Text = loadDBC.body.records[selectedID].record.EffectMultipleValue3.ToString();
                ChainTarget1.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.EffectChainTarget1;
                ChainTarget2.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.EffectChainTarget2;
                ChainTarget3.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.EffectChainTarget3;
                ItemType1.Text = loadDBC.body.records[selectedID].record.EffectItemType1.ToString();
                ItemType2.Text = loadDBC.body.records[selectedID].record.EffectItemType2.ToString();
                ItemType3.Text = loadDBC.body.records[selectedID].record.EffectItemType3.ToString();
                MiscValueA1.Text = loadDBC.body.records[selectedID].record.EffectMiscValue1.ToString();
                MiscValueA2.Text = loadDBC.body.records[selectedID].record.EffectMiscValue2.ToString();
                MiscValueA3.Text = loadDBC.body.records[selectedID].record.EffectMiscValue3.ToString();
                MiscValueB1.Text = loadDBC.body.records[selectedID].record.EffectMiscValueB1.ToString();
                MiscValueB2.Text = loadDBC.body.records[selectedID].record.EffectMiscValueB2.ToString();
                MiscValueB3.Text = loadDBC.body.records[selectedID].record.EffectMiscValueB3.ToString();
                TriggerSpell1.Text = loadDBC.body.records[selectedID].record.EffectTriggerSpell1.ToString();
                TriggerSpell2.Text = loadDBC.body.records[selectedID].record.EffectTriggerSpell2.ToString();
                TriggerSpell3.Text = loadDBC.body.records[selectedID].record.EffectTriggerSpell3.ToString();
                PointsPerComboPoint1.Text = loadDBC.body.records[selectedID].record.EffectPointsPerComboPoint1.ToString();
                PointsPerComboPoint2.Text = loadDBC.body.records[selectedID].record.EffectPointsPerComboPoint2.ToString();
                PointsPerComboPoint3.Text = loadDBC.body.records[selectedID].record.EffectPointsPerComboPoint3.ToString();
                SpellMask11.Text = loadDBC.body.records[selectedID].record.EffectSpellClassMaskA1.ToString();
                SpellMask21.Text = loadDBC.body.records[selectedID].record.EffectSpellClassMaskA2.ToString();
                SpellMask31.Text = loadDBC.body.records[selectedID].record.EffectSpellClassMaskA3.ToString();
                SpellMask12.Text = loadDBC.body.records[selectedID].record.EffectSpellClassMaskB1.ToString();
                SpellMask22.Text = loadDBC.body.records[selectedID].record.EffectSpellClassMaskB2.ToString();
                SpellMask32.Text = loadDBC.body.records[selectedID].record.EffectSpellClassMaskB3.ToString();
                SpellMask13.Text = loadDBC.body.records[selectedID].record.EffectSpellClassMaskC1.ToString();
                SpellMask23.Text = loadDBC.body.records[selectedID].record.EffectSpellClassMaskC2.ToString();
                SpellMask33.Text = loadDBC.body.records[selectedID].record.EffectSpellClassMaskC3.ToString();
                SpellVisual1.Text = loadDBC.body.records[selectedID].record.SpellVisual1.ToString();
                SpellVisual2.Text = loadDBC.body.records[selectedID].record.SpellVisual2.ToString();
                ManaCostPercent.Text = loadDBC.body.records[selectedID].record.ManaCostPercentage.ToString();
                StartRecoveryCategory.Text = loadDBC.body.records[selectedID].record.StartRecoveryCategory.ToString();
                StartRecoveryTime.Text = loadDBC.body.records[selectedID].record.StartRecoveryTime.ToString();
                MaxTargetsLevel.Text = loadDBC.body.records[selectedID].record.MaximumTargetLevel.ToString();
                SpellFamilyName.Text = loadDBC.body.records[selectedID].record.SpellFamilyName.ToString();
                MaxTargets.Text = loadDBC.body.records[selectedID].record.MaximumAffectedTargets.ToString();
                SpellDamageType.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.DamageClass;
                PreventionType.SelectedIndex = (Int32)loadDBC.body.records[selectedID].record.PreventionType;
                EffectDamageMultiplier1.Text = loadDBC.body.records[selectedID].record.EffectDamageMultiplier1.ToString();
                EffectDamageMultiplier2.Text = loadDBC.body.records[selectedID].record.EffectDamageMultiplier2.ToString();
                EffectDamageMultiplier3.Text = loadDBC.body.records[selectedID].record.EffectDamageMultiplier3.ToString();

                loadTotemCategories.UpdateTotemCategoriesSelection();
                loadAreaGroups.UpdateAreaGroupSelection();

                mask = loadDBC.body.records[selectedID].record.SchoolMask;

                S1.IsChecked = ((mask & 0x01) != 0) ? true : false;
                S2.IsChecked = ((mask & 0x02) != 0) ? true : false;
                S3.IsChecked = ((mask & 0x04) != 0) ? true : false;
                S4.IsChecked = ((mask & 0x08) != 0) ? true : false;
                S5.IsChecked = ((mask & 0x10) != 0) ? true : false;
                S6.IsChecked = ((mask & 0x20) != 0) ? true : false;
                S7.IsChecked = ((mask & 0x40) != 0) ? true : false;

                loadRuneCosts.UpdateSpellRuneCostSelection();

                SpellMissileID.Text = loadDBC.body.records[selectedID].record.SpellMissileID.ToString();
                EffectBonusMultiplier1.Text = loadDBC.body.records[selectedID].record.EffectBonusMultiplier1.ToString();
                EffectBonusMultiplier2.Text = loadDBC.body.records[selectedID].record.EffectBonusMultiplier2.ToString();
                EffectBonusMultiplier3.Text = loadDBC.body.records[selectedID].record.EffectBonusMultiplier3.ToString();

                loadDescriptionVariables.UpdateSpellDescriptionVariablesSelection();
                loadDifficulties.UpdateDifficultySelection();

                updating = false;
            }

            catch (Exception ex)
            {
                HandleErrorMessage(ex.Message);

                updating = false;
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = sender as TabControl;

            if (item.SelectedIndex == 0) { PopulateSelectSpell(); }
            else if (item.SelectedIndex == item.Items.Count - 1) { PrepareIconEditor(); }
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
                selectedID = (UInt32)(((ListBox)sender).SelectedIndex);

                if (selectedID >= loadDBC.body.records.Length || loadDBC.body.records[selectedID].spellName == null || loadDBC.body.records[selectedID].spellName.Length == 0 || loadDBC.body.records[selectedID].spellName[0] == null)
                {
                    await this.ShowMessageAsync("Spell Editor", "Something went wrong trying to select this spell.");

                    PopulateSelectSpell();
                }

                else { UpdateMainWindow(); }
            }
        }

        static object Lock = new object();

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (loadDBC == null || updating) { return; }
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
            if (loadDBC == null || updating) { return; }

            if (sender == RequiresSpellFocus)
            {
                for (int i = 0; i < loadFocusObjects.body.lookup.Count; ++i)
                {
                    if (loadFocusObjects.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        loadDBC.body.records[selectedID].record.RequiresSpellFocus = (UInt32)loadFocusObjects.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.AreaGroupID = (UInt32)loadAreaGroups.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.Category = (UInt32)loadCategories.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.Dispel = (UInt32)loadDispels.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.Mechanic = (UInt32)loadMechanics.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.CastingTimeIndex = (UInt32)loadCastTimes.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.DurationIndex = (UInt32)loadDurations.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.SpellDifficultyID = (UInt32)loadDifficulties.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.RangeIndex = (UInt32)loadRanges.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.EffectRadiusIndex1 = (UInt32)loadRadiuses.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.EffectRadiusIndex2 = (UInt32)loadRadiuses.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.EffectRadiusIndex3 = (UInt32)loadRadiuses.body.lookup[i].ID;

                        break;
                    }
                }
            }

            if (sender == EquippedItemClass)
            {
                for (int i = 0; i < loadItemClasses.body.lookup.Count; ++i)
                {
                    if (EquippedItemClass.SelectedIndex == 5) { EquippedItemInventoryTypeGrid.IsEnabled = true; }

                    else
                    {
                        foreach (CheckBox box in equippedItemInventoryTypeMaskBoxes) { box.IsChecked = false; }

                        EquippedItemInventoryTypeGrid.IsEnabled = false;
                    }

                    if (loadItemClasses.body.lookup[i].comboBoxIndex == ((ComboBox)sender).SelectedIndex)
                    {
                        loadDBC.body.records[selectedID].record.EquippedItemClass = (Int32)loadItemClasses.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.TotemCategory1 = (UInt32)loadTotemCategories.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.TotemCategory2 = (UInt32)loadTotemCategories.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.RuneCostID = (UInt32)loadRuneCosts.body.lookup[i].ID;

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
                        loadDBC.body.records[selectedID].record.SpellDescriptionVariableID = (UInt32)loadDescriptionVariables.body.lookup[i].ID;

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
    };

    class SpellDBC
    {
        // Begin Window
        private MainWindow main;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public Spell_DBC_Body body;
        // End DBCs

        // Begin Files
        private long fileSize;
        // End Files

        public bool LoadDBCFile(MainWindow window)
        {
            main = window;

            try
            {
                FileStream fileStream = new FileStream("DBC/Spell.dbc", FileMode.Open);
                fileSize = fileStream.Length;
                int count = Marshal.SizeOf(typeof(DBC_Header));
                byte[] readBuffer = new byte[count];
                BinaryReader reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
                handle.Free();
                body.records = new Spell_DBC_RecordMap[header.RecordCount];

                for (UInt32 i = 0; i < header.RecordCount; ++i)
                {
                    count = Marshal.SizeOf(typeof(Spell_DBC_Record));
                    readBuffer = new byte[count];
                    readBuffer = reader.ReadBytes(count);
                    handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                    body.records[i].record = (Spell_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Spell_DBC_Record));
                    handle.Free();
                }

                string StringBlock;

                Dictionary<UInt32, VirtualStrTableEntry> strings = new Dictionary<UInt32, VirtualStrTableEntry>();

                StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

                string temp = "";

                UInt32 lastString = 0;
                UInt32 counter = 0;
                Int32 length = new System.Globalization.StringInfo(StringBlock).LengthInTextElements;

                while (counter < length)
                {
                    var t = StringBlock[(int)counter];

                    if (t == '\0')
                    {
                        VirtualStrTableEntry n = new VirtualStrTableEntry();

                        n.Value = temp;
                        n.NewValue = 0;

                        strings.Add(lastString, n);

                        lastString += (UInt32)Encoding.UTF8.GetByteCount(temp) + 1;

                        temp = "";
                    }

                    else { temp += t; }

                    ++counter;
                }

                StringBlock = null;

                for (int i = 0; i < body.records.Length; ++i)
                {
                    body.records[i].spellName = new string[9];
                    body.records[i].spellRank = new string[9];
                    body.records[i].spellDesc = new string[9];
                    body.records[i].spellTool = new string[9];

                    for (int j = 0; j < 9; ++j)
                    {
                        body.records[i].spellName[j] = strings[body.records[i].record.SpellName[j]].Value;
                        body.records[i].spellRank[j] = strings[body.records[i].record.SpellRank[j]].Value;
                        body.records[i].spellDesc[j] = strings[body.records[i].record.SpellDescription[j]].Value;
                        body.records[i].spellTool[j] = strings[body.records[i].record.SpellToolTip[j]].Value;
                    }
                }

                reader.Close();
                fileStream.Close();
            }

            catch (Exception ex)
            {
                main.HandleErrorMessage(ex.Message);

                return false;
            }

            return true;
        }

        public bool SaveDBCFile()
        {
            try
            {
                UInt32 stringBlockOffset = 1;

                Dictionary<int, UInt32> offsetStorage = new Dictionary<int, UInt32>();
                Dictionary<UInt32, string> reverseStorage = new Dictionary<UInt32, string>();

                for (UInt32 i = 0; i < header.RecordCount; ++i)
                {
                    for (UInt32 j = 0; j < 9; ++j)
                    {
                        if (body.records[i].spellName[j].Length == 0) { body.records[i].record.SpellName[j] = 0; }
                        else
                        {
                            int key = body.records[i].spellName[j].GetHashCode();

                            if (offsetStorage.ContainsKey(key)) { body.records[i].record.SpellName[j] = offsetStorage[key]; }
                            else
                            {
                                body.records[i].record.SpellName[j] = stringBlockOffset;
                                stringBlockOffset += (UInt32)Encoding.UTF8.GetByteCount(body.records[i].spellName[j]) + 1;
                                offsetStorage.Add(key, body.records[i].record.SpellName[j]);
                                reverseStorage.Add(body.records[i].record.SpellName[j], body.records[i].spellName[j]);
                            }
                        }

                        if (body.records[i].spellRank[j].Length == 0) { body.records[i].record.SpellRank[j] = 0; }
                        else
                        {
                            int key = body.records[i].spellRank[j].GetHashCode();

                            if (offsetStorage.ContainsKey(key)) { body.records[i].record.SpellRank[j] = offsetStorage[key]; }
                            else
                            {
                                body.records[i].record.SpellRank[j] = stringBlockOffset;
                                stringBlockOffset += (UInt32)Encoding.UTF8.GetByteCount(body.records[i].spellRank[j]) + 1;
                                offsetStorage.Add(key, body.records[i].record.SpellRank[j]);
                                reverseStorage.Add(body.records[i].record.SpellRank[j], body.records[i].spellRank[j]);
                            }
                        }

                        if (body.records[i].spellTool[j].Length == 0) { body.records[i].record.SpellToolTip[j] = 0; }
                        else
                        {
                            int key = body.records[i].spellTool[j].GetHashCode();

                            if (offsetStorage.ContainsKey(key)) { body.records[i].record.SpellToolTip[j] = offsetStorage[key]; }
                            else
                            {
                                body.records[i].record.SpellToolTip[j] = stringBlockOffset;
                                stringBlockOffset += (UInt32)Encoding.UTF8.GetByteCount(body.records[i].spellTool[j]) + 1;
                                offsetStorage.Add(key, body.records[i].record.SpellToolTip[j]);
                                reverseStorage.Add(body.records[i].record.SpellToolTip[j], body.records[i].spellTool[j]);
                            }
                        }

                        if (body.records[i].spellDesc[j].Length == 0) { body.records[i].record.SpellDescription[j] = 0; }
                        else
                        {
                            int key = body.records[i].spellDesc[j].GetHashCode();

                            if (offsetStorage.ContainsKey(key)) { body.records[i].record.SpellDescription[j] = offsetStorage[key]; }
                            else
                            {
                                body.records[i].record.SpellDescription[j] = stringBlockOffset;
                                stringBlockOffset += (UInt32)Encoding.UTF8.GetByteCount(body.records[i].spellDesc[j]) + 1;
                                offsetStorage.Add(key, body.records[i].record.SpellDescription[j]);
                                reverseStorage.Add(body.records[i].record.SpellDescription[j], body.records[i].spellDesc[j]);
                            }
                        }
                    }
                }

                header.StringBlockSize = (int)stringBlockOffset;

                if (File.Exists("DBC/Spell.dbc")) { File.Delete("DBC/Spell.dbc"); }

                FileStream fileStream = new FileStream("DBC/Spell.dbc", FileMode.Create);
                BinaryWriter writer = new BinaryWriter(fileStream);
                int count = Marshal.SizeOf(typeof(DBC_Header));
                byte[] buffer = new byte[count];
                GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                Marshal.StructureToPtr(header, handle.AddrOfPinnedObject(), true);
                writer.Write(buffer, 0, count);
                handle.Free();

                for (UInt32 i = 0; i < header.RecordCount; ++i)
                {
                    count = Marshal.SizeOf(typeof(Spell_DBC_Record));
                    buffer = new byte[count];
                    handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    Marshal.StructureToPtr(body.records[i].record, handle.AddrOfPinnedObject(), true);
                    writer.Write(buffer, 0, count);
                    handle.Free();
                }

                UInt32[] offsetsStored = offsetStorage.Values.ToArray<UInt32>();

                writer.Write(Encoding.UTF8.GetBytes("\0"));

                for (int i = 0; i < offsetsStored.Length; ++i) { writer.Write(Encoding.UTF8.GetBytes(reverseStorage[offsetsStored[i]] + "\0")); }

                writer.Close();
                fileStream.Close();
            }

            catch (Exception ex)
            {
                main.HandleErrorMessage(ex.Message);

                return false;
            }

            return true;
        }
    }

    public struct Spell_DBC_Body
    {
        public Spell_DBC_RecordMap[] records;
    };

    public struct Spell_DBC_RecordMap
    {
        public Spell_DBC_Record record;
        public string[] spellName;
        public string[] spellRank;
        public string[] spellDesc;
        public string[] spellTool;
    };

    public struct Spell_DBC_Record
    {
        public UInt32 ID;
        public UInt32 Category;
        public UInt32 Dispel;
        public UInt32 Mechanic;
        public UInt32 Attributes;
        public UInt32 AttributesEx;
        public UInt32 AttributesEx2;
        public UInt32 AttributesEx3;
        public UInt32 AttributesEx4;
        public UInt32 AttributesEx5;
        public UInt32 AttributesEx6;
        public UInt32 AttributesEx7;
        public UInt32 Stances;
        public UInt32 Unknown1;
        public UInt32 StancesNot;
        public UInt32 Unknown2;
        public UInt32 Targets;
        public UInt32 TargetCreatureType;
        public UInt32 RequiresSpellFocus;
        public UInt32 FacingCasterFlags;
        public UInt32 CasterAuraState;
        public UInt32 TargetAuraState;
        public UInt32 CasterAuraStateNot;
        public UInt32 TargetAuraStateNot;
        public UInt32 CasterAuraSpell;
        public UInt32 TargetAuraSpell;
        public UInt32 ExcludeCasterAuraSpell;
        public UInt32 ExcludeTargetAuraSpell;
        public UInt32 CastingTimeIndex;
        public UInt32 RecoveryTime;
        public UInt32 CategoryRecoveryTime;
        public UInt32 InterruptFlags;
        public UInt32 AuraInterruptFlags;
        public UInt32 ChannelInterruptFlags;
        public UInt32 ProcFlags;
        public UInt32 ProcChance;
        public UInt32 ProcCharges;
        public UInt32 MaximumLevel;
        public UInt32 BaseLevel;
        public UInt32 SpellLevel;
        public UInt32 DurationIndex;
        public UInt32 PowerType;
        public UInt32 ManaCost;
        public UInt32 ManaCostPerLevel;
        public UInt32 ManaPerSecond;
        public UInt32 ManaPerSecondPerLevel;
        public UInt32 RangeIndex;
        public float Speed;
        public UInt32 ModalNextSpell;
        public UInt32 StackAmount;
        public UInt32 Totem1;
        public UInt32 Totem2;
        public Int32 Reagent1;
        public Int32 Reagent2;
        public Int32 Reagent3;
        public Int32 Reagent4;
        public Int32 Reagent5;
        public Int32 Reagent6;
        public Int32 Reagent7;
        public Int32 Reagent8;
        public UInt32 ReagentCount1;
        public UInt32 ReagentCount2;
        public UInt32 ReagentCount3;
        public UInt32 ReagentCount4;
        public UInt32 ReagentCount5;
        public UInt32 ReagentCount6;
        public UInt32 ReagentCount7;
        public UInt32 ReagentCount8;
        public Int32 EquippedItemClass;
        public Int32 EquippedItemSubClassMask;
        public Int32 EquippedItemInventoryTypeMask;
        public UInt32 Effect1;
        public UInt32 Effect2;
        public UInt32 Effect3;
        public Int32 EffectDieSides1;
        public Int32 EffectDieSides2;
        public Int32 EffectDieSides3;
        public float EffectRealPointsPerLevel1;
        public float EffectRealPointsPerLevel2;
        public float EffectRealPointsPerLevel3;
        public Int32 EffectBasePoints1;
        public Int32 EffectBasePoints2;
        public Int32 EffectBasePoints3;
        public UInt32 EffectMechanic1;
        public UInt32 EffectMechanic2;
        public UInt32 EffectMechanic3;
        public UInt32 EffectImplicitTargetA1;
        public UInt32 EffectImplicitTargetA2;
        public UInt32 EffectImplicitTargetA3;
        public UInt32 EffectImplicitTargetB1;
        public UInt32 EffectImplicitTargetB2;
        public UInt32 EffectImplicitTargetB3;
        public UInt32 EffectRadiusIndex1;
        public UInt32 EffectRadiusIndex2;
        public UInt32 EffectRadiusIndex3;
        public UInt32 EffectApplyAuraName1;
        public UInt32 EffectApplyAuraName2;
        public UInt32 EffectApplyAuraName3;
        public UInt32 EffectAmplitude1;
        public UInt32 EffectAmplitude2;
        public UInt32 EffectAmplitude3;
        public float EffectMultipleValue1;
        public float EffectMultipleValue2;
        public float EffectMultipleValue3;
        public UInt32 EffectChainTarget1;
        public UInt32 EffectChainTarget2;
        public UInt32 EffectChainTarget3;
        public UInt32 EffectItemType1;
        public UInt32 EffectItemType2;
        public UInt32 EffectItemType3;
        public Int32 EffectMiscValue1;
        public Int32 EffectMiscValue2;
        public Int32 EffectMiscValue3;
        public Int32 EffectMiscValueB1;
        public Int32 EffectMiscValueB2;
        public Int32 EffectMiscValueB3;
        public UInt32 EffectTriggerSpell1;
        public UInt32 EffectTriggerSpell2;
        public UInt32 EffectTriggerSpell3;
        public float EffectPointsPerComboPoint1;
        public float EffectPointsPerComboPoint2;
        public float EffectPointsPerComboPoint3;
        public UInt32 EffectSpellClassMaskA1;
        public UInt32 EffectSpellClassMaskA2;
        public UInt32 EffectSpellClassMaskA3;	
        public UInt32 EffectSpellClassMaskB1;
        public UInt32 EffectSpellClassMaskB2;
        public UInt32 EffectSpellClassMaskB3;
        public UInt32 EffectSpellClassMaskC1;
        public UInt32 EffectSpellClassMaskC2;
        public UInt32 EffectSpellClassMaskC3;
        public UInt32 SpellVisual1;
        public UInt32 SpellVisual2;
        public UInt32 SpellIconID;
        public UInt32 ActiveIconID;
        public UInt32 SpellPriority;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public UInt32[] SpellName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public UInt32[] SpellNameFlag;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public UInt32[] SpellRank;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public UInt32[] SpellRankFlags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public UInt32[] SpellDescription;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public UInt32[] SpellDescriptionFlags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public UInt32[] SpellToolTip;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public UInt32[] SpellToolTipFlags;
        public UInt32 ManaCostPercentage;
        public UInt32 StartRecoveryCategory;
        public UInt32 StartRecoveryTime;
        public UInt32 MaximumTargetLevel;
        public UInt32 SpellFamilyName;
        public UInt32 SpellFamilyFlags;
        public UInt32 SpellFamilyFlags1;
        public UInt32 SpellFamilyFlags2;
        public UInt32 MaximumAffectedTargets;
        public UInt32 DamageClass;
        public UInt32 PreventionType;
        public UInt32 StanceBarOrder;
        public float EffectDamageMultiplier1;
        public float EffectDamageMultiplier2;
        public float EffectDamageMultiplier3;
        public UInt32 MinimumFactionId;
        public UInt32 MinimumReputation;
        public UInt32 RequiredAuraVision;
        public UInt32 TotemCategory1;
        public UInt32 TotemCategory2;
        public UInt32 AreaGroupID;
        public UInt32 SchoolMask;
        public UInt32 RuneCostID;
        public UInt32 SpellMissileID;
        public UInt32 PowerDisplayId;
        public float EffectBonusMultiplier1;
        public float EffectBonusMultiplier2;
        public float EffectBonusMultiplier3;
        public UInt32 SpellDescriptionVariableID;
        public UInt32 SpellDifficultyID;
    };

    class SpellCategory
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellCategory_DBC_Map body;
        // End DBCs

        public SpellCategory(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Flags = new UInt32();
            }

            if (!File.Exists("DBC/SpellCategory.dbc"))
            {
                main.HandleErrorMessage("SpellCategory.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellCategory.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellCategory_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellCategory_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellCategory_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellCategory_DBC_Record));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellCategoryLookup>();

            int boxIndex = 1;

            main.Category.Items.Add(0);

            SpellCategoryLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int id = (int)body.records[i].ID;

                SpellCategoryLookup temp;

                temp.ID = id;
                temp.comboBoxIndex = boxIndex;

                main.Category.Items.Add(id);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateCategorySelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.Category;

            if (ID == 0)
            {
                main.Category.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.Category.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }
    }

    public struct SpellCategory_DBC_Map
    {
        public SpellCategory_DBC_Record[] records;
        public List<SpellCategoryLookup> lookup;
    };

    public struct SpellCategoryLookup
    {
        public int ID;
        public int comboBoxIndex;
    };

    public struct SpellCategory_DBC_Record
    {
        public UInt32 ID;
        public UInt32 Flags;
    };

    class SpellDispelType
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellDispel_DBC_Map body;
        // End DBCs

        public SpellDispelType(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Name = new UInt32[16];
                body.records[i].NameFlags = new UInt32();
                body.records[i].Combinations = new UInt32();
                body.records[i].ImmunityPossible = new UInt32();
                body.records[i].InternalName = new UInt32();
            }

            if (!File.Exists("DBC/SpellDispelType.dbc"))
            {
                main.HandleErrorMessage("SpellDispelType.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellDispelType.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellDispel_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellDispel_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellDispel_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDispel_DBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellDispel_DBC_Lookup>();

            int boxIndex = 0;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].Name[0];

                if (offset == 0) { continue; }

                int returnValue = offset;

                string toAdd = "";

                while (body.StringBlock[offset] != '\0') { toAdd += body.StringBlock[offset++]; }

                SpellDispel_DBC_Lookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.offset = returnValue;
                temp.stringHash = toAdd.GetHashCode();
                temp.comboBoxIndex = boxIndex;

                main.DispelType.Items.Add(toAdd);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateDispelSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.Dispel;

            for (int i = 0; i < header.RecordCount; ++i)
            {
                if (ID == body.records[i].ID)
                {
                    main.DispelType.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct SpellDispel_DBC_Map
        {
            public SpellDispel_DBC_Record[] records;
            public List<SpellDispel_DBC_Lookup> lookup;
            public string StringBlock;
        };

        public struct SpellDispel_DBC_Lookup
        {
            public int ID;
            public int offset;
            public int stringHash;
            public int comboBoxIndex;
        };

        public struct SpellDispel_DBC_Record
        {
            public UInt32 ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] Name;
            public UInt32 NameFlags;
            public UInt32 Combinations;
            public UInt32 ImmunityPossible;
            public UInt32 InternalName;
        };
    };

    class SpellMechanic
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public Mechanic_DBC_Map body;
        // End DBCs

        public SpellMechanic(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Name = new UInt32[16];
                body.records[i].NameFlags = new UInt32();
            }

            if (!File.Exists("DBC/SpellMechanic.dbc"))
            {
                main.HandleErrorMessage("SpellMechanic.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellMechanic.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new Mechanic_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(Mechanic_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (Mechanic_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Mechanic_DBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

            reader.Close();
            fileStream.Close();

            body.lookup = new List<MechanicLookup>();

            int boxIndex = 1;

            main.MechanicType.Items.Add("None");

            MechanicLookup t;

            t.ID = 0;
            t.offset = 0;
            t.stringHash = "None".GetHashCode();
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].Name[0];

                if (offset == 0) { continue; }

                int returnValue = offset;

                string toAdd = "";

                while (body.StringBlock[offset] != '\0') { toAdd += body.StringBlock[offset++]; }

                MechanicLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.offset = returnValue;
                temp.stringHash = toAdd.GetHashCode();
                temp.comboBoxIndex = boxIndex;

                main.MechanicType.Items.Add(toAdd.Remove(1).ToUpper() + toAdd.Substring(1));

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateMechanicSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.Mechanic;

            if (ID == 0)
            {
                main.MechanicType.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.MechanicType.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct Mechanic_DBC_Map
        {
            public Mechanic_DBC_Record[] records;
            public List<MechanicLookup> lookup;
            public string StringBlock;
        };

        public struct MechanicLookup
        {
            public int ID;
            public int offset;
            public int stringHash;
            public int comboBoxIndex;
        };

        public struct Mechanic_DBC_Record
        {
            public UInt32 ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] Name;
            public UInt32 NameFlags;
        };
    };

    class SpellFocusObject
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellFocusObject_DBC_Map body;
        // End DBCs

        public SpellFocusObject(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Name = new UInt32[16];
                body.records[i].Flags = new UInt32();
            }

            if (!File.Exists("DBC/SpellFocusObject.dbc"))
            {
                main.HandleErrorMessage("SpellFocusObject.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellFocusObject.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellFocusObject_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellFocusObject_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellFocusObject_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellFocusObject_DBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellFocusObjectLookup>();

            int boxIndex = 1;

            main.RequiresSpellFocus.Items.Add("None");

            SpellFocusObjectLookup t;

            t.ID = 0;
            t.offset = 0;
            t.stringHash = "None".GetHashCode();
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].Name[0];

                if (offset == 0) { continue; }

                int returnValue = offset;

                string toAdd = "";

                while (body.StringBlock[offset] != '\0') { toAdd += body.StringBlock[offset++]; }

                SpellFocusObjectLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.offset = returnValue;
                temp.stringHash = toAdd.GetHashCode();
                temp.comboBoxIndex = boxIndex;

                main.RequiresSpellFocus.Items.Add(toAdd);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateSpellFocusObjectSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.RequiresSpellFocus;

            if (ID == 0)
            {
                main.RequiresSpellFocus.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.RequiresSpellFocus.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct SpellFocusObject_DBC_Map
        {
            public SpellFocusObject_DBC_Record[] records;
            public List<SpellFocusObjectLookup> lookup;
            public string StringBlock;
        };

        public struct SpellFocusObjectLookup
        {
            public int ID;
            public int offset;
            public int stringHash;
            public int comboBoxIndex;
        };

        public struct SpellFocusObject_DBC_Record
        {
            public UInt32 ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] Name;
            public UInt32 Flags;
        };
    };

    class AreaGroup
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public AreaGroup_DBC_Map body;
        // End DBCs

        public AreaGroup(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].AreaID = new UInt32[6];
                body.records[i].NextGroup = new UInt32();
            }

            if (!File.Exists("DBC/AreaGroup.dbc"))
            {
                main.HandleErrorMessage("AreaGroup.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/AreaGroup.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new AreaGroup_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(AreaGroup_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (AreaGroup_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(AreaGroup_DBC_Record));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<AreaGroupLookup>();

            int boxIndex = 1;

            main.AreaGroup.Items.Add(0);

            AreaGroupLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int id = (int)body.records[i].ID;

                AreaGroupLookup temp;

                temp.ID = id;
                temp.comboBoxIndex = boxIndex;

                main.AreaGroup.Items.Add(id);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateAreaGroupSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.AreaGroupID;

            if (ID == 0)
            {
                main.AreaGroup.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.AreaGroup.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }
    }

    public struct AreaGroup_DBC_Map
    {
        public AreaGroup_DBC_Record[] records;
        public List<AreaGroupLookup> lookup;
    };

    public struct AreaGroupLookup
    {
        public int ID;
        public int comboBoxIndex;
    };

    public struct AreaGroup_DBC_Record
    {
        public UInt32 ID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public UInt32[] AreaID;
        public UInt32 NextGroup;
    };

    class SpellCastTimes
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellCastTimes_DBC_Map body;
        // End DBCs

        public SpellCastTimes(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].CastingTime = new Int32();
                body.records[i].CastingTimePerLevel = new float();
                body.records[i].MinimumCastingTime = new Int32();
            }

            if (!File.Exists("DBC/SpellCastTimes.dbc"))
            {
                main.HandleErrorMessage("SpellCastTimes.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellCastTimes.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellCastTimes_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellCastTimes_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellCastTimes_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellCastTimes_DBC_Record));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellCastTimeLookup>();

            int boxIndex = 1;

            main.CastTime.Items.Add(0);

            SpellCastTimeLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int castTime = (int)body.records[i].CastingTime;

                SpellCastTimeLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.CastTime.Items.Add(castTime);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateCastTimeSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.CastingTimeIndex;

            if (ID == 0)
            {
                main.CastTime.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.CastTime.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }
    }

    public struct SpellCastTimes_DBC_Map
    {
        public SpellCastTimes_DBC_Record[] records;
        public List<SpellCastTimeLookup> lookup;
    };

    public struct SpellCastTimeLookup
    {
        public int ID;
        public int comboBoxIndex;
    };

    public struct SpellCastTimes_DBC_Record
    {
        public UInt32 ID;
        public Int32 CastingTime;
        public float CastingTimePerLevel;
        public Int32 MinimumCastingTime;
    };

    class SpellDuration
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellDuration_DBC_Body body;
        // End DBCs

        public SpellDuration(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].BaseDuration = new Int32();
                body.records[i].PerLevel = new Int32();
                body.records[i].MaximumDuration = new Int32();
            }

            if (!File.Exists("DBC/SpellDuration.dbc"))
            {
                main.HandleErrorMessage("SpellDuration.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellDuration.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();
            body.records = new SpellDurationRecord[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellDurationRecord));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellDurationRecord)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDurationRecord));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellDurationLookup>();

            int boxIndex = 1;

            main.Duration.Items.Add(0);

            SpellDurationLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int baseDuration = (int)body.records[i].BaseDuration;

                SpellDurationLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.Duration.Items.Add(baseDuration);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateDurationIndexes()
        {
            int ID = (int)spell.body.records[main.selectedID].record.DurationIndex;

            if (ID == 0)
            {
                main.Duration.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.Duration.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct SpellDuration_DBC_Body
        {
            public SpellDurationRecord[] records;
            public List<SpellDurationLookup> lookup;
        };

        public struct SpellDurationLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct SpellDurationRecord
        {
            public UInt32 ID;
            public Int32 BaseDuration;
            public Int32 PerLevel;
            public Int32 MaximumDuration;
        };
    };

    class SpellDifficulty
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellDifficulty_DBC_Map body;
        // End DBCs

        public SpellDifficulty(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Difficulties = new UInt32[4];
            }

            if (!File.Exists("DBC/SpellDifficulty.dbc"))
            {
                main.HandleErrorMessage("SpellDifficulty.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellDifficulty.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellDifficulty_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellDifficulty_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellDifficulty_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDifficulty_DBC_Record));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellDifficultyLookup>();

            int boxIndex = 1;

            main.Difficulty.Items.Add(0);

            SpellDifficultyLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int id = (int)body.records[i].ID;

                SpellDifficultyLookup temp;

                temp.ID = id;
                temp.comboBoxIndex = boxIndex;

                main.Difficulty.Items.Add(id);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateDifficultySelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.SpellDifficultyID;

            if (ID == 0)
            {
                main.Difficulty.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.Difficulty.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }
    }

    public struct SpellDifficulty_DBC_Map
    {
        public SpellDifficulty_DBC_Record[] records;
        public List<SpellDifficultyLookup> lookup;
    };

    public struct SpellDifficultyLookup
    {
        public int ID;
        public int comboBoxIndex;
    };

    public struct SpellDifficulty_DBC_Record
    {
        public UInt32 ID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public UInt32[] Difficulties;
    };

    class SpellIconDBC
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public Icon_DBC_Map body;
        // End DBCs

        // Begin Other
        private static bool loadedAllIcons = false;
        // End Other

        public SpellIconDBC(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Name = new UInt32();
            }
        }

        public Task LoadImages()
        {
            return (new TaskFactory()).StartNew(() =>
            {
                if (!File.Exists("DBC/SpellIcon.dbc"))
                {
                    main.HandleErrorMessage("SpellIcon.dbc was not found!");

                    return;
                }

                FileStream fileStream = new FileStream("DBC/SpellIcon.dbc", FileMode.Open);
                int count = Marshal.SizeOf(typeof(DBC_Header));
                byte[] readBuffer = new byte[count];
                BinaryReader reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
                handle.Free();

                body.records = new Icon_DBC_Record[header.RecordCount];

                for (UInt32 i = 0; i < header.RecordCount; ++i)
                {
                    count = Marshal.SizeOf(typeof(Icon_DBC_Record));
                    readBuffer = new byte[count];
                    reader = new BinaryReader(fileStream);
                    readBuffer = reader.ReadBytes(count);
                    handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                    body.records[i] = (Icon_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Icon_DBC_Record));
                    handle.Free();
                }

                body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

                reader.Close();
                fileStream.Close();

                UpdateMainWindowIcons();
            });
        }

        public async void UpdateMainWindowIcons()
        {
            if (spell == null) { return; }

            UInt32 iconInt = spell.body.records[main.selectedID].record.SpellIconID;
            UInt32 iconActiveInt = spell.body.records[main.selectedID].record.ActiveIconID;
            UInt32 selectedRecord = UInt32.MaxValue;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                if (body.records[i].ID == iconInt)
                {
                    selectedRecord = i;

                    break;
                }

                if (body.records[i].ID == iconActiveInt)
                {
                    selectedRecord = i;

                    break;
                }
            }

            string icon = "";

            int offset = 0;

            try
            {
                if (selectedRecord == UInt32.MaxValue) { throw new Exception("The icon for this spell does not exist in the SpellIcon.dbc"); }

                offset = (int)body.records[selectedRecord].Name;

                while (body.StringBlock[offset] != '\0') { icon += body.StringBlock[offset++]; }

                if (!File.Exists(icon + ".blp")) { throw new Exception("File could not be found: " + "Icons\\" + icon + ".blp"); }
            }

            catch (Exception ex)
            {
                main.HandleErrorMessage(ex.Message);

                return;
            }

            FileStream fileStream = new FileStream(icon + ".blp", FileMode.Open);

            SereniaBLPLib.BlpFile image;

            image = new SereniaBLPLib.BlpFile(fileStream);

            Bitmap bit = image.getBitmap(0);

            await Task.Factory.StartNew(() =>
            {
                main.CurrentIcon.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bit.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(bit.Width, bit.Height));
            }, CancellationToken.None, TaskCreationOptions.None, main.UIScheduler);

            image.close();
            fileStream.Close();

            if (!loadedAllIcons)
            {
                loadedAllIcons = true;

                int currentOffset = 1;

                string[] icons = body.StringBlock.Split('\0');

                int iconIndex = 0;
                int columnsUsed = icons.Length / 11;
                int rowsToDo = columnsUsed / 2;

                for (int j = -rowsToDo; j <= rowsToDo; ++j)
                {
                    for (int i = -5; i < 6; ++i)
                    {
                        ++iconIndex;

                        if (iconIndex >= icons.Length - 1) { break; }

                        int this_icons_offset = currentOffset;

                        currentOffset += icons[iconIndex].Length + 1;

                        if (!File.Exists(icons[iconIndex] + ".blp"))
                        {
                            Console.WriteLine("Warning: Icon not found: " + icons[iconIndex] + ".blp");

                            continue;
                        }

                        fileStream = new FileStream(icons[iconIndex] + ".blp", FileMode.Open);
                        image = new SereniaBLPLib.BlpFile(fileStream);
                        bit = image.getBitmap(0);

                        await Task.Factory.StartNew(() =>
                        {
                            System.Windows.Controls.Image temp = new System.Windows.Controls.Image();

                            temp.Width = 64;
                            temp.Height = 64;
                            temp.Margin = new System.Windows.Thickness(139 * i, 139 * j, 0, 0);
                            temp.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bit.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(bit.Width, bit.Height));
                            temp.Name = "Index_" + this_icons_offset;
                            temp.MouseDown += this.ImageDown;

                            main.IconGrid.Children.Add(temp);
                        }, CancellationToken.None, TaskCreationOptions.None, main.UIScheduler);

                        image.close();
                        fileStream.Close();
                    }
                }
            }
        }

        void ImageDown(object sender, EventArgs e)
        {
            main.NewIcon.Source = ((System.Windows.Controls.Image)sender).Source;

            System.Windows.Controls.Image temp = (System.Windows.Controls.Image)sender;

            UInt32 offset = UInt32.Parse(temp.Name.Substring(6));
            UInt32 ID = 0;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                if (body.records[i].Name == offset)
                {
                    ID = body.records[i].ID;

                    break;
                }
            }

            main.newIconID = ID;
        }

        public struct Icon_DBC_Map
        {
            public Icon_DBC_Record[] records;
            public string StringBlock;
        };

        public struct Icon_DBC_Record
        {
            public UInt32 ID;
            public UInt32 Name;
        };
    };

    class SpellRadius
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellRadiusMap body;
        // End DBCs

        public SpellRadius(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Radius = new float();
                body.records[i].RadiusPerLevel = new float();
                body.records[i].MaximumRadius = new float();
            }

            if (!File.Exists("DBC/SpellRadius.dbc"))
            {
                main.HandleErrorMessage("SpellRadius.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellRadius.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellRadiusRecord[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellRadiusRecord));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellRadiusRecord)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellRadiusRecord));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<RadiusLookup>();

            int boxIndex = 1;

            main.RadiusIndex1.Items.Add("0 - 0");
            main.RadiusIndex2.Items.Add("0 - 0");
            main.RadiusIndex3.Items.Add("0 - 0");

            RadiusLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int radius = (int)body.records[i].Radius;
                int maximumRadius = (int)body.records[i].MaximumRadius;

                RadiusLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.RadiusIndex1.Items.Add(radius + " - " + maximumRadius);
                main.RadiusIndex2.Items.Add(radius + " - " + maximumRadius);
                main.RadiusIndex3.Items.Add(radius + " - " + maximumRadius);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateRadiusIndexes()
        {
            int[] IDs = { (int)spell.body.records[main.selectedID].record.EffectRadiusIndex1, (int)spell.body.records[main.selectedID].record.EffectRadiusIndex2, (int)spell.body.records[main.selectedID].record.EffectRadiusIndex3 };

            for (int j = 0; j < IDs.Length; ++j)
            {
                int ID = IDs[j];

                if (ID == 0)
                {
                    switch (j)
                    {
                        case 0:
                        {
                            main.RadiusIndex1.SelectedIndex = 0;

                            break;
                        }

                        case 1:
                        {
                            main.RadiusIndex2.SelectedIndex = 0;

                            break;
                        }

                        case 2:
                        {
                            main.RadiusIndex3.SelectedIndex = 0;

                            break;
                        }

                        default: { break; }
                    }

                    continue;
                }

                for (int i = 0; i < body.lookup.Count; ++i)
                {
                    if (ID == body.lookup[i].ID)
                    {
                        switch (j)
                        {
                            case 0:
                            {
                                main.RadiusIndex1.SelectedIndex = body.lookup[i].comboBoxIndex;

                                break;
                            }

                            case 1:
                            {
                                main.RadiusIndex2.SelectedIndex = body.lookup[i].comboBoxIndex;

                                break;
                            }

                            case 2:
                            {
                                main.RadiusIndex3.SelectedIndex = body.lookup[i].comboBoxIndex;

                                break;
                            }

                            default: { break; }
                        }

                        continue;
                    }
                }
            }
        }

        public struct SpellRadiusMap
        {
            public SpellRadiusRecord[] records;
            public List<RadiusLookup> lookup;
        };

        public struct RadiusLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct SpellRadiusRecord
        {
            public UInt32 ID;
            public float Radius;
            public float RadiusPerLevel;
            public float MaximumRadius;
        };
    };

    class SpellRange
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellRange_DBC_Map body;
        // End DBCs

        public SpellRange(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].MinimumRangeHostile = new float();
                body.records[i].MinimumRangeFriend = new float();
                body.records[i].MaximumRangeHostile = new float();
                body.records[i].MaximumRangeFriend = new float();
                body.records[i].Type = new Int32();
                body.records[i].Name = new UInt32[16];
                body.records[i].NameFlags = new UInt32();
                body.records[i].ShortName = new UInt32[16];
                body.records[i].ShortNameFlags = new UInt32();
            }

            if (!File.Exists("DBC/SpellRange.dbc"))
            {
                main.HandleErrorMessage("SpellRange.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellRange.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellRange_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellRange_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellRange_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellRange_DBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellRangeLookup>();

            int boxIndex = 0;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].Name[0];
                int MinimumRangeHostile = (int)body.records[i].MinimumRangeHostile;
                int MaximumRangeHostile = (int)body.records[i].MaximumRangeHostile;
                int MinimumRangeFriend = (int)body.records[i].MinimumRangeFriend;
                int MaximumRangeFriend = (int)body.records[i].MaximumRangeFriend;

                if (offset == 0) { continue; }

                int returnValue = offset;

                string toAdd = "";

                while (body.StringBlock[offset] != 0) { toAdd += body.StringBlock[offset++]; }

                SpellRangeLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.Range.Items.Add(toAdd + "\t\t - " + "Hostile: " + MinimumRangeHostile + " - " + MaximumRangeHostile + "\t Friend: " + MinimumRangeFriend + " - " + MaximumRangeFriend);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateSpellRangeSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.RangeIndex;

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.Range.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct SpellRange_DBC_Map
        {
            public SpellRange_DBC_Record[] records;
            public List<SpellRangeLookup> lookup;
            public string StringBlock;
        };

        public struct SpellRangeLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct SpellRange_DBC_Record
        {
            public UInt32 ID;
            public float MinimumRangeHostile;
            public float MinimumRangeFriend;
            public float MaximumRangeHostile;
            public float MaximumRangeFriend;
            public Int32 Type;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] Name;
            public UInt32 NameFlags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] ShortName;
            public UInt32 ShortNameFlags;
        };
    };

    class ItemClass
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public ItemClass_DBC_Map body;
        // End DBCs

        public ItemClass(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].SecondaryID = new UInt32();
                body.records[i].IsWeapon = new UInt32();
                body.records[i].Name = new UInt32[16];
                body.records[i].Flags = new UInt32();
            }

            if (!File.Exists("DBC/ItemClass.dbc"))
            {
                main.HandleErrorMessage("ItemClass.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/ItemClass.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new ItemClass_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(ItemClass_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (ItemClass_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(ItemClass_DBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

            reader.Close();
            fileStream.Close();

            body.lookup = new List<ItemClassLookup>();

            int boxIndex = 1;

            main.EquippedItemClass.Items.Add("None");

            ItemClassLookup t;

            t.ID = -1;
            t.comboBoxIndex = -1;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].Name[0];

                if (offset == 0) { continue; }

                int returnValue = offset;

                string toAdd = "";

                while (body.StringBlock[offset] != 0) { toAdd += body.StringBlock[offset++]; }

                ItemClassLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.EquippedItemClass.Items.Add(toAdd);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateItemClassSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.EquippedItemClass;

            if (ID == -1)
            {
                main.EquippedItemClass.SelectedIndex = 0;

                return;
            }

            if (ID == 4) { main.EquippedItemInventoryTypeGrid.IsEnabled = true; }
            else
            {
                foreach (CheckBox box in main.equippedItemInventoryTypeMaskBoxes) { box.IsChecked = false; }

                main.EquippedItemInventoryTypeGrid.IsEnabled = false;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.EquippedItemClass.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct ItemClass_DBC_Map
        {
            public ItemClass_DBC_Record[] records;
            public List<ItemClassLookup> lookup;
            public string StringBlock;
        };

        public struct ItemClassLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct ItemClass_DBC_Record
        {
            public UInt32 ID;
            public UInt32 SecondaryID;
            public UInt32 IsWeapon;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] Name;
            public UInt32 Flags;
        };
    };

    class TotemCategory
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public TotemCategory_DBC_Map body;
        // End DBCs

        public TotemCategory(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Name = new UInt32[16];
                body.records[i].NameFlags = new UInt32();
                body.records[i].CategoryType = new UInt32();
                body.records[i].CategoryMask = new UInt32();
            }

            if (!File.Exists("DBC/TotemCategory.dbc"))
            {
                main.HandleErrorMessage("TotemCategory.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/TotemCategory.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new TotemCategory_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(TotemCategory_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (TotemCategory_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(TotemCategory_DBC_Record));
                handle.Free();
            }

            body.StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

            reader.Close();
            fileStream.Close();

            body.lookup = new List<TotemCategoryLookup>();

            int boxIndex = 1;

            main.TotemCategory1.Items.Add("None");
            main.TotemCategory2.Items.Add("None");

            TotemCategoryLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int offset = (int)body.records[i].Name[0];

                if (offset == 0) { continue; }

                int returnValue = offset;

                string toAdd = "";

                while (body.StringBlock[offset] != 0) { toAdd += body.StringBlock[offset++]; }

                TotemCategoryLookup temp;

                temp.ID = (int)body.records[i].ID;
                temp.comboBoxIndex = boxIndex;

                main.TotemCategory1.Items.Add(toAdd);
                main.TotemCategory2.Items.Add(toAdd);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateTotemCategoriesSelection()
        {
            int[] IDs = { (int)spell.body.records[main.selectedID].record.TotemCategory1, (int)spell.body.records[main.selectedID].record.TotemCategory2 };

            for (int j = 0; j < IDs.Length; ++j)
            {
                int ID = IDs[j];

                if (ID == 0)
                {
                    switch (j)
                    {
                        case 0:
                        {
                            main.TotemCategory1.SelectedIndex = 0;

                            break;
                        }

                        case 1:
                        {
                            main.TotemCategory2.SelectedIndex = 0;

                            break;
                        }

                        default: { break; }
                    }

                    continue;
                }

                for (int i = 0; i < body.lookup.Count; ++i)
                {
                    if (ID == body.lookup[i].ID)
                    {
                        switch (j)
                        {
                            case 0:
                            {
                                main.TotemCategory1.SelectedIndex = body.lookup[i].comboBoxIndex;

                                break;
                            }

                            case 1:
                            {
                                main.TotemCategory2.SelectedIndex = body.lookup[i].comboBoxIndex;

                                break;
                            }

                            default: { break; }
                        }

                        continue;
                    }
                }
            }
        }

        public struct TotemCategory_DBC_Map
        {
            public TotemCategory_DBC_Record[] records;
            public List<TotemCategoryLookup> lookup;
            public string StringBlock;
        };

        public struct TotemCategoryLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct TotemCategory_DBC_Record
        {
            public UInt32 ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public UInt32[] Name;
            public UInt32 NameFlags;
            public UInt32 CategoryType;
            public UInt32 CategoryMask;
        };
    };

    class SpellRuneCost
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellRuneCost_DBC_Map body;
        // End DBCs

        public SpellRuneCost(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].RuneCost = new UInt32[3];
                body.records[i].RunePowerGain = new UInt32();
            }

            if (!File.Exists("DBC/SpellRuneCost.dbc"))
            {
                main.HandleErrorMessage("SpellRuneCost.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellRuneCost.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellRuneCost_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellRuneCost_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellRuneCost_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellRuneCost_DBC_Record));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellRuneCostLookup>();

            int boxIndex = 1;

            main.RuneCost.Items.Add(0);

            SpellRuneCostLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int id = (int)body.records[i].ID;

                SpellRuneCostLookup temp;

                temp.ID = id;
                temp.comboBoxIndex = boxIndex;

                main.RuneCost.Items.Add(id);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateSpellRuneCostSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.RuneCostID;

            if (ID == 0)
            {
                main.RuneCost.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.RuneCost.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct SpellRuneCost_DBC_Map
        {
            public SpellRuneCost_DBC_Record[] records;
            public List<SpellRuneCostLookup> lookup;
        };

        public struct SpellRuneCostLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct SpellRuneCost_DBC_Record
        {
            public UInt32 ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public UInt32[] RuneCost;
            public UInt32 RunePowerGain;
        };
    };

    class SpellDescriptionVariables
    {
        // Begin Window
        private MainWindow main;
        private SpellDBC spell;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public SpellDescriptionVariables_DBC_Map body;
        // End DBCs

        public SpellDescriptionVariables(MainWindow window, SpellDBC spellDBC)
        {
            main = window;
            spell = spellDBC;

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                body.records[i].ID = new UInt32();
                body.records[i].Formula = new UInt32();
            }

            if (!File.Exists("DBC/SpellDescriptionVariables.dbc"))
            {
                main.HandleErrorMessage("SpellDescriptionVariables.dbc was not found!");

                return;
            }

            FileStream fileStream = new FileStream("DBC/SpellDescriptionVariables.dbc", FileMode.Open);
            int count = Marshal.SizeOf(typeof(DBC_Header));
            byte[] readBuffer = new byte[count];
            BinaryReader reader = new BinaryReader(fileStream);
            readBuffer = reader.ReadBytes(count);
            GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
            header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
            handle.Free();

            body.records = new SpellDescriptionVariables_DBC_Record[header.RecordCount];

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                count = Marshal.SizeOf(typeof(SpellDescriptionVariables_DBC_Record));
                readBuffer = new byte[count];
                reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                body.records[i] = (SpellDescriptionVariables_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDescriptionVariables_DBC_Record));
                handle.Free();
            }

            reader.Close();
            fileStream.Close();

            body.lookup = new List<SpellDescriptionVariablesLookup>();

            int boxIndex = 1;

            main.SpellDescriptionVariables.Items.Add(0);

            SpellDescriptionVariablesLookup t;

            t.ID = 0;
            t.comboBoxIndex = 0;

            body.lookup.Add(t);

            for (UInt32 i = 0; i < header.RecordCount; ++i)
            {
                int id = (int)body.records[i].ID;

                SpellDescriptionVariablesLookup temp;

                temp.ID = id;
                temp.comboBoxIndex = boxIndex;

                main.SpellDescriptionVariables.Items.Add(id);

                body.lookup.Add(temp);

                boxIndex++;
            }
        }

        public void UpdateSpellDescriptionVariablesSelection()
        {
            int ID = (int)spell.body.records[main.selectedID].record.SpellDescriptionVariableID;

            if (ID == 0)
            {
                main.SpellDescriptionVariables.SelectedIndex = 0;

                return;
            }

            for (int i = 0; i < body.lookup.Count; ++i)
            {
                if (ID == body.lookup[i].ID)
                {
                    main.SpellDescriptionVariables.SelectedIndex = body.lookup[i].comboBoxIndex;

                    break;
                }
            }
        }

        public struct SpellDescriptionVariables_DBC_Map
        {
            public SpellDescriptionVariables_DBC_Record[] records;
            public List<SpellDescriptionVariablesLookup> lookup;
        };

        public struct SpellDescriptionVariablesLookup
        {
            public int ID;
            public int comboBoxIndex;
        };

        public struct SpellDescriptionVariables_DBC_Record
        {
            public UInt32 ID;
            public UInt32 Formula;
        };
    };
};
