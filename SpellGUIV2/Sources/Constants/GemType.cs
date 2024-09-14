
namespace SpellEditor.Sources.Constants
{
    public class GemType
    {
        public readonly uint Type;
        public readonly uint IconId;
        public readonly string Name;

        public GemType(uint type, uint iconId, string name)
        {
            Type = type;
            IconId = iconId;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
