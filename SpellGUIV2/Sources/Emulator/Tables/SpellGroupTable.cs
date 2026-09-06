using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SpellEditor.Sources.Emulator.Tables
{
    /// <summary>Group membership, see SpellMgr::LoadSpellGroups.</summary>
    public class SpellGroupTable : WorldTable
    {
        public SpellGroupTable(Emulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_group";

        public override string OrderBy => "`id`";

        public override bool InheritsRankChain => true;

        // GetSpellSpellGroupMapBounds looks up the first spell in the chain
        public override string SpellFilter(RankChain chain) => In("spell_id", chain.SpellId, chain.FirstSpellId);

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "id", IsKey = true, NewRowValue = NextGroupId };
            yield return new WorldColumn { Name = "spell_id", Kind = ColumnKind.Int, IsKey = true, IsPrimarySpellKey = true };
        }

        /// <summary>Custom groups start above the band the core declares itself.</summary>
        private string NextGroupId(NewRowContext context)
        {
            try
            {
                var existing = context.Database.QuerySingleValue($"SELECT MAX(`id`) FROM `{Name}`");
                var max = existing == null ? 0u : Convert.ToUInt32(existing);
                return Math.Max(Emulator.SpellGroupDbRangeMin, max + 1).ToString();
            }
            catch (Exception)
            {
                return Emulator.SpellGroupDbRangeMin.ToString();
            }
        }

        public override string Validate(WorldColumn column, uint value)
        {
            if (column.Name != "id" || value < Emulator.SpellGroupCoreRangeMax || value > Emulator.SpellGroupDbRangeMin)
                return null;

            return string.Format(Localise("emu_error_group_range",
                "group ids from {0} to {1} are reserved for groups the core declares itself, use {1} or higher"),
                Emulator.SpellGroupCoreRangeMax, Emulator.SpellGroupDbRangeMin);
        }

        /// <summary>A negative spell id is a nested group rather than a spell here.</summary>
        public override string DescribeNegativeSpellId(int id) =>
            string.Format(Localise("emu_nested_group", "Group {0}"), Math.Abs(id));

        /// <summary>Annotates each membership with what else is in the group, one query per group.</summary>
        public override IReadOnlyList<string> DescribeRows(WorldDatabase database, DataTable rows)
        {
            var descriptions = new List<string>();
            foreach (DataRow row in rows.Rows)
            {
                var groupId = row["id"] as string;
                if (string.IsNullOrWhiteSpace(groupId) || !uint.TryParse(groupId, out var id))
                {
                    descriptions.Add(string.Empty);
                    continue;
                }

                try
                {
                    var members = database.Query($"SELECT `spell_id` FROM `{Name}` WHERE `id` = {id} LIMIT 200");
                    var ids = members.Rows.Cast<DataRow>().Select(member => member[0].ToString()).ToList();
                    var others = ids.Where(value => value != (row["spell_id"] as string)).ToList();
                    descriptions.Add(others.Count == 0
                        ? Localise("emu_group_only_member", "This spell is the only member of the group.")
                        : string.Format(Localise("emu_group_members", "{0} other member(s): {1}"),
                            others.Count,
                            string.Join(", ", others.Take(12)) + (others.Count > 12 ? ", ..." : string.Empty)));
                }
                catch (Exception)
                {
                    descriptions.Add(string.Empty);
                }
            }
            return descriptions;
        }

        public override string FallbackHeader => "Groups";

        public override string FallbackDescription =>
            "Groups this spell belongs to. spell_group_stack_rules then decides how the members stack.";

        public override string FallbackEmpty => "This spell is not in any group.";

        public override string[] FallbackLabels => new[] { "Group id", "Spell" };

        public override string[] FallbackTooltips => new[]
        {
            "Group id. Ids 1 to 4 are defined by the core, custom groups must be 1000 or greater.",
            "Spell in the group."
        };
    }
}
