
namespace SpellEditor.Sources.Constants
{
    public class GemType
    {
        public readonly uint Type;
        public readonly uint IconId;
        public readonly string Name;
        public readonly uint SkillDiscoverySpellId;
        public readonly uint ItemDisplayId;
        public readonly uint SkillId;
        public readonly uint AchievementCategory;

        public GemType(uint type, uint iconId, string name, uint skillDiscoverySpellId, uint itemDisplayId, uint skillId, uint achievementCategory)
        {
            Type = type;
            IconId = iconId;
            Name = name;
            SkillDiscoverySpellId = skillDiscoverySpellId;
            ItemDisplayId = itemDisplayId;
            SkillId = skillId;
            AchievementCategory = achievementCategory;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
