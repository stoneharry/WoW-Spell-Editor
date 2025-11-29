using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using NLog;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.DBC;
using SpellEditor.Sources.Tools.VisualTools;
using SpellEditor.Sources.VersionControl;

namespace SpellEditor.Sources.AI
{
    public static class AiSpellMapper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // PUBLIC ENTRY ------------------------------------------------------

        public static void ApplyDefinitionToRow(AiSpellDefinition def, DataRow row)
        {
            if (def == null)
                throw new ArgumentNullException(nameof(def));
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            row.BeginEdit();

            ApplyBasicInfo(def, row);
            ApplySchoolMechanicDispel(def, row);
            ApplyAttributes(def, row);
            ApplyTiming(def, row);
            ApplyPower(def, row);
            ApplyTargeting(def, row);
            ApplyEffects(def, row);
            ApplyIcon(def, row);
            ApplyVisual(def, row);

            row.EndEdit();
        }

        // BASIC INFO --------------------------------------------------------

        private static void ApplyBasicInfo(AiSpellDefinition def, DataRow row)
        {
            if (!string.IsNullOrWhiteSpace(def.Name) && row.Table.Columns.Contains("SpellName0"))
                row["SpellName0"] = def.Name;
            else
                row["SpellName0"] = "";

            if (!string.IsNullOrWhiteSpace(def.Description) && row.Table.Columns.Contains("SpellDescription0"))
                row["SpellDescription0"] = def.Description;
            else
                row["SpellDescription0"] = "";  // FIXED from 0 to ""

            if (!string.IsNullOrWhiteSpace(def.ClassFamily) && row.Table.Columns.Contains("SpellFamilyName"))
                row["SpellFamilyName"] = MapClassFamily(def.ClassFamily);
            else
                row["SpellFamilyName"] = 0;

            if (def.Rank.HasValue && row.Table.Columns.Contains("SpellDescription2"))
            {
                // Optional rank mapping
            }
        }

        private static uint MapClassFamily(string family)
        {
            if (string.IsNullOrWhiteSpace(family))
                return 0;

            switch (family.Trim().ToLowerInvariant())
            {
                case "mage": return 3;
                case "warrior": return 4;
                case "warlock": return 5;
                case "priest": return 6;
                case "druid": return 7;
                case "rogue": return 8;
                case "hunter": return 9;
                case "paladin": return 10;
                case "shaman": return 11;
                case "deathknight":
                case "death knight": return 15;
                default: return 0;
            }
        }

        // SCHOOL / MECHANIC / DISPEL ---------------------------------------

        private static void ApplySchoolMechanicDispel(AiSpellDefinition def, DataRow row)
        {
            if (!string.IsNullOrWhiteSpace(def.School) && row.Table.Columns.Contains("SchoolMask"))
                row["SchoolMask"] = MapSchool(def.School);
            else
                row["SchoolMask"] = 0;

            if (!string.IsNullOrWhiteSpace(def.Mechanic) && row.Table.Columns.Contains("Mechanic"))
                row["Mechanic"] = MapMechanic(def.Mechanic);
            else
                row["Mechanic"] = 0;

            if (!string.IsNullOrWhiteSpace(def.DispelType) && row.Table.Columns.Contains("Dispel"))
                row["Dispel"] = MapDispel(def.DispelType);
            else
                row["Dispel"] = 0;
        }

        private static uint MapSchool(string school)
        {
            switch (school.Trim().ToLowerInvariant())
            {
                case "physical": return 0x01;
                case "holy": return 0x02;
                case "fire": return 0x04;
                case "nature": return 0x08;
                case "frost": return 0x10;
                case "shadow": return 0x20;
                case "arcane": return 0x40;
                default: return 0x01;
            }
        }

        private static uint MapMechanic(string mechanic)
        {
            switch (mechanic.Trim().ToLowerInvariant())
            {
                case "charm": return 1;
                case "disorient": return 2;
                case "disarm": return 3;
                case "fear": return 5;
                case "root": return 7;
                case "slow":
                case "snare": return 11;
                case "silence": return 9;
                case "sleep": return 10;
                case "stun": return 12;   // FIXED from 25 → 12
                default: return 0;
            }
        }

        private static uint MapDispel(string dispel)
        {
            switch (dispel.Trim().ToLowerInvariant())
            {
                case "magic": return 1;
                case "curse": return 2;
                case "disease": return 3;
                case "poison": return 4;
                case "stealth": return 5;
                case "invisibility": return 6;
                case "enrage": return 7;
                default: return 0;
            }
        }

        // TIMING / DURATION / RANGE ----------------------------------------

        private static void ApplyTiming(AiSpellDefinition def, DataRow row)
        {
            if (def.CastTimeSeconds.HasValue && row.Table.Columns.Contains("CastingTimeIndex"))
            {
                var idx = FindBestCastTimeIndex(def.CastTimeSeconds.Value);
                if (idx.HasValue)
                    row["CastingTimeIndex"] = idx.Value;
            }

            if (def.CooldownSeconds.HasValue && row.Table.Columns.Contains("CategoryRecoveryTime"))
                row["CategoryRecoveryTime"] = (int)(def.CooldownSeconds.Value * 1000f);

            if (def.GlobalCooldownSeconds.HasValue && row.Table.Columns.Contains("RecoveryTime"))
                row["RecoveryTime"] = (int)(def.GlobalCooldownSeconds.Value * 1000f);

            if (def.RangeYards.HasValue && row.Table.Columns.Contains("RangeIndex"))
            {
                var idx = FindBestRangeIndex(def.RangeYards.Value);
                if (idx.HasValue)
                    row["RangeIndex"] = idx.Value;
            }

            if (def.DurationSeconds.HasValue && row.Table.Columns.Contains("DurationIndex"))
            {
                var idx = FindBestDurationIndex(def.DurationSeconds.Value);
                if (idx.HasValue)
                    row["DurationIndex"] = idx.Value;
            }
        }

        private static uint? FindBestCastTimeIndex(float seconds)
        {
            var dbc = DBCManager.GetInstance().FindDbcForBinding("SpellCastTimes") as SpellCastTimes;
            if (dbc == null) return null;

            double best = double.MaxValue;
            uint? bestId = null;

            foreach (var box in dbc.GetAllBoxes())
            {
                if (!float.TryParse(box.Name, out float ms))
                    continue;

                var sec = ms / 1000f;
                var diff = Math.Abs(sec - seconds);

                if (diff < best)
                {
                    best = diff;
                    bestId = box.ID == -1 ? null : (uint?)box.ID;
                }
            }
            return bestId;
        }

        private static uint? FindBestRangeIndex(float yards)
        {
            var dbc = DBCManager.GetInstance().FindDbcForBinding("SpellRange") as SpellRange;
            if (dbc == null) return null;

            double best = double.MaxValue;
            uint? bestId = null;

            foreach (var b in dbc.GetAllBoxes())
            {
                var box = b as SpellRange.SpellRangeBoxContainer;
                if (box == null) continue;

                if (!float.TryParse(box.RangeString, NumberStyles.Float, CultureInfo.InvariantCulture, out var r))
                    continue;

                var diff = Math.Abs(r - yards);
                if (diff < best)
                {
                    best = diff;
                    bestId = box.ID == -1 ? null : (uint?)box.ID;
                }
            }
            return bestId;
        }

        private static uint? FindBestDurationIndex(float seconds)
        {
            var dbc = DBCManager.GetInstance().FindDbcForBinding("SpellDuration") as SpellDuration;
            if (dbc == null) return null;

            double best = double.MaxValue;
            uint? bestId = null;

            foreach (var b in dbc.GetAllBoxes())
            {
                var d = b as SpellDuration.DurationBox;
                if (d == null) continue;

                var dur = d.baseDuration / 1000f;
                var diff = Math.Abs(dur - seconds);

                if (diff < best)
                {
                    best = diff;
                    bestId = b.ID == -1 ? null : (uint?)b.ID;
                }
            }
            return bestId;
        }

        // POWER COST --------------------------------------------------------

        // POWER COST --------------------------------------------------------

        private static void ApplyPower(AiSpellDefinition def, DataRow row)
        {
            // 1) POWER TYPE: use enum map from enUS.xaml when possible
            if (!string.IsNullOrWhiteSpace(def.PowerType) &&
                row.Table.Columns.Contains("PowerType"))
            {
                string ptRaw = def.PowerType.Trim();

                int existing = 0;
                try
                {
                    existing = Convert.ToInt32(row["PowerType"]);
                }
                catch
                {
                    existing = 0;
                }

                int newValue = existing;

                // Try to use the enum map derived from power_type_strings
                if (SpellDbcEnumProvider.PowerTypeNameToId != null &&
                    SpellDbcEnumProvider.PowerTypeNameToId.Count > 0)
                {
                    // normalize: Mana / mana / MANA → "Mana"
                    // keys in PowerTypeNameToId should be the *display strings*
                    // from power_type_strings, e.g. "Mana", "Rage", "Runic Power"
                    string normalized = ptRaw;

                    // exact match first
                    uint id;
                    if (SpellDbcEnumProvider.PowerTypeNameToId.TryGetValue(normalized, out id))
                    {
                        newValue = (int)id;
                    }
                    else
                    {
                        // common normalizations
                        string lower = ptRaw.ToLowerInvariant();
                        if (lower == "runicpower") normalized = "Runic Power";
                        else if (lower == "none") normalized = "Mana"; // safest default

                        if (SpellDbcEnumProvider.PowerTypeNameToId.TryGetValue(normalized, out id))
                        {
                            newValue = (int)id;
                        }
                        else
                        {
                            // last fallback: map a few known synonyms
                            switch (lower)
                            {
                                case "mana": newValue = 0; break;
                                case "rage": newValue = 1; break;
                                case "focus": newValue = 2; break;
                                case "energy": newValue = 3; break;
                                case "happiness": newValue = 4; break;
                                case "runes": newValue = 5; break;
                                case "runic power":
                                case "runicpower": newValue = 6; break;
                                case "health":
                                case "life":
                                    // there isn't a real "Health" power type in 3.3.5a spells;
                                    // most health-cost spells are still Mana-type spells. Use Mana here.
                                    newValue = 0;
                                    break;
                                default:
                                    newValue = existing; // don't break a valid existing value
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    // No enum provider? fall back to simple mapping only
                    string lower = ptRaw.ToLowerInvariant();
                    switch (lower)
                    {
                        case "mana": newValue = 0; break;
                        case "rage": newValue = 1; break;
                        case "focus": newValue = 2; break;
                        case "energy": newValue = 3; break;
                        case "happiness": newValue = 4; break;
                        case "runes": newValue = 5; break;
                        case "runic power":
                        case "runicpower": newValue = 6; break;
                        default: newValue = existing; break;
                    }
                }

                // IMPORTANT: store as Int32, not uint
                row["PowerType"] = newValue;
            }

            // 2) POWER COST (only really makes sense for Mana)
            if (row.Table.Columns.Contains("ManaCost") &&
                def.PowerType != null &&
                def.PowerType.Trim().Equals("mana", StringComparison.OrdinalIgnoreCase) &&
                def.PowerCost.HasValue)
            {
                row["ManaCost"] = def.PowerCost.Value;
            }

            if (row.Table.Columns.Contains("ManaCostPercentage") &&
                def.PowerCostPercentage.HasValue)
            {
                row["ManaCostPercentage"] = def.PowerCostPercentage.Value;
            }
        }


        // TARGETING ---------------------------------------------------------

        private static void ApplyTargeting(AiSpellDefinition def, DataRow row)
        {
            // Targeting handled per-effect
        }

        private static void MapTarget(string target, out uint a, out uint b)
        {
            a = 0;
            b = 0;

            if (string.IsNullOrWhiteSpace(target))
                return;

            switch (target.Trim().ToLowerInvariant())
            {
                case "self": a = 1; break;
                case "enemy": a = 6; break;
                case "friendly": a = 21; break;
                case "area": a = 7; break;
                case "cone": a = 15; break;
                case "chain": a = 45; break;
                default: a = 6; break;
            }
        }

        // EFFECTS (1–3) -----------------------------------------------------

        private static void ApplyEffects(AiSpellDefinition def, DataRow row)
        {
            var effects = new List<AiEffectDefinition>();
            if (def.Effects != null)
                effects.AddRange(def.Effects);

            if (effects.Count == 0)
            {
                if (def.DirectDamage.HasValue)
                {
                    effects.Add(new AiEffectDefinition
                    {
                        Type = "Damage",
                        BasePoints = def.DirectDamage.Value
                    });
                }

                if (def.PeriodicDamage.HasValue)
                {
                    effects.Add(new AiEffectDefinition
                    {
                        Type = "ApplyAura",
                        Aura = "PeriodicDamage",
                        DamagePerSecond = def.PeriodicDamage.Value,
                        AmplitudeSeconds = def.PeriodicIntervalSeconds ?? 1.0f
                    });
                }
            }

            for (int i = 0; i < 3; ++i)
            {
                if (i >= effects.Count)
                    break;

                int slot = i + 1;
                var eff = effects[i];

                string effectCol = $"Effect{slot}";
                if (row.Table.Columns.Contains(effectCol))
                    row[effectCol] = MapEffectType(eff.Type, eff.Aura);

                string auraCol = $"EffectApplyAuraName{slot}";
                if (row.Table.Columns.Contains(auraCol))
                    row[auraCol] = MapAura(eff.Aura);

                string baseCol = $"EffectBasePoints{slot}";
                if (row.Table.Columns.Contains(baseCol))
                {
                    int basePoints = 0;

                    if (eff.BasePoints.HasValue)
                        basePoints = (int)Math.Round(eff.BasePoints.Value) - 1;
                    else if (eff.DamagePerSecond.HasValue && eff.AmplitudeSeconds.HasValue)
                        basePoints = (int)Math.Round(eff.DamagePerSecond.Value * eff.AmplitudeSeconds.Value) - 1;

                    if (basePoints < 0) basePoints = 0;
                    row[baseCol] = basePoints;
                }

                string dieCol = $"EffectDieSides{slot}";
                if (row.Table.Columns.Contains(dieCol) && eff.DieSides.HasValue)
                    row[dieCol] = (int)Math.Round(eff.DieSides.Value);

                string ampCol = $"EffectAmplitude{slot}";
                if (row.Table.Columns.Contains(ampCol) && eff.AmplitudeSeconds.HasValue)
                    row[ampCol] = (int)(eff.AmplitudeSeconds.Value * 1000f);

                string radCol = $"EffectRadiusIndex{slot}";
                float? radiusYards = eff.RadiusYards ?? def.RadiusYards;
                if (row.Table.Columns.Contains(radCol) && radiusYards.HasValue)
                {
                    var idx = FindBestRadiusIndex(radiusYards.Value);
                    if (idx.HasValue)
                        row[radCol] = idx.Value;
                }

                string aCol = $"EffectImplicitTargetA{slot}";
                string bCol = $"EffectImplicitTargetB{slot}";
                if (row.Table.Columns.Contains(aCol))
                {
                    MapTarget(eff.Target, out uint a, out uint b);
                    row[aCol] = a;
                    if (row.Table.Columns.Contains(bCol))
                        row[bCol] = b;
                }
            }
        }

        private static uint MapEffectType(string type, string aura)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                // Infer from aura when possible
                if (!string.IsNullOrWhiteSpace(aura))
                {
                    var lowerAura = aura.Trim().ToLowerInvariant();
                    if (lowerAura.Contains("periodicdamage"))
                        return 6; // APPLY_AURA + periodic damage aura
                    if (lowerAura.Contains("periodicheal"))
                        return 6; // APPLY_AURA + periodic heal aura
                    if (lowerAura.Contains("stun") || lowerAura.Contains("root") || lowerAura.Contains("decreasespeed"))
                        return 6; // APPLY_AURA crowd-control
                }

                // Default: SCHOOL_DAMAGE
                return 2;
            }

            var t = type.Trim().ToLowerInvariant();

            // Anything explicitly "ApplyAura" is, well, APPLY_AURA
            if (t == "applyaura" || t == "apply_aura")
                return 6;

            // Periodic helpers -> implemented as APPLY_AURA + periodic aura
            if (t == "periodicdamage" || t == "periodic_damage" || t == "dot")
                return 6;
            if (t == "periodicheal" || t == "periodic_heal" || t == "hot")
                return 6;

            // Crowd control helpers – always done as auras
            if (t == "stun" || t == "root" || t == "slow" || t == "silence" || t == "fear")
                return 6;

            // Direct effects
            if (t == "damage" || t == "schooldamage")
                return 2;   // SCHOOL_DAMAGE
            if (t == "heal")
                return 10;  // HEAL

            // Safe default
            return 2;
        }

        private static uint MapAura(string aura)
        {
            if (string.IsNullOrWhiteSpace(aura))
                return 0;

            var a = aura.Trim().ToLowerInvariant();

            // Periodic auras
            if (a == "periodicdamage" || a == "periodic_damage" || a == "dot")
                return 3;   // SPELL_AURA_PERIODIC_DAMAGE
            if (a == "periodicheal" || a == "periodic_heal" || a == "hot")
                return 8;   // SPELL_AURA_PERIODIC_HEAL

            // Movement / CC
            if (a == "moddecreasespeed" || a == "slow" || a == "snare")
                return 11;  // SPELL_AURA_MOD_DECREASE_SPEED
            if (a == "modstun" || a == "stun")
                return 12;  // SPELL_AURA_MOD_STUN
            if (a == "modroot" || a == "root")
                return 7;   // SPELL_AURA_MOD_ROOT
            if (a == "modsilence" || a == "silence")
                return 27;  // SPELL_AURA_MOD_SILENCE (WotLK)

            // You can keep extending this block with more aura names as needed.

            return 0;
        }

        private static uint? FindBestRadiusIndex(float yards)
        {
            var dbc = DBCManager.GetInstance().FindDbcForBinding("SpellRadius") as SpellRadius;
            if (dbc == null) return null;

            double best = double.MaxValue;
            uint? id = null;

            foreach (var box in dbc.GetAllBoxes())
            {
                var name = box.Name;
                var first = name.Contains(" ") ? name.Substring(0, name.IndexOf(' ')) : name;

                if (!float.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out var r))
                    continue;

                var diff = Math.Abs(r - yards);
                if (diff < best)
                {
                    best = diff;
                    id = box.ID == -1 ? null : (uint?)box.ID;
                }
            }
            return id;
        }

        // ATTRIBUTES / FLAGS -----------------------------------------------

        /// <summary>
        /// Map high-level booleans like IsPassive / IsChanneled into Spell.dbc Attributes.
        /// We keep this intentionally conservative so we don't stomp existing flags.
        /// </summary>
        private static void ApplyAttributes(AiSpellDefinition def, DataRow row)
        {
            // Attr0: passive + (future) negative/beneficial hints
            if (row.Table.Columns.Contains("Attributes"))
            {
                uint attr0;
                try
                {
                    attr0 = Convert.ToUInt32(row["Attributes"]);
                }
                catch
                {
                    attr0 = 0;
                }

                // Passive flag: SPELL_ATTR0_PASSIVE = 0x00000040
                if (def.IsPassive.HasValue)
                {
                    const uint PASSIVE = 0x00000040;
                    if (def.IsPassive.Value)
                        attr0 |= PASSIVE;
                    else
                        attr0 &= ~PASSIVE;
                }

                // NOTE: If you later add IsHarmful/IsBeneficial to AiSpellDefinition,
                // you can toggle SPELL_ATTR0_NEGATIVE_1 (0x04000000) here.

                row["Attributes"] = attr0;
            }

            // Attr1: channeled
            if (row.Table.Columns.Contains("AttributesEx"))
            {
                uint attr1;
                try
                {
                    attr1 = Convert.ToUInt32(row["AttributesEx"]);
                }
                catch
                {
                    attr1 = 0;
                }

                // Channeled: SPELL_ATTR1_CHANNELED_1 (0x00000004) and
                // SPELL_ATTR1_CHANNELED_2 (0x00000040)
                if (def.IsChanneled.HasValue)
                {
                    const uint CHANNELED_1 = 0x00000004;
                    const uint CHANNELED_2 = 0x00000040;

                    if (def.IsChanneled.Value)
                        attr1 |= (CHANNELED_1 | CHANNELED_2);
                    else
                        attr1 &= ~(CHANNELED_1 | CHANNELED_2);
                }

                row["AttributesEx"] = attr1;
            }
        }


        // ICON ---------------------------------------------------------------------------------

        private static void ApplyIcon(AiSpellDefinition def, DataRow row)
        {
            if (!row.Table.Columns.Contains("SpellIconID"))
                return;

            if (string.IsNullOrWhiteSpace(def.Icon))
                return;

            uint iconId = PickBestIconId(def.Icon);
            if (iconId != 0)
                row["SpellIconID"] = iconId;
        }

        public static uint PickBestIconId(string hint)
        {
            if (string.IsNullOrWhiteSpace(hint))
                return 0;

            try
            {
                var iconDbc = DBCManager.GetInstance().FindDbcForBinding("SpellIcon") as SpellIconDBC;
                if (iconDbc == null || iconDbc.Lookups == null)
                    return 0;

                string h = hint.ToLower().Trim();
                const double temperature = 0.6;

                var weightedList = new List<Tuple<uint, double>>();

                foreach (var icon in iconDbc.Lookups)
                {
                    string name = icon.Name.ToLowerInvariant();
                    int score = 0;

                    if (name.Contains(h))
                        score += 100;

                    if ((h.Contains("frost") || h.Contains("ice") || h.Contains("cold")) &&
                        (name.Contains("frost") || name.Contains("ice") || name.Contains("blue")))
                        score += 60;

                    if ((h.Contains("fire") || h.Contains("flame") || h.Contains("burn")) &&
                        (name.Contains("fire") || name.Contains("flame") || name.Contains("red")))
                        score += 60;

                    if ((h.Contains("shadow") || h.Contains("void")) &&
                        (name.Contains("shadow") || name.Contains("purple")))
                        score += 50;

                    if (h.Contains("holy") || h.Contains("light"))
                        if (name.Contains("holy") || name.Contains("yellow"))
                            score += 50;

                    if (h.Contains("heal"))
                        if (name.Contains("green") || name.Contains("holy"))
                            score += 40;

                    double weight = Math.Exp(score / temperature);
                    weightedList.Add(Tuple.Create((uint)icon.ID, weight));
                }

                double total = weightedList.Sum(w => w.Item2);
                if (total <= 0)
                    return 0;

                // Deterministic RNG based on hint string, so preview & apply agree
                int seed = hint.GetHashCode();
                var rng = new Random(seed);

                double roll = rng.NextDouble() * total;
                double cum = 0;

                foreach (var pair in weightedList)
                {
                    cum += pair.Item2;
                    if (roll <= cum)
                        return pair.Item1;
                }

                return weightedList.Last().Item1;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "PickBestIconId failed");
                return 0;
            }
        }

        // VISUAL ---------------------------------------------------------------------------------

        private static void ApplyVisual(AiSpellDefinition def, DataRow row)
        {
            if (!row.Table.Columns.Contains("SpellVisual"))
                return;

            if (string.IsNullOrWhiteSpace(def.VisualName))
                return;

            uint visualId = PickBestVisualId(def.VisualName);
            if (visualId != 0)
                row["SpellVisual1"] = (int)visualId;
        }

        private static uint PickBestVisualId(string hint)
        {
            if (string.IsNullOrWhiteSpace(hint))
                return 0;

            // TODO: Implement later
            /*
            try
            {
                
                var visualDbc = DBCManager.GetInstance().FindDbcForBinding("SpellVisual");
                if (visualDbc == null || visualDbc.Lookups == null || visualDbc.Lookups.Count == 0)
                    return 0;

                string h = hint.ToLowerInvariant().Trim();
                int bestScore = int.MinValue;
                uint bestId = 0;

                foreach (var v in visualDbc.Lookups)
                {
                    string name = v.Name.ToLowerInvariant();
                    int score = 0;

                    if (name.Contains(h)) score += 100;

                    if (h.Contains("fire") && (name.Contains("fire") || name.Contains("flame") || name.Contains("explosion")))
                        score += 50;

                    if (h.Contains("shadow") && (name.Contains("shadow") || name.Contains("bolt")))
                        score += 50;

                    // etc. add similar soft matches as needed

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestId = v.ID;
                    }
                }

                return bestId;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "PickBestVisualId failed");
                return 0;
            }
            */
            return 0;
        }

    }
}
