namespace SpellEditor.Sources.Emulator.TrinityCore
{
    /// <summary>
    /// TrinityCore 3.3.5, the TDB schema. The defaults on Emulator and on the shared tables were
    /// written against this core, so there is nothing to override here beyond its own enums.
    /// </summary>
    public class TrinityCoreEmulator : Emulator
    {
        public override string Id => "TrinityCore";

        public override string Name => "TrinityCore";

        public override string Description => "TrinityCore 3.3.5 world database (TDB schema).";

        public override string DefaultUser => "trinity";

        public override string DefaultDatabase => "world";

        public override FlagSet CustomAttributes => TrinityCoreEnums.CustomAttributes;

        public override FlagSet ProcAttributes => TrinityCoreEnums.ProcAttributes;
    }
}
