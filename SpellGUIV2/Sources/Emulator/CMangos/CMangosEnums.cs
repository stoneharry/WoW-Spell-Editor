using System.Linq;

namespace SpellEditor.Sources.Emulator.CMangos
{
    /// <summary>
    /// Spell enums for the CMaNGOS branches. The three of them agree on most of this, so a list is
    /// only split where an expansion actually changed it.
    /// </summary>
    public static class CMangosEnums
    {
        private static uint[] Bits(int count) => Enumerable.Range(0, count).Select(index => 1u << index).ToArray();

        private static readonly string[] ProcFlagNames =
        {
            "None",
            "On tick (heartbeat)", "Killed a target",
            "Melee auto attack done", "Melee auto attack taken",
            "Melee weapon ability done", "Melee weapon ability taken",
            "Ranged auto attack done", "Ranged auto attack taken",
            "Ranged weapon ability done", "Ranged weapon ability taken",
            "Positive none damage class spell done", "Positive none damage class spell taken",
            "Negative none damage class spell done", "Negative none damage class spell taken",
            "Positive magic damage class spell done", "Positive magic damage class spell taken",
            "Negative magic damage class spell done", "Negative magic damage class spell taken",
            "Periodic done", "Periodic taken",
            "Any damage taken", "Trap activation",
            "Main hand attack done", "Off hand attack done",
            "Death"
        };

        /// <summary>Classic stops one bit short, it has no death proc.</summary>
        public static readonly FlagSet ProcFlagsClassic = new FlagSet("ProcFlags", "emu_cm_proc_strings",
            new uint[] { 0 }.Concat(Bits(24)).ToArray(),
            ProcFlagNames.Take(25).ToArray());

        public static readonly FlagSet ProcFlags = new FlagSet("ProcFlags", "emu_cm_proc_death_strings",
            new uint[] { 0 }.Concat(Bits(25)).ToArray(),
            ProcFlagNames);

        private static readonly uint[] ProcExValues =
        {
            0,
            0x000001, 0x000002, 0x000004, 0x000008, 0x000010, 0x000020, 0x000040, 0x000080,
            0x000100, 0x000200, 0x000400, 0x000800, 0x001000, 0x002000, 0x004000, 0x008000,
            0x010000, 0x020000, 0x040000, 0x080000, 0x100000, 0x2000000
        };

        private static string[] ProcExNames(string bit0x2000) => new[]
        {
            "None (hit or crit only)",
            "Normal (non critical) hit", "Critical", "Miss", "Full resist", "Dodge", "Parry",
            "Block", "Evade", "Immune", "Deflect", "Absorb", "Reflect", "Interrupt",
            bit0x2000, "Reserved 2", "Reserved 3",
            "Trigger even when no damage was done", "Trigger once and then stop",
            "Periodic heal rather than periodic damage", "On the end of the cast only",
            "Spell was redirected by a magnet", "Set by the core for a heal over time, not for the database"
        };

        public static readonly FlagSet ProcFlagsEx = new FlagSet("ProcFlagsEx", "emu_cm_proc_ex_strings",
            ProcExValues, ProcExNames("Reserved 1"));

        /// <summary>WotLK put a full block result on the bit the others leave reserved.</summary>
        public static readonly FlagSet ProcFlagsExWotlk = new FlagSet("ProcFlagsEx", "emu_cmw_proc_ex_strings",
            ProcExValues, ProcExNames("Full block"));

        /// <summary>Classic has no unstable or Shattrath elixirs.</summary>
        public static readonly FlagSet ElixirMaskClassic = new FlagSet("ElixirMask", "emu_cm_elixir_strings",
            new uint[] { 0x01, 0x02, 0x10 },
            new[] { "Battle elixir", "Guardian elixir", "Well fed" });

        public static readonly FlagSet ElixirMask = new FlagSet("ElixirMask", "emu_cm_elixir_tbc_strings",
            new uint[] { 0x01, 0x02, 0x04, 0x08, 0x10 },
            new[] { "Battle elixir", "Guardian elixir", "Unstable elixir", "Shattrath elixir", "Well fed" });

        /// <summary>See SpellGroupRule in SpellStacking.h.</summary>
        public static readonly EnumSet SpellGroupRules = new EnumSet("emu_cm_group_rule_strings",
            new long[] { 1, 2 },
            new[] { "Only one of the group at a time", "Only one of the group per caster" });
    }
}
