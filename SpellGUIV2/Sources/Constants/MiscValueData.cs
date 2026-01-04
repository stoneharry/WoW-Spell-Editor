using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellEditor.Sources.DBC;

namespace SpellEditor.Sources.Constants
{
    public enum spellEffectTypes
    {
        NONE = 0,
        INSTAKILL = 1,
        SCHOOL_DAMAGE = 2,
        DUMMY = 3,
        PORTAL_TELEPORT = 4,
        TELEPORT_UNITS = 5,
        APPLY_AURA = 6,
        ENVIRONMENTAL_DAMAGE = 7,
        POWER_DRAIN = 8,
        HEALTH_LEECH = 9,
        HEAL = 10,
        BIND = 11,
        PORTAL = 12,
        RITUAL_BASE = 13,
        RITUAL_SPECIALIZE = 14,
        RITUAL_ACTIVATE_PORTAL = 15,
        QUEST_COMPLETE = 16,
        WEAPON_DAMAGE_NOSCHOOL = 17,
        RESURRECT = 18,
        ADD_EXTRA_ATTACKS = 19,
        DODGE = 20,
        EVADE = 21,
        PARRY = 22,
        BLOCK = 23,
        CREATE_ITEM = 24,
        WEAPON = 25,
        DEFENSE = 26,
        PERSISTENT_AREA_AURA = 27,
        SUMMON = 28,
        LEAP = 29,
        ENERGIZE = 30,
        WEAPON_PERCENT_DAMAGE = 31,
        TRIGGER_MISSILE = 32,
        OPEN_LOCK = 33,
        SUMMON_CHANGE_ITEM = 34,
        APPLY_AREA_AURA_PARTY = 35,
        LEARN_SPELL = 36,
        SPELL_DEFENSE = 37,
        DISPEL = 38,
        LANGUAGE = 39,
        DUAL_WIELD = 40,
        JUMP = 41, // vanilla : summon wild
        JUMP_DEST = 42, // vanilla : summon guardian
        TELEPORT_UNITS_FACE_CASTER = 43,
        SKILL_STEP = 44,
        ADD_HONOR = 45,
        SPAWN = 46,
        TRADE_SKILL = 47,
        STEALTH = 48,
        DETECT = 49,
        TRANS_DOOR = 50, // also named summon object
        FORCE_CRITICAL_HIT = 51,
        GUARANTEE_HIT = 52,
        ENCHANT_ITEM = 53,
        ENCHANT_ITEM_TEMPORARY = 54,
        TAMECREATURE = 55,
        SUMMON_PET = 56,
        LEARN_PET_SPELL = 57,
        WEAPON_DAMAGE = 58,
        CREATE_RANDOM_ITEM = 59, // also named Open Lock (Item)
        PROFICIENCY = 60,
        SEND_EVENT = 61,
        POWER_BURN = 62,
        THREAT = 63,
        TRIGGER_SPELL = 64,
        APPLY_AREA_AURA_RAID = 65, // vanilla : health funnel
        CREATE_MANA_GEM = 66, // vanilla : power funnel
        HEAL_MAX_HEALTH = 67,
        INTERRUPT_CAST = 68,
        DISTRACT = 69,
        PULL = 70,
        PICKPOCKET = 71,
        ADD_FARSIGHT = 72,
        UNTRAIN_TALENTS = 73,
        APPLY_GLYPH = 74, // vanilla : summon totem
        HEAL_MECHANICAL = 75,
        SUMMON_OBJECT_WILD = 76,
        SCRIPT_EFFECT = 77,
        ATTACK = 78,
        SANCTUARY = 79,
        ADD_COMBO_POINTS = 80,
        CREATE_HOUSE = 81,
        BIND_SIGHT = 82,
        DUEL = 83,
        STUCK = 84,
        SUMMON_PLAYER = 85,
        ACTIVATE_OBJECT = 86,
        GAMEOBJECT_DAMAGE = 87, // vanilla : Summon Totem (slot 1)
        GAMEOBJECT_REPAIR = 88, // vanilla : Summon Totem (slot 2)
        GAMEOBJECT_SET_DESTRUCTION_STATE = 89, // vanilla : Summon Totem (slot 3)
        KILL_CREDIT = 90, // vanilla : Summon Totem (slot 4)
        THREAT_ALL = 91,
        ENCHANT_HELD_ITEM = 92,
        FORCE_DESELECT = 93, // vanilla : summon phantasm
        SELF_RESURRECT = 94,
        SKINNING = 95,
        CHARGE = 96,
        CAST_BUTTON = 97,
        KNOCK_BACK = 98,
        DISENCHANT = 99,
        INEBRIATE = 100,
        FEED_PET = 101,
        DISMISS_PET = 102,
        REPUTATION = 103,
        SUMMON_OBJECT_SLOT1 = 104,
        SUMMON_OBJECT_SLOT2 = 105,
        SUMMON_OBJECT_SLOT3 = 106,
        SUMMON_OBJECT_SLOT4 = 107,
        DISPEL_MECHANIC = 108,
        RESURRECT_PET = 109,
        DESTROY_ALL_TOTEMS = 110,
        DURABILITY_DAMAGE = 111,
        UNUSED_112 = 112,
        RESURRECT_NEW = 113,
        ATTACK_ME = 114,
        DURABILITY_DAMAGE_PCT = 115,
        SKIN_PLAYER_CORPSE = 116,
        SPIRIT_HEAL = 117,
        SKILL = 118,
        APPLY_AREA_AURA_PET = 119,
        TELEPORT_GRAVEYARD = 120,
        NORMALIZED_WEAPON_DMG = 121,
        UNUSED_122 = 122,
        SEND_TAXI = 123,
        PULL_TOWARDS = 124,
        MODIFY_THREAT_PERCENT = 125,
        STEAL_BENEFICIAL_BUFF = 126,
        PROSPECTING = 127,
        APPLY_AREA_AURA_FRIEND = 128,
        APPLY_AREA_AURA_ENEMY = 129,
        REDIRECT_THREAT = 130,
        PLAY_SOUND = 131,
        PLAY_MUSIC = 132,
        UNLEARN_SPECIALIZATION = 133,
        KILL_CREDIT2 = 134,
        CALL_PET = 135,
        HEAL_PCT = 136,
        ENERGIZE_PCT = 137,
        LEAP_BACK = 138,
        CLEAR_QUEST = 139,
        FORCE_CAST = 140,
        FORCE_CAST_WITH_VALUE = 141,
        TRIGGER_SPELL_WITH_VALUE = 142,
        APPLY_AREA_AURA_OWNER = 143,
        KNOCK_BACK_DEST = 144,
        PULL_TOWARDS_DEST = 145,
        ACTIVATE_RUNE = 146,
        QUEST_FAIL = 147,
        TRIGGER_MISSILE_SPELL_WITH_VALUE = 148,
        CHARGE_DEST = 149,
        QUEST_START = 150,
        TRIGGER_SPELL_2 = 151,
        SUMMON_RAF_FRIEND = 152,
        CREATE_TAMED_PET = 153,
        DISCOVER_TAXI = 154,
        TITAN_GRIP = 155,
        ENCHANT_ITEM_PRISMATIC = 156,
        CREATE_ITEM_2 = 157,
        MILLING = 158,
        ALLOW_RENAME_PET = 159,
        FORCE_CAST_2 = 160,
        TALENT_SPEC_COUNT = 161,
        TALENT_SPEC_SELECT = 162,
        UNUSED_163 = 163,
        REMOVE_AURA = 164
    }

    public enum MiscValueType
    {
        UNKNOWN = -1, // for unknown stuff
        None = 0,
        SkillId = 1,
        CreatureId = 2,
        SchoolsMask = 3,
        DispelType = 4,
        StatType = 5,
        LanguageType = 6,
        CreatureDisplayInfo = 7,
        LockType = 8,
        CreatureType = 9, // not masked
        SpellEffect = 10,
        AuraState = 11, // StateEffect 
        GameObjectId = 12,
        Enchantment = 13,
        ShapeshiftForm = 14, // ShapeshiftType
        PowerType = 15,
        // SPECIAL_MISC_CAST_UI_OBSOLETE = 16,
        SpellMechanic = 17,
        ItemId = 18,
        ActivateObjectActions = 19, // https://wowdev.wiki/EnumeratedString#SpellEffect::MiscValue%5B0%5D_for_Effect_86
        InvisibilityType = 20,
        WhatToModify = 21, // AuraModType
        WhatToModify2 = 22,
        ScriptPackage = 23, // might be able to get this from db
        // FactionUNKNOWN = 24, // factionID in officia as name as 24, maybe some duplicate
        FactionId = 25,
        CreatureImmunities = 26,
        SlotType = 27, // equipped_item_inventory_type_mask_strings. may be itemsubclass 7 (trade goods)
        CreatureTypesMask = 28,
        // AuraVision = 29,
        TaxiPath = 30,
        StealthType = 31,
        CombatRatingMask = 32,
        SkillCategory = 33,
        SkinningType = 34,
        PreventCombatResults = 35,
        Sound = 36,
        // Music = 37,
        FactionTemplate = 39,
        SummonProperties = 43, // named SummonData officialy, but it's summonProperties.dbc
        ScreenEffect = 44,
        DestructibleState = 45,
        Loot = 47,
        OverrideSpells = 52,
        Glyph = 50, // glyphproperties.dbc
        PowerTypesMask = 53,
        Number = 56,
        Quest = 58,

        Phase = 63, // phase mask ?
        TaxiVendor = 73, // TaxiNodes.dbc? could be some creature list
        JumpChargeParams = 137,

        RuneType = 1000, // not in official enum...
        Dummy = 1001
    }

    public enum RessourceType
    {
        None,
        DBC, // data found in client DBC
        StringList, // Data stored in language xaml ressource file
        DB // data stored in emulator database
    }

    public enum ControlType
    {
        TextBox,
        ComboboxList,
        ComboboxMask,
        ListSelector
    }

    public class MiscValueTypeData
    {
        public ControlType ControlType { get; }
        public RessourceType RessourceType { get; }

        public MiscValueTypeData(ControlType control_type, RessourceType ressource_type)
        {
            this.ControlType = control_type;
            this.RessourceType = ressource_type;
        }
    }


    public class SpellEffectData
    {
        public bool usesPoints = true;
        public string PointsName { get; }
        // bool UsesMechanic
        // bool UsesTargetB = false;
        public bool UsesAmplitude = false;
        public bool usesMultipleValue = false;
        public bool UsesChainTarget = true;
        public bool UsesItemType = false;
        public bool UsesSpell = false; // trigger spell
        public MiscValueType MiscValueA = MiscValueType.None;
        public MiscValueType MiscValueB = MiscValueType.None;
        // public bool usesClassMask = false; // TODO
        public bool usesAura = false;
        // radius is based on target types?
        public bool usesDamageMultipler = true; // TODO
        public bool usesBonusMultiplier = true; // TODO

    };

    public struct MiscValuePair
    {
        public MiscValueType ValueA { get; }
        public MiscValueType ValueB { get; }

        public MiscValuePair(MiscValueType valueA = MiscValueType.None, MiscValueType valueB = MiscValueType.None)
        {
            this.ValueA = valueA;
            this.ValueB = valueB;
        }
    };

    // misc value related permanant data
    public static class MiscValueConstants
    {

        // map effects/auras to special miscvalues
        public static readonly Dictionary<spellEffectTypes, SpellEffectData> spellEffectsData = new Dictionary<spellEffectTypes, SpellEffectData>
        {
            { spellEffectTypes.INSTAKILL, new SpellEffectData() { UsesChainTarget = true } },
            { spellEffectTypes.SCHOOL_DAMAGE, new SpellEffectData() { usesPoints = true, UsesChainTarget = true } },
            { spellEffectTypes.DUMMY, new SpellEffectData() { usesPoints = true, UsesAmplitude = true, usesMultipleValue = true, UsesChainTarget = true,
                            UsesItemType = true, UsesSpell = true, MiscValueA = MiscValueType.Number, MiscValueB = MiscValueType.Number } }, // allow all
            { spellEffectTypes.PORTAL_TELEPORT, new SpellEffectData() },
            { spellEffectTypes.TELEPORT_UNITS, new SpellEffectData() },
            { spellEffectTypes.APPLY_AURA, new SpellEffectData() { UsesChainTarget = true, usesPoints = true, usesAura = true } },
            { spellEffectTypes.ENVIRONMENTAL_DAMAGE, new SpellEffectData(){ usesPoints = true } },
            { spellEffectTypes.POWER_DRAIN, new SpellEffectData() { UsesChainTarget = true, usesPoints = true, usesMultipleValue = true, MiscValueA = MiscValueType.PowerType } },
            { spellEffectTypes.HEALTH_LEECH, new SpellEffectData() { UsesChainTarget = true, usesPoints = true, usesMultipleValue = true,  } },
            { spellEffectTypes.HEAL, new SpellEffectData() { usesPoints = true, UsesChainTarget = true } },
            { spellEffectTypes.BIND, new SpellEffectData() { MiscValueA = MiscValueType.UNKNOWN } }, // miscvalue1 seems to be some type of db location entry
            { spellEffectTypes.PORTAL, new SpellEffectData() },
            { spellEffectTypes.RITUAL_BASE, new SpellEffectData() },
            { spellEffectTypes.RITUAL_SPECIALIZE, new SpellEffectData() },
            { spellEffectTypes.RITUAL_ACTIVATE_PORTAL, new SpellEffectData() },
            { spellEffectTypes.QUEST_COMPLETE, new SpellEffectData() },
            { spellEffectTypes.WEAPON_DAMAGE_NOSCHOOL, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.RESURRECT, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.ADD_EXTRA_ATTACKS, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.DODGE, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.EVADE, new SpellEffectData() },
            { spellEffectTypes.PARRY, new SpellEffectData() },
            { spellEffectTypes.BLOCK, new SpellEffectData() },
            { spellEffectTypes.CREATE_ITEM, new SpellEffectData() { usesPoints = true, UsesItemType = true } },
            { spellEffectTypes.WEAPON, new SpellEffectData() },
            { spellEffectTypes.DEFENSE, new SpellEffectData() },
            { spellEffectTypes.PERSISTENT_AREA_AURA, new SpellEffectData() { usesAura = true } },
            { spellEffectTypes.SUMMON, new SpellEffectData() { usesPoints = true, usesMultipleValue = true, MiscValueA = MiscValueType.CreatureId, MiscValueB = MiscValueType.SummonProperties } },
            { spellEffectTypes.LEAP, new SpellEffectData() },
            { spellEffectTypes.ENERGIZE, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.PowerType } },
            { spellEffectTypes.WEAPON_PERCENT_DAMAGE, new SpellEffectData() { usesPoints = true, UsesChainTarget = true } },
            { spellEffectTypes.TRIGGER_MISSILE, new SpellEffectData() { usesPoints = true, UsesSpell = true } },
            { spellEffectTypes.OPEN_LOCK, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.LockType } },
            { spellEffectTypes.SUMMON_CHANGE_ITEM, new SpellEffectData() { UsesItemType = true } },
            { spellEffectTypes.APPLY_AREA_AURA_PARTY, new SpellEffectData() {usesAura = true } },
            { spellEffectTypes.LEARN_SPELL, new SpellEffectData() { usesPoints = true, UsesSpell = true } },
            { spellEffectTypes.SPELL_DEFENSE, new SpellEffectData() },
            { spellEffectTypes.DISPEL, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.DispelType } },
            { spellEffectTypes.LANGUAGE, new SpellEffectData() { MiscValueA = MiscValueType.LanguageType } },
            { spellEffectTypes.DUAL_WIELD, new SpellEffectData() },
            { spellEffectTypes.JUMP, new SpellEffectData() { usesMultipleValue = true, MiscValueA = MiscValueType.JumpChargeParams, MiscValueB = MiscValueType.JumpChargeParams } },
            { spellEffectTypes.JUMP_DEST, new SpellEffectData() { usesMultipleValue = true, MiscValueA = MiscValueType.JumpChargeParams, MiscValueB = MiscValueType.JumpChargeParams } },
            { spellEffectTypes.TELEPORT_UNITS_FACE_CASTER, new SpellEffectData() },
            { spellEffectTypes.SKILL_STEP, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.SkillId } },
            { spellEffectTypes.ADD_HONOR, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.SPAWN, new SpellEffectData() },
            { spellEffectTypes.TRADE_SKILL, new SpellEffectData() },
            { spellEffectTypes.STEALTH, new SpellEffectData() },
            { spellEffectTypes.DETECT, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.TRANS_DOOR, new SpellEffectData() { MiscValueA = MiscValueType.GameObjectId } },
            { spellEffectTypes.FORCE_CRITICAL_HIT, new SpellEffectData() },
            { spellEffectTypes.GUARANTEE_HIT, new SpellEffectData() },
            { spellEffectTypes.ENCHANT_ITEM, new SpellEffectData() { UsesItemType = true, MiscValueA = MiscValueType.Enchantment, MiscValueB = MiscValueType.UNKNOWN } }, // may be itemsubclass 7 (trade goods) 14 = armor, 15 = weapon. This might define veluns but TC uses item requirement instead
            { spellEffectTypes.ENCHANT_ITEM_TEMPORARY, new SpellEffectData() { MiscValueA = MiscValueType.Enchantment } },
            { spellEffectTypes.TAMECREATURE, new SpellEffectData() { } },
            { spellEffectTypes.SUMMON_PET, new SpellEffectData() { usesPoints = true, usesMultipleValue = true, MiscValueA = MiscValueType.CreatureId } },
            { spellEffectTypes.LEARN_PET_SPELL, new SpellEffectData() { UsesSpell = true } },
            { spellEffectTypes.WEAPON_DAMAGE, new SpellEffectData() { usesPoints = true, UsesChainTarget = true } },
            { spellEffectTypes.CREATE_RANDOM_ITEM, new SpellEffectData() { MiscValueA = MiscValueType.Loot } },
            { spellEffectTypes.PROFICIENCY, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.SEND_EVENT, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.ScriptPackage } }, // event_scripts id
            { spellEffectTypes.POWER_BURN, new SpellEffectData() { usesPoints = true, usesMultipleValue = true, UsesChainTarget = true } },
            { spellEffectTypes.THREAT, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.TRIGGER_SPELL, new SpellEffectData() { usesPoints = true, UsesSpell = true } },
            { spellEffectTypes.APPLY_AREA_AURA_RAID, new SpellEffectData() { usesAura = true } },
            { spellEffectTypes.CREATE_MANA_GEM, new SpellEffectData() { usesPoints = true, UsesItemType = true } },
            { spellEffectTypes.HEAL_MAX_HEALTH, new SpellEffectData() },
            { spellEffectTypes.INTERRUPT_CAST, new SpellEffectData() },
            { spellEffectTypes.DISTRACT, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.PULL, new SpellEffectData() },
            { spellEffectTypes.PICKPOCKET, new SpellEffectData() },
            { spellEffectTypes.ADD_FARSIGHT, new SpellEffectData() },
            { spellEffectTypes.UNTRAIN_TALENTS, new SpellEffectData() },
            { spellEffectTypes.APPLY_GLYPH, new SpellEffectData() { MiscValueA= MiscValueType.Glyph } },
            { spellEffectTypes.HEAL_MECHANICAL, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.SUMMON_OBJECT_WILD, new SpellEffectData() { MiscValueA = MiscValueType.GameObjectId } },
            { spellEffectTypes.SCRIPT_EFFECT, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.ScriptPackage } },
            { spellEffectTypes.ATTACK, new SpellEffectData() },
            { spellEffectTypes.SANCTUARY, new SpellEffectData() },
            { spellEffectTypes.ADD_COMBO_POINTS, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.CREATE_HOUSE, new SpellEffectData() { MiscValueA = MiscValueType.UNKNOWN } },
            { spellEffectTypes.BIND_SIGHT, new SpellEffectData() },
            { spellEffectTypes.DUEL, new SpellEffectData() { MiscValueA = MiscValueType.UNKNOWN } },
            { spellEffectTypes.STUCK, new SpellEffectData() },
            { spellEffectTypes.SUMMON_PLAYER, new SpellEffectData() },
            { spellEffectTypes.ACTIVATE_OBJECT, new SpellEffectData() { MiscValueA = MiscValueType.ActivateObjectActions } },
            { spellEffectTypes.GAMEOBJECT_DAMAGE, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.GAMEOBJECT_REPAIR, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.GAMEOBJECT_SET_DESTRUCTION_STATE, new SpellEffectData() { MiscValueA = MiscValueType.DestructibleState  } },
            { spellEffectTypes.KILL_CREDIT, new SpellEffectData() { MiscValueA = MiscValueType.CreatureId } },
            { spellEffectTypes.THREAT_ALL, new SpellEffectData() },
            { spellEffectTypes.ENCHANT_HELD_ITEM, new SpellEffectData() { MiscValueA = MiscValueType.Enchantment } },
            { spellEffectTypes.FORCE_DESELECT, new SpellEffectData() },
            { spellEffectTypes.SELF_RESURRECT, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.Number } }, // miscvaue = mana
            { spellEffectTypes.SKINNING, new SpellEffectData() { MiscValueA = MiscValueType.SkinningType } },
            { spellEffectTypes.CHARGE, new SpellEffectData() },
            { spellEffectTypes.CAST_BUTTON, new SpellEffectData() { MiscValueA = MiscValueType.UNKNOWN, MiscValueB = MiscValueType.UNKNOWN } }, // also summon critter before 3.2.  // uint32 button_id = effectInfo->MiscValue + 132; uint32 n_buttons = effectInfo->MiscValueB;
            { spellEffectTypes.KNOCK_BACK, new SpellEffectData() { usesPoints = true, UsesChainTarget = true, MiscValueA = MiscValueType.Number, MiscValueB = MiscValueType.Number } },
            { spellEffectTypes.DISENCHANT, new SpellEffectData() },
            { spellEffectTypes.INEBRIATE, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.FEED_PET, new SpellEffectData() { usesPoints = true, UsesSpell = true } },
            { spellEffectTypes.DISMISS_PET, new SpellEffectData() },
            { spellEffectTypes.REPUTATION, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.FactionId } },
            { spellEffectTypes.SUMMON_OBJECT_SLOT1, new SpellEffectData() { MiscValueA = MiscValueType.GameObjectId } },
            { spellEffectTypes.SUMMON_OBJECT_SLOT2, new SpellEffectData() { MiscValueA = MiscValueType.GameObjectId } },
            { spellEffectTypes.SUMMON_OBJECT_SLOT3, new SpellEffectData() { MiscValueA = MiscValueType.GameObjectId } },
            { spellEffectTypes.SUMMON_OBJECT_SLOT4, new SpellEffectData() { MiscValueA = MiscValueType.GameObjectId } },
            { spellEffectTypes.DISPEL_MECHANIC, new SpellEffectData() { UsesChainTarget = true, MiscValueA = MiscValueType.SpellMechanic } },
            { spellEffectTypes.RESURRECT_PET, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.DESTROY_ALL_TOTEMS, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.DURABILITY_DAMAGE, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.SlotType } }, // 15 = weapon, 16 = shield, ranged = 17, 4 = chest armor. same as effect 53
            { spellEffectTypes.UNUSED_112, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.RESURRECT_NEW, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.Number } },
            { spellEffectTypes.ATTACK_ME, new SpellEffectData() },
            { spellEffectTypes.DURABILITY_DAMAGE_PCT, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.SlotType } },
            { spellEffectTypes.SKIN_PLAYER_CORPSE, new SpellEffectData() },
            { spellEffectTypes.SPIRIT_HEAL, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.SKILL, new SpellEffectData() { MiscValueA = MiscValueType.SkillId } },
            { spellEffectTypes.APPLY_AREA_AURA_PET, new SpellEffectData() { usesAura = true } },
            { spellEffectTypes.TELEPORT_GRAVEYARD, new SpellEffectData() },
            { spellEffectTypes.NORMALIZED_WEAPON_DMG, new SpellEffectData() { usesPoints = true, UsesChainTarget = true } },
            { spellEffectTypes.UNUSED_122, new SpellEffectData() },
            { spellEffectTypes.SEND_TAXI, new SpellEffectData() { MiscValueA = MiscValueType.TaxiPath } },
            { spellEffectTypes.PULL_TOWARDS, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.Number } },
            { spellEffectTypes.MODIFY_THREAT_PERCENT, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.STEAL_BENEFICIAL_BUFF, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.UNKNOWN } }, // unknown
            { spellEffectTypes.PROSPECTING, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.APPLY_AREA_AURA_FRIEND, new SpellEffectData() { usesAura = true } },
            { spellEffectTypes.APPLY_AREA_AURA_ENEMY, new SpellEffectData() { usesAura = true } },
            { spellEffectTypes.REDIRECT_THREAT, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.PLAY_SOUND, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.Sound } },
            { spellEffectTypes.PLAY_MUSIC, new SpellEffectData() { MiscValueA = MiscValueType.Sound } },
            { spellEffectTypes.UNLEARN_SPECIALIZATION, new SpellEffectData() { UsesSpell = true } },
            { spellEffectTypes.KILL_CREDIT2, new SpellEffectData() { MiscValueA = MiscValueType.CreatureId } },
            { spellEffectTypes.CALL_PET, new SpellEffectData() },
            { spellEffectTypes.HEAL_PCT, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.ENERGIZE_PCT, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.PowerType } },
            { spellEffectTypes.LEAP_BACK, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.Number } },
            { spellEffectTypes.CLEAR_QUEST, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.Quest } },
            { spellEffectTypes.FORCE_CAST, new SpellEffectData() { UsesSpell = true } },
            { spellEffectTypes.FORCE_CAST_WITH_VALUE, new SpellEffectData() { usesPoints = true, UsesSpell = true } },
            { spellEffectTypes.TRIGGER_SPELL_WITH_VALUE, new SpellEffectData() { usesPoints = true, UsesSpell = true } },
            { spellEffectTypes.APPLY_AREA_AURA_OWNER, new SpellEffectData() { usesAura = true } },
            { spellEffectTypes.KNOCK_BACK_DEST, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.Number } },
            { spellEffectTypes.PULL_TOWARDS_DEST, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.Number } },
            { spellEffectTypes.ACTIVATE_RUNE, new SpellEffectData() { usesPoints = true, MiscValueA = MiscValueType.RuneType } },
            { spellEffectTypes.QUEST_FAIL, new SpellEffectData() { MiscValueA = MiscValueType.Quest } },
            { spellEffectTypes.TRIGGER_MISSILE_SPELL_WITH_VALUE, new SpellEffectData() { usesPoints = true, UsesSpell = true } },
            { spellEffectTypes.CHARGE_DEST, new SpellEffectData() },
            { spellEffectTypes.QUEST_START, new SpellEffectData() { MiscValueA = MiscValueType.Quest } },
            { spellEffectTypes.TRIGGER_SPELL_2, new SpellEffectData() { UsesSpell = true } },
            { spellEffectTypes.SUMMON_RAF_FRIEND, new SpellEffectData() { UsesSpell = true } },
            { spellEffectTypes.CREATE_TAMED_PET, new SpellEffectData() { MiscValueA = MiscValueType.CreatureId } },
            { spellEffectTypes.DISCOVER_TAXI, new SpellEffectData() { MiscValueA = MiscValueType.TaxiVendor } }, // TaxiNodes.dbc
            { spellEffectTypes.TITAN_GRIP, new SpellEffectData() { UsesSpell = true } },
            { spellEffectTypes.ENCHANT_ITEM_PRISMATIC, new SpellEffectData() { MiscValueA = MiscValueType.Enchantment } },
            { spellEffectTypes.CREATE_ITEM_2, new SpellEffectData() { UsesItemType = true, MiscValueA = MiscValueType.Loot } },
            { spellEffectTypes.MILLING, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.ALLOW_RENAME_PET, new SpellEffectData() },
            { spellEffectTypes.FORCE_CAST_2, new SpellEffectData() { UsesSpell = true } },
            { spellEffectTypes.TALENT_SPEC_COUNT, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.TALENT_SPEC_SELECT, new SpellEffectData() { usesPoints = true } },
            { spellEffectTypes.UNUSED_163, new SpellEffectData() },
            { spellEffectTypes.REMOVE_AURA, new SpellEffectData() { usesPoints = true, UsesSpell = true} },
        };


        public static readonly Dictionary<int, MiscValuePair> auraMiscValues = new Dictionary<int, MiscValuePair>
        {
            { 4, new MiscValuePair(MiscValueType.Dummy, MiscValueType.Dummy) }, // DUMMY

            { 10, new MiscValuePair(MiscValueType.SchoolsMask) }, // SPELL_AURA_MOD_THREAT
            { 13, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 14, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 16, new MiscValuePair(MiscValueType.StealthType) },
            { 17, new MiscValuePair(MiscValueType.StealthType) },
            { 18, new MiscValuePair(MiscValueType.InvisibilityType) },
            { 19, new MiscValuePair(MiscValueType.InvisibilityType) },
            { 21, new MiscValuePair(MiscValueType.PowerTypesMask) }, // Mask
            { 22, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 24, new MiscValuePair(MiscValueType.PowerType) }, // not masked
            { 29, new MiscValuePair(MiscValueType.StatType) },
            { 30, new MiscValuePair(MiscValueType.SkillId) },
            { 35, new MiscValuePair(MiscValueType.PowerType) }, // Not masked !
            { 36, new MiscValuePair(MiscValueType.ShapeshiftForm) },
            { 37, new MiscValuePair(MiscValueType.SpellEffect) }, // SPELL_AURA_EFFECT_IMMUNITY
            { 38, new MiscValuePair(MiscValueType.AuraState) }, // used by SPELL_AURA_STATE_IMMUNITY
            { 39, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 40, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 41, new MiscValuePair(MiscValueType.DispelType) },
            { 44, new MiscValuePair(MiscValueType.CreatureType) },
            { 45, new MiscValuePair(MiscValueType.LockType) },
            { 56, new MiscValuePair(MiscValueType.CreatureId) },
            { 59, new MiscValuePair(MiscValueType.CreatureTypesMask) },
            { 69, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 71, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 72, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 73, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 74, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 75, new MiscValuePair(MiscValueType.LanguageType) },
            { 77, new MiscValuePair(MiscValueType.SpellMechanic) },
            { 78, new MiscValuePair(MiscValueType.CreatureId) },
            { 79, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 80, new MiscValuePair(MiscValueType.StatType) },
            { 81, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 83, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 85, new MiscValuePair(MiscValueType.PowerType) }, // not masked, checked later xpacs
            // 86 exists in vanilla/bc
            { 87, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 90, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 97, new MiscValuePair(MiscValueType.PowerTypesMask) }, // this is actually a mask
            { 98, new MiscValuePair(MiscValueType.SkillId) },
            { 101, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 102, new MiscValuePair(MiscValueType.CreatureTypesMask) },
            { 107, new MiscValuePair(MiscValueType.WhatToModify) },
            { 108, new MiscValuePair(MiscValueType.WhatToModify) },
            { 109, new MiscValuePair(MiscValueType.WhatToModify2) }, // ? fromSpellAuraNames.dbc
            { 110, new MiscValuePair(MiscValueType.PowerType) }, // not masked
            { 111, new MiscValuePair(MiscValueType.WhatToModify2) }, // fromSpellAuraNames.dbc
            { 112, new MiscValuePair(MiscValueType.ScriptPackage) },
            { 113, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 114, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 115, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 117, new MiscValuePair(MiscValueType.SpellMechanic) },
            { 118, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 131, new MiscValuePair(MiscValueType.CreatureTypesMask) },
            { 132, new MiscValuePair(MiscValueType.PowerType) }, // never more than 0
            { 135, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 136, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 137, new MiscValuePair(MiscValueType.StatType) },
            { 139, new MiscValuePair(MiscValueType.FactionId) },
            { 142, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 143, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 147, new MiscValuePair(MiscValueType.CreatureImmunities) },
            { 151, new MiscValuePair(MiscValueType.StealthType) },
            { 153, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 162, new MiscValuePair(MiscValueType.PowerType) },
            { 163, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 168, new MiscValuePair(MiscValueType.CreatureTypesMask) },
            { 169, new MiscValuePair(MiscValueType.CreatureTypesMask) },
            { 174, new MiscValuePair(MiscValueType.SchoolsMask, MiscValueType.StatType) },
            { 175, new MiscValuePair(MiscValueType.StatType) },
            { 178, new MiscValuePair(MiscValueType.DispelType) },
            { 179, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 180, new MiscValuePair(MiscValueType.CreatureTypesMask) },// TODO : mask ?
            { 182, new MiscValuePair(MiscValueType.StatType) },// always 1 in wrath, emulators are hardcoded to armor which is incorrect
            { 183, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 186, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 189, new MiscValuePair(MiscValueType.CombatRatingMask) },
            { 190, new MiscValuePair(MiscValueType.FactionId) },
            { 194, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 195, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 199, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 201, new MiscValuePair(MiscValueType.CreatureId) },
            { 202, new MiscValuePair(MiscValueType.PreventCombatResults) }, // SPELL_AURA_IGNORE_COMBAT_RESULT 
            { 205, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 212, new MiscValuePair(MiscValueType.StatType) },
            { 219, new MiscValuePair(MiscValueType.StatType) },
            { 220, new MiscValuePair(MiscValueType.CombatRatingMask, MiscValueType.StatType) },
            { 229, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 232, new MiscValuePair(MiscValueType.SpellMechanic) },
            { 233, new MiscValuePair(MiscValueType.CreatureId) },
            { 234, new MiscValuePair(MiscValueType.SpellMechanic) },
            { 244, new MiscValuePair(MiscValueType.LanguageType) },
            { 245, new MiscValuePair(MiscValueType.DispelType) },
            { 246, new MiscValuePair(MiscValueType.DispelType) },
            // {248, new MiscValueType[] { MiscValueType.CombatRating } },// SPELL_AURA_MOD_COMBAT_RESULT_CHANCE. ??? uses 2 = dodge
            { 249, new MiscValuePair(MiscValueType.RuneType, MiscValueType.RuneType) },
            { 255, new MiscValuePair(MiscValueType.SpellMechanic) },
            // 256 UNKNOWN
            { 259, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 260, new MiscValuePair(MiscValueType.ScreenEffect) }, //SPELL_AURA_SCREEN_EFFECT 
            { 261, new MiscValuePair(MiscValueType.Phase) },
            { 267, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 268, new MiscValuePair(MiscValueType.StatType) },
            { 269, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 270, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 271, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 276, new MiscValuePair(MiscValueType.SpellMechanic) }, // unused
            { 293, new MiscValuePair(MiscValueType.OverrideSpells) },
            { 294, new MiscValuePair(MiscValueType.PowerTypesMask) }, // seems to be mask because mana = 1 instead of 0
            { 300, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 301, new MiscValuePair(MiscValueType.SchoolsMask) },
            { 303, new MiscValuePair(MiscValueType.AuraState) }, // TODO
            { 310, new MiscValuePair(MiscValueType.SchoolsMask) },
        };

        // map miscvalue types to UI element
        public static readonly Dictionary<MiscValueType, string> miscValueMappedStringsRessource = new Dictionary<MiscValueType, string>
        {
            {MiscValueType.SchoolsMask, "school_names" },
            {MiscValueType.StealthType, "stealth_types" },
            {MiscValueType.StatType, "stat_names" },
            {MiscValueType.InvisibilityType, "invisibility_strings" },
            {MiscValueType.PowerType, "power_types" },
            {MiscValueType.PowerTypesMask,"power_types_mask" },
            {MiscValueType.WhatToModify, "what_to_mod_1" },
            // {MiscValueType.WhatToModify2, "what_to_mod_2" },
            // {MiscValueType.CreatureImmunitiesMask, "Creature_immunities_mask" },
            {MiscValueType.CombatRatingMask, "combat_ratings" },
            {MiscValueType.PreventCombatResults, "combat_results" }, // trinitycore : MeleeHitOutcome enum
            {MiscValueType.SlotType, "equipped_item_inventory_type_mask_strings" },

            {MiscValueType.SpellEffect, "spell_effect_names_clear" },
            {MiscValueType.AuraState, "spell_aura_effect_names" },
            {MiscValueType.SkinningType, "skinning_types" },
            {MiscValueType.ActivateObjectActions, "activate_object_actions_names" },
            {MiscValueType.DestructibleState, "destructible_states_names" },

        };

        // maps miscvalues to the associated dbc file name
        public static readonly Dictionary<MiscValueType, string> miscValueMappedDbcRessource = new Dictionary<MiscValueType, string>
        {
            {MiscValueType.SkillId, "SkillLine" },
            {MiscValueType.DispelType, "SpellDispelType" },
            {MiscValueType.LanguageType, "Languages" },
            {MiscValueType.CreatureDisplayInfo, "CreatureDisplayInfo" },
            {MiscValueType.LockType, "LockType" },
            {MiscValueType.CreatureType, "CreatureType" },
            {MiscValueType.Enchantment, "SpellItemEnchantment" },
            {MiscValueType.ShapeshiftForm, "SpellShapeshiftForm" },
            {MiscValueType.SpellMechanic, "SpellMechanic" },
            {MiscValueType.FactionTemplate, "FactionTemplate" }, // ?
            {MiscValueType.FactionId, "Faction" },
            {MiscValueType.CreatureTypesMask, "CreatureType" },
            {MiscValueType.TaxiPath, "TaxiPath" },
            {MiscValueType.SkillCategory, "SkillLineCategory" },
            {MiscValueType.ScreenEffect, "ScreenEffect" },
            {MiscValueType.OverrideSpells, "OverrideSpellData" },
            {MiscValueType.Sound, "SoundEntries" },
            {MiscValueType.SummonProperties, "SummonProperties" },
            {MiscValueType.Glyph, "GlyphProperties" },
            {MiscValueType.TaxiVendor, "TaxiNodes" },

        };

        public static readonly Dictionary<MiscValueType, MiscValueTypeData> MiscValueTypesData = new Dictionary<MiscValueType, MiscValueTypeData>
        {
            { MiscValueType.UNKNOWN, new MiscValueTypeData(ControlType.TextBox, RessourceType.None) },

            { MiscValueType.Dummy, new MiscValueTypeData(ControlType.TextBox, RessourceType.None) },
            { MiscValueType.SkillId, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DBC) },
            { MiscValueType.CreatureId, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DB) },
            { MiscValueType.SchoolsMask, new MiscValueTypeData(ControlType.ComboboxMask, RessourceType.StringList) },
            { MiscValueType.DispelType, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.DBC) },
            { MiscValueType.StatType, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.StringList) },
            { MiscValueType.LanguageType, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.DBC) },
            { MiscValueType.CreatureDisplayInfo, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DBC) },
            { MiscValueType.LockType, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.DBC) },
            { MiscValueType.CreatureType, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.DBC) },
            { MiscValueType.SpellEffect, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.StringList) },
            { MiscValueType.AuraState, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.StringList) },
            { MiscValueType.GameObjectId, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DB) },
            { MiscValueType.Enchantment, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DBC) },
            { MiscValueType.ShapeshiftForm, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.DBC) },
            { MiscValueType.PowerType, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.StringList) },
            // SPECIAL_MISC_CAST_UI_OBSOLETE = 16,
            { MiscValueType.SpellMechanic, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.DBC) },
            { MiscValueType.ItemId, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DB) }, // db for names
            { MiscValueType.ActivateObjectActions, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.StringList) },
            { MiscValueType.InvisibilityType, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.StringList) },
            { MiscValueType.WhatToModify, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.StringList) },
            { MiscValueType.WhatToModify2, new MiscValueTypeData(ControlType.TextBox, RessourceType.None) }, // TODO
            { MiscValueType.ScriptPackage, new MiscValueTypeData(ControlType.TextBox, RessourceType.None) }, // might be able to get this from db
            // { MiscValueType.FactionUNKNOWN, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DBC) },
            { MiscValueType.FactionId, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DBC) },

            // { MiscValueType.CreatureImmunitiesMask, new MiscValueTypeData(ControlType.ComboboxMask, RessourceType.StringList) }, // TODO
            { MiscValueType.CreatureImmunities, new MiscValueTypeData(ControlType.TextBox, RessourceType.DB) }, // this is actually a database record in blizz', hardcoded in core by emus

            { MiscValueType.SlotType, new MiscValueTypeData(ControlType.ListSelector, RessourceType.StringList) }, // TODO
            { MiscValueType.CreatureTypesMask, new MiscValueTypeData(ControlType.ComboboxMask, RessourceType.DBC) },
            // AuraVision = 29,
            { MiscValueType.TaxiPath, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DBC) },
            { MiscValueType.StealthType, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.StringList) },
            { MiscValueType.CombatRatingMask, new MiscValueTypeData(ControlType.ComboboxMask, RessourceType.StringList) },
            { MiscValueType.SkillCategory, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.DBC) },
            { MiscValueType.SkinningType, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.StringList) },
            { MiscValueType.PreventCombatResults, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.StringList) },
            { MiscValueType.Sound, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DBC) }, // TODO
            { MiscValueType.FactionTemplate, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DBC) },// TODO
            { MiscValueType.SummonProperties, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DBC) }, // TODO
            { MiscValueType.DestructibleState, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.StringList) },
            { MiscValueType.Loot, new MiscValueTypeData(ControlType.TextBox, RessourceType.None) },
            { MiscValueType.Glyph, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DBC) },

            { MiscValueType.ScreenEffect, new MiscValueTypeData(ControlType.ComboboxList, RessourceType.DBC) },
            { MiscValueType.OverrideSpells, new MiscValueTypeData(ControlType.TextBox, RessourceType.None) }, // TODO dbc
            { MiscValueType.Number, new MiscValueTypeData(ControlType.TextBox, RessourceType.None) },

            { MiscValueType.Quest, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DB) },
            { MiscValueType.Phase, new MiscValueTypeData(ControlType.TextBox, RessourceType.None) },
            { MiscValueType.RuneType, new MiscValueTypeData(ControlType.TextBox, RessourceType.None) },
            { MiscValueType.PowerTypesMask, new MiscValueTypeData(ControlType.ComboboxMask, RessourceType.StringList) },

            { MiscValueType.TaxiVendor, new MiscValueTypeData(ControlType.ListSelector, RessourceType.DBC) },


            { MiscValueType.JumpChargeParams, new MiscValueTypeData(ControlType.TextBox, RessourceType.None) },

        };

        public static void VerifyData()
        {
            // Enforces implementing all known effects
            foreach (spellEffectTypes effect in Enum.GetValues(typeof(spellEffectTypes)))
            {
                if (effect != spellEffectTypes.NONE)
                    Debug.Assert(MiscValueConstants.spellEffectsData.ContainsKey(effect), $"Spell Effect {effect} doesn't have data in spellEffectsData");
            }

            // foreach (var aura in MiscValueConstants.auraMiscValues)
            // {
            // 
            // }

            foreach (MiscValueType miscvalue in Enum.GetValues(typeof(MiscValueType)))
            {
                if (miscvalue != MiscValueType.None && miscvalue != MiscValueType.UNKNOWN)
                    Debug.Assert(MiscValueConstants.MiscValueTypesData.ContainsKey(miscvalue), $"Misc value {miscvalue} doesn't have data in MiscValueTypesData");

            }

            foreach (var miscvaluedata in MiscValueConstants.MiscValueTypesData)
            {
                if (miscvaluedata.Value.RessourceType == RessourceType.None && miscvaluedata.Value.ControlType != ControlType.TextBox)
                    throw new Exception("found ressource type none in a non textbox control");

                // verify data mapping definitions exists
                if (miscvaluedata.Value.RessourceType == RessourceType.StringList)
                {
                    Debug.Assert(MiscValueConstants.miscValueMappedStringsRessource.ContainsKey(miscvaluedata.Key),
                        $"Misc value {miscvaluedata.Key.ToString()} doesn't have data in miscValueMappedStringsRessource");
                }
                else if (miscvaluedata.Value.RessourceType == RessourceType.DBC)
                {
                    Debug.Assert(MiscValueConstants.miscValueMappedDbcRessource.ContainsKey(miscvaluedata.Key),
                        $"Misc value {miscvaluedata.Key.ToString()} doesn't have data in miscValueMappedDbcRessource");
                }
            }
        }

    };

}