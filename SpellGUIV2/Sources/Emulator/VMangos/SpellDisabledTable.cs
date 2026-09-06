using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.VMangos
{
    /// <summary>
    /// VMangos only, see ObjectMgr::LoadSpellDisabledEntrys. This one is not build gated.
    /// </summary>
    public class SpellDisabledTable : VMangosTable
    {
        public SpellDisabledTable(VMangosEmulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_disabled";

        public override string OrderBy => "`entry`";

        public override bool IsSingleRow => true;

        // Keyed on the exact spell id, nothing is inherited by a rank
        public override string SpellFilter(RankChain chain) => $"`entry` = {chain.SpellId}";

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "entry", IsKey = true, IsPrimarySpellKey = true };
        }

        public override string FallbackHeader => "Disabled";

        public override string FallbackDescription =>
            "A row here stops players casting the spell and stops trainers teaching it. Game masters can still cast it.";

        public override string FallbackEmpty => "This spell is not disabled.";

        public override string[] FallbackLabels => new[] { "Spell" };

        public override string[] FallbackTooltips => new[] { "Spell to turn off." };
    }
}
