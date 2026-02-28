using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using NLog;

namespace SpellEditor.Sources.AI
{
    /// <summary>
    /// Provides mappings from human-readable names to spell DBC enum IDs.
    /// Values are loaded from the language XAML file (e.g. enUS.xaml) so they
    /// always stay in sync with the main editor UI.
    /// </summary>
    public static class SpellDbcEnumProvider
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static bool _initialized;

        public static Dictionary<string, uint> EffectNameToId { get; private set; }
        public static Dictionary<string, uint> AuraNameToId { get; private set; }
        public static Dictionary<string, uint> TargetNameToId { get; private set; }
        public static Dictionary<string, uint> MechanicNameToId { get; private set; }
        public static Dictionary<string, uint> PowerTypeNameToId { get; private set; }

        /// <summary>
        /// Call once before using AiSpellMapper. Safe to call multiple times.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string xamlPath = Path.Combine(exeDir, "enUS.xaml");
                if (!File.Exists(xamlPath))
                {
                    Logger.Warn("SpellDbcEnumProvider: enUS.xaml not found next to executable, enum mappings disabled.");
                    EffectNameToId = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                    AuraNameToId = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                    TargetNameToId = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                    MechanicNameToId = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                    PowerTypeNameToId = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                    _initialized = true;
                    return;
                }

                using (var stream = File.OpenRead(xamlPath))
                {
                    var dict = (ResourceDictionary)XamlReader.Load(stream);

                    // Lists where the ID is just the index
                    EffectNameToId = LoadList(dict, "spell_effect_names", useIndexAsId: true);
                    AuraNameToId = LoadList(dict, "spell_aura_effect_names", useIndexAsId: true);
                    TargetNameToId = LoadList(dict, "target_strings", useIndexAsId: true);
                    MechanicNameToId = LoadList(dict, "mechanic_names", useIndexAsId: true);

                    // "0 - Mana | 1 - Rage | ..."
                    PowerTypeNameToId = LoadNumberedList(dict, "power_types");

                    AddEffectSynonyms();
                    AddAuraSynonyms();
                    AddTargetSynonyms();
                    AddPowerTypeSynonyms();

                    _initialized = true;
                    Logger.Info("SpellDbcEnumProvider initialised successfully.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialise SpellDbcEnumProvider.");
                EffectNameToId = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                AuraNameToId = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                TargetNameToId = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                MechanicNameToId = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                PowerTypeNameToId = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                _initialized = true;
            }
        }

        private static Dictionary<string, uint> LoadList(ResourceDictionary dict, string key, bool useIndexAsId)
        {
            var result = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!dict.Contains(key))
                    return result;

                string raw = dict[key] as string;
                if (raw == null)
                    return result;

                var parts = raw.Split('|');
                for (int i = 0; i < parts.Length; i++)
                {
                    string name = parts[i].Trim();
                    if (string.IsNullOrEmpty(name))
                        continue;

                    uint id = useIndexAsId ? (uint)i : 0;
                    if (!result.ContainsKey(name))
                        result.Add(name, id);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "LoadList failed for key {0}", key);
            }

            return result;
        }

        /// <summary>
        /// For lists in the form "0 - Name | 1 - Name2 | ...".
        /// </summary>
        private static Dictionary<string, uint> LoadNumberedList(ResourceDictionary dict, string key)
        {
            var result = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!dict.Contains(key))
                    return result;

                string raw = dict[key] as string;
                if (raw == null)
                    return result;

                var parts = raw.Split('|');
                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;

                    int dash = trimmed.IndexOf('-');
                    if (dash < 0)
                        continue;

                    string idStr = trimmed.Substring(0, dash).Trim();
                    string label = trimmed.Substring(dash + 1).Trim();

                    uint id;
                    if (!uint.TryParse(idStr, out id))
                        continue;

                    if (!result.ContainsKey(label))
                        result.Add(label, id);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "LoadNumberedList failed for key {0}", key);
            }

            return result;
        }

        private static void AddOrReplace(Dictionary<string, uint> map, uint id, params string[] keys)
        {
            if (map == null)
                return;

            foreach (var k in keys)
            {
                if (string.IsNullOrWhiteSpace(k))
                    continue;

                if (!map.ContainsKey(k))
                    map[k] = id;
            }
        }

        private static void AddEffectSynonyms()
        {
            if (EffectNameToId == null || EffectNameToId.Count == 0)
                return;

            uint id;
            if (EffectNameToId.TryGetValue("SCHOOL_DAMAGE", out id))
                AddOrReplace(EffectNameToId, id, "Damage", "damage", "DirectDamage");

            if (EffectNameToId.TryGetValue("HEAL", out id))
                AddOrReplace(EffectNameToId, id, "Heal", "heal", "DirectHeal");

            if (EffectNameToId.TryGetValue("APPLY_AURA", out id))
                AddOrReplace(EffectNameToId, id, "ApplyAura", "Apply Aura", "applyaura");
        }

        private static void AddAuraSynonyms()
        {
            if (AuraNameToId == null || AuraNameToId.Count == 0)
                return;

            uint id;
            if (AuraNameToId.TryGetValue("Periodic Damage", out id) ||
                AuraNameToId.TryGetValue("PERIODIC_DAMAGE", out id))
            {
                AddOrReplace(AuraNameToId, id, "PeriodicDamage", "Periodic_Damage", "periodicdamage");
            }

            if (AuraNameToId.TryGetValue("Periodic Heal", out id) ||
                AuraNameToId.TryGetValue("PERIODIC_HEAL", out id))
            {
                AddOrReplace(AuraNameToId, id, "PeriodicHeal", "Periodic_Heal", "periodicheal");
            }

            if (AuraNameToId.TryGetValue("Mod Decrease Speed", out id) ||
                AuraNameToId.TryGetValue("MOD_DECREASE_SPEED", out id))
            {
                AddOrReplace(AuraNameToId, id, "Slow", "Snare", "ModDecreaseSpeed", "moddecreasespeed");
            }

            if (AuraNameToId.TryGetValue("Mod Stun", out id) ||
                AuraNameToId.TryGetValue("MOD_STUN", out id))
            {
                AddOrReplace(AuraNameToId, id, "Stun", "modstun");
            }
        }

        private static void AddTargetSynonyms()
        {
            if (TargetNameToId == null || TargetNameToId.Count == 0)
                return;

            foreach (var kv in TargetNameToId)
            {
                var name = kv.Key.ToUpperInvariant();

                if (name.Contains("TARGET_UNIT_CASTER") || name.Contains("SELF"))
                    AddOrReplace(TargetNameToId, kv.Value, "Self", "Caster");

                if (name.Contains("TARGET_UNIT_TARGET_ENEMY"))
                    AddOrReplace(TargetNameToId, kv.Value, "Enemy", "TargetEnemy", "SingleEnemy");

                if (name.Contains("TARGET_UNIT_TARGET_ALLY") || name.Contains("FRIEND"))
                    AddOrReplace(TargetNameToId, kv.Value, "Friendly", "Ally");

                if (name.Contains("AREA") || name.Contains("LOCATION"))
                    AddOrReplace(TargetNameToId, kv.Value, "Area", "AreaAroundTarget", "AreaAroundCaster");

                if (name.Contains("CHAIN"))
                    AddOrReplace(TargetNameToId, kv.Value, "Chain");

                if (name.Contains("CONE"))
                    AddOrReplace(TargetNameToId, kv.Value, "Cone");
            }
        }

        private static void AddPowerTypeSynonyms()
        {
            if (PowerTypeNameToId == null || PowerTypeNameToId.Count == 0)
                return;

            uint id;
            if (PowerTypeNameToId.TryGetValue("Mana", out id))
                AddOrReplace(PowerTypeNameToId, id, "Mana", "mana");

            if (PowerTypeNameToId.TryGetValue("Rage", out id))
                AddOrReplace(PowerTypeNameToId, id, "Rage", "rage");

            if (PowerTypeNameToId.TryGetValue("Focus", out id))
                AddOrReplace(PowerTypeNameToId, id, "Focus", "focus");

            if (PowerTypeNameToId.TryGetValue("Energy", out id))
                AddOrReplace(PowerTypeNameToId, id, "Energy", "energy");

            if (PowerTypeNameToId.TryGetValue("Happiness", out id))
                AddOrReplace(PowerTypeNameToId, id, "Happiness", "happiness");

            if (PowerTypeNameToId.TryGetValue("Runic Power", out id))
                AddOrReplace(PowerTypeNameToId, id, "RunicPower", "Runic Power", "runic", "runicpower");

            if (PowerTypeNameToId.TryGetValue("Rune", out id) ||
                PowerTypeNameToId.TryGetValue("Runes", out id))
            {
                AddOrReplace(PowerTypeNameToId, id, "Runes", "runes");
            }

            if (PowerTypeNameToId.TryGetValue("Health", out id))
                AddOrReplace(PowerTypeNameToId, id, "Health", "health");
        }
    }
}
