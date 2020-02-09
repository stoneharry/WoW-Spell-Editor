using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SpellEditor.Sources.Controls.Visual
{
    public interface IVisualListEntry
    {
        void DeleteItemClick(object sender, RoutedEventArgs args);
        void PasteItemClick(object sender, RoutedEventArgs args);
        void CopyItemClick(object sender, RoutedEventArgs args);

        void SetDeleteClickAction(Action<IVisualListEntry> deleteEntryAction);
        void SetCopyClickAction(Action<IVisualListEntry> copyClickAction);
        void SetPasteClickAction(Action<IVisualListEntry> pasteClickAction);
    }
}
