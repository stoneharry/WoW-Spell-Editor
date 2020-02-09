using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SpellEditor.Sources.Controls.Visual;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.Tools.VisualTools;

namespace SpellEditor.Sources.Controls
{
    public class VisualKitListEntry : StackPanel, IVisualListEntry
    {
        public readonly string KitName;
        public readonly DataRow KitRecord;
        public readonly uint ParentVisualId;
        private readonly IDatabaseAdapter _Adapter;
        private List<VisualEffectListEntry> _Attachments;
        private List<VisualEffectListEntry> _Effects;
        private Action<IVisualListEntry> _DeleteClickAction;
        private Action<IVisualListEntry> _CopyClickAction;
        private Action<IVisualListEntry> _PasteClickAction;
        private StackPanel _ConfirmDeletePanel;

        public VisualKitListEntry(string key, uint visualId, DataRow kitRecord, IDatabaseAdapter adapter)
        {
            Orientation = Orientation.Horizontal;
            ParentVisualId = visualId;
            KitName = key;
            KitRecord = kitRecord;
            _Adapter = adapter;

            _Attachments = findAttachments(adapter);
            _Effects = findAllEffects(adapter);
            buildSelf();
        }

        private void buildSelf()
        {
            var id = uint.Parse(KitRecord["ID"].ToString());
            var label = new TextBlock
            {
                Text = $"{ id } - { KitName }\n{ GetAllEffects() }",
                Margin = new Thickness(5),
                MinWidth = 275.00
            };
            Children.Add(label);

            ContextMenu = new VisualContextMenu(this);
        }

        private List<VisualEffectListEntry> findAllEffects(IDatabaseAdapter adapter)
        {
            var effectList = new List<VisualEffectListEntry>();
            foreach (var key in VisualController.EffectColumnKeys)
            {
                var effectIdStr = KitRecord[key].ToString();
                var success = uint.TryParse(effectIdStr, out var effectId);
                if (!success || effectId == 0)
                {
                    continue;
                }
                var effectResults = adapter.Query("SELECT * FROM spellvisualeffectname WHERE ID = " + effectId);
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
            return effectList;
        }

        private List<VisualEffectListEntry> findAttachments(IDatabaseAdapter adapter)
        {
            var attachments = new List<VisualEffectListEntry>();
            var attachResults = adapter.Query("SELECT * FROM spellvisualkitmodelattach WHERE ParentSpellVisualKitId = " + KitRecord["ID"]);
            if (attachResults == null || attachResults.Rows.Count == 0)
            {
                return attachments;
            }
            foreach (DataRow attachRecord in attachResults.Rows)
            {
                var effectResults = adapter.Query("SELECT * FROM spellvisualeffectname WHERE ID = " + attachRecord["SpellVisualEffectNameId"]);
                if (effectResults == null || effectResults.Rows.Count == 0)
                {
                    continue;
                }
                var effectRecord = effectResults.Rows[0];
                var name = effectRecord["Name"].ToString();
                var effectPath = effectRecord["FilePath"].ToString();
                var attachmentName = SpellVisualKitModelAttach.LookupAttachmentIndex(int.Parse(attachRecord["AttachmentId"].ToString()));
                var key = $"{ attachRecord["ID"] } - { attachmentName } Attachment - { name }\n { effectPath }";
                attachments.Add(new VisualEffectListEntry(key, effectRecord, attachRecord));
            }
            return attachments;
        }

        public void PasteItemClick(object sender, RoutedEventArgs args)
        {
            _PasteClickAction.Invoke(this);
        }

        public void CopyItemClick(object sender, RoutedEventArgs args)
        {
            VisualController.SetCopiedKitEntry(this);
            _CopyClickAction.Invoke(this);
        }

        public void DeleteItemClick(object sender, RoutedEventArgs args)
        {
            if (_ConfirmDeletePanel != null)
            {
                return;
            }
            var panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            var confirmDeleteButton = new Button
            {
                Content = "CONFIRM DELETE:\nThis change is\nsaved immediately",
                Margin = new Thickness(5),
                MinWidth = 100
            };
            confirmDeleteButton.Click += (_sender, _args) => {
                if (_DeleteClickAction == null)
                {
                    return;
                }
                // Stop and delete everything in this instance
                _ConfirmDeletePanel = null;
                DeleteKitFromVisual();
                Children.Clear();
                _DeleteClickAction.Invoke(this);
                _DeleteClickAction = null;
            };
            var cancelButton = new Button
            {
                Content = "Cancel",
                Margin = new Thickness(5),
                MinWidth = 100
            };
            cancelButton.Click += (_sender, _args) =>
            {
                _ConfirmDeletePanel = null;
                panel.Children.Clear();
                Children.Remove(panel);
            };
            panel.Children.Add(confirmDeleteButton);
            panel.Children.Add(cancelButton);
            Children.Add(panel);
            _ConfirmDeletePanel = panel;
        }

        private void DeleteKitFromVisual()
        {
            _Adapter.Execute($"UPDATE spellvisual SET { KitName } = 0 WHERE ID = { ParentVisualId }");
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

        public List<VisualEffectListEntry> GetAllEffectsAndAttachmentsEntries()
        {
            var listEntries = new List<VisualEffectListEntry>();

            _Effects.ForEach(listEntries.Add);
            _Attachments.ForEach(listEntries.Add);

            return listEntries;
        }

        public void SetDeleteClickAction(Action<IVisualListEntry> deleteEntryAction)
        {
            _DeleteClickAction = deleteEntryAction;
        }

        public void SetCopyClickAction(Action<IVisualListEntry> copyClickAction)
        {
            _CopyClickAction = copyClickAction;
        }

        public void SetPasteClickAction(Action<IVisualListEntry> pasteClickAction)
        {
            _PasteClickAction = pasteClickAction;
        }
    }
}
