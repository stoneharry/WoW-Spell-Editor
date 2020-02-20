using SpellEditor.Sources.Controls.Visual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls
{
    public class VisualPasteListEntry : AbstractVisualListEntry, IVisualListEntry
    {
        public readonly IVisualListEntry CopyEntry;
        private readonly ComboBox _keyComboBox;

        public VisualPasteListEntry(IVisualListEntry copyEntry, List<string> availableKeys)
        {
            Orientation = Orientation.Horizontal;
            CopyEntry = copyEntry;

            _keyComboBox = new ComboBox
            {
                Margin = new Thickness(5),
                MinWidth = 100.00
            };
            BuildSelf(availableKeys);
        }

        private void BuildSelf(List<string> availableKeys)
        {
            _keyComboBox.ItemsSource = availableKeys;
            if (availableKeys.Count > 0)
            {
                _keyComboBox.SelectedIndex = 0;
            }
            var confirmBtn = new Button
            {
                Content = TryFindResource("VisualConfirmListEntry") ?? "Confirm Paste",
                Margin = new Thickness(5),
                MinWidth = 100.00
            };
            confirmBtn.Click += (sender, e) => InvokePasteAction();
            var cancelBtn = new Button
            {
                Content = TryFindResource("VisualCancelListEntry") ?? "Cancel",
                Margin = new Thickness(5),
                MinWidth = 100.00
            };
            cancelBtn.Click += (sender, e) => InvokeDeleteAction();

            if (CopyEntry is VisualEffectListEntry effectEntry && effectEntry.IsAttachment)
            {
                Children.Add(new Label() { Content = "Attachment " + effectEntry.AttachRecord[0].ToString() });
            }
            else
            {
                Children.Add(_keyComboBox);
            }
            Children.Add(confirmBtn);
            Children.Add(cancelBtn);
        }

        public string SelectedKey() => _keyComboBox.SelectedItem?.ToString();
    }
}
