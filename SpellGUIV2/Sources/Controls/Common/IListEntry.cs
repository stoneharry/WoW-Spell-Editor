using System;
using System.Windows;

namespace SpellEditor.Sources.Controls.Common
{
    public interface IListEntry
    {
        void DeleteItemClick(object sender, RoutedEventArgs args);
        void PasteItemClick(object sender, RoutedEventArgs args);
        void CopyItemClick(object sender, RoutedEventArgs args);

        void CancelItemClick(object sender, RoutedEventArgs args);

        void SetDeleteClickAction(Action<IListEntry> deleteEntryAction);
        void SetCopyClickAction(Action<IListEntry> copyClickAction);
        void SetPasteClickAction(Action<IListEntry> pasteClickAction);

        void SetCancelClickAction(Action<IListEntry> cancelClickAction);
    }
}
