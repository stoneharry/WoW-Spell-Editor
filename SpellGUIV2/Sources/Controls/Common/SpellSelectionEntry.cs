using SpellEditor.Sources.BLP;
using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls.Common
{
    public class SpellSelectionEntry : StackPanel
    {
        private Image _Image;
        private TextBlock _Text;
        private bool _Dirty = false;

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

            _Text.Text = BuildText(row, language);
            uint.TryParse(row["SpellIconID"].ToString(), out uint iconId);
            _Image.ToolTip = iconId.ToString();
            _Dirty = true;
        }

        private void IsSpellListEntryVisibileChanged(object o, DependencyPropertyChangedEventArgs args)
        {
            var image = o as Image;
            if (!(bool)args.NewValue)
            {
                image.Source = null;
                return;
            }
            if (image.Source != null && !_Dirty)
            {
                return;
            }
            var loadIcons = (SpellIconDBC)DBCManager.GetInstance().FindDbcForBinding("SpellIcon");
            if (loadIcons != null)
            {
                _Dirty = false;
                var iconId = uint.Parse(image.ToolTip.ToString());
                var filePath = loadIcons.GetIconPath(iconId) + ".blp";
                image.Source = BlpManager.GetInstance().GetImageSourceFromBlpPath(filePath);
            }
        }

        private string BuildText(DataRow row, int language) => $" {row["id"]} - {row[$"SpellName{language - 1}"]}\n  {row[$"SpellRank{language - 1}"]}";
    }
}
