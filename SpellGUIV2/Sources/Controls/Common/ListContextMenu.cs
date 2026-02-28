using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls.Common
{
    class ListContextMenu : ContextMenu
    {
        private MenuItem _PasteItem;

        public ListContextMenu(IListEntry entry, bool addSeparators, MenuType menuType)
        {
            var list = BuildMenuTypeItems(entry, menuType);
            var deleteItem = new MenuItem
            {
                Header = TryFindResource("VisualDeleteContextMenu") ?? "Delete"
            };
            deleteItem.Click += entry.DeleteItemClick;
            var cancelItem = new MenuItem
            {
                Header = TryFindResource("VisualCancelContextMenu") ?? "Cancel"
            };
            cancelItem.Click += entry.CancelItemClick;
            foreach (var item in list)
            {
                Items.Add(item);
                if (addSeparators) Items.Add(new Separator());
            }
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

        private List<MenuItem> BuildMenuTypeItems(IListEntry entry, MenuType menuType)
        {
            switch (menuType)
            {
                case MenuType.CopyPaste:
                    return BuildCopyPasteMenu(entry);
                case MenuType.Duplicate:
                    return BuildDuplicateMenu(entry);
                default:
                    throw new Exception("Unhandled type: " + menuType);
            }
        }

        private List<MenuItem> BuildCopyPasteMenu(IListEntry entry)
        {
            var items = new List<MenuItem>();
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
            items.Add(copyItem);
            items.Add(_PasteItem);
            return items;
        }

        private List<MenuItem> BuildDuplicateMenu(IListEntry entry)
        {
            var items = new List<MenuItem>();
            var duplicateItem = new MenuItem
            {
                Header = TryFindResource("ListDuplicateContextMenu") ?? "Duplicate"
            };
            duplicateItem.Click += entry.CopyItemClick;
            items.Add(duplicateItem);
            return items;
        }

        public void SetCanPaste(bool canPaste) => _PasteItem.IsEnabled = canPaste;

        public enum MenuType
        {
            CopyPaste,
            Duplicate
        }
    }
}