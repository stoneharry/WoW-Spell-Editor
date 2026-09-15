using System.Data;

namespace SpellEditor.Sources.Controls.SpellSelectList
{
    // Plain data view of a spell row. The main list caches these so other lists can show the same
    // spells without querying the database or building a second set of UI elements.
    public class SpellRecord
    {
        public uint Id { get; }
        public string Name { get; }
        public string Icon { get; }

        public SpellRecord(DataRow row, int language)
        {
            uint.TryParse(row["id"].ToString(), out uint id);
            uint.TryParse(row["SpellIconID"].ToString(), out uint iconId);

            Id = id;
            Name = BuildText(row, language);
            Icon = iconId.ToString();
        }

        public static string BuildText(DataRow row, int language) =>
            $" {row["id"]} - {row[$"SpellName{language - 1}"]}\n  {row[$"SpellRank{language - 1}"]}";

        public override string ToString() => Name;
    }
}
