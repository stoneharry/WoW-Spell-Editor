using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.Tools.VisualTools;

namespace SpellEditor.Sources.Controls
{
    public class VisualKitListEntry : StackPanel
    {
        public readonly string KitName;
        public readonly DataRow KitRecord;
        private List<VisualEffectListEntry> _Attachments;
        private readonly IDatabaseAdapter _Adapter;

        public VisualKitListEntry(string key, DataRow kitRecord, IDatabaseAdapter adapter)
        {
            Orientation = Orientation.Horizontal;
            KitName = key;
            KitRecord = kitRecord;
            _Adapter = adapter;

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
            var attachResults = _Adapter.Query("SELECT * FROM spellvisualkitmodelattach WHERE ParentSpellVisualKitId = " + KitRecord["ID"]);
            if (attachResults == null || attachResults.Rows.Count == 0)
            {
                return;
            }
            foreach (DataRow attachRecord in attachResults.Rows)
            {
                var effectResults = _Adapter.Query("SELECT * FROM spellvisualeffectname WHERE ID = " + attachRecord["SpellVisualEffectNameId"]);
                if (effectResults == null || effectResults.Rows.Count == 0)
                {
                    continue;
                }
                var effectRecord = effectResults.Rows[0];
                var name = effectRecord["Name"].ToString();
                var effectPath = effectRecord["FilePath"].ToString();
                var attachmentName = SpellVisualKitModelAttach.LookupAttachmentIndex(int.Parse(attachRecord["AttachmentId"].ToString()));
                var key = $"{ attachmentName } Attachment - { name }\n { effectPath }";
                _Attachments.Add(new VisualEffectListEntry(key, effectRecord, attachRecord));
            }
        }

        private string GetAllEffects()
        {
            List<string> effectsFound = new List<string>();
            foreach (var kitKey in VisualController.EffectColumnKeys)
            {
                var effectIdStr = KitRecord[kitKey].ToString();
                var success = uint.TryParse(effectIdStr, out var effectId);
                if (!success || effectId == 0)
                {
                    continue;
                }
                var effectResults = _Adapter.Query("SELECT * FROM spellvisualeffectname WHERE ID = " + effectId);
                if (effectResults == null || effectResults.Rows.Count == 0)
                {
                    continue;
                }
                var effectRecord = effectResults.Rows[0];
                var effectPath = effectRecord["FilePath"].ToString();
                effectPath = effectPath.Length > 70 ? effectPath.Substring(0, 67) + "..." : effectPath;
                effectsFound.Add(" " + effectPath);
            }
            return string.Join("\n", effectsFound);
        }

        public List<VisualEffectListEntry> GetAllEffectEntries()
        {
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
                var effectResults = _Adapter.Query("SELECT * FROM spellvisualeffectname WHERE ID = " +effectId);
                if (effectResults == null || effectResults.Rows.Count == 0)
                {
                    continue;
                }
                var effectRecord = effectResults.Rows[0];
                var name = effectRecord["Name"].ToString();
                var effectPath = effectRecord["FilePath"].ToString();
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
