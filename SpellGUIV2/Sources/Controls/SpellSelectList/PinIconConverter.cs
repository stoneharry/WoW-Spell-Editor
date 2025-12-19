using System;
using System.Globalization;
using System.Windows.Data;

namespace SpellEditor.Sources.Controls.SpellSelectList
{
    public class PinIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isPinned = (bool)value;
            return isPinned ? "📌" : "📍";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
