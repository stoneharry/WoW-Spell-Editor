using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace SpellEditor.Sources.Emulator.VMangos
{
    /// <summary>
    /// Shared base for the VMangos tables. Almost every spell table there carries build_min and
    /// build_max and the core only loads a row when the build it was compiled for falls inside
    /// that range, so the columns are editable and a row outside the range says so.
    /// </summary>
    public abstract class VMangosTable : WorldTable
    {
        protected VMangosTable(VMangosEmulator emulator) : base(emulator)
        {
            Core = emulator;
        }

        /// <summary>The same object as Emulator, typed so the build can be read off it.</summary>
        protected VMangosEmulator Core { get; }

        // spell_threat and spell_group exist on the 3.3.5 cores too but with other columns, so
        // every table here takes its own keys rather than sharing a name with a different shape
        public override string HeaderResourceKey => $"emu_vm_tab_{Name}";
        public override string DescriptionResourceKey => $"emu_vm_desc_{Name}";
        public override string EmptyResourceKey => $"emu_vm_empty_{Name}";
        public override string LabelsResourceKey => $"emu_vm_labels_{Name}";
        public override string TooltipsResourceKey => $"emu_vm_tips_{Name}";

        protected WorldColumn BuildMinColumn() => new WorldColumn
        {
            Name = "build_min",
            IsKey = true,
            DefaultValue = "0"
        };

        protected WorldColumn BuildMaxColumn() => new WorldColumn
        {
            Name = "build_max",
            IsKey = true,
            DefaultValue = Core.ClientBuild.ToString(CultureInfo.InvariantCulture)
        };

        public override IReadOnlyList<string> DescribeRows(WorldDatabase database, DataTable rows)
        {
            if (FindColumn("build_min") == null)
                return null;

            var notes = new List<string>();
            foreach (DataRow row in rows.Rows)
                notes.Add(DescribeBuildRange(row));
            return notes;
        }

        /// <summary>Empty while the row is one the running core would load.</summary>
        protected string DescribeBuildRange(DataRow row)
        {
            if (!TryReadBuild(row, "build_min", out var min) || !TryReadBuild(row, "build_max", out var max))
                return string.Empty;
            if (Core.ClientBuild >= min && Core.ClientBuild <= max)
                return string.Empty;

            return string.Format(Localise("emu_vm_build_skipped",
                "Written for builds {0} to {1}, so a core built for {2} does not load this row."),
                min, max, Core.ClientBuild);
        }

        private static bool TryReadBuild(DataRow row, string column, out uint value)
        {
            value = 0;
            if (!row.Table.Columns.Contains(column))
                return false;
            return uint.TryParse((row[column] as string ?? string.Empty).Trim(),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>The two build labels every table here ends with.</summary>
        protected static string[] WithBuildLabels(params string[] labels)
        {
            var all = new List<string>(labels) { "First build", "Last build" };
            return all.ToArray();
        }

        protected static string[] WithBuildTooltips(params string[] tooltips)
        {
            var all = new List<string>(tooltips)
            {
                "Oldest client build the row applies to. 0 means no lower limit.",
                "Newest client build the row applies to. The core skips the row unless the build it was compiled for falls in the range."
            };
            return all.ToArray();
        }
    }
}
