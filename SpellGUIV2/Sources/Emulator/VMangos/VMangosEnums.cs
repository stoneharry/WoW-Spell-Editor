using System.Linq;

namespace SpellEditor.Sources.Emulator.VMangos
{
    /// <summary>
    /// 1.12 spell enums. The proc flags in particular are not the 3.3.5 ones with a couple of
    /// entries missing, the bits mean different things, so VMangos carries its own lists.
    /// </summary>
    public static class VMangosEnums
    {
        /// <summary>See ProcFlags in SpellDefines.h. Bit 0 is the heartbeat, not a kill.</summary>
        public static readonly FlagSet ProcFlags = new FlagSet("ProcFlags", "emu_vm_proc_strings",
            new uint[] { 0 }.Concat(Enumerable.Range(0, 24).Select(index => 1u << index)).ToArray(),
            new[]
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
                "Main hand attack done", "Off hand attack done"
            });

        /// <summary>See ProcFlagsEx in SpellDefines.h.</summary>
        public static readonly FlagSet ProcFlagsEx = new FlagSet("ProcFlagsEx", "emu_vm_proc_ex_strings",
            new uint[]
            {
                0,
                0x00001, 0x00002, 0x00004, 0x00008, 0x00010, 0x00020, 0x00040, 0x00080,
                0x00100, 0x00200, 0x00400, 0x00800, 0x01000, 0x02000, 0x04000, 0x08000,
                0x10000, 0x20000, 0x40000, 0x80000
            },
            new[]
            {
                "None (hit or crit only)",
                "Normal (non critical) hit", "Critical", "Miss", "Full resist", "Dodge", "Parry",
                "Block", "Evade", "Immune", "Deflect", "Absorb", "Reflect", "Interrupt",
                "Reserved 1", "Reserved 2", "Reserved 3",
                "Always trigger, whatever the other flags say", "Never proc from a periodic tick",
                "Periodic heal rather than periodic damage", "On the end of the cast only"
            });

        /// <summary>Battle and guardian together make a flask, see GetSpellElixirSpecific.</summary>
        public static readonly FlagSet ElixirMask = new FlagSet("ElixirMask", "emu_vm_elixir_strings",
            new uint[] { 0x01, 0x02, 0x10 },
            new[] { "Battle elixir", "Guardian elixir", "Well fed" });
    }
}
