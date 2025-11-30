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
}
