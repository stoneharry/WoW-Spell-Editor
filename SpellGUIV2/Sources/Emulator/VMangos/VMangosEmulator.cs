using SpellEditor.Sources.Emulator.Tables;

namespace SpellEditor.Sources.Emulator.VMangos
{
    /// <summary>
    /// VMangos, the 1.12.1 vanilla core. It shares far less with the 3.3.5 cores than they share
    /// with each other, so most of the default tables are dropped or swapped for a 1.12 version.
    /// There is no spell_custom_attr here, so that panel is hidden as well.
    /// </summary>
    public class VMangosEmulator : Emulator
    {
        public override string Id => "VMangos";

        public override string Name => "VMangos";

        public override string Description => "VMangos world database, the 1.12.1 vanilla schema.";

        public override string DefaultUser => "mangos";

        public override string DefaultDatabase => "mangos";

        /// <summary>
        /// SUPPORTED_CLIENT_BUILD from Progression.h. VMangos ships set to 1.12.1 and the rows are
        /// gated on it, so it decides which of them the running core actually reads.
        /// </summary>
        public uint ClientBuild => 5875;

        protected override void ConfigureTables(WorldTableSet tables)
        {
            base.ConfigureTables(tables);

            // 1.12 has no equivalent of any of these
            tables.Remove<SpellLinkedSpellTable>();
            tables.Remove<SpellScriptNamesTable>();
            tables.Remove<SpellBonusDataTable>();

            // Same job as the 3.3.5 table, different name or different columns
            tables.Replace<SpellProcTable>(new SpellProcEventTable(this));
            tables.Replace<SpellGroupTable>(new VMangosSpellGroupTable(this));
            tables.Replace<SpellThreatTable>(new VMangosSpellThreatTable(this));
            tables.Replace<SpellRanksTable>(new SpellChainTable(this));

            tables.Add(new SpellElixirTable(this));
            tables.Add(new SpellDisabledTable(this));
        }

        /// <summary>The chain lives in spell_chain rather than spell_ranks.</summary>
        public override string RankChainQuery(uint spellId) =>
            "SELECT other.`spell_id`, other.`rank` FROM `spell_chain` AS mine " +
            "JOIN `spell_chain` AS other ON other.`first_spell` = mine.`first_spell` " +
            $"WHERE mine.`spell_id` = {spellId} AND {ClientBuild} BETWEEN other.`build_min` AND other.`build_max` " +
            "ORDER BY other.`rank`";
    }
}
