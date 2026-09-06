namespace SpellEditor.Sources.Emulator.CMangos.Tbc
{
    /// <summary>
    /// CMaNGOS TBC, the 2.4.3 branch. It takes the shared table set as it is, TBC only widened a
    /// couple of the value lists.
    /// </summary>
    public class CMangosTbcEmulator : CMangosEmulator
    {
        public override string Id => "CMangosTbc";

        public override string Name => "CMaNGOS TBC";

        public override string Description => "CMaNGOS TBC world database, the stock tbcmangos schema.";

        public override string DefaultDatabase => "tbcmangos";

        /// <summary>TBC added the death proc.</summary>
        public override FlagSet ProcFlags => CMangosEnums.ProcFlags;

        public override FlagSet ProcFlagsEx => CMangosEnums.ProcFlagsEx;

        /// <summary>TBC added the unstable and Shattrath elixirs.</summary>
        public override FlagSet ElixirMask => CMangosEnums.ElixirMask;
    }
}
