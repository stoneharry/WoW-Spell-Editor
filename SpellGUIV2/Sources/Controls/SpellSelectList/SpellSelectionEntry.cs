using SpellEditor.Sources.BLP;
using SpellEditor.Sources.Controls.Common;
using SpellEditor.Sources.DBC;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls.SpellSelectList
{
    public class SpellSelectionEntry : AbstractListEntry
    {
        private uint _SpellId;
        private readonly Image _Image;
        private readonly TextBlock _Text;
        private bool _Dirty = false;
        private StackPanel _ConfirmDeletePanel = null;
        private StackPanel _DuplicatePanel = null;
        private TextBox _DuplicateIdBox = null;

        public SpellSelectionEntry()
        {
            // Setup stack panel
            Orientation = Orientation.Horizontal;
            // Build icon
            var image = new Image()
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(1)
            };
            image.IsVisibleChanged += IsSpellListEntryVisibileChanged;
            // Build main text block
            var textBlock = new TextBlock();
            // Add to children
            Children.Add(image);
            Children.Add(textBlock);
            // Save references to member variables
            _Image = image;
            _Text = textBlock;
        }

        public void RefreshEntry(DataRow row, int language)
        {
            uint.TryParse(row["id"].ToString(), out _SpellId);
            _Text.Text = BuildText(row, language);
            uint.TryParse(row["SpellIconID"].ToString(), out uint iconId);
            _Image.ToolTip = iconId.ToString();
            _Dirty = true;
        }

        public uint GetSpellId() => _SpellId;

        public uint GetDuplicateSpellId() => _DuplicateIdBox == null ? 0 : uint.Parse(_DuplicateIdBox.Text);

        private void IsSpellListEntryVisibileChanged(object o, DependencyPropertyChangedEventArgs args)
        {
            var image = o as Image;
            // If not visible then unload icon
            if (!(bool)args.NewValue)
            {
                image.Source = null;
                return;
            }
            // If we have a source and we are not dirty then do nothing
            if (image.Source != null && !_Dirty)
            {
                return;
            }
            // Try to load icon
            var loadIcons = (SpellIconDBC)DBCManager.GetInstance().FindDbcForBinding("SpellIcon");
            if (loadIcons != null)
            {
                _Dirty = false;
                var iconId = uint.Parse(image.ToolTip.ToString());
                var filePath = loadIcons.GetIconPath(iconId) + ".blp";
                image.Source = BlpManager.GetInstance().GetImageSourceFromBlpPath(filePath);
            }
            // Load context menu
            if (ContextMenu == null)
            {
                ContextMenu = new ListContextMenu(this, true, ListContextMenu.MenuType.Duplicate);
            }
        }

        private string BuildText(DataRow row, int language) => $" {row["id"]} - {row[$"SpellName{language - 1}"]}\n  {row[$"SpellRank{language - 1}"]}";

        public override void CopyItemClick(object sender, RoutedEventArgs args)
        {
            if (_DuplicatePanel != null)
            {
                _DuplicatePanel.Visibility = Visibility.Visible;
                return;
            }
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            _DuplicateIdBox = new TextBox
            {
                Text = (GetSpellId() + 1).ToString(),
                Margin = new Thickness(2)
            };
            var confirmButton = new Button
            {
                Content = TryFindResource("SpellSelectListEntryConfirm") ?? "Confirm\nDuplicate",
                Margin = new Thickness(2),
                MinWidth = 80
            };
            confirmButton.Click += (_sender, _args) =>
            {
                // Stop and delete everything in this instance
                _DuplicatePanel.Visibility = Visibility.Collapsed;
                InvokeCopyAction();
            };
            var cancelButton = new Button
            {
                Content = TryFindResource("VisualCancelListEntry") ?? "Cancel",
                Margin = new Thickness(2),
                MinWidth = 80
            };
            cancelButton.Click += (_sender, _args) =>
            {
                _DuplicatePanel.Visibility = Visibility.Collapsed;
            };
            panel.Children.Add(_DuplicateIdBox);
            panel.Children.Add(confirmButton);
            panel.Children.Add(cancelButton);
            Children.Add(panel);
            _DuplicatePanel = panel;
        }

        public override void DeleteItemClick(object sender, RoutedEventArgs args)
        {
            if (_ConfirmDeletePanel != null)
            {
                _ConfirmDeletePanel.Visibility = Visibility.Visible;
                return;
            }
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            var confirmDeleteButton = new Button
            {
                Content = TryFindResource("SpellSelectListEntryConfirm") ?? "Confirm\nDelete",
                Margin = new Thickness(3),
                MinWidth = 80
            };
            confirmDeleteButton.Click += (_sender, _args) => 
            {
                // Stop and delete everything in this instance
                _ConfirmDeletePanel.Visibility = Visibility.Collapsed;
                InvokeDeleteAction();
            };
            var cancelButton = new Button
            {
                Content = TryFindResource("VisualCancelListEntry") ?? "Cancel",
                Margin = new Thickness(3),
                MinWidth = 80
            };
            cancelButton.Click += (_sender, _args) =>
            {
                _ConfirmDeletePanel.Visibility = Visibility.Collapsed;
            };
            panel.Children.Add(confirmDeleteButton);
            panel.Children.Add(cancelButton);
            Children.Add(panel);
            _ConfirmDeletePanel = panel;
        }

        public override void CancelItemClick(object sender, RoutedEventArgs args)
        {
            if (_ConfirmDeletePanel != null)
                _ConfirmDeletePanel.Visibility = Visibility.Collapsed;
            if (_DuplicatePanel != null)
                _DuplicatePanel.Visibility = Visibility.Collapsed;
        }
    }
}