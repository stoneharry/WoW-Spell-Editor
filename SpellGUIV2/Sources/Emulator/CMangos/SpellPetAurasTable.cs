using System.Collections.Generic;

namespace SpellEditor.Sources.Emulator.CMangos
{
    /// <summary>
    /// Auras the pet picks up while the owner has this one, see SpellMgr::LoadSpellPetAuras.
    /// WotLK keys these on the effect index as well, see WotlkSpellPetAurasTable.
    /// </summary>
    public class SpellPetAurasTable : CMangosTable
    {
        public SpellPetAurasTable(CMangosEmulator emulator) : base(emulator)
        {
        }

        public override string Name => "spell_pet_auras";

        public override string OrderBy => "`spell`";

        // Keyed on the exact spell id, the loader has no rank handling
        public override string SpellFilter(RankChain chain) => $"`spell` = {chain.SpellId}";

        protected override IEnumerable<WorldColumn> CreateColumns()
        {
            yield return new WorldColumn { Name = "spell", IsKey = true, IsPrimarySpellKey = true };
            yield return new WorldColumn { Name = "pet", IsKey = true };
            yield return new WorldColumn { Name = "aura", ShowSpellPicker = true };
        }

        public override string FallbackHeader => "Pet auras";

        public override string FallbackDescription =>
            "Auras the pet gets while the owner has this spell.";

        public override string FallbackEmpty => "This spell gives the pet nothing.";

        public override string[] FallbackLabels => new[] { "Spell", "Pet", "Pet aura" };

        public override string[] FallbackTooltips => new[]
        {
            "Spell on the owner.",
            "Creature id of the pet the aura is for. 0 covers every pet.",
            "Spell applied to the pet."
        };
    }
}
