
using System.Xml.Linq;

namespace SpellEditor.Sources.Gem
{
    public class Spell
    {
        public readonly uint Id;

        public Spell(uint id)
        {
            Id = id;
        }
        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
