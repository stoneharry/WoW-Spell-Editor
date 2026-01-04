using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using SpellEditor.Sources.DBC;

namespace SpellEditor.Sources.AI
{
    /// <summary>
    /// Extracts an AI-editable semantic snapshot of the currently selected spell row.
    ///
    /// This is NOT a reverse AiSpellMapper:
    /// - It reads existing DBC values and expresses them in AiSpellDefinition terms.
    /// - It is intentionally lossy and only extracts fields that are meaningful to AI edits.
    /// </summary>
    public static class AiSpellSemanticExtractor
    {
        private static bool _reverseMapsBuilt;
        private static readonly Dictionary<uint, string> _effectIdToName = new Dictionary<uint, string>();
        private static readonly Dictionary<uint, string> _auraIdToName = new Dictionary<uint, string>();
        private static readonly Dictionary<uint, string> _powerTypeIdToName = new Dictionary<uint, string>();
        private static readonly Dictionary<uint, string> _mechanicIdToName = new Dictionary<uint, string>();

        public static AiSpellDefinition Extract(DataRow row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            EnsureReverseMaps();

            var def = new AiSpellDefinition();

            ExtractBasicInfo(row, def);
            ExtractStacks(row, def);
            ExtractDuration(row, def);
            ExtractPower(row, def);
            ExtractRange(row, def);
            ExtractSchool(row, def);
            ExtractMechanic(row, def);

            ExtractEffects(row, def);

            // Optional helpers for easier reasoning (when unambiguous)
            ExtractSimpleDamageHelpers(def);

            return def;
        }

        // --------------------------------------------------------------------
        // BASIC
        // --------------------------------------------------------------------

        private static void ExtractBasicInfo(DataRow row, AiSpellDefinition def)
        {
            def.Name = GetString(row, "SpellName0");
            def.Description = GetString(row, "SpellDescription0");
            def.Tooltip = GetString(row, "SpellTooltip0");

            // Rank: "Rank X" -> X
            var rankText = GetString(row, "SpellRank0");
            if (!string.IsNullOrWhiteSpace(rankText) &&
                rankText.StartsWith("Rank ", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(rankText.Substring(5).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int rank))
            {
                def.Rank = rank;
            }

            // Class family (optional)
            if (TryGetUInt(row, "SpellFamilyName", out uint fam) && fam != 0)
            {
                // Your mapper uses MapClassFamily(def.ClassFamily) to go name->id.
                // Here we keep it simple: if the AI needs it later, it can be inferred
                // from similar spells; leaving null avoids false labels.
                // (If you want, we can add an id->name mapping later.)
                def.ClassFamily = null;
            }
        }

        // --------------------------------------------------------------------
        // STACKS / DURATION
        // --------------------------------------------------------------------

        private static void ExtractStacks(DataRow row, AiSpellDefinition def)
        {
            if (TryGetInt(row, "StackAmount", out int stacks) && stacks > 0)
                def.MaxStacks = stacks;
        }

        private static void ExtractDuration(DataRow row, AiSpellDefinition def)
        {
            if (!TryGetInt(row, "DurationIndex", out int durId) || durId <= 0)
                return;

            float? seconds = TryResolveDurationSecondsFromDbc(durId);
            if (seconds.HasValue && seconds.Value > 0)
                def.DurationSeconds = seconds.Value;
        }

        // --------------------------------------------------------------------
        // POWER / RANGE / SCHOOL / MECHANIC
        // --------------------------------------------------------------------

        private static void ExtractPower(DataRow row, AiSpellDefinition def)
        {
            if (TryGetUInt(row, "PowerType", out uint pt))
                def.PowerType = ResolvePowerTypeName(pt);

            if (TryGetInt(row, "PowerCost", out int cost) && cost > 0)
                def.PowerCost = cost;

            if (TryGetInt(row, "PowerCostPct", out int pct) && pct > 0)
                def.PowerCostPercentage = pct;
        }

        private static void ExtractRange(DataRow row, AiSpellDefinition def)
        {
            if (!TryGetInt(row, "RangeIndex", out int rangeId) || rangeId <= 0)
                return;

            float? maxRange = TryResolveRangeMaxYardsFromDbc(rangeId);
            if (maxRange.HasValue)
                def.RangeYards = maxRange.Value;
        }

        private static void ExtractSchool(DataRow row, AiSpellDefinition def)
        {
            if (!TryGetUInt(row, "SchoolMask", out uint mask))
                return;

            // Match your similar-spell finder mapping
            switch (mask)
            {
                case 0x01: def.School = "Physical"; break;
                case 0x02: def.School = "Holy"; break;
                case 0x04: def.School = "Fire"; break;
                case 0x08: def.School = "Nature"; break;
                case 0x10: def.School = "Frost"; break;
                case 0x20: def.School = "Shadow"; break;
                case 0x40: def.School = "Arcane"; break;
                default:
                    // Mixed/unknown -> leave null so AI doesn't "lock" a school incorrectly
                    def.School = null;
                    break;
            }
        }

        private static void ExtractMechanic(DataRow row, AiSpellDefinition def)
        {
            if (TryGetUInt(row, "Mechanic", out uint mech))
            {
                def.Mechanic = ResolveMechanicName(mech);
            }
        }

        // --------------------------------------------------------------------
        // EFFECTS (critical for "double damage")
        // --------------------------------------------------------------------

        private static void ExtractEffects(DataRow row, AiSpellDefinition def)
        {
            var list = new List<AiEffectDefinition>();

            for (int slot = 1; slot <= 3; slot++)
            {
                if (!TryGetUInt(row, $"Effect{slot}", out uint effectId) || effectId == 0)
                    continue;

                var eff = new AiEffectDefinition();

                eff.Type = ResolveEffectName(effectId);

                if (TryGetUInt(row, $"EffectApplyAuraName{slot}", out uint auraId) && auraId != 0)
                    eff.Aura = ResolveAuraName(auraId);

                // BasePoints stored as N-1, real is +1
                if (TryGetInt(row, $"EffectBasePoints{slot}", out int basePointsRaw))
                    eff.BasePoints = basePointsRaw + 1;

                if (TryGetInt(row, $"EffectDieSides{slot}", out int dieSides) && dieSides > 0)
                    eff.DieSides = dieSides;

                // Amplitude stored in ms
                if (TryGetInt(row, $"EffectAmplitude{slot}", out int ampMs) && ampMs > 0)
                    eff.AmplitudeSeconds = ampMs / 1000f;

                // RadiusIndex -> yards
                if (TryGetUInt(row, $"EffectRadiusIndex{slot}", out uint radiusId) && radiusId > 0)
                {
                    float? r = TryResolveRadiusYardsFromDbc((int)radiusId);
                    if (r.HasValue)
                        eff.RadiusYards = r.Value;
                }

                if (TryGetUInt(row, $"EffectChainTarget{slot}", out uint chain) && chain > 0)
                    eff.ChainTargets = (int)chain;

                if (TryGetInt(row, $"EffectMiscValue{slot}", out int misc) && misc != 0)
                    eff.MiscValue = misc;

                if (TryGetInt(row, $"EffectMiscValueB{slot}", out int miscB) && miscB != 0)
                    eff.MiscValueB = miscB;

                if (TryGetInt(row, $"EffectTriggerSpell{slot}", out int trig) && trig > 0)
                    eff.TriggerSpellId = trig;

                // Multipliers
                float? mult = GetFloat(row, $"EffectMultipleValue{slot}");
                if (mult.HasValue)
                    eff.ValueMultiplier = mult.Value;

                float? dmgMult = GetFloat(row, $"EffectDamageMultiplier{slot}");
                if (dmgMult.HasValue)
                    eff.DamageMultiplier = dmgMult.Value;

                // Targeting (optional; helps AI reason about AoE vs ST)
                // We store your semantic target token rather than raw ids.
                if (TryGetUInt(row, $"EffectImplicitTargetA{slot}", out uint targA) && targA != 0)
                {
                    eff.Target = GuessTargetToken(targA);
                }

                list.Add(eff);
            }

            if (list.Count > 0)
                def.Effects = list;
        }

        /// <summary>
        /// Populate DirectDamage / PeriodicDamage helpers when unambiguous.
        /// These are optional and only help the AI do relative edits.
        /// </summary>
        private static void ExtractSimpleDamageHelpers(AiSpellDefinition def)
        {
            if (def.Effects == null || def.Effects.Count != 1)
                return;

            var eff = def.Effects[0];

            // Direct damage
            if (eff.Type != null &&
                eff.Type.Equals("SCHOOL_DAMAGE", StringComparison.OrdinalIgnoreCase) &&
                eff.Aura == null &&
                eff.BasePoints.HasValue)
            {
                def.DirectDamage = (int)Math.Round(eff.BasePoints.Value);
                return;
            }

            // Periodic damage (DoT)
            if (eff.Aura != null &&
                eff.Aura.Equals("PeriodicDamage", StringComparison.OrdinalIgnoreCase) &&
                eff.BasePoints.HasValue &&
                eff.AmplitudeSeconds.HasValue &&
                def.DurationSeconds.HasValue &&
                eff.AmplitudeSeconds.Value > 0)
            {
                float ticks = def.DurationSeconds.Value / eff.AmplitudeSeconds.Value;
                if (ticks > 0.5f)
                {
                    def.PeriodicIntervalSeconds = eff.AmplitudeSeconds.Value;
                    def.PeriodicDamage = (int)Math.Round(eff.BasePoints.Value * ticks);
                }
            }
        }

        // --------------------------------------------------------------------
        // Reverse enum maps (ID -> name)
        // --------------------------------------------------------------------

        private static void EnsureReverseMaps()
        {
            if (_reverseMapsBuilt)
                return;

            // Must be initialized before dictionaries are populated
            SpellDbcEnumProvider.Initialize();
            AiEnumRegistry.Initialize();

            BuildReverseMap(SpellDbcEnumProvider.EffectNameToId, _effectIdToName);
            BuildReverseMap(SpellDbcEnumProvider.AuraNameToId, _auraIdToName);
            BuildReverseMap(SpellDbcEnumProvider.PowerTypeNameToId, _powerTypeIdToName);
            BuildReverseMap(SpellDbcEnumProvider.MechanicNameToId, _mechanicIdToName);

            // Also include raw enum names from AiEnumRegistry as fallback
            BuildReverseMap(AiEnumRegistry.EffectNameToId, _effectIdToName);
            BuildReverseMap(AiEnumRegistry.AuraNameToId, _auraIdToName);

            _reverseMapsBuilt = true;
        }

        private static void BuildReverseMap(Dictionary<string, uint> forward, Dictionary<uint, string> reverse)
        {
            if (forward == null)
                return;

            foreach (var kv in forward)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                    continue;

                // Keep first name seen for an ID (stable enough)
                if (!reverse.ContainsKey(kv.Value))
                    reverse[kv.Value] = kv.Key;
            }
        }

        private static string ResolveEffectName(uint id)
        {
            if (_effectIdToName.TryGetValue(id, out var name))
                return name;

            // If unknown, just return numeric tag; AI will usually avoid touching it
            return "Effect(" + id + ")";
        }

        private static string ResolveAuraName(uint id)
        {
            if (_auraIdToName.TryGetValue(id, out var name))
                return name;

            return "Aura(" + id + ")";
        }

        private static string ResolvePowerTypeName(uint id)
        {
            if (_powerTypeIdToName.TryGetValue(id, out var name))
                return name;

            // Fallback matches your AiSimilarSpellFinder mapping
            switch (id)
            {
                case 0: return "Mana";
                case 1: return "Rage";
                case 2: return "Focus";
                case 3: return "Energy";
                case 4: return "Happiness";
                case 5: return "Runes";
                case 6: return "Runic Power";
                case 7: return "Soul Shards";
                case 8: return "Eclipse";
                case 9: return "Holy Power";
                default: return "PowerType(" + id + ")";
            }
        }

        private static string ResolveMechanicName(uint id)
        {
            if (_mechanicIdToName.TryGetValue(id, out var name))
                return name;

            // Conservative fallback
            if (id == 0) return "None";
            return "Mechanic(" + id + ")";
        }

        // --------------------------------------------------------------------
        // DBC lookups via DBCManager (reflection-safe)
        // --------------------------------------------------------------------

        private static float? TryResolveRadiusYardsFromDbc(int radiusId)
        {
            var boxName = TryGetDbcBoxNameById("SpellRadius", radiusId);
            if (string.IsNullOrWhiteSpace(boxName))
                return null;

            // Your mapper parses the first token as float in FindBestRadiusIndex()
            // so we do the same in reverse.
            var first = boxName.Contains(" ") ? boxName.Substring(0, boxName.IndexOf(' ')) : boxName;
            if (float.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out var r))
                return r;

            return null;
        }

        private static float? TryResolveRangeMaxYardsFromDbc(int rangeId)
        {
            var boxName = TryGetDbcBoxNameById("SpellRange", rangeId);
            if (string.IsNullOrWhiteSpace(boxName))
                return null;

            // SpellRange names vary by editor build; try to extract a "Max:" token first.
            // Otherwise, fall back to parsing the first float found.
            if (TryExtractFloatAfterLabel(boxName, "Max", out float max))
                return max;

            if (TryExtractFirstFloat(boxName, out float any))
                return any;

            return null;
        }

        private static float? TryResolveDurationSecondsFromDbc(int durationId)
        {
            var boxName = TryGetDbcBoxNameById("SpellDuration", durationId);
            if (string.IsNullOrWhiteSpace(boxName))
                return null;

            // Your UI shows names like:
            // "BaseDur: 10sec - 1000  MaxDur: 10sec - 1000  PerLevel: 0"
            // We'll prefer MaxDur if present, else BaseDur.
            if (TryExtractSecondsToken(boxName, "MaxDur", out float max))
                return max;

            if (TryExtractSecondsToken(boxName, "BaseDur", out float baseDur))
                return baseDur;

            // fallback: first float in seconds-ish string
            if (TryExtractFirstFloat(boxName, out float any))
                return any;

            return null;
        }

        private static string TryGetDbcBoxNameById(string binding, int id)
        {
            try
            {
                var dbc = DBCManager.GetInstance().FindDbcForBinding(binding);
                if (dbc == null)
                    return null;

                // Call dbc.GetAllBoxes() via reflection to avoid hard dependency on specific DBC types.
                var mi = dbc.GetType().GetMethod("GetAllBoxes", BindingFlags.Public | BindingFlags.Instance);
                if (mi == null)
                    return null;

                var boxesObj = mi.Invoke(dbc, null);
                if (boxesObj == null)
                    return null;

                foreach (var box in (System.Collections.IEnumerable)boxesObj)
                {
                    var idProp = box.GetType().GetProperty("ID", BindingFlags.Public | BindingFlags.Instance);
                    var nameProp = box.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                    if (idProp == null || nameProp == null)
                        continue;

                    var idObj = idProp.GetValue(box, null);
                    if (idObj == null)
                        continue;

                    int boxId;
                    if (!int.TryParse(idObj.ToString(), out boxId))
                        continue;

                    if (boxId == id)
                    {
                        var nameObj = nameProp.GetValue(box, null);
                        return nameObj?.ToString();
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // --------------------------------------------------------------------
        // Parsing helpers for DBC box names
        // --------------------------------------------------------------------

        private static bool TryExtractSecondsToken(string text, string label, out float seconds)
        {
            seconds = 0f;
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(label))
                return false;

            // Find "Label:" then find "sec"
            int idx = text.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            // search forward for "sec"
            int secIdx = text.IndexOf("sec", idx, StringComparison.OrdinalIgnoreCase);
            if (secIdx < 0)
                return false;

            // Look backwards from "sec" to find the number
            int start = secIdx - 1;
            while (start >= 0 && (char.IsDigit(text[start]) || text[start] == '.' || text[start] == ' '))
                start--;

            string num = text.Substring(start + 1, secIdx - (start + 1)).Trim();
            return float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds);
        }

        private static bool TryExtractFloatAfterLabel(string text, string label, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(label))
                return false;

            // Accept "Max:" or "Max"
            int idx = text.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            // Seek first digit after label
            for (int i = idx; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                {
                    int j = i;
                    while (j < text.Length && (char.IsDigit(text[j]) || text[j] == '.'))
                        j++;

                    string num = text.Substring(i, j - i);
                    return float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                }
            }

            return false;
        }

        private static bool TryExtractFirstFloat(string text, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]) && text[i] != '.')
                    continue;

                int j = i;
                while (j < text.Length && (char.IsDigit(text[j]) || text[j] == '.'))
                    j++;

                string num = text.Substring(i, j - i);
                return float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }

            return false;
        }

        // --------------------------------------------------------------------
        // Target guessing (very conservative)
        // --------------------------------------------------------------------

        private static string GuessTargetToken(uint implicitTargetA)
        {
            // Mirrors MapTarget in AiSpellMapper but in reverse, only for common cases.
            switch (implicitTargetA)
            {
                case 1: return "Self";      // TARGET_UNIT_CASTER
                case 6: return "Enemy";     // TARGET_UNIT_TARGET_ENEMY
                case 21: return "Friendly"; // TARGET_UNIT_TARGET_ALLY
                case 7: return "Area";      // TARGET_SRC_CASTER
                default:
                    return null;
            }
        }

        // --------------------------------------------------------------------
        // DataRow helpers
        // --------------------------------------------------------------------

        private static bool TryGetInt(DataRow row, string col, out int value)
        {
            value = 0;
            if (row?.Table == null || !row.Table.Columns.Contains(col))
                return false;

            var obj = row[col];
            if (obj == null || obj == DBNull.Value)
                return false;

            return int.TryParse(obj.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryGetUInt(DataRow row, string col, out uint value)
        {
            value = 0;
            if (row?.Table == null || !row.Table.Columns.Contains(col))
                return false;

            var obj = row[col];
            if (obj == null || obj == DBNull.Value)
                return false;

            return uint.TryParse(obj.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static string GetString(DataRow row, string col)
        {
            if (row?.Table == null || !row.Table.Columns.Contains(col))
                return null;

            var obj = row[col];
            if (obj == null || obj == DBNull.Value)
                return null;

            var s = obj.ToString();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        private static float? GetFloat(DataRow row, string col)
        {
            if (row?.Table == null || !row.Table.Columns.Contains(col))
                return null;

            var obj = row[col];
            if (obj == null || obj == DBNull.Value)
                return null;

            if (float.TryParse(obj.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                return v;

            return null;
        }
    }
}
