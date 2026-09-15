using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SpellEditor.Sources.Emulator
{
    /// <summary>
    /// One editable world table, scoped to a spell. The defaults here describe how the tables that
    /// every supported core shares behave. A core that reads a table differently subclasses the
    /// default in its own folder and overrides only what changed, see AzerothCoreSpellGroupTable.
    /// </summary>
    public abstract class WorldTable
    {
        private IReadOnlyList<WorldColumn> _columns;

        protected WorldTable(Emulator emulator)
        {
            Emulator = emulator;
        }

        /// <summary>The core this table was built for, so overrides can read its rules.</summary>
        public Emulator Emulator { get; }

        public abstract string Name { get; }

        /// <summary>WHERE clause finding every row the core would apply to the spell.</summary>
        public abstract string SpellFilter(RankChain chain);

        public abstract string OrderBy { get; }

        /// <summary>A row on the first rank covers every rank, so later ranks show it too.</summary>
        public virtual bool InheritsRankChain => false;

        /// <summary>One row per spell at most, so the editor shows fields rather than a list.</summary>
        public virtual bool IsSingleRow => false;

        /// <summary>False on the table that is the rank chain, it would only repeat itself.</summary>
        public virtual bool ShowsRankChainSummary => true;

        protected abstract IEnumerable<WorldColumn> CreateColumns();

        public IReadOnlyList<WorldColumn> Columns => _columns ?? (_columns = CreateColumns().ToList());

        #region Language file keys
        public virtual string HeaderResourceKey => $"emu_tab_{Name}";
        public virtual string DescriptionResourceKey => $"emu_desc_{Name}";
        public virtual string EmptyResourceKey => $"emu_empty_{Name}";
        /// <summary>One pipe separated label per column in column order.</summary>
        public virtual string LabelsResourceKey => $"emu_labels_{Name}";
        /// <summary>One pipe separated tooltip per column in column order.</summary>
        public virtual string TooltipsResourceKey => $"emu_tips_{Name}";

        public abstract string FallbackHeader { get; }
        public abstract string FallbackDescription { get; }
        public abstract string FallbackEmpty { get; }
        public abstract string[] FallbackLabels { get; }
        public abstract string[] FallbackTooltips { get; }

        public string Header => Localise(HeaderResourceKey, FallbackHeader);

        public string Description => Localise(DescriptionResourceKey, FallbackDescription);

        public string EmptyMessage => Localise(EmptyResourceKey, FallbackEmpty);

        /// <summary>Falls back to English when a translation is missing or out of sync.</summary>
        public void ApplyLocalisedText()
        {
            var labels = WorldEnums.ReadStringList(LabelsResourceKey, Columns.Count) ?? FallbackLabels;
            var tooltips = WorldEnums.ReadStringList(TooltipsResourceKey, Columns.Count) ?? FallbackTooltips;
            for (var i = 0; i < Columns.Count; ++i)
            {
                Columns[i].Label = labels[i];
                Columns[i].Tooltip = tooltips[i];
            }
        }

        protected static string Localise(string key, string fallback)
        {
            var resource = System.Windows.Application.Current?.TryFindResource(key) as string;
            return string.IsNullOrWhiteSpace(resource) ? fallback : resource.Trim();
        }
        #endregion

        #region Hooks a subclass can take over
        /// <summary>Extra context line per row, worked out on the loading thread. Null for none.</summary>
        public virtual IReadOnlyList<string> DescribeRows(WorldDatabase database, DataTable rows) => null;

        /// <summary>What a negative id means in a spell column here, null when it is just a spell.</summary>
        public virtual string DescribeNegativeSpellId(int id) => null;

        /// <summary>Null when the value is fine, otherwise why the core would reject it.</summary>
        public virtual string Validate(WorldColumn column, uint value) => null;
        #endregion

        public IEnumerable<WorldColumn> KeyColumns => Columns.Where(column => column.IsKey);

        public WorldColumn PrimarySpellColumn => Columns.FirstOrDefault(column => column.IsPrimarySpellKey);

        public WorldColumn FindColumn(string name) =>
            Columns.FirstOrDefault(column => column.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        /// <summary>Duplicates dropped, a spell that is its own first rank must not repeat.</summary>
        protected static string In(string column, params long[] ids) =>
            $"`{column}` IN ({string.Join(", ", ids.Distinct())})";

        public override string ToString() => Name;
    }
}
