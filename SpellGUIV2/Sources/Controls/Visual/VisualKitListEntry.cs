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
using SpellEditor.Sources.VersionControl;

namespace SpellEditor.Sources.Controls
{
    public class VisualKitListEntry : AbstractVisualListEntry, IVisualListEntry
    {
        public readonly string KitName;
        public readonly DataRow KitRecord;
        public readonly uint ParentVisualId;
        private readonly IDatabaseAdapter _Adapter;
        private List<VisualEffectListEntry> _Attachments;
        private List<VisualEffectListEntry> _Effects;
        private StackPanel _ConfirmDeletePanel;

        public VisualKitListEntry(string key, uint visualId, DataRow kitRecord, IDatabaseAdapter adapter)
        {
            Orientation = Orientation.Horizontal;
            ParentVisualId = visualId;
            KitName = key;
            KitRecord = kitRecord;
            _Adapter = adapter;
            _Attachments = WoWVersionManager.IsWotlkOrGreaterSelected ?
                findAttachments(adapter) :
                new List<VisualEffectListEntry>();
            _Effects = findAllEffects(adapter);
            buildSelf();
        }

        private void buildSelf()
        {
            var id = uint.Parse(KitRecord[0].ToString());
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
            foreach (var key in WoWVersionManager.GetInstance().LookupKeyResource().EffectColumnKeys)
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
                var parentId = uint.Parse(KitRecord[0].ToString());
                effectList.Add(new VisualEffectListEntry(key, parentId, ParentVisualId, effectRecord, adapter));
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
                var parentId = uint.Parse(KitRecord[0].ToString());
                attachments.Add(new VisualEffectListEntry(parentId, ParentVisualId, effectRecord, attachRecord, adapter));
            }
            return attachments;
        }

        public override void CopyItemClick(object sender, RoutedEventArgs args)
        {
            VisualController.SetCopiedKitEntry(this);
            InvokeCopyAction();
        }

        public override void DeleteItemClick(object sender, RoutedEventArgs args)
        {
            if (_ConfirmDeletePanel != null)
            {
                return;
            }
            var panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            var confirmDeleteButton = new Button
            {
                Content = TryFindResource("VisualDeleteListEntryConfirm") ?? "CONFIRM DELETE:\nThis change is\nsaved immediately",
                Margin = new Thickness(5),
                MinWidth = 100
            };
            confirmDeleteButton.Click += (_sender, _args) => {
                // Stop and delete everything in this instance
                _ConfirmDeletePanel = null;
                DeleteKitFromVisual();
                Children.Clear();
                InvokeDeleteAction();
            };
            var cancelButton = new Button
            {
                Content = TryFindResource("VisualCancelListEntry") ?? "Cancel",
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
            var effectsFound = new List<string>();
            foreach (var kitKey in WoWVersionManager.GetInstance().LookupKeyResource().EffectColumnKeys)
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

        public List<IVisualListEntry> GetAllEffectsAndAttachmentsEntries()
        {
            var listEntries = new List<IVisualListEntry>();

            _Effects.ForEach(listEntries.Add);
            _Attachments.ForEach(listEntries.Add);

            return listEntries;
        }
    }
}
