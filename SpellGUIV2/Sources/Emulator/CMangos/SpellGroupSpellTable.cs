using System;
using System.Collections.Generic;
using System.Data;

namespace SpellEditor.Sources.Emulator.CMangos
{
    /// <summary>
    /// Group membership, see SpellStacker::LoadSpellGroups. CMaNGOS splits this in two, the group
    /// itself with its stacking rule lives in spell_group and the members here.
    /// </summary>
    public class SpellGroupSpellTable : CMangosTable
    {
        /// <summary>The core packs the group into a bit of a 64 bit mask, see SpellGroupId::MAX.</summary>
        private const uint MaxGroupId = 64;

        public SpellGroupSpellTable(CMangosEmulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_group_spell";

        public override string OrderBy => "`Id`";

        // Keyed on the exact spell id, the loader has no rank handling
        public override string SpellFilter(RankChain chain) => $"`SpellId` = {chain.SpellId}";

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "Id", IsKey = true, DefaultValue = "1" };
            yield return new WorldColumn { Name = "SpellId", IsKey = true, IsPrimarySpellKey = true };
        }

        public override string Validate(WorldColumn column, uint value)
        {
            if (column.Name != "Id" || (value >= 1 && value <= MaxGroupId))
                return null;

            return string.Format(Localise("emu_cm_error_group_range",
                "the core packs the group into a bit of a 64 bit mask, so the id has to be from 1 to {0}"),
                MaxGroupId);
        }

        /// <summary>Says what the group is, since the rule and the name live in the other table.</summary>
        public override IReadOnlyList<string> DescribeRows(WorldDatabase database, DataTable rows)
        {
            var descriptions = new List<string>();
            foreach (DataRow row in rows.Rows)
            {
                if (!uint.TryParse((row["Id"] as string ?? string.Empty).Trim(), out var id))
                {
                    descriptions.Add(string.Empty);
                    continue;
                }

                try
                {
                    var group = database.Query($"SELECT `Rule`, `Name` FROM `spell_group` WHERE `Id` = {id} LIMIT 1");
                    if (group.Rows.Count == 0)
                    {
                        descriptions.Add(string.Format(Localise("emu_cm_group_missing",
                            "No group {0} in spell_group, so the core throws this row away."), id));
                        continue;
                    }

                    var rule = CMangosEnums.SpellGroupRules.Describe(group.Rows[0][0].ToString());
                    descriptions.Add(string.Format(Localise("emu_cm_group_rule", "{0}, rule {1}."),
                        group.Rows[0][1], rule));
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
            "Groups this spell belongs to. The group itself, with the stacking rule and the name, lives in spell_group.";

        public override string FallbackEmpty => "This spell is not in any group.";

        public override string[] FallbackLabels => new[] { "Group id", "Spell" };

        public override string[] FallbackTooltips => new[]
        {
            "Group the spell belongs to. It has to already exist in spell_group, and the id has to be from 1 to 64.",
            "Spell in the group."
        };
    }
}
