using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls
{
    public class VisualEffectListEntry : StackPanel
    {
        public readonly string EffectName;
        public readonly Dictionary<string, object> EffectRecord;

        public VisualEffectListEntry(string key, Dictionary<string, object> effectRecord)
        {
            Orientation = Orientation.Horizontal;
            EffectName = key;
            EffectRecord = effectRecord;

            BuildSelf();
        }

        private void BuildSelf()
        {
            var label = new Label()
            {
                Content = $"{ EffectRecord["ID"] } - { EffectName }",
                Margin = new Thickness(5),
                MinWidth = 275.00
            };
            Children.Add(label);

            ContextMenu = new ContextMenu();
            ContextMenu.Items.Add(new MenuItem()
            {
                Header = "Copy to new kit"
            });
            ContextMenu.Items.Add(new Separator());
            ContextMenu.Items.Add(new MenuItem()
            {
                Header = "Delete"
            });
        }
    }
}
