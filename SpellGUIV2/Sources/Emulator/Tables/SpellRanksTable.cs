using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.Tables
{
    /// <summary>The rank chain itself, see SpellMgr::LoadSpellRanks.</summary>
    public class SpellRanksTable : WorldTable
    {
        public SpellRanksTable(Emulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_ranks";

        public override string OrderBy => "`rank`";

        /// <summary>This tab is the chain, saying it again above the rows adds nothing.</summary>
        public override bool ShowsRankChainSummary => false;

        // The whole chain, a chain only makes sense read end to end
        public override string SpellFilter(RankChain chain) => $"`first_spell_id` = {chain.FirstSpellId}";

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn
            {
                Name = "first_spell_id",
                IsKey = true,
                ShowSpellPicker = true,
                NewRowValue = context => context.Chain.FirstSpellId.ToString()
            };
            yield return new WorldColumn
            {
                Name = "spell_id",
                ShowSpellPicker = true,
                NewRowValue = context => context.Chain.SpellId.ToString()
            };
            yield return new WorldColumn { Name = "rank", DefaultValue = "1", NewRowValue = NextRank };
        }

        /// <summary>Counts from what is on screen so adding several rows does not repeat a rank.</summary>
        private static string NextRank(NewRowContext context)
        {
            var highest = 0u;
            foreach (var row in context.Rows)
            {
                if (uint.TryParse((row["rank"] as string ?? string.Empty).Trim(), out var rank) && rank > highest)
                    highest = rank;
            }
            return (highest + 1).ToString();
        }

        public override string FallbackHeader => "Ranks";

        public override string FallbackDescription =>
            "The rank chain this spell is part of. Proc data, coefficients, threat, groups and scripts written against rank 1 apply to the whole chain.";

        public override string FallbackEmpty =>
            "This spell is not part of a rank chain. Talent ranks come from Talent.dbc rather than this table, so they are never listed here.";

        public override string[] FallbackLabels => new[] { "First rank", "Spell", "Rank" };

        public override string[] FallbackTooltips => new[]
        {
            "Rank 1 of the chain. Every row in the same chain repeats this id.",
            "Spell that sits at this rank. Each spell can only be in one chain.",
            "Position in the chain, starting at 1. The core drops the chain if the ranks are not 1 to n with no gaps."
        };
    }
}
