using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.Tables
{
    /// <summary>Spell to spell links, see SpellMgr::LoadSpellLinked.</summary>
    public class SpellLinkedSpellTable : WorldTable
    {
        public SpellLinkedSpellTable(Emulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_linked_spell";

        public override string OrderBy => "`type`, `spell_effect`";

        // LoadSpellLinked keys on the exact id, nothing is inherited by a rank
        public override string SpellFilter(RankChain chain) => $"ABS(`spell_trigger`) = {chain.SpellId}";

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn
            {
                Name = "spell_trigger",
                Kind = ColumnKind.Int,
                IsKey = true,
                IsPrimarySpellKey = true,
                NegativeOptionKey = "emu_option_on_aura_removed",
                FallbackNegativeOption = "Fire when the aura is removed"
            };
            yield return new WorldColumn
            {
                Name = "spell_effect",
                Kind = ColumnKind.Int,
                IsKey = true,
                ShowSpellPicker = true,
                NegativeOptionKey = "emu_option_remove_instead",
                FallbackNegativeOption = "Remove it, or grant immunity, instead of applying it"
            };
            yield return new WorldColumn { Name = "type", IsKey = true, Enum = WorldEnums.SpellLinkedTypes };
            yield return new WorldColumn { Name = "comment", Kind = ColumnKind.Text, DefaultValue = "", IsMultiline = true };
        }

        public override string FallbackHeader => "Links";

        public override string FallbackDescription =>
            "Casts, applies, or removes another spell when this one is cast, hits, or applies its aura.";

        public override string FallbackEmpty => "This spell is not linked to any other spell.";

        public override string[] FallbackLabels => new[] { "Trigger spell", "Linked spell", "Fires on", "Comment" };

        public override string[] FallbackTooltips => new[]
        {
            "Spell that triggers the link. A negative id means the link fires when the aura is removed.",
            "Spell that gets applied. A negative id removes that spell instead, or grants immunity for aura links.",
            "When the link fires. Cast, hit, or aura applied.",
            "Free text note, shown only in the database."
        };
    }
}
