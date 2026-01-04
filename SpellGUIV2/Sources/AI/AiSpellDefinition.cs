using System.Collections.Generic;

namespace SpellEditor.Sources.AI
{
    /// <summary>
    /// High-level semantic spell definition used by the AI → spell mapper.
    /// Keep fields OPTIONAL so the AI can omit them.
    /// </summary>
    public class AiSpellDefinition
    {
        // BASIC INFO --------------------------------------------------------
        public string Name { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Optional short tooltip line (maps to SpellTooltip0).
        /// If omitted, the client will fall back to the description,
        /// just like many stock spells do.
        /// </summary>
        public string Tooltip { get; set; }

        /// <summary> Fire, Frost, Arcane, Nature, Shadow, Holy, Physical </summary>
        public string School { get; set; }

        /// <summary> Magic, Curse, Disease, Poison, None </summary>
        public string DispelType { get; set; }

        /// <summary> Stun, Silence, Root, Fear, Slow, etc. </summary>
        public string Mechanic { get; set; }

        public float? RangeYards { get; set; }
        public float? RadiusYards { get; set; }
        public float? MaxTargets { get; set; }

        public float? CastTimeSeconds { get; set; }
        public float? ChannelTimeSeconds { get; set; }
        public float? CooldownSeconds { get; set; }
        public float? GlobalCooldownSeconds { get; set; }

        public bool? IsInstant { get; set; }
        public bool? IsChanneled { get; set; }
        public bool? IsPassive { get; set; }

        /// <summary> Enemy, Friendly, Self, Area, Cone, Chain, etc. </summary>
        public string TargetType { get; set; }


        // DAMAGE / AURA -----------------------------------------------------

        public int? DirectDamage { get; set; }
        public int? PeriodicDamage { get; set; }
        public float? PeriodicIntervalSeconds { get; set; }

        public float? DurationSeconds { get; set; }
        public float? PeriodicIntervalOverrideSeconds { get; set; }


        // POWER COST --------------------------------------------------------

        public string PowerType { get; set; }            // Mana, Rage, Energy, RunicPower, Health
        public int? PowerCost { get; set; }
        public int? PowerCostPercentage { get; set; }

        public int? RuneCostBlood { get; set; }
        public int? RuneCostFrost { get; set; }
        public int? RuneCostUnholy { get; set; }


        // LEVELS / SCALING --------------------------------------------------

        /// <summary>BaseLevel column – minimum caster level for full effect.</summary>
        public int? BaseLevel { get; set; }

        /// <summary>MaxLevel column – maximum caster level for full effect.</summary>
        public int? MaxLevel { get; set; }

        /// <summary>SpellLevel column – "recommended" or design level.</summary>
        public int? SpellLevel { get; set; }

        /// <summary>SpellDifficultyId – used to link difficulty/variant rows.</summary>
        public int? SpellDifficultyId { get; set; }

        /// <summary>Maximum stack count for auras (StackAmount column).</summary>
        public int? MaxStacks { get; set; }


        // FAMILY / VISUALS -------------------------------------------------

        public string ClassFamily { get; set; }          // e.g. Mage, Warrior
        public int? Rank { get; set; }

        public string Icon { get; set; }
        /// <summary>
        /// Resolved icon ID (set by the editor, NOT by the AI JSON).
        /// Lets us reuse the same icon for preview + DB write.
        /// </summary>
        public uint? IconId { get; set; }
        public int? VisualId { get; set; }
        public string VisualName { get; set; }


        // PROC / TRIGGERING -------------------------------------------------

        /// <summary>Raw ProcFlags bitmask. Maps to ProcFlags column.</summary>
        public uint? ProcFlags { get; set; }

        /// <summary>Proc chance in percent (0–100). Maps to ProcChance.</summary>
        public int? ProcChance { get; set; }

        /// <summary>Maximum number of procs before the aura is removed.</summary>
        public int? ProcCharges { get; set; }

        /// <summary>Internal proc cooldown in seconds (ProcCooldown ms in DBC).</summary>
        public float? ProcCooldownSeconds { get; set; }

        /// <summary>
        /// If set, the spell row replaces another spell (ReplacesSpellId column,
        /// if present in the user's DBC schema).
        /// </summary>
        public int? ReplacesSpellId { get; set; }


        // INTERRUPT / PUSHBACK ---------------------------------------------

        /// <summary>SpellInterruptFlags bitmask (InterruptFlags column).</summary>
        public uint? InterruptFlags { get; set; }

        /// <summary>AuraInterruptFlags bitmask.</summary>
        public uint? AuraInterruptFlags { get; set; }

        /// <summary>ChannelInterruptFlags bitmask.</summary>
        public uint? ChannelInterruptFlags { get; set; }


        // CATEGORY / RECOVERY ----------------------------------------------

        /// <summary>Spell's category (Category column).</summary>
        public int? CategoryId { get; set; }

        /// <summary>Shared cooldown category (StartRecoveryCategory column).</summary>
        public int? StartRecoveryCategory { get; set; }

        /// <summary>Shared cooldown duration in seconds (StartRecoveryTime column).</summary>
        public float? StartRecoveryTimeSeconds { get; set; }


        // REAGENTS / TOTEMS -------------------------------------------------

        /// <summary>
        /// Up to 8 reagent entries. Each maps into Reagent1–8 / ReagentCount1–8.
        /// </summary>
        public List<AiReagentDefinition> Reagents { get; set; } = new List<AiReagentDefinition>();

        /// <summary>Optional totems (Totem1 / Totem2). Use raw item IDs.</summary>
        public List<int> Totems { get; set; } = new List<int>();

        /// <summary>Optional totem categories (TotemCategory1 / TotemCategory2).</summary>
        public List<int> TotemCategories { get; set; } = new List<int>();


        // EQUIPMENT / FORMS / AREA -----------------------------------------

        /// <summary>Required equipped item class (EquippedItemClass).</summary>
        public int? EquippedItemClass { get; set; }

        /// <summary>EquippedItemSubClassMask (bitmask).</summary>
        public int? EquippedItemSubClassMask { get; set; }

        /// <summary>EquippedItemInventoryTypeMask (bitmask).</summary>
        public int? EquippedItemInventoryTypeMask { get; set; }

        /// <summary>Allowed shapeshift forms mask (Stances / ShapeshiftMask).</summary>
        public uint? ShapeshiftMask { get; set; }

        /// <summary>Disallowed shapeshift forms mask (StancesNot / ShapeshiftExcludeMask).</summary>
        public uint? ShapeshiftExcludeMask { get; set; }

        /// <summary>Stance bar order (stance button index, if used).</summary>
        public int? StanceBarOrder { get; set; }

        /// <summary>AreaGroupId restriction (AreaGroupId column).</summary>
        public int? AreaGroupId { get; set; }


        // MOVEMENT / MISSILE / MISC ----------------------------------------

        /// <summary>Projectile or missile spell ID (SpellMissileID).</summary>
        public int? MissileId { get; set; }

        /// <summary>Missile travel speed (Speed column).</summary>
        public float? Speed { get; set; }

        /// <summary>SpellFocusObject (RequiresSpellFocus column).</summary>
        public int? SpellFocusObject { get; set; }

        /// <summary>FacingCasterFlags bitmask.</summary>
        public uint? FacingCasterFlags { get; set; }

        /// <summary>DamageClass (DmgClass column – melee, ranged, magic).</summary>
        public int? DamageClass { get; set; }

        /// <summary>PreventionType (PreventionType column).</summary>
        public int? PreventionType { get; set; }


        // AURA / STATE REQUIREMENTS ----------------------------------------

        /// <summary>Required caster aura (CasterAuraSpell column).</summary>
        public int? RequiredCasterAuraId { get; set; }

        /// <summary>Required target aura.</summary>
        public int? RequiredTargetAuraId { get; set; }

        /// <summary>Caster aura that must NOT be present.</summary>
        public int? ExcludedCasterAuraId { get; set; }

        /// <summary>Target aura that must NOT be present.</summary>
        public int? ExcludedTargetAuraId { get; set; }

        /// <summary>Required caster aura state (CasterAuraState).</summary>
        public int? RequiredCasterAuraState { get; set; }

        /// <summary>Required target aura state.</summary>
        public int? RequiredTargetAuraState { get; set; }

        /// <summary>Forbidden caster aura state (CasterAuraStateNot).</summary>
        public int? ForbiddenCasterAuraState { get; set; }

        /// <summary>Forbidden target aura state (TargetAuraStateNot).</summary>
        public int? ForbiddenTargetAuraState { get; set; }

        /// <summary>Required aura vision (RequiredAuraVision).</summary>
        public int? RequiredAuraVision { get; set; }

        /// <summary>Minimum faction/reputation requirements (MinFactionId / MinReputation).</summary>
        public int? MinFactionId { get; set; }
        public int? MinReputation { get; set; }


        // EFFECTS -----------------------------------------------------------

        /// <summary>
        /// A list of up to ∞ semantic effects. Only the first 3 are used by spell.dbc.
        /// If empty, mapper synthesizes effects from DirectDamage / PeriodicDamage.
        /// </summary>
        public List<AiEffectDefinition> Effects { get; set; } = new List<AiEffectDefinition>();
    }


    /// <summary>
    /// A single high-level effect that maps to Effect1/Effect2/Effect3.
    /// </summary>
    public sealed class AiEffectDefinition
    {
        public string Type { get; set; }               // e.g. "Damage", "Heal", "ApplyAura", "Stun", "Summon"
        public string Aura { get; set; }               // e.g. "PeriodicDamage", "ModStun", "ModRoot"

        public float? BasePoints { get; set; }         // Raw effect amount (spell.dbc stores BasePoints+1)
        public float? DieSides { get; set; }           // Optional random range (die sides)
        public float? DamagePerSecond { get; set; }    // For DoTs/HoTs
        public float? AmplitudeSeconds { get; set; }   // Tick interval for periodic effects
        public float? RadiusYards { get; set; }        // AoE radius

        public string Target { get; set; }             // "Self", "Enemy", "Friendly", "Area", etc.

        public int? MiscValue { get; set; }            // Maps to EffectMiscValueN when set
        public int? MiscValueB { get; set; }           // Maps to EffectMiscValueBN when set

        public int? TriggerSpellId { get; set; }       // EffectTriggerSpellN
        public int? CreatureId { get; set; }           // Summon creature / kill credit, etc. (goes via MiscValue when appropriate)

        public int? ChainTargets { get; set; }         // EffectChainTargetN
        public float? WeaponDamagePercent { get; set; } // Optional helper; you can map into multipliers if desired
        public float? ValueMultiplier { get; set; }    // EffectMultipleValueN
        public float? DamageMultiplier { get; set; }   // EffectDamageMultiplierN
    }

    /// <summary>
    /// Simple reagent descriptor → maps to Reagent1–8 / ReagentCount1–8.
    /// </summary>
    public sealed class AiReagentDefinition
    {
        /// <summary>Item entry ID.</summary>
        public int ItemId { get; set; }

        /// <summary>Stack count required.</summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional fixed slot index (0–7). If omitted, mapper fills from slot 0 upward.
        /// </summary>
        public int? Slot { get; set; }
    }
}
