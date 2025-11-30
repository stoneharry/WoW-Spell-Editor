using System;

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
        public string SummaryText { get; set; }

        public override string ToString()
        {
            return SummaryText ?? (Name ?? string.Empty);
        }
    }
}
