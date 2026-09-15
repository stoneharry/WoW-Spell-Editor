using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.Tables
{
    /// <summary>Threat overrides, see SpellMgr::LoadSpellThreats.</summary>
    public class SpellThreatTable : WorldTable
    {
        public SpellThreatTable(Emulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_threat";

        public override string OrderBy => "`entry`";

        public override bool IsSingleRow => true;

        public override bool InheritsRankChain => true;

        // GetSpellThreatEntry falls back to the first spell in the chain
        public override string SpellFilter(RankChain chain) => In("entry", chain.SpellId, chain.FirstSpellId);

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "entry", IsKey = true, IsPrimarySpellKey = true };
            yield return new WorldColumn { Name = "flatMod", Kind = ColumnKind.Int, AllowNull = true, DefaultValue = "" };
            yield return new WorldColumn { Name = "pctMod", Kind = ColumnKind.Float, DefaultValue = "1" };
            yield return new WorldColumn { Name = "apPctMod", Kind = ColumnKind.Float };
        }

        public override string FallbackHeader => "Threat";

        public override string FallbackDescription => "Overrides how much threat this spell generates.";

        public override string FallbackEmpty =>
            "This spell has no threat override. Threat comes from the damage or healing done.";

        public override string[] FallbackLabels =>
            new[] { "Spell", "Flat threat", "Threat multiplier", "Attack power share" };

        public override string[] FallbackTooltips => new[]
        {
            "Spell id the threat data applies to.",
            "Flat threat added on top of the damage or healing done. Leave empty for none.",
            "Threat multiplier for the damage or healing done.",
            "Extra threat taken from a share of the caster attack power."
        };
    }
}
