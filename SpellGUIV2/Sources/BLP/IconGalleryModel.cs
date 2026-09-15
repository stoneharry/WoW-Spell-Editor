using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;

namespace SpellEditor.Sources.BLP
{
    public class IconGalleryModel : INotifyPropertyChanged
    {
        private const double Spacing = 8.0;

        private readonly List<IconEntry> _Entries = new List<IconEntry>();
        private readonly ListCollectionView _Icons;
        private double _ItemSize = 64.0;
        private string _Filter = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        public IconGalleryModel()
        {
            _Icons = new ListCollectionView(_Entries);
        }

        public ICollectionView Icons => _Icons;

        public double ItemSize
        {
            get => _ItemSize;
            set
            {
                if (_ItemSize == value)
                    return;
                _ItemSize = value;
                RaisePropertyChanged(nameof(ItemSize));
                RaisePropertyChanged(nameof(ItemWidth));
                RaisePropertyChanged(nameof(ItemHeight));
            }
        }

        public double ItemWidth => _ItemSize + Spacing;

        public double ItemHeight => _ItemSize + Spacing;

        public int Count => _Entries.Count;

        public void SetEntries(List<IconEntry> entries)
        {
            _Entries.Clear();
            _Entries.AddRange(entries);
            _Icons.Refresh();
        }

        public void SetFilter(string filter)
        {
            filter = filter == null ? string.Empty : filter.ToLowerInvariant();
            if (_Filter == filter)
                return;
            _Filter = filter;
            if (_Filter.Length == 0)
                _Icons.Filter = null;
            else if (_Icons.Filter == null)
                _Icons.Filter = MatchesFilter;
            else
                _Icons.Refresh();
        }

        private bool MatchesFilter(object item)
        {
            return item is IconEntry entry && entry.FilterKey.Contains(_Filter);
        }

        private void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
