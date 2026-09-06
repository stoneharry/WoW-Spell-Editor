using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.CMangos
{
    /// <summary>
    /// The rank chain, see SpellMgr::LoadSpellChains. The core works most chains out from Spell.dbc
    /// and the talent and trainer data first, so this table only holds the ones it could not.
    /// </summary>
    public class SpellChainTable : CMangosTable
    {
        public SpellChainTable(CMangosEmulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_chain";

        public override string OrderBy => "`rank`";

        /// <summary>This tab is the chain, saying it again above the rows adds nothing.</summary>
        public override bool ShowsRankChainSummary => false;

        // The whole chain, a chain only makes sense read end to end
        public override string SpellFilter(RankChain chain) =>
            $"(`first_spell` = {chain.FirstSpellId} OR `spell_id` = {chain.SpellId})";

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn
            {
                Name = "spell_id",
                IsKey = true,
                ShowSpellPicker = true,
                NewRowValue = context => context.Chain.SpellId.ToString()
            };
            yield return new WorldColumn { Name = "prev_spell", ShowSpellPicker = true };
            yield return new WorldColumn
            {
                Name = "first_spell",
                ShowSpellPicker = true,
                NewRowValue = context => context.Chain.FirstSpellId.ToString()
            };
            yield return new WorldColumn { Name = "rank", DefaultValue = "1", NewRowValue = NextRank };
            yield return new WorldColumn { Name = "req_spell", ShowSpellPicker = true };
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
            "The rank chain this spell is part of. Proc data and threat written against rank 1 apply to the whole chain.";

        public override string FallbackEmpty =>
            "No rows here for this spell. The core builds most chains from Spell.dbc, the talent data and the trainer lists, and only reads this table for the ones it cannot work out on its own.";

        public override string[] FallbackLabels => new[]
        {
            "Spell", "Previous rank", "First rank", "Rank", "Required spell"
        };

        public override string[] FallbackTooltips => new[]
        {
            "Spell that sits at this rank. Each spell can only be in one chain.",
            "Rank directly below this one. Leave at zero on the first rank.",
            "Rank 1 of the chain. Every row in the same chain repeats this id.",
            "Position in the chain, starting at 1.",
            "Spell that has to be known before this rank can be learned. Zero for none."
        };
    }
}
