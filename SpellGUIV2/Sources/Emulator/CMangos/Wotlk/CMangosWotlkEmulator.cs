namespace SpellEditor.Sources.Emulator.CMangos.Wotlk
{
    /// <summary>
    /// CMaNGOS WotLK, the 3.3.5 branch. WotLK widened the spell family mask, which changes the
    /// shape of two tables, and dropped spell_affect because the client data covers it by then.
    /// </summary>
    public class CMangosWotlkEmulator : CMangosEmulator
    {
        public override string Id => "CMangosWotlk";

        public override string Name => "CMaNGOS WotLK";

        public override string Description => "CMaNGOS WotLK world database, the stock wotlkmangos schema.";

        public override string DefaultDatabase => "wotlkmangos";

        public override FlagSet ProcFlags => CMangosEnums.ProcFlags;

        /// <summary>WotLK put a full block result on the bit the earlier branches leave reserved.</summary>
        public override FlagSet ProcFlagsEx => CMangosEnums.ProcFlagsExWotlk;

        public override FlagSet ElixirMask => CMangosEnums.ElixirMask;

        protected override void ConfigureTables(WorldTableSet tables)
        {
            base.ConfigureTables(tables);

            // The client data carries the family mask by WotLK, so the core stopped reading this
            tables.Remove<SpellAffectTable>();

            tables.Replace<SpellProcEventTable>(new WotlkSpellProcEventTable(this));
            tables.Replace<SpellPetAurasTable>(new WotlkSpellPetAurasTable(this));
        }
    }
}
