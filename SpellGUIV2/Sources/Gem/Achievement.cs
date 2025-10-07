
using System.Xml.Linq;

namespace SpellEditor.Sources.Gem
{
    public class Achievement
    {
        public readonly uint Id;

        public Achievement(uint id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
