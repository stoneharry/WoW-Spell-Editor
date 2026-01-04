using System;
using System.Collections.Generic;

namespace SpellEditor.Sources.AI
{
    /// <summary>
    /// Lightweight semantic summary of an existing spell, used as an example
    /// in the AI prompt.
    /// </summary>
    public sealed class AiSimilarSpellSummary
    {
        public uint Id { get; set; }
        public string Name { get; set; }

        // --- NEW OPTIONAL FIELDS (fully safe) ---
        public uint? SchoolMask { get; set; }
        public uint? Mechanic { get; set; }
        public uint? PowerType { get; set; }
        public uint? Visual1 { get; set; }
        public uint? Visual2 { get; set; }

        // Raw effect/aura preview (optional)
        public List<string> Effects { get; set; } = new List<string>();

        public string SummaryText { get; set; }

        public override string ToString()
            => SummaryText ?? Name ?? string.Empty;
    }

}
