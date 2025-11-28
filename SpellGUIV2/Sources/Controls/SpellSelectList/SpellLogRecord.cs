using System.ComponentModel;

namespace SpellEditor.Sources.Controls.SpellSelectList
{
    public class SpellLogRecord : INotifyPropertyChanged
    {
        private bool _isPinned;
        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned != value)
                {
                    _isPinned = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPinned)));
                }
            }
        }

        public string SpellLogName { get; set; }
        public string SpellLogIcon { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
