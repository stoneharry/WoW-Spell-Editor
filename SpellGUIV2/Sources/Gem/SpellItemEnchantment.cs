
namespace SpellEditor.Sources.Gem
{
    public class SpellItemEnchantment
    {
        public readonly uint Id;
        public readonly Item ItemCache;
        public readonly Spell TriggerSpell;
        public readonly Spell TempLearnSpell;

        public SpellItemEnchantment(uint id, Item itemCache, Spell triggerSpell, Spell tempLearnSpell)
        {
            Id = id;
            ItemCache = itemCache;
            TriggerSpell = triggerSpell;
            TempLearnSpell = tempLearnSpell;
        }
    }
}
