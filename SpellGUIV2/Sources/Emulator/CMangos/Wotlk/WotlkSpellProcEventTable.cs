using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.CMangos.Wotlk
{
    /// <summary>
    /// The WotLK spell family mask is 96 bits wide rather than 64, so the one column per effect the
    /// earlier branches use turns into three, and the loader stitches A, B and C back together.
    /// Everything else about the table is unchanged.
    /// </summary>
    public class WotlkSpellProcEventTable : SpellProcEventTable
    {
        public WotlkSpellProcEventTable(CMangosEmulator emulator) : base(emulator)
        {
        }

        protected override string KeyPrefix => "emu_cmw";

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "entry", IsKey = true, IsPrimarySpellKey = true };
            yield return new WorldColumn { Name = "SchoolMask", Flags = WorldEnums.SchoolMask };
            yield return new WorldColumn { Name = "SpellFamilyName", Enum = WorldEnums.SpellFamilyNames };
            yield return new WorldColumn { Name = "SpellFamilyMaskA0" };
            yield return new WorldColumn { Name = "SpellFamilyMaskA1" };
            yield return new WorldColumn { Name = "SpellFamilyMaskA2" };
            yield return new WorldColumn { Name = "SpellFamilyMaskB0" };
            yield return new WorldColumn { Name = "SpellFamilyMaskB1" };
            yield return new WorldColumn { Name = "SpellFamilyMaskB2" };
            yield return new WorldColumn { Name = "SpellFamilyMaskC0" };
            yield return new WorldColumn { Name = "SpellFamilyMaskC1" };
            yield return new WorldColumn { Name = "SpellFamilyMaskC2" };
            yield return new WorldColumn { Name = "procFlags", Flags = Core.ProcFlags };
            yield return new WorldColumn { Name = "procEx", Flags = Core.ProcFlagsEx };
            yield return new WorldColumn { Name = "ppmRate", Kind = ColumnKind.Float };
            yield return new WorldColumn { Name = "CustomChance", Kind = ColumnKind.Float };
            yield return new WorldColumn { Name = "Cooldown" };
        }

        public override string[] FallbackLabels => new[]
        {
            "Spell", "School mask", "Spell family",
            "Family mask A, effect 1", "Family mask A, effect 2", "Family mask A, effect 3",
            "Family mask B, effect 1", "Family mask B, effect 2", "Family mask B, effect 3",
            "Family mask C, effect 1", "Family mask C, effect 2", "Family mask C, effect 3",
            "Proc flags", "Proc extra flags", "Procs per minute", "Custom chance",
            "Internal cooldown (s)"
        };

        public override string[] FallbackTooltips => new[]
        {
            "Spell the aura sits on. Write the row against rank 1 and the core applies it to every rank.",
            "Only spells of these schools can set the proc off. Zero means any school.",
            "Only spells of this family can set the proc off, used together with the family masks.",
            "First 32 bits of the family mask matched against effect 1. Zero across A, B and C means any spell of the family.",
            "First 32 bits of the family mask matched against effect 2.",
            "First 32 bits of the family mask matched against effect 3.",
            "Middle 32 bits of the family mask matched against effect 1.",
            "Middle 32 bits of the family mask matched against effect 2.",
            "Middle 32 bits of the family mask matched against effect 3.",
            "Last 32 bits of the family mask matched against effect 1.",
            "Last 32 bits of the family mask matched against effect 2.",
            "Last 32 bits of the family mask matched against effect 3.",
            "What the wearer has to do for the proc to be considered. Zero falls back to the ProcFlags field in Spell.dbc.",
            "Narrows the proc down to particular hit results. None means a normal hit or a crit.",
            "Chance expressed as procs per minute of weapon speed. Zero turns it off.",
            "Flat percentage chance, overriding the ProcChance field in Spell.dbc. Zero turns it off.",
            "Seconds before the proc can happen again. Zero means no internal cooldown."
        };
    }
}
