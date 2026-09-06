namespace SpellEditor.Sources.Emulator.CMangos.Classic
{
    /// <summary>
    /// CMaNGOS Classic, the 1.12 branch. It is the only one of the three that still reads
    /// spell_facing, and its proc and elixir lists are a bit shorter than the later ones.
    /// </summary>
    public class CMangosClassicEmulator : CMangosEmulator
    {
        public override string Id => "CMangosClassic";

        public override string Name => "CMaNGOS Classic";

        public override string Description => "CMaNGOS Classic world database, the stock classicmangos schema.";

        public override string DefaultDatabase => "classicmangos";

        /// <summary>No death proc until TBC.</summary>
        public override FlagSet ProcFlags => CMangosEnums.ProcFlagsClassic;

        public override FlagSet ProcFlagsEx => CMangosEnums.ProcFlagsEx;

        /// <summary>No unstable or Shattrath elixirs until TBC.</summary>
        public override FlagSet ElixirMask => CMangosEnums.ElixirMaskClassic;

        protected override void ConfigureTables(WorldTableSet tables)
        {
            base.ConfigureTables(tables);
            tables.Add(new SpellFacingTable(this));
        }
    }
}
