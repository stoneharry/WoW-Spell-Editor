using System.Windows.Controls;

namespace SpellEditor.Sources.Controls
{
    class DBCBoxContainer
    {
        // Store as a long to support uint32 max value and -1 value (equipped item subclass)
        public long ID;
        public string Name;
        public int ComboBoxIndex;
        public Label NameLabel = null;

        public DBCBoxContainer(long ID, string Name, int ComboBoxIndex)
        {
            this.ID = ID;
            this.Name = Name;
            this.ComboBoxIndex = ComboBoxIndex;
        }

        public DBCBoxContainer(long ID, Label NameLabel, int ComboBoxIndex)
        {
            this.ID = ID;
            this.NameLabel = NameLabel;
            this.ComboBoxIndex = ComboBoxIndex;
        }

        public Label ItemLabel()
        {
            return NameLabel ?? new FriendlyLabel() { Content = Name };
        }
    }
}
