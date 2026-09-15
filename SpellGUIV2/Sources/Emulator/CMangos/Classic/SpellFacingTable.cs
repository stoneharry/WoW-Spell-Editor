using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.CMangos.Classic
{
    /// <summary>
    /// CMaNGOS Classic only, see SpellMgr::LoadSpellFacing. Neither TBC nor WotLK read it.
    /// </summary>
    public class SpellFacingTable : CMangosTable
    {
        public SpellFacingTable(CMangosEmulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_facing";

        public override string OrderBy => "`entry`";

        public override bool IsSingleRow => true;

        // Keyed on the exact spell id, the loader has no rank handling
        public override string SpellFilter(RankChain chain) => $"`entry` = {chain.SpellId}";

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "entry", IsKey = true, IsPrimarySpellKey = true };
            yield return new WorldColumn { Name = "facingcasterflag", DefaultValue = "1", Flags = FacingFlags };
        }

        /// <summary>Only one bit is defined, see SPELL_FACING_FLAG_INFRONT.</summary>
        private static readonly FlagSet FacingFlags = new FlagSet("SpellFacingFlags", "emu_cm_facing_strings",
            new uint[] { 0x1 },
            new[] { "Caster has to be facing the target" });

        public override string FallbackHeader => "Facing";

        public override string FallbackDescription =>
            "Makes the cast fail unless the caster is turned the right way.";

        public override string FallbackEmpty => "This spell has no facing requirement.";

        public override string[] FallbackLabels => new[] { "Spell", "Facing flags" };

        public override string[] FallbackTooltips => new[]
        {
            "Spell the requirement applies to.",
            "The cast fails with a facing error unless the target is inside the caster arc."
        };
    }
}
