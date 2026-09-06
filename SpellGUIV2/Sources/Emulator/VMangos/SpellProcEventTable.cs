using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.VMangos
{
    /// <summary>
    /// The 1.12 proc table, see SpellMgr::LoadSpellProcEvents. TrinityCore and AzerothCore replaced
    /// this with spell_proc, which splits the hit results into their own columns and drops the
    /// per minute rate.
    /// </summary>
    public class SpellProcEventTable : VMangosTable
    {
        public SpellProcEventTable(VMangosEmulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_proc_event";

        public override string OrderBy => "`entry`";

        public override bool IsSingleRow => true;

        /// <summary>The loader runs the rows through SpellRankHelper::FillHigherRanks.</summary>
        public override bool InheritsRankChain => true;

        public override string SpellFilter(RankChain chain) => In("entry", chain.SpellId, chain.FirstSpellId);

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "entry", IsKey = true, IsPrimarySpellKey = true };
            yield return new WorldColumn { Name = "SchoolMask", Flags = WorldEnums.SchoolMask };
            yield return new WorldColumn { Name = "SpellFamilyName", Enum = WorldEnums.SpellFamilyNames };
            yield return new WorldColumn { Name = "SpellFamilyMask0", Kind = ColumnKind.ULong };
            yield return new WorldColumn { Name = "SpellFamilyMask1", Kind = ColumnKind.ULong };
            yield return new WorldColumn { Name = "SpellFamilyMask2", Kind = ColumnKind.ULong };
            yield return new WorldColumn { Name = "procFlags", Flags = VMangosEnums.ProcFlags };
            yield return new WorldColumn { Name = "procEx", Flags = VMangosEnums.ProcFlagsEx };
            yield return new WorldColumn { Name = "ppmRate", Kind = ColumnKind.Float };
            yield return new WorldColumn { Name = "CustomChance", Kind = ColumnKind.Float };
            yield return new WorldColumn { Name = "Cooldown" };
            yield return BuildMinColumn();
            yield return BuildMaxColumn();
        }

        public override string FallbackHeader => "Proc";

        public override string FallbackDescription =>
            "When the aura on this spell fires. Anything left at zero falls back to what Spell.dbc says, so only fill in what needs to differ.";

        public override string FallbackEmpty =>
            "This spell has no proc event row. It procs on the conditions in Spell.dbc alone.";

        public override string[] FallbackLabels => WithBuildLabels(
            "Spell", "School mask", "Spell family", "Family mask effect 1", "Family mask effect 2",
            "Family mask effect 3", "Proc flags", "Proc extra flags", "Procs per minute",
            "Custom chance", "Internal cooldown (s)");

        public override string[] FallbackTooltips => WithBuildTooltips(
            "Spell the aura sits on. Write the row against rank 1 and the core applies it to every rank.",
            "Only spells of these schools can set the proc off. Zero means any school.",
            "Only spells of this family can set the proc off, used together with the family masks.",
            "Which spells of the family can set the proc off, matched against effect 1. Zero means any.",
            "Same for effect 2.",
            "Same for effect 3.",
            "What the wearer has to do for the proc to be considered. Zero falls back to the ProcFlags field in Spell.dbc.",
            "Narrows the proc down to particular hit results. None means a normal hit or a crit.",
            "Chance expressed as procs per minute of weapon speed. Zero turns it off.",
            "Flat percentage chance, overriding the ProcChance field in Spell.dbc. Zero turns it off.",
            "Seconds before the proc can happen again. Zero means no internal cooldown.");
    }
}
