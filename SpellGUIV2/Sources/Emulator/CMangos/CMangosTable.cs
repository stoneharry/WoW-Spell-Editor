namespace SpellEditor.Sources.Emulator.CMangos
{
    /// <summary>
    /// Shared base for the CMaNGOS tables. spell_threat and spell_chain exist on the other cores
    /// too but with other columns, so everything here takes its own language keys.
    /// </summary>
    public abstract class CMangosTable : WorldTable
    {
        protected CMangosTable(CMangosEmulator emulator) : base(emulator)
        {
            Core = emulator;
        }

        /// <summary>The same object as Emulator, typed so the branch enums can be read off it.</summary>
        protected CMangosEmulator Core { get; }

        /// <summary>Bumped by a table whose columns differ between branches.</summary>
        protected virtual string KeyPrefix => "emu_cm";

        public override string HeaderResourceKey => $"{KeyPrefix}_tab_{Name}";
        public override string DescriptionResourceKey => $"{KeyPrefix}_desc_{Name}";
        public override string EmptyResourceKey => $"{KeyPrefix}_empty_{Name}";
        public override string LabelsResourceKey => $"{KeyPrefix}_labels_{Name}";
        public override string TooltipsResourceKey => $"{KeyPrefix}_tips_{Name}";
    }
}
