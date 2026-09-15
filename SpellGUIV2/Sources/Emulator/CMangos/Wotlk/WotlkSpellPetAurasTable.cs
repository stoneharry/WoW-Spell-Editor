using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.CMangos.Wotlk
{
    /// <summary>
    /// WotLK keys the pet auras on the effect index as well, so one spell can hand the pet a
    /// different aura per effect.
    /// </summary>
    public class WotlkSpellPetAurasTable : SpellPetAurasTable
    {
        public WotlkSpellPetAurasTable(CMangosEmulator emulator) : base(emulator)
        {
        }

        protected override string KeyPrefix => "emu_cmw";

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "spell", IsKey = true, IsPrimarySpellKey = true };
            yield return new WorldColumn { Name = "effectId", IsKey = true, Enum = WorldEnums.EffectIndexValues };
            yield return new WorldColumn { Name = "pet", IsKey = true };
            yield return new WorldColumn { Name = "aura", ShowSpellPicker = true };
        }

        public override string[] FallbackLabels => new[] { "Spell", "Effect", "Pet", "Pet aura" };

        public override string[] FallbackTooltips => new[]
        {
            "Spell on the owner.",
            "Effect index the pet aura hangs off.",
            "Creature id of the pet the aura is for. 0 covers every pet.",
            "Spell applied to the pet."
        };
    }
}
