using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.Tables
{
    /// <summary>Coefficient overrides, see SpellMgr::LoadSpellBonuses.</summary>
    public class SpellBonusDataTable : WorldTable
    {
        public SpellBonusDataTable(Emulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_bonus_data";

        public override string OrderBy => "`entry`";

        public override bool IsSingleRow => true;

        public override bool InheritsRankChain => true;

        // GetSpellBonusData falls back to the first spell in the chain
        public override string SpellFilter(RankChain chain) => In("entry", chain.SpellId, chain.FirstSpellId);

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "entry", IsKey = true, IsPrimarySpellKey = true };
            yield return new WorldColumn { Name = "direct_bonus", Kind = ColumnKind.Float };
            yield return new WorldColumn { Name = "dot_bonus", Kind = ColumnKind.Float };
            yield return new WorldColumn { Name = "ap_bonus", Kind = ColumnKind.Float };
            yield return new WorldColumn { Name = "ap_dot_bonus", Kind = ColumnKind.Float };
            yield return new WorldColumn { Name = "comments", Kind = ColumnKind.Text, AllowNull = true, DefaultValue = "", IsMultiline = true };
        }

        public override string FallbackHeader => "Bonus";

        public override string FallbackDescription =>
            "Overrides the spell power and attack power coefficients the core would otherwise calculate.";

        public override string FallbackEmpty =>
            "This spell has no coefficient override. The core works them out from the cast time and effects.";

        public override string[] FallbackLabels => new[]
        {
            "Spell", "Direct spell power", "Periodic spell power", "Direct attack power",
            "Periodic attack power", "Comment"
        };

        public override string[] FallbackTooltips => new[]
        {
            "Spell id the coefficients apply to.",
            "Spell power coefficient for the direct damage or healing part.",
            "Spell power coefficient for the periodic part.",
            "Attack power coefficient for the direct damage or healing part.",
            "Attack power coefficient for the periodic part.",
            "Free text note, shown only in the database."
        };
    }
}
