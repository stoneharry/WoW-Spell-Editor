using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.AzerothCore
{
    /// <summary>
    /// AzerothCore only, TrinityCore has no equivalent table. See SpellMgr::LoadSpellCooldownOverrides.
    /// </summary>
    public class SpellCooldownOverridesTable : WorldTable
    {
        public SpellCooldownOverridesTable(Emulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_cooldown_overrides";

        public override string OrderBy => "`Id`";

        public override bool IsSingleRow => true;

        // Keyed on the exact spell id, nothing is inherited by a rank
        public override string SpellFilter(RankChain chain) => $"`Id` = {chain.SpellId}";

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "Id", IsKey = true, IsPrimarySpellKey = true };
            yield return new WorldColumn { Name = "RecoveryTime" };
            yield return new WorldColumn { Name = "CategoryRecoveryTime" };
            yield return new WorldColumn { Name = "StartRecoveryTime" };
            yield return new WorldColumn { Name = "StartRecoveryCategory" };
            yield return new WorldColumn { Name = "Comment", Kind = ColumnKind.Text, AllowNull = true, DefaultValue = "", IsMultiline = true };
        }

        public override string FallbackHeader => "Cooldown";

        public override string FallbackDescription =>
            "Overrides the cooldown fields this spell would otherwise take from Spell.dbc.";

        public override string FallbackEmpty =>
            "This spell has no cooldown override. The cooldowns come from Spell.dbc.";

        public override string[] FallbackLabels => new[]
        {
            "Spell", "Cooldown (ms)", "Category cooldown (ms)", "Global cooldown (ms)",
            "Global cooldown category", "Comment"
        };

        public override string[] FallbackTooltips => new[]
        {
            "Spell id the cooldowns apply to.",
            "Replaces RecoveryTime, the cooldown on the spell itself.",
            "Replaces CategoryRecoveryTime, the cooldown shared by everything in the same category.",
            "Replaces StartRecoveryTime, the global cooldown the cast triggers.",
            "Replaces StartRecoveryCategory, which global cooldown category the cast uses.",
            "Free text note, shown only in the database."
        };
    }
}
