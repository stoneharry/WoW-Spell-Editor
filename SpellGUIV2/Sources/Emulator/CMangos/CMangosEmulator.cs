namespace SpellEditor.Sources.Emulator.CMangos
{
    /// <summary>
    /// Shared base for the three CMaNGOS branches. None of the 3.3.5 default tables survive here,
    /// the names and the columns both differ, so ConfigureTables does not call base and builds the
    /// CMaNGOS set from scratch. A branch then adds, replaces or removes what its expansion changed.
    /// There is no spell_custom_attr on any of them.
    /// </summary>
    public abstract class CMangosEmulator : Emulator
    {
        public override string DefaultUser => "mangos";

        /// <summary>Proc flags, elixir kinds and hit results all moved between expansions.</summary>
        public abstract FlagSet ProcFlags { get; }
        public abstract FlagSet ProcFlagsEx { get; }
        public abstract FlagSet ElixirMask { get; }

        protected override void ConfigureTables(WorldTableSet tables)
        {
            tables.Add(new SpellProcEventTable(this));
            tables.Add(new SpellThreatTable(this));
            tables.Add(new SpellGroupSpellTable(this));
            tables.Add(new SpellElixirTable(this));
            tables.Add(new SpellLearnSpellTable(this));
            tables.Add(new SpellPetAurasTable(this));
            tables.Add(new SpellAffectTable(this));
            tables.Add(new SpellChainTable(this));
        }

        /// <summary>The chain lives in spell_chain rather than spell_ranks.</summary>
        public override string RankChainQuery(uint spellId) =>
            "SELECT other.`spell_id`, other.`rank` FROM `spell_chain` AS mine " +
            "JOIN `spell_chain` AS other ON other.`first_spell` = mine.`first_spell` " +
            $"WHERE mine.`spell_id` = {spellId} ORDER BY other.`rank`";
    }
}
