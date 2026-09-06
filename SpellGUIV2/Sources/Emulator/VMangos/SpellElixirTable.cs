using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.VMangos
{
    /// <summary>
    /// VMangos only, see SpellMgr::LoadSpellElixirs. The 3.3.5 cores read the same thing off the
    /// spell group ids the core declares itself.
    /// </summary>
    public class SpellElixirTable : VMangosTable
    {
        public SpellElixirTable(VMangosEmulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_elixir";

        public override string OrderBy => "`entry`";

        public override bool IsSingleRow => true;

        // Keyed on the exact spell id, nothing is inherited by a rank
        public override string SpellFilter(RankChain chain) => $"`entry` = {chain.SpellId}";

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "entry", IsKey = true, IsPrimarySpellKey = true };
            yield return new WorldColumn { Name = "mask", Flags = VMangosEnums.ElixirMask };
            yield return BuildMinColumn();
            yield return BuildMaxColumn();
        }

        public override string FallbackHeader => "Elixir";

        public override string FallbackDescription =>
            "Marks the spell as a battle or guardian elixir, which caps how many of each can be up at once. Ticking both makes it a flask.";

        public override string FallbackEmpty => "This spell is not an elixir or a well fed buff.";

        public override string[] FallbackLabels => WithBuildLabels("Spell", "Kind");

        public override string[] FallbackTooltips => WithBuildTooltips(
            "Buff the elixir applies, not the potion item.",
            "Battle and guardian together make a flask. Well fed is for food that sits in the potion spell family.");
    }
}
