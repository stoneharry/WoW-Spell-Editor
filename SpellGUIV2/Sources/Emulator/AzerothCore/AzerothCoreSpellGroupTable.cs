using SpellEditor.Sources.Emulator.Tables;

namespace SpellEditor.Sources.Emulator.AzerothCore
{
    /// <summary>
    /// Same schema as the shared table, but AzerothCore only declares two elixir groups, so the
    /// reserved band and the tooltip that explains it are different.
    /// </summary>
    public class AzerothCoreSpellGroupTable : SpellGroupTable
    {
        public AzerothCoreSpellGroupTable(Emulator emulator) : base(emulator)
        {
        }

        public override string TooltipsResourceKey => "emu_ac_tips_spell_group";

        public override string[] FallbackTooltips => new[]
        {
            "Group id. Ids 1 and 2 are defined by the core, custom groups must be 1000 or greater.",
            "Spell in the group."
        };
    }
}
