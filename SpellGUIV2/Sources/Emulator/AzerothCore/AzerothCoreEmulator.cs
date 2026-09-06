using SpellEditor.Sources.Emulator.Tables;

namespace SpellEditor.Sources.Emulator.AzerothCore
{
    /// <summary>
    /// AzerothCore, the stock acore_world schema. It shares every table TrinityCore has, so only
    /// the places the core reads them differently are overridden here.
    /// </summary>
    public class AzerothCoreEmulator : Emulator
    {
        public override string Id => "AzerothCore";

        public override string Name => "AzerothCore";

        public override string Description => "AzerothCore world database, the stock acore_world schema.";

        public override string DefaultUser => "acore";

        public override string DefaultDatabase => "acore_world";

        /// <summary>TrinityCore calls this column entry.</summary>
        public override string CustomAttrSpellColumn => "spell_id";

        public override FlagSet CustomAttributes => AzerothCoreEnums.CustomAttributes;

        public override FlagSet ProcAttributes => AzerothCoreEnums.ProcAttributes;

        /// <summary>Only elixir battle and guardian, so ids 3 and 4 are free here.</summary>
        public override uint SpellGroupCoreRangeMax => 3;

        protected override void ConfigureTables(WorldTableSet tables)
        {
            base.ConfigureTables(tables);
            tables.Replace<SpellGroupTable>(new AzerothCoreSpellGroupTable(this));
            tables.Add(new SpellCooldownOverridesTable(this));
        }
    }
}
