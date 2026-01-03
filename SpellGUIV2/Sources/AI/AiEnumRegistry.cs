using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SpellEditor.Sources.AI
{
    public static class AiEnumRegistry
    {
        public static Dictionary<string, uint> EffectNameToId;
        public static Dictionary<string, uint> AuraNameToId;
        public static Dictionary<string, uint> TargetNameToId;
        public static Dictionary<string, uint> MechanicNameToId;
        public static Dictionary<string, uint> PowerTypeNameToId;

        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized)
                return;

            EffectNameToId = LoadEnum("spell_effect_names");
            AuraNameToId = LoadEnum("spell_aura_effect_names");
            TargetNameToId = LoadEnum("target_a");
            MechanicNameToId = LoadEnum("mechanic_type_strings");
            PowerTypeNameToId = LoadEnum("power_type_strings");

            _initialized = true;
        }

        private static Dictionary<string, uint> LoadEnum(string key)
        {
            var dict = new Dictionary<string, uint>(StringComparer.InvariantCultureIgnoreCase);

            if (!(Application.Current.TryFindResource(key) is string raw))
                return dict;

            var items = raw.Split('|').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

            for (int i = 0; i < items.Count; i++)
            {
                dict[items[i]] = (uint)i;
                dict[items[i].Replace(" ", "")] = (uint)i;
                dict[items[i].ToLower()] = (uint)i;
            }

            return dict;
        }
    }
}
