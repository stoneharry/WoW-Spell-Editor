using SpellEditor.Sources.Controls.Visual;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls
{
    public class VisualPasteListEntry : StackPanel, IVisualListEntry
    {
        public readonly IVisualListEntry CopyEntry;
        private readonly ComboBox _keyComboBox;
        private Action<IVisualListEntry> _cancelAction;
        private Action<IVisualListEntry> _pasteAction;

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
                Content = "Confirm Paste",
                Margin = new Thickness(5),
                MinWidth = 100.00
            };
            confirmBtn.Click += ConfirmBtn_Click;
            var cancelBtn = new Button
            {
                Content = "Cancel",
                Margin = new Thickness(5),
                MinWidth = 100.00
            };

            cancelBtn.Click += CancelBtn_Click;

            Children.Add(_keyComboBox);
            Children.Add(confirmBtn);
            Children.Add(cancelBtn);
        }

        public string SelectedKey() => _keyComboBox.SelectedItem.ToString();

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            Children.Clear();
            _pasteAction.Invoke(this);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Children.Clear();
            _cancelAction.Invoke(this);
        }

        public void DeleteItemClick(object sender, RoutedEventArgs args)
        {
        }

        public void PasteItemClick(object sender, RoutedEventArgs args)
        {
        }

        public void CopyItemClick(object sender, RoutedEventArgs args)
        {
        }

        // Used as cancel
        public void SetDeleteClickAction(Action<IVisualListEntry> deleteEntryAction)
        {
            _cancelAction = deleteEntryAction;
        }

        public void SetPasteClickAction(Action<IVisualListEntry> pasteClickAction)
        {
            _pasteAction = pasteClickAction;
        }

        public void SetCopyClickAction(Action<IVisualListEntry> copyClickAction)
        {
        }
    }
}
