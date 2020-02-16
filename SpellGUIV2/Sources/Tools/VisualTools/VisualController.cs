using System;
using System.Collections.Generic;
using System.Linq;
using SpellEditor.Sources.Controls;
using SpellEditor.Sources.Controls.Visual;
using SpellEditor.Sources.Database;

namespace SpellEditor.Sources.Tools.VisualTools
{
    public class VisualController
    {
        public static string[] KitColumnKeys = new string[]
        {
            "PrecastKit",
            "CastKit",
            "ImpactKit",
            "StateKit",
            "StateDoneKit",
            "ChannelKit",
            "InstantAreaKit",
            "ImpactAreaKit"
        };
        public static string[] EffectColumnKeys = new string[]
        {
            "HeadEffect",
            "ChestEffect",
            "BaseEffect",
            "LeftHandEffect",
            "RightHandEffect",
            "BreathEffect",
            "LeftWeaponEffect",
            "RightWeaponEffect"
        };
        private static VisualKitListEntry _CopiedKitEntry;
        private static VisualEffectListEntry _CopiedEffectEntry;
        public readonly List<IVisualListEntry> VisualKits;
        public readonly uint VisualId;

        public VisualController(uint id, IDatabaseAdapter adapter)
        {
            VisualId = id;
            VisualKits = GetAllKitEntries(adapter);
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
            foreach (var key in KitColumnKeys)
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
            if (item is VisualKitListEntry)
            {
                availableKeys = KitColumnKeys.ToList();
                usedKeys = VisualKits.Select(kitEntry => kitEntry as VisualKitListEntry)
                    .Select(kitEntry => kitEntry?.KitName)
                    .ToList();
            }
            else if (item is VisualEffectListEntry effectEntry)
            {
                availableKeys = EffectColumnKeys.ToList();
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
