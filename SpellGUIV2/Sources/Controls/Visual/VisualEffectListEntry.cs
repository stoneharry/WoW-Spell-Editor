using SpellEditor.Sources.Controls.Visual;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.Tools.VisualTools;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SpellEditor.Sources.Controls
{
    public class VisualEffectListEntry : AbstractVisualListEntry, IVisualListEntry
    {
        public readonly string EffectName;
        public readonly uint ParentKitId;
        public readonly uint ParentVisualId;
        public readonly DataRow EffectRecord;
        public readonly DataRow AttachRecord;
        public bool IsAttachment => AttachRecord != null;
        private readonly IDatabaseAdapter _Adapter;
        private StackPanel _ConfirmDeletePanel;

        public VisualEffectListEntry(string key, uint parentKitId, uint parentVisualId, DataRow effectRecord, IDatabaseAdapter adapter)
        {
            Orientation = Orientation.Horizontal;
            EffectName = key;
            ParentKitId = parentKitId;
            ParentVisualId = parentVisualId;
            EffectRecord = effectRecord;
            _Adapter = adapter;

            BuildSelf();
        }

        public VisualEffectListEntry(uint parentKitId, uint parentVisualId, DataRow effectRecord, DataRow attachRecord, IDatabaseAdapter adapter)
        {
            Orientation = Orientation.Horizontal;
            ParentKitId = parentKitId;
            ParentVisualId = parentVisualId;
            EffectRecord = effectRecord;
            _Adapter = adapter;
            AttachRecord = attachRecord;

            BuildSelf();
        }

        private void BuildSelf()
        {
            var id = IsAttachment ? AttachRecord[0].ToString() : EffectRecord[0].ToString();
            var name = EffectRecord["Name"].ToString();
            var effectPath = EffectRecord["FilePath"].ToString();
            string label;
            if (IsAttachment)
            {
                var attachmentId = uint.Parse(AttachRecord["AttachmentId"].ToString());
                var attachmentName = SpellVisualKitModelAttach.LookupAttachmentIndex(attachmentId);
                label = $"{ id } - { attachmentName } Attachment - { name }\n { effectPath }";
            }
            else
            {
                label = $"{ id } - { EffectName } - { name }\n { effectPath }";
            }
            var textBlock = new TextBlock
            {
                Text = label,
                Margin = new Thickness(5),
                MinWidth = 275.00
            };
            Children.Add(textBlock);

            ContextMenu = new VisualContextMenu(this);
        }

        public override void CopyItemClick(object sender, RoutedEventArgs args)
        {
            VisualController.SetCopiedEffectEntry(this);
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
                DeleteEntryFromKit();
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

        private void DeleteEntryFromKit()
        {
            if (IsAttachment)
            {
                _Adapter.Execute($"DELETE FROM spellvisualkitmodelattach WHERE ID = { AttachRecord[0] }");
            }
            else
            {
                _Adapter.Execute($"UPDATE spellvisualkit SET { EffectName } = 0 WHERE ID = { ParentKitId }");
            }
        }
    }
}
