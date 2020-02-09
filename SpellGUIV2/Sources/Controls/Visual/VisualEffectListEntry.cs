using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls
{
    public class VisualEffectListEntry : StackPanel
    {
        public readonly string EffectName;
        public readonly DataRow EffectRecord;
        public readonly DataRow AttachRecord;

        public VisualEffectListEntry(string key, DataRow effectRecord)
        {
            Orientation = Orientation.Horizontal;
            EffectName = key;
            EffectRecord = effectRecord;

            BuildSelf();
        }

        public VisualEffectListEntry(string key, DataRow effectRecord, DataRow attachRecord) : this(key, effectRecord)
        {
            AttachRecord = attachRecord;
        }

        private void BuildSelf()
        {
            var label = new TextBlock
            {
                Text = $"{ EffectRecord["ID"] } - { EffectName }",
                Margin = new Thickness(5),
                MinWidth = 275.00
            };
            Children.Add(label);

            ContextMenu = new ContextMenu();
            var copyItem = new MenuItem
            {
                Header = "Copy"
            };
            //copyItem.Click += CopyItem_Click;
            var pasteItem = new MenuItem
            {
                Header = "Paste",
                IsEnabled = false
            };
            //pasteItem.Click += PasteItem_Click;
            var deleteItem = new MenuItem
            {
                Header = "Delete"
            };
            //deleteItem.Click += DeleteItem_Click;

            ContextMenu.Items.Add(copyItem);
            ContextMenu.Items.Add(new Separator());
            ContextMenu.Items.Add(pasteItem);
            ContextMenu.Items.Add(new Separator());
            ContextMenu.Items.Add(deleteItem);
        }
    }
}
