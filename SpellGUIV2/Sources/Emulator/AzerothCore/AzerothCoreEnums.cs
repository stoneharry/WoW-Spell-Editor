using System.Linq;

namespace SpellEditor.Sources.Emulator.AzerothCore
{
    /// <summary>The spell enums that are AzerothCore's own, from SpellMgr.h and SpellInfo.h.</summary>
    public static class AzerothCoreEnums
    {
        /// <summary>Same as TrinityCore plus PROC_ATTR_USE_STACKS_FOR_CHARGES at 0x010.</summary>
        public static readonly FlagSet ProcAttributes = new FlagSet("ProcAttributes", "emu_ac_proc_attributes_strings",
            new uint[] { 0x001, 0x002, 0x004, 0x008, 0x010, 0x080, 0x100 },
            new[]
            {
                "Proc target must give exp or honor", "Can proc from triggered spells",
                "Triggering spell must have a mana cost", "Triggering spell must be affected by this aura",
                "Consume a stack rather than a charge", "Reduced proc chance above level 60",
                "Cannot proc from a spell cast by an item"
            });

        /// <summary>Diverges from TrinityCore from bit 7 on, so it is a list of its own.</summary>
        public static readonly FlagSet CustomAttributes = new FlagSet("SpellCustomAttributes", "emu_ac_custom_attr_strings",
            Enumerable.Range(0, 32).Select(index => 1u << index).ToArray(),
            new[]
            {
                "Enchant proc", "Cone back", "Cone line", "Share damage", "No initial threat", "Aura is crowd control",
                "Does not break stealth", "Does not set the PvP flag", "Direct damage", "Charge", "Pickpocket",
                "Ignore evade", "Negative effect 1", "Negative effect 2", "Negative effect 3", "Ignore armor",
                "Target must face caster", "Caster must be behind target", "Allow in flight target",
                "Needs ammo data", "Binary spell", "No positive taken bonus", "Single aura stack",
                "Physical school counts as magic", "Aura cannot be saved", "Positive effect 1", "Positive effect 2",
                "Positive effect 3", "Force send category cooldowns", "Only one area aura", "Encounter reward",
                "Bypass mechanic immunity"
            });
    }
}
