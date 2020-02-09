using System;
using System.Collections.Generic;
using System.Linq;
using SpellEditor.Sources.Controls;
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

        public readonly List<VisualKitListEntry> VisualKits;

        private readonly uint _SelectedVisualId;

        public VisualController(uint id, IDatabaseAdapter adapter)
        {
            _SelectedVisualId = id;
            VisualKits = GetAllKitEntries(adapter);
        }

        private List<VisualKitListEntry> GetAllKitEntries(IDatabaseAdapter adapter)
        {
            var kitList = new List<VisualKitListEntry>();
            var visualResults = adapter.Query("SELECT * FROM spellvisual WHERE ID = " + _SelectedVisualId);
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
    }
}
