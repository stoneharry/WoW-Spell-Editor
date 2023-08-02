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
        private Image _Image;
        private TextBlock _Text;
        private bool _Dirty = false;
        private static SpellSelectionEntry _CopiedEntry = null;
        private StackPanel _ConfirmDeletePanel = null;

        public SpellSelectionEntry()
        {
            // Setup stack panel
            Orientation = Orientation.Horizontal;
            // Build icon
            var image = new Image()
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(1, 1, 1, 1)
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
                ContextMenu = new ListContextMenu(this, false);
            }
        }

        private string BuildText(DataRow row, int language) => $" {row["id"]} - {row[$"SpellName{language - 1}"]}\n  {row[$"SpellRank{language - 1}"]}";

        public override void CopyItemClick(object sender, RoutedEventArgs args)
        {
            _CopiedEntry = this;
            InvokeCopyAction();
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
            confirmDeleteButton.Click += (_sender, _args) => {
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
    }
}