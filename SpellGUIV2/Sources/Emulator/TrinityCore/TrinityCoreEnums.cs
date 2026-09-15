using System.Linq;

namespace SpellEditor.Sources.Emulator.TrinityCore
{
    /// <summary>The spell enums that are TrinityCore's own, from SpellMgr.h and SpellInfo.h.</summary>
    public static class TrinityCoreEnums
    {
        public static readonly FlagSet ProcAttributes = new FlagSet("ProcAttributes", "emu_tc_proc_attributes_strings",
            new uint[] { 0x001, 0x002, 0x004, 0x008, 0x080, 0x100 },
            new[]
            {
                "Proc target must give exp or honor", "Can proc from triggered spells",
                "Triggering spell must have a mana cost", "Triggering spell must be affected by this aura",
                "Reduced proc chance above level 60", "Cannot proc from a spell cast by an item"
            });

        public static readonly FlagSet CustomAttributes = new FlagSet("SpellCustomAttributes", "emu_tc_custom_attr_strings",
            Enumerable.Range(0, 26).Select(index => 1u << index).ToArray(),
            new[]
            {
                "Enchant proc", "Cone back", "Cone line", "Share damage", "No initial threat", "Aura is crowd control",
                "Does not break stealth", "Can crit", "Direct damage", "Charge", "Pickpocket", "Rolling periodic",
                "Negative effect 1", "Negative effect 2", "Negative effect 3", "Ignore armor",
                "Target must face caster", "Caster must be behind target", "Allow in flight target",
                "Needs ammo data", "Binary spell", "Physical school counts as magic", "Deprecated liquid aura (do not reuse)",
                "Is talent (master branch only)", "Aura cannot be saved", "Can target any private object (master branch only)"
            });
    }
}
