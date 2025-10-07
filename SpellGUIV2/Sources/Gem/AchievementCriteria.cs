
using System.Xml.Linq;

namespace SpellEditor.Sources.Gem
{
    public class AchievementCriteria
    {
        public readonly uint Id;
        public readonly Achievement Parent;
        public readonly Item RequiredItem;

        public AchievementCriteria(uint id, Achievement parent, Item requiredItem)
        {
            Id = id;
            Parent = parent;
            RequiredItem = requiredItem;
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
