using System.Windows.Controls;

namespace SpellEditor.Sources.Controls
{
    public class DBCBoxContainer
    {
        // Store as a long to support uint32 max value and -1 value (equipped item subclass)
        public long ID;
        public string Name;
        public int ComboBoxIndex;
        public Label NameLabel;

        public DBCBoxContainer(long id, string name, int comboBoxIndex)
        {
            ID = id;
            Name = name;
            ComboBoxIndex = comboBoxIndex;
        }

        public DBCBoxContainer(long id, Label nameLabel, int comboBoxIndex)
        {
            ID = id;
            NameLabel = nameLabel;
            ComboBoxIndex = comboBoxIndex;
        }

        public Label ItemLabel()
        {
            return NameLabel ?? new FriendlyLabel() { Content = Name };
        }
    }
}
