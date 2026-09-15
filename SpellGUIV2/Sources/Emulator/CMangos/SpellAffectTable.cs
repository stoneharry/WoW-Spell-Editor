using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.CMangos
{
    /// <summary>
    /// Classic and TBC only, see SpellMgr::LoadSpellAffects. WotLK dropped the table because the
    /// client data carries the mask itself by then.
    /// </summary>
    public class SpellAffectTable : CMangosTable
    {
        public SpellAffectTable(CMangosEmulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_affect";

        public override string OrderBy => "`effectId`";

        // Keyed on the exact spell id, the loader has no rank handling
        public override string SpellFilter(RankChain chain) => $"`entry` = {chain.SpellId}";

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "entry", IsKey = true, IsPrimarySpellKey = true };
            yield return new WorldColumn { Name = "effectId", IsKey = true, Enum = WorldEnums.EffectIndexValues };
            yield return new WorldColumn { Name = "SpellFamilyMask", Kind = ColumnKind.ULong };
        }

        public override string FallbackHeader => "Affects";

        public override string FallbackDescription =>
            "Which spells a talent or aura effect changes, when the mask in Spell.dbc is wrong or missing.";

        public override string FallbackEmpty =>
            "No overrides for this spell. The effects use the family mask from Spell.dbc.";

        public override string[] FallbackLabels => new[] { "Spell", "Effect", "Affected spell family mask" };

        public override string[] FallbackTooltips => new[]
        {
            "Spell holding the effect, usually a talent.",
            "Effect index the mask replaces the Spell.dbc one on.",
            "Spells of the same family the effect applies to. Zero makes the effect apply to nothing at all."
        };
    }
}
