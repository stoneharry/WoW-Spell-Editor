using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.Tables
{
    /// <summary>Proc conditions, see SpellMgr::LoadSpellProcs.</summary>
    public class SpellProcTable : WorldTable
    {
        public SpellProcTable(Emulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_proc";

        public override string OrderBy => "`SpellId`";

        public override bool IsSingleRow => true;

        public override bool InheritsRankChain => true;

        // Negative id means every rank, always written against the first
        public override string SpellFilter(RankChain chain) =>
            In("SpellId", chain.SpellId, -chain.SpellId, -chain.FirstSpellId);

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn
            {
                Name = "SpellId",
                Kind = ColumnKind.Int,
                IsKey = true,
                IsPrimarySpellKey = true,
                NegativeOptionKey = "emu_option_all_ranks",
                FallbackNegativeOption = "Apply to every rank of this spell"
            };
            yield return new WorldColumn { Name = "SchoolMask", Flags = WorldEnums.SchoolMask };
            yield return new WorldColumn { Name = "SpellFamilyName", Enum = WorldEnums.SpellFamilyNames };
            yield return new WorldColumn { Name = "SpellFamilyMask0" };
            yield return new WorldColumn { Name = "SpellFamilyMask1" };
            yield return new WorldColumn { Name = "SpellFamilyMask2" };
            yield return new WorldColumn { Name = "ProcFlags", Flags = WorldEnums.ProcFlags };
            yield return new WorldColumn { Name = "SpellTypeMask", Flags = WorldEnums.ProcSpellTypeMask };
            yield return new WorldColumn { Name = "SpellPhaseMask", Flags = WorldEnums.ProcSpellPhaseMask };
            yield return new WorldColumn { Name = "HitMask", Flags = WorldEnums.ProcHitMask };
            yield return new WorldColumn { Name = "AttributesMask", Flags = Emulator.ProcAttributes };
            yield return new WorldColumn { Name = "DisableEffectsMask", Flags = WorldEnums.EffectIndexMask };
            yield return new WorldColumn { Name = "ProcsPerMinute", Kind = ColumnKind.Float };
            yield return new WorldColumn { Name = "Chance", Kind = ColumnKind.Float };
            yield return new WorldColumn { Name = "Cooldown" };
            yield return new WorldColumn { Name = "Charges" };
        }

        public override string FallbackHeader => "Proc";

        public override string FallbackDescription =>
            "Controls when and how often the aura on this spell procs.";

        public override string FallbackEmpty =>
            "This spell has no proc data. The core falls back to the proc chance and flags in Spell.dbc.";

        public override string[] FallbackLabels => new[]
        {
            "Spell", "Required school", "Required spell family", "Family mask 1", "Family mask 2",
            "Family mask 3", "Proc flags", "Required spell type", "Proc phase", "Required hit result",
            "Proc attributes", "Effects that do not proc", "Procs per minute", "Proc chance",
            "Internal cooldown (ms)", "Charges"
        };

        public override string[] FallbackTooltips => new[]
        {
            "Spell id the proc data belongs to. A negative id applies it to every rank of the spell.",
            "If not zero, the triggering spell must be of one of these schools.",
            "If not zero, the triggering spell must have this SpellFamilyName.",
            "If not zero, the triggering spell must match these SpellFamilyFlags (first 32 bits).",
            "If not zero, the triggering spell must match these SpellFamilyFlags (second 32 bits).",
            "If not zero, the triggering spell must match these SpellFamilyFlags (third 32 bits).",
            "If not zero, overrides the ProcTypeMask from Spell.dbc and decides which events can proc.",
            "If not zero, the triggering spell must do damage, healing, or neither.",
            "If not zero, the phase of the cast the proc happens on.",
            "If not zero, the hit result required for the proc. Defaults to normal and critical hits.",
            "Extra proc requirements, see ProcAttributes in SpellMgr.h.",
            "Spell effects that should not proc this aura.",
            "If not zero, proc chance is this value multiplied by the caster weapon speed over 60.",
            "If not zero, overrides the proc chance from Spell.dbc. Ignored when procs per minute is set.",
            "Internal cooldown in milliseconds between procs.",
            "If not zero, overrides the proc charges from Spell.dbc. Zero means unlimited."
        };
    }
}
