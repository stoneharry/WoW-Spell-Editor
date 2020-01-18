using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.Tools.VisualTools
{
    class VisualController
    {
        private readonly Dictionary<string, object> _DerivedValueStore = new Dictionary<string, object>();

        public VisualController(uint id)
        {
            var effectList = new List<string>();
            var visualDbc = DBCManager.GetInstance().FindDbcForBinding("SpellVisual");
            var visualRecord = visualDbc.LookupRecord(id);
            if (visualRecord == null)
            {
                return;
            }
            var recordsToLookup = new string[]
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
            var effectsToLookup = new string[]
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
            var visualKitDbc = DBCManager.GetInstance().FindDbcForBinding("SpellVisualKit");
            var visualEffectDbc = (SpellVisualEffectName)DBCManager.GetInstance().FindDbcForBinding("SpellVisualEffectName");
            foreach (var key in recordsToLookup)
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
                foreach (var kitKey in effectsToLookup)
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
                    effectList.Add(effectPath);
                }
            }
            Console.WriteLine("Matched effects:\n" + string.Join("\n", effectList));
        }

        public string HeadEffectPath() => _DerivedValueStore.ContainsKey("HeadEffect") ? _DerivedValueStore["HeadEffect"].ToString() : "(none)";
        public string ChestEffectPath() => _DerivedValueStore.ContainsKey("ChestEffect") ? _DerivedValueStore["ChestEffect"].ToString() : "(none)";
        public string BaseEffectPath() => _DerivedValueStore.ContainsKey("BaseEffect") ? _DerivedValueStore["BaseEffect"].ToString() : "(none)";
        public string LeftHandEffectPath() => _DerivedValueStore.ContainsKey("LeftHandEffect") ? _DerivedValueStore["LeftHandEffect"].ToString() : "(none)";
        public string RightHandEffectPath() => _DerivedValueStore.ContainsKey("RightHandEffect") ? _DerivedValueStore["RightHandEffect"].ToString() : "(none)";
        public string BreathEffectPath() => _DerivedValueStore.ContainsKey("BreathEffect") ? _DerivedValueStore["BreathEffect"].ToString() : "(none)";
        public string LeftWeaponEffectPath() => _DerivedValueStore.ContainsKey("LeftWeaponEffect") ? _DerivedValueStore["LeftWeaponEffect"].ToString() : "(none)";
        public string RightWeaponEffectPath() => _DerivedValueStore.ContainsKey("RightWeaponEffect") ? _DerivedValueStore["RightWeaponEffect"].ToString() : "(none)";
    }
}
