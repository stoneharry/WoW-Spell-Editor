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

        /// <summary>Raw ProcFlags bitmask. If set, written directly to ProcFlags.</summary>
        public uint? ProcFlags { get; set; }

        /// <summary>Proc chance in percent (0–100). Maps to ProcChance.</summary>
        public int? ProcChance { get; set; }

        /// <summary>Maximum number of procs before the aura is removed.</summary>
        public int? ProcCharges { get; set; }

        /// <summary>Internal proc cooldown in seconds (ProcCooldown, ms in DBC).</summary>
        public float? ProcCooldownSeconds { get; set; }


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

        /// <summary>CategoryRecoveryTime (seconds, mapped to ms).</summary>
        public float? CategoryCooldownSeconds { get; set; }

        /// <summary>StartRecoveryCategory (e.g. shared recovery groups).</summary>
        public int? StartRecoveryCategory { get; set; }

        /// <summary>StartRecoveryTime in seconds (mapped to ms).</summary>
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

        /// <summary>SpellFocusObject (e.g. blacksmith anvil).</summary>
        public int? SpellFocusObject { get; set; }

        /// <summary>Required spell ID to cast (RequiresSpellFocus / prerequisite spells etc.).</summary>
        public int? RequiresSpell { get; set; }

        /// <summary>FacingCasterFlags bitmask.</summary>
        public uint? FacingCasterFlags { get; set; }

        /// <summary>DamageClass (e.g. Melee, Ranged, Magic).</summary>
        public int? DamageClass { get; set; }

        /// <summary>PreventionType (e.g. silence, pacify, etc.).</summary>
        public int? PreventionType { get; set; }


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
        public int? CreatureId { get; set; }           // Summon creature / kill credit, etc.

        public int? ChainTargets { get; set; }         // EffectChainTargetN
        public float? WeaponDamagePercent { get; set; } // Convenience helper → EffectDamageMultiplierN
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
