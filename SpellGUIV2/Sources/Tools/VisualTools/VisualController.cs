using System;
using System.Collections.Generic;
using System.Linq;
using SpellEditor.Sources.Controls;
using SpellEditor.Sources.Controls.Visual;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.VersionControl;

namespace SpellEditor.Sources.Tools.VisualTools
{
    public class VisualController
    {
        private static VisualKitListEntry _CopiedKitEntry;
        private static VisualEffectListEntry _CopiedEffectEntry;
        public readonly List<IVisualListEntry> VisualKits;
        public readonly uint VisualId;
        public uint MissileModel { get; private set; }
        public uint MissileMotion { get; set; }
        public uint NextLoadAttachmentId;
        public bool CancelNextLoad = false;

        public VisualController(uint id, IDatabaseAdapter adapter)
        {
            VisualId = id;
            if (WoWVersionManager.IsTbcOrGreaterSelected)
            {
                VisualKits = GetAllKitEntries(adapter);
            }
        }

        private List<IVisualListEntry> GetAllKitEntries(IDatabaseAdapter adapter)
        {
            var kitList = new List<IVisualListEntry>();
            var visualResults = adapter.Query("SELECT * FROM spellvisual WHERE ID = " + VisualId);
            if (visualResults == null || visualResults.Rows.Count == 0)
            {
                return kitList;
            }
            var visualRecord = visualResults.Rows[0];
            MissileModel = uint.Parse(visualRecord["MissileModel"].ToString());
            MissileMotion = uint.Parse(visualRecord["MissileMotion"].ToString());
            foreach (var key in WoWVersionManager.GetInstance().LookupKeyResource().KitColumnKeys)
            {
                var kitIdStr = visualRecord[key].ToString();
                var success = uint.TryParse(kitIdStr, out var kitId);
                if (!success || kitId == 0)
                {
                    continue;
                }
                var kitResults = adapter.Query("SELECT * FROM spellvisualkit WHERE ID = " + kitId);
                if (kitResults == null || kitResults.Rows.Count == 0)
                {
                    continue;
                }
                var kitRecord = kitResults.Rows[0];
                var visualId = uint.Parse(visualRecord["ID"].ToString());
                kitList.Add(new VisualKitListEntry(key, visualId, kitRecord, adapter));
            }
            return kitList;
        }

        public static void SetCopiedKitEntry(VisualKitListEntry entry) => _CopiedKitEntry = entry;

        public static void SetCopiedEffectEntry(VisualEffectListEntry entry) => _CopiedEffectEntry = entry;

        public static VisualKitListEntry GetCopiedKitEntry() => _CopiedKitEntry;

        public static VisualEffectListEntry GetCopiedEffectEntry() => _CopiedEffectEntry;

        public List<string> GetAvailableFields(IVisualListEntry item)
        {
            List<string> availableKeys;
            List<string> usedKeys;
            var keyResource = WoWVersionManager.GetInstance().LookupKeyResource();
            if (item is VisualKitListEntry)
            {
                availableKeys = keyResource.KitColumnKeys.ToList();
                usedKeys = VisualKits.Select(kitEntry => kitEntry as VisualKitListEntry)
                    .Select(kitEntry => kitEntry?.KitName)
                    .ToList();
            }
            else if (item is VisualEffectListEntry effectEntry)
            {
                availableKeys = keyResource.EffectColumnKeys.ToList();
                usedKeys = VisualKits.Select(kitEntry => kitEntry as VisualKitListEntry)
                    .Where(kitEntry => uint.Parse(kitEntry.KitRecord[0].ToString()) == effectEntry.ParentKitId)
                    .Select(kitEntry => kitEntry?.GetAllEffectsAndAttachmentsEntries())
                    .SelectMany(list => list.Select(listEntry => listEntry as VisualEffectListEntry)
                        .Select(listEntry => listEntry.EffectName))
                    .ToList();
            }
            else
            {
                throw new Exception($"Unknown IVisualListEntry type: { item.GetType() }\n{ item }");
            }
            availableKeys.RemoveAll(key => usedKeys.Contains(key));
            return availableKeys;
        }
    }
}
