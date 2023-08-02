using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls.Common
{
    class ListContextMenu : ContextMenu
    {
        private readonly MenuItem _PasteItem;

        public ListContextMenu(IListEntry entry, bool addSeparators)
        {
            var copyItem = new MenuItem
            {
                Header = TryFindResource("VisualCopyContextMenu") ?? "Copy"
            };
            copyItem.Click += entry.CopyItemClick;
            _PasteItem = new MenuItem
            {
                Header = TryFindResource("VisualPasteContextMenu") ?? "Paste",
                IsEnabled = false
            };
            _PasteItem.Click += entry.PasteItemClick;
            var deleteItem = new MenuItem
            {
                Header = TryFindResource("VisualDeleteContextMenu") ?? "Delete"
            };
            deleteItem.Click += entry.DeleteItemClick;
            var cancelItem = new MenuItem
            {
                Header = TryFindResource("VisualCancelContextMenu") ?? "Cancel"
            };
            Items.Add(copyItem);
            if (addSeparators) Items.Add(new Separator());
            Items.Add(_PasteItem);
            if (addSeparators) Items.Add(new Separator());
            Items.Add(deleteItem);
            if (addSeparators) Items.Add(new Separator());
            Items.Add(cancelItem);
        }

        public ListContextMenu(RoutedEventHandler pasteAction, bool addSeparators)
        {
            _PasteItem = new MenuItem
            {
                Header = TryFindResource("VisualPasteContextMenu") ?? "Paste",
                IsEnabled = false
            };
            _PasteItem.Click += pasteAction;
            var cancelItem = new MenuItem
            {
                Header = TryFindResource("VisualCancelContextMenu") ?? "Cancel"
            };
            Items.Add(_PasteItem);
            if (addSeparators) Items.Add(new Separator());
            Items.Add(cancelItem);
        }

        public void SetCanPaste(bool canPaste) => _PasteItem.IsEnabled = canPaste;
    }
}