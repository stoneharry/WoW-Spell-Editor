using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.Tools.VisualTools;

namespace SpellEditor.Sources.Controls
{
    public class VisualKitListEntry : StackPanel
    {
        public readonly string KitName;
        public readonly Dictionary<string, object> KitRecord;

        public VisualKitListEntry(string key, Dictionary<string, object> kitRecord)
        {
            Orientation = Orientation.Horizontal;
            KitName = key;
            KitRecord = kitRecord;

            BuildSelf();
        }

        private void BuildSelf()
        {
            var label = new Label()
            {
                Content = $"{ KitRecord["ID"] } - { KitName }\n{ GetAllEffects() }",
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

        private string GetAllEffects()
        {
            List<string> effectsFound = new List<string>();
            var visualEffectDbc = (SpellVisualEffectName)DBCManager.GetInstance().FindDbcForBinding("SpellVisualEffectName");
            foreach (var kitKey in VisualController.EffectColumnKeys)
            {
                var effectIdStr = KitRecord[kitKey].ToString();
                var success = uint.TryParse(effectIdStr, out var effectId);
                if (!success || effectId == 0)
                {
                    continue;
                }
                var effectRecord = visualEffectDbc.LookupRecord(effectId);
                if (effectRecord == null)
                {
                    continue;
                }
                var effectPath = visualEffectDbc.LookupStringOffset(uint.Parse(effectRecord["FilePath"].ToString()));
                effectPath = effectPath.Length > 70 ? effectPath.Substring(0, 67) + "..." : effectPath;
                effectsFound.Add(" " + effectPath);
            }
            return string.Join("\n", effectsFound);
        }

        public List<VisualEffectListEntry> GetAllEffectEntries()
        {
            var visualEffectDbc = DBCManager.GetInstance().FindDbcForBinding("SpellVisualEffectName") as SpellVisualEffectName;
            var effectList = new List<VisualEffectListEntry>();
            foreach (var key in VisualController.EffectColumnKeys)
            {
                var effectIdStr = KitRecord[key].ToString();
                var success = uint.TryParse(effectIdStr, out var effectId);
                if (!success || effectId == 0)
                {
                    continue;
                }
                var effectRecord = visualEffectDbc.LookupRecord(effectId);
                if (effectRecord == null)
                {
                    continue;
                }
                var name = visualEffectDbc.LookupStringOffset(uint.Parse(effectRecord["Name"].ToString()));
                var effectPath = visualEffectDbc.LookupStringOffset(uint.Parse(effectRecord["FilePath"].ToString()));
                var label = $"{ name }\n { effectPath }";
                effectList.Add(new VisualEffectListEntry(label, effectRecord));
            }
            return effectList;
        }
    }
}
