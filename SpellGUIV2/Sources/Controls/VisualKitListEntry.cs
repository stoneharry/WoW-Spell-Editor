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
        private List<VisualEffectListEntry> _Attachments;

        public VisualKitListEntry(string key, Dictionary<string, object> kitRecord)
        {
            Orientation = Orientation.Horizontal;
            KitName = key;
            KitRecord = kitRecord;

            BuildSelf();
        }

        private void BuildSelf()
        {
            var label = new TextBlock()
            {
                Text = $"{ KitRecord["ID"] } - { KitName }\n{ GetAllEffects() }",
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

            // Find attachments
            _Attachments = new List<VisualEffectListEntry>();
            var attachDbc = DBCManager.GetInstance().FindDbcForBinding("SpellVisualKitModelAttach") as SpellVisualKitModelAttach;
            var attachments = attachDbc.LookupRecords(uint.Parse(KitRecord["ID"].ToString()));
            if (attachments.Count > 0)
            {
                var effectDbc = DBCManager.GetInstance().FindDbcForBinding("SpellVisualEffectName") as SpellVisualEffectName;
                foreach (var attachRecord in attachments)
                {
                    var effectRecord = effectDbc.LookupRecord(uint.Parse(attachRecord["SpellVisualEffectNameId"].ToString()));
                    if (effectRecord == null)
                    {
                        continue;
                    }
                    var name = effectDbc.LookupStringOffset(uint.Parse(effectRecord["Name"].ToString()));
                    var effectPath = effectDbc.LookupStringOffset(uint.Parse(effectRecord["FilePath"].ToString()));
                    var attachmentName = attachDbc.LookupAttachmentIndex(int.Parse(attachRecord["AttachmentId"].ToString()));
                    var key = $"{ attachmentName } Attachment - { name }\n { effectPath }";
                    _Attachments.Add(new VisualEffectListEntry(key, effectRecord, attachRecord));
                }
            }
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
            // Handle effects
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
                var label = $"{ key } - { name }\n { effectPath }";
                effectList.Add(new VisualEffectListEntry(label, effectRecord));
            }
            // Handle attachments
            foreach (var attachment in _Attachments)
            {
                effectList.Add(attachment);
            }
            return effectList;
        }
    }
}
