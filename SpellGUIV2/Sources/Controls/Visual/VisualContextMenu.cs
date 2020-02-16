using SpellEditor.Sources.Tools.VisualTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls.Visual
{
    class VisualContextMenu : ContextMenu
    {
        private readonly MenuItem _PasteItem;

        public VisualContextMenu(IVisualListEntry entry)
        {
            var copyItem = new MenuItem
            {
                Header = "Copy"
            };
            copyItem.Click += entry.CopyItemClick;
            _PasteItem = new MenuItem
            {
                Header = "Paste",
                IsEnabled = false
            };
            _PasteItem.Click += entry.PasteItemClick;
            var deleteItem = new MenuItem
            {
                Header = "Delete"
            };
            deleteItem.Click += entry.DeleteItemClick;
            var cancelItem = new MenuItem
            {
                Header = "Cancel"
            };
            Items.Add(copyItem);
            Items.Add(new Separator());
            Items.Add(_PasteItem);
            Items.Add(new Separator());
            Items.Add(deleteItem);
            Items.Add(new Separator());
            Items.Add(cancelItem);
        }

        public VisualContextMenu(RoutedEventHandler pasteAction)
        {
            _PasteItem = new MenuItem
            {
                Header = "Paste",
                IsEnabled = false
            };
            _PasteItem.Click += pasteAction;
            var cancelItem = new MenuItem
            {
                Header = "Cancel"
            };
            Items.Add(_PasteItem);
            Items.Add(new Separator());
            Items.Add(cancelItem);
        }

        public void SetCanPaste(bool canPaste)
        {
            _PasteItem.IsEnabled = canPaste;
        }
    }
}
