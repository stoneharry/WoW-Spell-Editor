using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.CMangos
{
    /// <summary>
    /// Spells taught alongside this one, see SpellMgr::LoadSpellLearnSpells. The core also works
    /// these out from the learn effects in Spell.dbc, this table is for the rest.
    /// </summary>
    public class SpellLearnSpellTable : CMangosTable
    {
        public SpellLearnSpellTable(CMangosEmulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_learn_spell";

        public override string OrderBy => "`SpellID`";

        // Keyed on the exact spell id, the loader has no rank handling
        public override string SpellFilter(RankChain chain) => $"`entry` = {chain.SpellId}";

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "entry", IsKey = true, IsPrimarySpellKey = true };
            yield return new WorldColumn { Name = "SpellID", IsKey = true, ShowSpellPicker = true };
            yield return new WorldColumn { Name = "Active", DefaultValue = "1" };
        }

        public override string FallbackHeader => "Teaches";

        public override string FallbackDescription =>
            "Other spells learned at the same time as this one.";

        public override string FallbackEmpty => "Learning this spell does not teach anything else.";

        public override string[] FallbackLabels => new[] { "Spell", "Also learns", "Active" };

        public override string[] FallbackTooltips => new[]
        {
            "Spell being learned.",
            "Spell handed over with it.",
            "1 puts the learned spell on the action bar as a normal castable spell, 0 keeps it passive and hidden."
        };
    }
}
