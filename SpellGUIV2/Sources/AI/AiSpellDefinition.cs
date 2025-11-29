using System;

namespace SpellEditor.Sources.AI
{
    /// <summary>
    /// High-level, human-friendly representation of a spell.
    /// The LLM will output exactly this shape as JSON.
    /// </summary>
    public class AiSpellDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Desired maximum range in yards (e.g., 5, 30, 40).
        /// </summary>
        public float? RangeYards { get; set; }

        /// <summary>
        /// Desired initial direct damage. We’ll map to EffectBasePoints1 + 1.
        /// </summary>
        public int? DirectDamage { get; set; }
    }

    /// <summary>
    /// Wraps the raw LLM content and the parsed definition.
    /// </summary>
    public class AiSpellResult
    {
        public string RawContent { get; set; }
        public AiSpellDefinition Definition { get; set; }
    }
}
