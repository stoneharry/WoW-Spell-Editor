using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls.SpellSelectList
{
    public class SpellSelectContextMenu : ContextMenu
    {
        private readonly MenuItem _PasteItem;

        public SpellSelectContextMenu()
        {
            // Create items
            var copyItem = new MenuItem
            {
                Header = TryFindResource("VisualCopyContextMenu") ?? "Copy"
            };
            //copyItem.Click += CopyItemClick;
            _PasteItem = new MenuItem
            {
                Header = TryFindResource("VisualPasteContextMenu") ?? "Paste",
                IsEnabled = false
            };
            //_PasteItem.Click += PasteItemClick;
            var deleteItem = new MenuItem
            {
                Header = TryFindResource("VisualDeleteContextMenu") ?? "Delete"
            };
            //deleteItem.Click += DeleteItemClick;
            var cancelItem = new MenuItem
            {
                Header = TryFindResource("VisualCancelContextMenu") ?? "Cancel"
            };
            // Add items to menu
            Items.Add(copyItem);
            Items.Add(_PasteItem);
            Items.Add(deleteItem);
            Items.Add(cancelItem);
        }

        public SpellSelectContextMenu(RoutedEventHandler pasteAction)
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
            Items.Add(new Separator());
            Items.Add(cancelItem);
        }

        public void SetCanPaste(bool canPaste) => _PasteItem.IsEnabled = canPaste;
    }
}
