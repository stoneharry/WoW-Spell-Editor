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
        private readonly Dictionary<string, object> _DerivedValueStore = new Dictionary<string, object>();
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

        public List<KitListEntry> GetAllKitListEntries()
        {
            var kitList = new List<KitListEntry>();
            var visualDbc = DBCManager.GetInstance().FindDbcForBinding("SpellVisual");
            var visualRecord = visualDbc.LookupRecord(_SelectedVisualId);
            if (visualRecord == null)
            {
                return kitList;
            }
            var visualKitDbc = DBCManager.GetInstance().FindDbcForBinding("SpellVisualKit");
            //var visualEffectDbc = (SpellVisualEffectName)DBCManager.GetInstance().FindDbcForBinding("SpellVisualEffectName");
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
                kitList.Add(new KitListEntry(key, kitRecord));
                /*foreach (var kitKey in _EffectColumnKeys)
                {
                    var effectIdStr = kitRecord[kitKey].ToString();
                    success = uint.TryParse(effectIdStr, out var effectId);
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
                    if (_DerivedValueStore.ContainsKey(kitKey))
                    {
                        _DerivedValueStore[kitKey] = _DerivedValueStore[kitKey] + ", " + effectPath;
                    }
                    else
                    {
                        _DerivedValueStore.Add(kitKey, effectPath);
                    }
                    kitList.Add(new KitListEntry());
                }*/
            }
            return kitList;
        }
    }
}
