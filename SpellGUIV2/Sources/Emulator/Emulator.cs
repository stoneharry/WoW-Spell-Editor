using SpellEditor.Sources.Emulator.AzerothCore;
using SpellEditor.Sources.Emulator.CMangos.Classic;
using SpellEditor.Sources.Emulator.CMangos.Tbc;
using SpellEditor.Sources.Emulator.CMangos.Wotlk;
using SpellEditor.Sources.Emulator.Tables;
using SpellEditor.Sources.Emulator.TrinityCore;
using SpellEditor.Sources.Emulator.VMangos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SpellEditor.Sources.Emulator
{
    /// <summary>
    /// One supported server core. The defaults on this class describe what the supported 3.3.5
    /// cores have in common, a core subclasses it in its own folder and overrides only where it
    /// differs. Adding a core is a new subclass plus an entry in Emulators.All.
    /// </summary>
    public abstract class Emulator
    {
        private IReadOnlyList<WorldTable> _tables;

        /// <summary>Written to config.xml, so it must not change once shipped.</summary>
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string DefaultUser { get; }
        public virtual string DefaultDatabase => "world";

        #region spell_custom_attr
        public virtual string CustomAttrTable => "spell_custom_attr";
        public virtual string CustomAttrSpellColumn => "entry";
        public virtual string CustomAttrValueColumn => "attributes";

        /// <summary>Null on a core with no spell_custom_attr table, the panel is hidden then.</summary>
        public virtual FlagSet CustomAttributes => null;
        #endregion

        #region Spell rules the cores disagree on
        /// <summary>Null on a core whose proc table has no attributes column.</summary>
        public virtual FlagSet ProcAttributes => null;

        /// <summary>Ids from this value up to SpellGroupDbRangeMin are reserved by the core.</summary>
        public virtual uint SpellGroupCoreRangeMax => 5;
        public virtual uint SpellGroupDbRangeMin => 1000;

        /// <summary>
        /// Every rank of the chain the spell belongs to, as spell id and rank. Most world tables
        /// are written against rank 1 and the core applies them to the rest of the chain.
        /// </summary>
        public virtual string RankChainQuery(uint spellId) =>
            "SELECT other.`spell_id`, other.`rank` FROM `spell_ranks` AS mine " +
            "JOIN `spell_ranks` AS other ON other.`first_spell_id` = mine.`first_spell_id` " +
            $"WHERE mine.`spell_id` = {spellId} ORDER BY other.`rank`";
        #endregion

        /// <summary>Built once on first use, the columns carry per core labels.</summary>
        public IReadOnlyList<WorldTable> Tables => _tables ?? (_tables = BuildTables());

        /// <summary>
        /// The tables every supported core shares. A subclass calls base and then replaces, adds,
        /// or removes entries for its own schema.
        /// </summary>
        protected virtual void ConfigureTables(WorldTableSet tables)
        {
            tables.Add(new SpellProcTable(this));
            tables.Add(new SpellLinkedSpellTable(this));
            tables.Add(new SpellScriptNamesTable(this));
            tables.Add(new SpellBonusDataTable(this));
            tables.Add(new SpellGroupTable(this));
            tables.Add(new SpellThreatTable(this));
            tables.Add(new SpellRanksTable(this));
        }

        private IReadOnlyList<WorldTable> BuildTables()
        {
            var set = new WorldTableSet();
            ConfigureTables(set);
            return set.ToList();
        }

        public override string ToString() => Name;
    }

    /// <summary>The table list while a core is still shaping it, see Emulator.ConfigureTables.</summary>
    public class WorldTableSet : IEnumerable<WorldTable>
    {
        private readonly List<WorldTable> _tables = new List<WorldTable>();

        public void Add(WorldTable table) => _tables.Add(table);

        /// <summary>Swaps a default table for a subclass that reads it the way this core does.</summary>
        public void Replace<T>(WorldTable table) where T : WorldTable
        {
            var index = _tables.FindIndex(entry => entry is T);
            if (index < 0)
                _tables.Add(table);
            else
                _tables[index] = table;
        }

        /// <summary>Drops a table this core does not have. False when it was not in the set.</summary>
        public bool Remove<T>() where T : WorldTable => _tables.RemoveAll(entry => entry is T) > 0;

        public IEnumerator<WorldTable> GetEnumerator() => _tables.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class Emulators
    {
        public static readonly Emulator TrinityCore = new TrinityCoreEmulator();

        public static readonly Emulator AzerothCore = new AzerothCoreEmulator();

        public static readonly Emulator VMangos = new VMangosEmulator();

        public static readonly Emulator CMangosWotlk = new CMangosWotlkEmulator();

        public static readonly Emulator CMangosTbc = new CMangosTbcEmulator();

        public static readonly Emulator CMangosClassic = new CMangosClassicEmulator();

        public static readonly IReadOnlyList<Emulator> All = new List<Emulator>
        {
            TrinityCore,
            AzerothCore,
            CMangosWotlk,
            CMangosTbc,
            CMangosClassic,
            VMangos
        };

        public static Emulator Default => TrinityCore;

        public static Emulator Lookup(string id) =>
            All.FirstOrDefault(entry => entry.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) ?? Default;

        public static Emulator Current => Lookup(Config.Config.EmulatorType);
    }
}
