
using System.Xml.Linq;

namespace SpellEditor.Sources.Gem
{
    public class SkillDiscovery
    {
        public readonly uint Id;
        public readonly uint ReqSpell;
        public readonly uint Item;

        public SkillDiscovery(uint id, uint reqSpell, uint item)
        {
            Id = id;
            ReqSpell = reqSpell;
            Item = item;
        }
        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
