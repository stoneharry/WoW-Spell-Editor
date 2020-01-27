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
            var label = new TextBlock()
            {
                Text = $"{ EffectRecord["ID"] } - { EffectName }",
                Margin = new Thickness(5),
                MinWidth = 275.00
            };
            Children.Add(label);

            ContextMenu = new ContextMenu();
            ContextMenu.Items.Add(new MenuItem()
            {
                Header = "Copy to new effect"
            });
            ContextMenu.Items.Add(new Separator());
            ContextMenu.Items.Add(new MenuItem()
            {
                Header = "Delete"
            });
        }
    }
}
