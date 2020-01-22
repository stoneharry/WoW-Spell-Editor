using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.Tools.VisualTools;

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
            var deleteBtn = new Button()
            {
                Content = "Delete",
                Margin = new Thickness(5),
                MaxWidth = 175.00,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            // FIXME(Harry): Handle delete button click
            Children.Add(label);
            Children.Add(deleteBtn);
        }
    }
}
