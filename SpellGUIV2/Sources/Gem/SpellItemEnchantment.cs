
namespace SpellEditor.Sources.Gem
{
    public class SpellItemEnchantment
    {
        public readonly uint Id;
        public readonly string Name;
        public readonly Item ItemCache;
        public readonly Spell TriggerSpell;
        public readonly Spell TempLearnSpell;

        public SpellItemEnchantment(uint id, string name, Item itemCache, Spell triggerSpell, Spell tempLearnSpell)
        {
            Id = id;
            Name = name;
            ItemCache = itemCache;
            TriggerSpell = triggerSpell;
            TempLearnSpell = tempLearnSpell;
        }
        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
