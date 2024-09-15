using SpellEditor.Sources.BLP;
using SpellEditor.Sources.Constants;
using SpellEditor.Sources.Controls.Common;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.Gem;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpellEditor.Sources.Controls.SpellSelectList
{
    public class GemSelectionEntry : AbstractListEntry
    {
        public uint GemId;
        public string GemName;
        public GemType GemTypeEntry;
        public SpellItemEnchantment SpellItemEnchantmentEntry;
        public Achievement AchievementEntry;
        public AchievementCriteria AchievementCriteriaEntry;

        private readonly Image _Image;
        private readonly TextBlock _Text;
        private bool _Dirty = false;
        private readonly StackPanel _ConfirmDeletePanel = null;
        private readonly StackPanel _DuplicatePanel = null;
        private readonly TextBox _DuplicateIdBox = null;

        public GemSelectionEntry()
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

        public void RefreshEntry(DataRow row)
        {
            uint.TryParse(row["id"].ToString(), out GemId);
            GemName = row["sRefName0"].ToString();
            _Text.Text = BuildText(row);
            uint.TryParse(row["gemType"].ToString(), out var gemType);
            var entry = GemTypeManager.Instance.LookupGemType(gemType);
            GemTypeEntry = entry;
            _Image.ToolTip = entry.IconId.ToString();

            SpellItemEnchantmentEntry = new SpellItemEnchantment
            (
                uint.Parse(row["SpellItemEnchantmentRef"].ToString()),
                new Item(uint.Parse(row["ItemCache"].ToString())),
                new Spell(uint.Parse(row["TriggerSpell"].ToString())),
                new Spell(uint.Parse(row["TempLearnSpell"].ToString()))
            );
            AchievementEntry = new Achievement
            (
                uint.Parse(row["Achievement"].ToString())
            );
            AchievementCriteriaEntry = new AchievementCriteria
            (
                uint.Parse(row["AchievementCriteria"].ToString()), 
                AchievementEntry,
                SpellItemEnchantmentEntry.ItemCache
            );

            _Dirty = true;
        }

        public uint GetGemId() => GemId;

        public uint GetDuplicateSpellId() => _DuplicateIdBox == null ? 0 : uint.Parse(_DuplicateIdBox.Text);

        public void UpdateDuplicateText(uint newId)
        {
            if (_DuplicateIdBox != null)
                _DuplicateIdBox.Text = newId.ToString();
        }

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
        }

        private string BuildText(DataRow row) => $" {row["id"]} - {row["sRefName0"]}\n  TriggerSpellId: {row["objectId1"]} - ItemId: {row["ItemCache"]}";

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}