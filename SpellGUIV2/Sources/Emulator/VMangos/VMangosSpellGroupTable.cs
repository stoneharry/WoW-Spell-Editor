using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace SpellEditor.Sources.Emulator.VMangos
{
    /// <summary>
    /// See SpellMgr::LoadSpellGroups. The 3.3.5 cores key this on id and spell_id, 1.12 adds
    /// group_spell_id, which orders the members inside the group, and its spell_id is unsigned so
    /// there are no nested groups.
    /// </summary>
    public class VMangosSpellGroupTable : VMangosTable
    {
        public VMangosSpellGroupTable(VMangosEmulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_group";

        public override string OrderBy => "`group_id`, `group_spell_id`";

        public override bool InheritsRankChain => true;

        public override string SpellFilter(RankChain chain) => In("spell_id", chain.SpellId, chain.FirstSpellId);

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "group_id", IsKey = true, NewRowValue = NextGroupId };
            yield return new WorldColumn { Name = "group_spell_id", IsKey = true, NewRowValue = NextGroupSpellId };
            yield return new WorldColumn { Name = "spell_id", IsKey = true, IsPrimarySpellKey = true };
            yield return BuildMinColumn();
            yield return BuildMaxColumn();
        }

        /// <summary>Nothing reserves a range here, so it is just the next id that is free.</summary>
        private string NextGroupId(NewRowContext context)
        {
            try
            {
                var existing = context.Database.QuerySingleValue($"SELECT MAX(`group_id`) FROM `{Name}`");
                var max = existing == null ? 0u : Convert.ToUInt32(existing);
                return (max + 1).ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return "1";
            }
        }

        private static string NextGroupSpellId(NewRowContext context)
        {
            var highest = 0u;
            foreach (var row in context.Rows)
            {
                if (uint.TryParse((row["group_spell_id"] as string ?? string.Empty).Trim(), out var index) &&
                    index >= highest)
                    highest = index + 1;
            }
            return highest.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Says what else is in the group, on top of the build note from the base.</summary>
        public override IReadOnlyList<string> DescribeRows(WorldDatabase database, DataTable rows)
        {
            var descriptions = new List<string>();
            foreach (DataRow row in rows.Rows)
            {
                var build = DescribeBuildRange(row);
                if (!uint.TryParse((row["group_id"] as string ?? string.Empty).Trim(), out var id))
                {
                    descriptions.Add(build);
                    continue;
                }

                try
                {
                    var members = database.Query($"SELECT `spell_id` FROM `{Name}` WHERE `group_id` = {id} LIMIT 200");
                    var others = members.Rows.Cast<DataRow>()
                        .Select(member => member[0].ToString())
                        .Where(value => value != (row["spell_id"] as string))
                        .ToList();

                    var summary = others.Count == 0
                        ? Localise("emu_group_only_member", "This spell is the only member of the group.")
                        : string.Format(Localise("emu_group_members", "{0} other member(s): {1}"),
                            others.Count, string.Join(", ", others.Take(12)) + (others.Count > 12 ? ", ..." : string.Empty));

                    descriptions.Add(string.IsNullOrEmpty(build) ? summary : build + " " + summary);
                }
                catch (Exception)
                {
                    descriptions.Add(build);
                }
            }
            return descriptions;
        }

        public override string FallbackHeader => "Groups";

        public override string FallbackDescription =>
            "Groups this spell belongs to. spell_group_stack_rules then decides how the members stack against each other.";

        public override string FallbackEmpty => "This spell is not in any group.";

        public override string[] FallbackLabels => WithBuildLabels("Group id", "Position in group", "Spell");

        public override string[] FallbackTooltips => WithBuildTooltips(
            "Group the spell belongs to. Any id is allowed, the core reserves none of them.",
            "Orders the members inside the group. It only decides the order the core reads them in.",
            "Spell in the group.");
    }
}
