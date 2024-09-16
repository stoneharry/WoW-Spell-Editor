
using System.Xml.Linq;

namespace SpellEditor.Sources.Gem
{
    public class Item
    {
        public readonly uint Id;

        public Item(uint id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
