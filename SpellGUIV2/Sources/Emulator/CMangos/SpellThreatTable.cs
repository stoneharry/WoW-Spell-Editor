using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.CMangos
{
    /// <summary>
    /// See SpellMgr::LoadSpellThreats. Same idea as the 3.3.5 table of this name but with an
    /// effect mask on the end, and VMangos dropped the attack power part.
    /// </summary>
    public class SpellThreatTable : CMangosTable
    {
        public SpellThreatTable(CMangosEmulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_threat";

        public override string OrderBy => "`entry`";

        public override bool IsSingleRow => true;

        /// <summary>The loader runs the rows through SpellRankHelper::FillHigherRanks.</summary>
        public override bool InheritsRankChain => true;

        public override string SpellFilter(RankChain chain) => In("entry", chain.SpellId, chain.FirstSpellId);

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "entry", IsKey = true, IsPrimarySpellKey = true };
            yield return new WorldColumn { Name = "Threat" };
            yield return new WorldColumn { Name = "multiplier", Kind = ColumnKind.Float, DefaultValue = "1" };
            yield return new WorldColumn { Name = "ap_bonus", Kind = ColumnKind.Float };
            yield return new WorldColumn { Name = "InverseEffectMask", Flags = WorldEnums.EffectIndexMask };
        }

        public override string FallbackHeader => "Threat";

        public override string FallbackDescription =>
            "How much threat this spell makes on top of the damage or healing it does.";

        public override string FallbackEmpty =>
            "This spell has no threat row. It makes the threat its damage or healing earns and nothing more.";

        public override string[] FallbackLabels => new[]
        {
            "Spell", "Flat threat", "Threat multiplier", "Attack power share", "Effects the flat bonus skips"
        };

        public override string[] FallbackTooltips => new[]
        {
            "Spell the threat applies to. Write the row against rank 1 and the core applies it to every rank.",
            "Threat added on top of the damage or healing, split between everything the spell hits.",
            "Multiplies the threat the damage or healing would have made on its own. 1 leaves it alone.",
            "Extra threat taken from a share of the caster attack power.",
            "Effects that do not get the flat bonus, so a spell that both damages and applies an aura can put the threat on one of them."
        };
    }
}
