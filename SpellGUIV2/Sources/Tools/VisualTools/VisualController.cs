using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SpellEditor.Sources.Controls;

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

        private readonly uint _SelectedVisualId;

        public VisualController(uint id)
        {
            _SelectedVisualId = id;
        }

        public List<VisualKitListEntry> GetAllKitEntries()
        {
            var kitList = new List<VisualKitListEntry>();
            var visualDbc = DBCManager.GetInstance().FindDbcForBinding("SpellVisual") as SpellVisual;
            var visualRecord = visualDbc.LookupRecord(_SelectedVisualId);
            if (visualRecord == null)
            {
                return kitList;
            }
            var visualKitDbc = DBCManager.GetInstance().FindDbcForBinding("SpellVisualKit") as SpellVisualKit;
            foreach (var key in KitColumnKeys)
            {
                var kitIdStr = visualRecord[key].ToString();
                var success = uint.TryParse(kitIdStr, out var kitId);
                if (!success || kitId == 0)
                {
                    continue;
                }
                var kitRecord = visualKitDbc.LookupRecord(kitId);
                if (kitRecord == null)
                {
                    continue;
                }
                kitList.Add(new VisualKitListEntry(key, kitRecord));
            }
            return kitList;
        }
    }
}
