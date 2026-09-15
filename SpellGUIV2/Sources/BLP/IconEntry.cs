using System.ComponentModel;
using System.Windows.Media;

namespace SpellEditor.Sources.BLP
{
    public class IconEntry : INotifyPropertyChanged
    {
        private static readonly PropertyChangedEventArgs SourceChangedArgs = new PropertyChangedEventArgs(nameof(Source));

        private ImageSource _Source;
        private bool _Requested;

        public uint Id { get; }
        public uint Offset { get; }
        public string Name { get; }
        public string FilePath { get; }
        public string ToolTip { get; }
        public string FilterKey { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public IconEntry(uint id, uint offset, string name)
        {
            Id = id;
            Offset = offset;
            Name = name;
            FilePath = name + ".blp";
            ToolTip = id + " - " + FilePath;
            FilterKey = ToolTip.ToLowerInvariant();
        }

        public ImageSource Source
        {
            get
            {
                if (!_Requested)
                {
                    _Requested = true;
                    BlpManager.GetInstance().RequestImageSource(FilePath, OnImageLoaded);
                }
                return _Source;
            }
        }

        private void OnImageLoaded(ImageSource source)
        {
            _Source = source;
            PropertyChanged?.Invoke(this, SourceChangedArgs);
        }
    }
}
