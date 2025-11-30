using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpellEditor.Sources.AI
{
    /// <summary>
    /// Semantic helper that loads effect / aura enum names from enUS.xaml
    /// and exposes fuzzy name → numeric ID mapping for the AI layer.
    /// </summary>
    public static class AiSemanticRegistry
    {
        private static bool _initialized;

        // Keys are normalized (lowercase, no whitespace or underscores)
        public static Dictionary<string, uint> EffectNameToId { get; private set; }
        public static Dictionary<string, uint> AuraNameToId { get; private set; }

        public static void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = true;

            EffectNameToId = BuildIndexFromResource("spell_effect_names");
            AuraNameToId = BuildIndexFromResource("spell_aura_effect_names");

            // Register a handful of high-level aliases so the AI can say
            // "Damage", "Heal", "Dot", "Hot", "Stun", "Slow" etc.
            RegisterEffectAliases();
            RegisterAuraAliases();
        }

        private static Dictionary<string, uint> BuildIndexFromResource(string resourceKey)
        {
            var dict = new Dictionary<string, uint>(StringComparer.InvariantCultureIgnoreCase);

            try
            {
                var raw = Application.Current != null
                    ? Application.Current.TryFindResource(resourceKey) as string
                    : null;

                if (string.IsNullOrEmpty(raw))
                    return dict;

                var entries = raw.Split(new[] { '|' }, StringSplitOptions.None);

                // SpellEffect / SpellAura enums are 0-based in WotLK:
                // 0 = NONE, 1 = FIRST_REAL_ENTRY, ...
                uint index = 0;
                foreach (var entry in entries)
                {
                    var name = entry.Trim();
                    if (name.Length == 0)
                        continue;

                    string primary = name;
                    AddKey(dict, primary, index);

                    // Also index on the first token (e.g. "PORTAL_TELEPORT unused" → "PORTAL_TELEPORT")
                    var firstToken = primary.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0];
                    if (!string.Equals(firstToken, primary, StringComparison.InvariantCultureIgnoreCase))
                        AddKey(dict, firstToken, index);

                    index++;
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Warn(ex, "AiSemanticRegistry.BuildIndexFromResource failed for {0}", resourceKey);
            }

            return dict;
        }

        private static void AddKey(Dictionary<string, uint> dict, string rawKey, uint value)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
                return;

            var norm = NormalizeKey(rawKey);
            if (norm.Length == 0)
                return;

            if (!dict.ContainsKey(norm))
                dict[norm] = value;
        }

        private static string NormalizeKey(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            var chars = raw
                .Where(c => !char.IsWhiteSpace(c) && c != '\r' && c != '\n' && c != '\t')
                .ToArray();

            var s = new string(chars);
            s = s.Replace("_", string.Empty);
            return s.ToLowerInvariant();
        }

        private static void RegisterEffectAliases()
        {
            // Map simple semantic names to core effect IDs where possible.
            uint id;

            // SCHOOL_DAMAGE
            if (TryLookupEffect("SCHOOL_DAMAGE", out id))
            {
                EffectNameToId[NormalizeKey("Damage")] = id;
                EffectNameToId[NormalizeKey("DirectDamage")] = id;
                EffectNameToId[NormalizeKey("Nuke")] = id;
            }

            // HEAL
            if (TryLookupEffect("HEAL", out id))
            {
                EffectNameToId[NormalizeKey("Heal")] = id;
                EffectNameToId[NormalizeKey("DirectHeal")] = id;
            }

            // APPLY_AURA – used for all aura-based helpers (stuns, DoTs, HoTs, slows, etc.)
            if (TryLookupEffect("APPLY_AURA", out id))
            {
                EffectNameToId[NormalizeKey("ApplyAura")] = id;
                EffectNameToId[NormalizeKey("PeriodicDamage")] = id;
                EffectNameToId[NormalizeKey("PeriodicHeal")] = id;
                EffectNameToId[NormalizeKey("Dot")] = id;
                EffectNameToId[NormalizeKey("Hot")] = id;
                EffectNameToId[NormalizeKey("StunEffect")] = id;
                EffectNameToId[NormalizeKey("RootEffect")] = id;
                EffectNameToId[NormalizeKey("SlowEffect")] = id;
            }

            // Some frequently used special effects
            if (TryLookupEffect("ENERGIZE", out id))
            {
                EffectNameToId[NormalizeKey("Energize")] = id;
                EffectNameToId[NormalizeKey("RestorePower")] = id;
            }

            if (TryLookupEffect("DUMMY", out id))
            {
                EffectNameToId[NormalizeKey("Dummy")] = id;
            }

            if (TryLookupEffect("TRIGGER_SPELL", out id))
            {
                EffectNameToId[NormalizeKey("TriggerSpell")] = id;
                EffectNameToId[NormalizeKey("ProcSpell")] = id;
            }

            if (TryLookupEffect("KNOCK_BACK", out id))
            {
                EffectNameToId[NormalizeKey("Knockback")] = id;
            }
        }

        private static void RegisterAuraAliases()
        {
            uint id;

            // PERIODIC_DAMAGE
            if (TryLookupAura("Periodic Damage", out id))
            {
                AuraNameToId[NormalizeKey("PeriodicDamage")] = id;
                AuraNameToId[NormalizeKey("Dot")] = id;
                AuraNameToId[NormalizeKey("DamageOverTime")] = id;
            }

            // PERIODIC_HEAL
            if (TryLookupAura("Periodic Heal", out id))
            {
                AuraNameToId[NormalizeKey("PeriodicHeal")] = id;
                AuraNameToId[NormalizeKey("Hot")] = id;
                AuraNameToId[NormalizeKey("HealOverTime")] = id;
            }

            // MOD_STUN
            if (TryLookupAura("Stun", out id) || TryLookupAura("Mod Stun", out id))
            {
                AuraNameToId[NormalizeKey("Stun")] = id;
                AuraNameToId[NormalizeKey("ModStun")] = id;
            }

            // MOD_DECREASE_SPEED
            if (TryLookupAura("Mod Decrease Speed", out id) || TryLookupAura("Slow", out id))
            {
                AuraNameToId[NormalizeKey("Slow")] = id;
                AuraNameToId[NormalizeKey("Snare")] = id;
                AuraNameToId[NormalizeKey("ModDecreaseSpeed")] = id;
            }

            // ROOT
            if (TryLookupAura("Rooted", out id) || TryLookupAura("Root", out id))
            {
                AuraNameToId[NormalizeKey("Root")] = id;
                AuraNameToId[NormalizeKey("ModRoot")] = id;
            }
        }

        private static bool TryLookupEffect(string key, out uint id)
        {
            id = 0;
            if (EffectNameToId == null || EffectNameToId.Count == 0)
                return false;

            var norm = NormalizeKey(key);
            return EffectNameToId.TryGetValue(norm, out id);
        }

        private static bool TryLookupAura(string key, out uint id)
        {
            id = 0;
            if (AuraNameToId == null || AuraNameToId.Count == 0)
                return false;

            var norm = NormalizeKey(key);
            return AuraNameToId.TryGetValue(norm, out id);
        }

        /// <summary>
        /// Resolve an Effect ID from an AI-level type + optional aura.
        /// </summary>
        public static bool TryResolveEffectId(string type, string aura, out uint id)
        {
            EnsureInitialized();
            id = 0;

            // 1) Direct lookup by type
            if (!string.IsNullOrWhiteSpace(type))
            {
                var key = NormalizeKey(type);
                if (EffectNameToId.TryGetValue(key, out id))
                    return true;
            }

            // 2) Infer from aura if type is missing
            if (string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(aura))
            {
                // Most aura-based effects are implemented via APPLY_AURA
                if (TryLookupEffect("APPLY_AURA", out id))
                    return true;
            }

            // 3) Friendly fallbacks
            var t = (type ?? string.Empty).Trim().ToLowerInvariant();

            if (t == "damage" || t == "directdamage" || t == "nuke")
            {
                if (TryLookupEffect("SCHOOL_DAMAGE", out id))
                    return true;
            }

            if (t == "heal" || t == "directheal")
            {
                if (TryLookupEffect("HEAL", out id))
                    return true;
            }

            if (t == "applyaura")
            {
                if (TryLookupEffect("APPLY_AURA", out id))
                    return true;
            }

            if (t == "periodicdamage" || t == "dot")
            {
                if (TryLookupEffect("APPLY_AURA", out id))
                    return true;
            }

            if (t == "periodicheal" || t == "hot")
            {
                if (TryLookupEffect("APPLY_AURA", out id))
                    return true;
            }

            if (t == "knockback")
            {
                if (TryLookupEffect("KNOCK_BACK", out id))
                    return true;
            }

            // Final hard-coded fallback: SCHOOL_DAMAGE
            if (TryLookupEffect("SCHOOL_DAMAGE", out id))
                return true;

            id = 2;
            return true;
        }

        /// <summary>
        /// Resolve an Aura ID from a friendly name such as "PeriodicDamage", "Stun", "Slow", etc.
        /// </summary>
        public static bool TryResolveAuraId(string aura, out uint id)
        {
            EnsureInitialized();
            id = 0;

            if (string.IsNullOrWhiteSpace(aura))
                return false;

            var key = NormalizeKey(aura);

            if (AuraNameToId.TryGetValue(key, out id))
                return true;

            // Extra friendly synonyms for safety
            var lower = aura.Trim().ToLowerInvariant();
            if (lower == "dot")
            {
                if (TryLookupAura("Periodic Damage", out id))
                    return true;
            }
            if (lower == "hot")
            {
                if (TryLookupAura("Periodic Heal", out id))
                    return true;
            }
            if (lower == "slow" || lower == "snare")
            {
                if (TryLookupAura("Mod Decrease Speed", out id))
                    return true;
            }
            if (lower == "stun")
            {
                if (TryLookupAura("Stun", out id) || TryLookupAura("Mod Stun", out id))
                    return true;
            }
            if (lower == "root")
            {
                if (TryLookupAura("Rooted", out id) || TryLookupAura("Root", out id))
                    return true;
            }

            return false;
        }
    }
}
