using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.Tables
{
    /// <summary>C++ scripts bound to a spell, see ObjectMgr::LoadSpellScriptNames.</summary>
    public class SpellScriptNamesTable : WorldTable
    {
        public SpellScriptNamesTable(Emulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_script_names";

        public override string OrderBy => "`ScriptName`";

        public override bool InheritsRankChain => true;

        // Same negative id convention as spell_proc
        public override string SpellFilter(RankChain chain) =>
            In("spell_id", chain.SpellId, -chain.SpellId, -chain.FirstSpellId);

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn
            {
                Name = "spell_id",
                Kind = ColumnKind.Int,
                IsKey = true,
                IsPrimarySpellKey = true,
                NegativeOptionKey = "emu_option_all_ranks",
                FallbackNegativeOption = "Apply to every rank of this spell"
            };
            yield return new WorldColumn { Name = "ScriptName", Kind = ColumnKind.Text, IsKey = true, DefaultValue = "" };
        }

        public override string FallbackHeader => "Scripts";

        public override string FallbackDescription =>
            "Attaches a C++ SpellScript or AuraScript from the server binary to this spell.";

        public override string FallbackEmpty => "No scripts are attached to this spell.";

        public override string[] FallbackLabels => new[] { "Spell", "Script name" };

        public override string[] FallbackTooltips => new[]
        {
            "Spell id the script is attached to. A negative id applies it to every rank of the spell.",
            "Name the script registers itself with in the server source."
        };
    }
}
