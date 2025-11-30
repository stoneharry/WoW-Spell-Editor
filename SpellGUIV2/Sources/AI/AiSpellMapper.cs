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

            AiEnumRegistry.Initialize();

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

            if (!string.IsNullOrWhiteSpace(def.Description) && row.Table.Columns.Contains("SpellDescription0"))
                row["SpellDescription0"] = def.Description;

            if (!string.IsNullOrWhiteSpace(def.ClassFamily) && row.Table.Columns.Contains("SpellFamilyName"))
                row["SpellFamilyName"] = MapClassFamily(def.ClassFamily);

            // TODO: Implement SpellRank0 and SpellTooltip0
            if (def.Rank.HasValue && row.Table.Columns.Contains("SpellRank0"))
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

            if (!string.IsNullOrWhiteSpace(def.Mechanic) && row.Table.Columns.Contains("Mechanic"))
                row["Mechanic"] = MapMechanic(def.Mechanic);

            if (!string.IsNullOrWhiteSpace(def.DispelType) && row.Table.Columns.Contains("Dispel"))
                row["Dispel"] = MapDispel(def.DispelType);
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
            else if (def.IsInstant == true && row.Table.Columns.Contains("CastingTimeIndex"))
            {
                // If explicitly marked instant and no explicit cast time provided,
                // snap to the closest 0-second cast time entry.
                var idx = FindBestCastTimeIndex(0f);
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
            else if (def.IsChanneled == true && def.ChannelTimeSeconds.HasValue && row.Table.Columns.Contains("DurationIndex"))
            {
                // If no explicit DurationSeconds but a channel time is specified,
                // map the channel length to a duration entry.
                var idx = FindBestDurationIndex(def.ChannelTimeSeconds.Value);
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

            // 3) RUNE COST (Death Knight) – reuse existing SpellRuneCost rows only.
            if ((def.RuneCostBlood ?? 0) > 0 ||
                (def.RuneCostFrost ?? 0) > 0 ||
                (def.RuneCostUnholy ?? 0) > 0)
            {
                int blood = def.RuneCostBlood ?? 0;
                int frost = def.RuneCostFrost ?? 0;
                int unholy = def.RuneCostUnholy ?? 0;

                var runeId = FindBestRuneCostId(blood, frost, unholy);
                if (runeId.HasValue && row.Table.Columns.Contains("RuneCost"))
                {
                    row["RuneCost"] = (int)runeId.Value;
                }
            }
        }


        // TARGETING ---------------------------------------------------------

        private static void ApplyTargeting(AiSpellDefinition def, DataRow row)
        {
            // Targeting handled per-effect

            // Optional high-level cap on targets hit (e.g. Chain Lightning jumps).
            if (def.MaxTargets.HasValue && row.Table.Columns.Contains("MaximumAffectedTargets"))
            {
                var maxTargets = (int)Math.Max(0, Math.Round(def.MaxTargets.Value));
                row["MaximumAffectedTargets"] = maxTargets;
            }
        }

        private static void MapTarget(string target, out uint a, out uint b)
        {
            a = 0;
            b = 0;

            if (string.IsNullOrWhiteSpace(target))
                return;

            string t = target.Trim().ToLowerInvariant();

            switch (t)
            {
                case "self":
                    a = 1; b = 0;       // TARGET_UNIT_CASTER
                    break;

                case "enemy":
                    a = 6; b = 0;       // TARGET_UNIT_TARGET_ENEMY
                    break;

                case "friendly":
                    a = 21; b = 0;      // TARGET_UNIT_TARGET_ALLY
                    break;

                case "area":
                case "aoe":
                case "radius":
                case "area around caster":
                    a = 7;  // TARGET_SRC_CASTER
                    b = 16; // TARGET_DEST_DEST
                    break;

                case "ground":
                case "ground-target":
                case "place":
                case "location":
                    a = 1; // TARGET_UNIT_CASTER
                    b = 16; // TARGET_DEST_DEST (location)
                    break;

                case "cone":
                    a = 1; // TARGET_UNIT_CASTER
                    b = 22; // TARGET_DEST_CASTER_FRONT
                    break;

                case "chain":
                    a = 45; // chain start
                    b = 6;  // next unit
                    break;

                case "enemy area":
                    a = 6;  // target enemy
                    b = 16; // destination
                    break;

                case "friendly area":
                    a = 21;
                    b = 16;
                    break;

                default:
                    a = 6;
                    b = 0;
                    break;
            }
        }
        private static void ApplyEffects(AiSpellDefinition def, DataRow row)
        {
            // ------------------------------------------------------------
            // 1) Build working effect list from definition or fallbacks
            // ------------------------------------------------------------
            var effects = new List<AiEffectDefinition>();

            if (def.Effects != null && def.Effects.Count > 0)
            {
                effects.AddRange(def.Effects);
            }
            else
            {
                // Fallback: direct damage
                if (def.DirectDamage.HasValue)
                {
                    effects.Add(new AiEffectDefinition
                    {
                        Type = "Damage",
                        BasePoints = def.DirectDamage.Value
                    });
                }

                // Fallback: DoT
                if (def.PeriodicDamage.HasValue)
                {
                    float dps = def.PeriodicDamage.Value;

                    float amplitudeSeconds =
                        def.PeriodicIntervalOverrideSeconds
                        ?? def.PeriodicIntervalSeconds
                        ?? (def.DurationSeconds.HasValue && def.DurationSeconds.Value >= 3.0f
                                ? Math.Max(1.0f, def.DurationSeconds.Value / 5.0f)
                                : 1.0f);

                    effects.Add(new AiEffectDefinition
                    {
                        Type = "ApplyAura",
                        Aura = "PeriodicDamage",
                        DamagePerSecond = dps,
                        AmplitudeSeconds = amplitudeSeconds
                    });
                }
            }

            // ------------------------------------------------------------
            // 2) Clear all effect slots (avoid leftover values)
            // ------------------------------------------------------------
            for (int slot = 1; slot <= 3; ++slot)
            {
                SafeSet(row, $"Effect{slot}", 0);
                SafeSet(row, $"EffectApplyAuraName{slot}", 0);
                SafeSet(row, $"EffectBasePoints{slot}", 0);
                SafeSet(row, $"EffectDieSides{slot}", 0);
                SafeSet(row, $"EffectAmplitude{slot}", 0);
                SafeSet(row, $"EffectRadiusIndex{slot}", 0);
                SafeSet(row, $"EffectImplicitTargetA{slot}", 0);
                SafeSet(row, $"EffectImplicitTargetB{slot}", 0);
                SafeSet(row, $"EffectMiscValue{slot}", 0);
                SafeSet(row, $"EffectMiscValueB{slot}", 0);
                SafeSet(row, $"EffectTriggerSpell{slot}", 0);
                SafeSet(row, $"EffectChainTarget{slot}", 0);
                SafeSet(row, $"EffectMultipleValue{slot}", 0f);
                SafeSet(row, $"EffectDamageMultiplier{slot}", 0f);
            }

            // ------------------------------------------------------------
            // 3) Apply up to 3 effects
            // ------------------------------------------------------------
            int count = Math.Min(3, effects.Count);

            for (int i = 0; i < count; ++i)
            {
                int slot = i + 1;
                AiEffectDefinition eff = effects[i];

                // --------------------------------------------------------
                // AURA INFERENCE (if effect type is known CC)
                // --------------------------------------------------------
                if (eff.Aura == null && !string.IsNullOrWhiteSpace(eff.Type))
                {
                    switch (eff.Type.Trim().ToLowerInvariant())
                    {
                        case "stun": eff.Aura = "ModStun"; break;
                        case "root": eff.Aura = "ModRoot"; break;
                        case "slow": eff.Aura = "ModDecreaseSpeed"; break;
                        case "silence": eff.Aura = "ModSilence"; break;
                        case "fear": eff.Aura = "ModFear"; break;
                        case "charm": eff.Aura = "ModCharm"; break;
                    }
                }

                // --------------------------------------------------------
                // EFFECT TYPE (EffectN)
                // --------------------------------------------------------
                uint effectId = MapEffectType(eff.Type, eff.Aura);
                SafeSet(row, $"Effect{slot}", effectId);

                // Weapon damage override
                if (eff.WeaponDamagePercent.HasValue)
                {
                    SafeSet(row, $"Effect{slot}", 1); // SPELL_EFFECT_WEAPON_DAMAGE
                }

                // --------------------------------------------------------
                // AURA NAME (EffectApplyAuraNameN)
                // --------------------------------------------------------
                uint auraId = MapAura(eff.Aura);
                SafeSet(row, $"EffectApplyAuraName{slot}", auraId);

                // --------------------------------------------------------
                // BASE POINTS
                // --------------------------------------------------------
                int basePoints = 0;

                if (eff.BasePoints.HasValue)
                {
                    basePoints = (int)Math.Round(eff.BasePoints.Value) - 1;
                }
                else if (eff.DamagePerSecond.HasValue && eff.AmplitudeSeconds.HasValue)
                {
                    basePoints = (int)Math.Round(eff.DamagePerSecond.Value * eff.AmplitudeSeconds.Value) - 1;
                }

                if (basePoints < 0) basePoints = 0;
                SafeSet(row, $"EffectBasePoints{slot}", basePoints);

                // --------------------------------------------------------
                // DIE SIDES
                // --------------------------------------------------------
                if (eff.DieSides.HasValue)
                    SafeSet(row, $"EffectDieSides{slot}", (int)Math.Round(eff.DieSides.Value));

                // --------------------------------------------------------
                // PERIODIC AMPLITUDE
                // --------------------------------------------------------
                float? ampSec = eff.AmplitudeSeconds ?? def.PeriodicIntervalOverrideSeconds;
                if (ampSec.HasValue)
                    SafeSet(row, $"EffectAmplitude{slot}", (int)(ampSec.Value * 1000f));

                // --------------------------------------------------------
                // RADIUS
                // --------------------------------------------------------
                float? radius = eff.RadiusYards ?? def.RadiusYards;
                if (radius.HasValue)
                {
                    uint? idx = FindBestRadiusIndex(radius.Value);
                    if (idx.HasValue)
                        SafeSet(row, $"EffectRadiusIndex{slot}", idx.Value);
                }

                // --------------------------------------------------------
                // TARGETING — uses ResolveEffectTarget FIX
                // --------------------------------------------------------
                string target = ResolveEffectTarget(eff, def);
                if (!string.IsNullOrWhiteSpace(target))
                {
                    MapTarget(target, out uint targA, out uint targB);
                    SafeSet(row, $"EffectImplicitTargetA{slot}", targA);
                    SafeSet(row, $"EffectImplicitTargetB{slot}", targB);
                }

                // --------------------------------------------------------
                // MISC VALUES
                // --------------------------------------------------------
                int? misc = ComputeEffectMiscValue(def, eff);
                if (misc.HasValue)
                    SafeSet(row, $"EffectMiscValue{slot}", misc.Value);

                int? miscB = ComputeEffectMiscValueB(def, eff);
                if (miscB.HasValue)
                    SafeSet(row, $"EffectMiscValueB{slot}", miscB.Value);

                // --------------------------------------------------------
                // TRIGGER SPELL
                // --------------------------------------------------------
                if (eff.TriggerSpellId.HasValue)
                    SafeSet(row, $"EffectTriggerSpell{slot}", eff.TriggerSpellId.Value);

                // --------------------------------------------------------
                // CHAIN TARGETS
                // --------------------------------------------------------
                if (eff.ChainTargets.HasValue)
                    SafeSet(row, $"EffectChainTarget{slot}", eff.ChainTargets.Value);

                // --------------------------------------------------------
                // MULTIPLIERS
                // --------------------------------------------------------
                if (eff.ValueMultiplier.HasValue)
                    SafeSet(row, $"EffectMultipleValue{slot}", eff.ValueMultiplier.Value);

                // Damage multiplier
                if (eff.DamageMultiplier.HasValue)
                    SafeSet(row, $"EffectDamageMultiplier{slot}", eff.DamageMultiplier.Value);

                // Weapon damage percent → damage multiplier
                if (eff.WeaponDamagePercent.HasValue)
                {
                    float dmgMult = eff.WeaponDamagePercent.Value / 100f;
                    SafeSet(row, $"EffectDamageMultiplier{slot}", dmgMult);
                }

                // --------------------------------------------------------
                // SUMMON HANDLING
                // --------------------------------------------------------
                if (eff.Type != null
                    && eff.Type.Trim().Equals("summon", StringComparison.OrdinalIgnoreCase)
                    && eff.CreatureId.HasValue)
                {
                    SafeSet(row, $"Effect{slot}", 28); // SPELL_EFFECT_SUMMON
                    SafeSet(row, $"EffectMiscValue{slot}", eff.CreatureId.Value);
                }
            }
        }

        /// <summary>
        /// Heuristically populate EffectMiscValueN based on existing high-level
        /// info (School, PowerType, Aura, Type), but FIRST honor any explicit
        /// AiEffectDefinition.MiscValue / CreatureId provided by the AI.
        /// 
        /// This is intentionally conservative: we only handle cases where
        /// SpellEffects.cpp / SpellAuraEffects.cpp clearly use MiscValue and
        /// we can infer it from the data we already have.
        /// </summary>
        private static int? ComputeEffectMiscValue(AiSpellDefinition def, AiEffectDefinition eff)
        {
            // 1) Explicit override
            if (eff.MiscValue.HasValue)
                return eff.MiscValue.Value;

            // 2) Summon creature ID
            if (!string.IsNullOrWhiteSpace(eff.Type) &&
                eff.Type.Trim().Equals("summon", StringComparison.OrdinalIgnoreCase) &&
                eff.CreatureId.HasValue)
            {
                return eff.CreatureId.Value;
            }

            string t = eff.Type?.Trim().ToLowerInvariant();
            string aura = eff.Aura?.Trim().ToLowerInvariant();

            // 3) Power drain / energize / leech
            if (t == "drainmana" || t == "energize" ||
                t == "leechmana" || t == "drainhealth" || t == "leechhealth")
            {
                return MapPowerTypeToMisc(def.PowerType);
            }

            // 4) Periodic leech based on aura
            if (aura == "periodicleech" || t == "periodicleech")
                return MapPowerTypeToMisc(def.PowerType);

            // 5) Periodic damage / school-based auras
            if (aura == "periodicdamage" || aura == "schooldamage" || aura == "schoolabsorb")
            {
                return (int)MapSchool(def.School);
            }

            // 6) Stat modifications
            if (aura != null && aura.StartsWith("modstat"))
                return -1; // "all stats"

            // 7) Unknown → no misc
            return null;
        }

        /// <summary>
        /// EffectMiscValueBN:
        ///  - If the AI explicitly sets MiscValueB, we copy it directly.
        ///  - Otherwise we currently do not infer anything automatically; this
        ///    keeps behaviour conservative and avoids guessing IDs.
        /// 
        /// You can extend this later (e.g. chain jumps, SummonPropertiesId, etc.)
        /// once you're happy with the pipeline.
        /// </summary>
        private static int? ComputeEffectMiscValueB(AiSpellDefinition def, AiEffectDefinition eff)
        {
            if (eff.MiscValueB.HasValue)
                return eff.MiscValueB.Value;

            string t = eff.Type?.Trim().ToLowerInvariant();
            string aura = eff.Aura?.Trim().ToLowerInvariant();

            // 1) Drains / leeches power type secondary
            if (t == "drainmana" || t == "leechmana" || t == "energize")
                return MapPowerTypeToMisc(def.PowerType);

            // 2) No B value needed
            return null;
        }


        /// <summary>
        /// Map the high-level PowerType string ("Mana", "Rage", etc.) to the
        /// numeric Powers enum used by the DBC (EffectMiscValue).
        /// 
        /// We rely on SpellDbcEnumProvider.PowerTypeNameToId so this stays
        /// in sync with the editor's main enum mappings.
        /// </summary>
        private static int? MapPowerTypeToMisc(string powerType)
        {
            if (string.IsNullOrWhiteSpace(powerType))
                return null;

            SpellDbcEnumProvider.Initialize();

            uint id;
            if (SpellDbcEnumProvider.PowerTypeNameToId != null &&
                SpellDbcEnumProvider.PowerTypeNameToId.TryGetValue(powerType.Trim(), out id))
            {
                return (int)id;
            }

            // Fallback: very defensive hard-coded mapping if the XAML mappings
            // don't contain the exact label the AI used.
            string t = powerType.Trim().ToLowerInvariant();
            switch (t)
            {
                case "mana": return 0;
                case "rage": return 1;
                case "focus": return 2;
                case "energy": return 3;
                case "happiness": return 4;
                case "runes":
                case "rune": return 5;
                case "runicpower":
                case "runic power": return 6;
                case "health":
                case "hp": return 0; // health-based costs usually modelled differently; keep safe
                default:
                    return null;
            }
        }

        /// <summary>
        /// Decide which semantic target string to use for a specific effect.
        /// Priority:
        ///   1) Effect.Target (per-effect override)
        ///   2) def.TargetType (top-level)
        ///   3) Heuristic based on effect type and whether it is AoE
        /// </summary>
        private static string ResolveEffectTarget(AiEffectDefinition eff, AiSpellDefinition def)
        {
            // 1) Explicit effect-level override
            if (!string.IsNullOrWhiteSpace(eff.Target))
                return eff.Target.Trim();

            // 2) Spell-level override
            if (!string.IsNullOrWhiteSpace(def.TargetType))
                return def.TargetType.Trim();

            string type = eff.Type?.Trim().ToLowerInvariant();
            string aura = eff.Aura?.Trim().ToLowerInvariant();

            // 3) AoE → use Area
            if (eff.RadiusYards.HasValue || def.RadiusYards.HasValue)
                return "Area";

            // 4) Control → default to Enemy
            if (type == "stun" || type == "root" || type == "slow" || type == "silence" ||
                type == "fear" || type == "charm" || type == "disarm" || type == "knockback")
                return "Enemy";

            // 5) Healing
            if (type == "heal" || type == "periodicheal" || aura == "periodicheal")
                return "Friendly";

            // 6) Direct or periodic damage
            if (type == "damage" || type == "periodicdamage" || aura == "periodicdamage")
                return "Enemy";

            // 7) Drain / Leech
            if (type?.StartsWith("drain") == true || type?.StartsWith("leech") == true)
                return "Enemy";

            // 8) Summons do not strictly need target; default Enemy for offensive summons
            if (type == "summon")
                return "Self";

            // 9) Energy / resource gains → self
            if (type == "energize")
                return "Self";

            // Fallback
            return "Enemy";
        }

        private static uint MapEffectType(string type, string aura)
        {
            AiEnumRegistry.Initialize();
            AiSemanticRegistry.EnsureInitialized();

            // Null → fallback
            if (string.IsNullOrWhiteSpace(type))
            {
                // If there's an aura, it's APPLY_AURA
                if (!string.IsNullOrWhiteSpace(aura))
                    return 6; // SPELL_EFFECT_APPLY_AURA

                return 2; // SCHOOL_DAMAGE fallback
            }

            string t = type.Trim().ToLowerInvariant();
            uint id;

            // ------------------------------------------------------------
            // 1) Semantic registry first (your master alias system)
            // ------------------------------------------------------------
            if (AiSemanticRegistry.TryResolveEffectId(type, aura, out id))
                return id;

            // ------------------------------------------------------------
            // 2) Weapon Damage explicit handling
            // ------------------------------------------------------------
            if (t == "weapondamage" || t == "applyweapondamage" || t == "meleedamage" || t == "swingdamage")
                return 1; // SPELL_EFFECT_WEAPON_DAMAGE

            // ------------------------------------------------------------
            // 3) Summon
            // ------------------------------------------------------------
            if (t == "summon")
                return 28; // SPELL_EFFECT_SUMMON

            // ------------------------------------------------------------
            // 4) Interrupt
            // ------------------------------------------------------------
            if (t == "interrupt")
                return 19; // SPELL_EFFECT_INTERRUPT_CAST

            // ------------------------------------------------------------
            // 5) Threat
            // ------------------------------------------------------------
            if (t == "threat")
                return 5; // SPELL_EFFECT_THREAT

            // ------------------------------------------------------------
            // 6) Default effect-name resolution from client enums
            // ------------------------------------------------------------
            if (AiEnumRegistry.EffectNameToId.TryGetValue(type.Trim(), out id))
                return id;

            // ------------------------------------------------------------
            // 7) Aura present → default APPLY_AURA
            // ------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(aura))
                return 6; // SPELL_EFFECT_APPLY_AURA

            // ------------------------------------------------------------
            // 8) Final fallback → SCHOOL_DAMAGE
            // ------------------------------------------------------------
            return 2; // SPELL_EFFECT_SCHOOL_DAMAGE
        }
        private static uint MapAura(string aura)
        {
            AiEnumRegistry.Initialize();
            AiSemanticRegistry.EnsureInitialized();

            if (string.IsNullOrWhiteSpace(aura))
                return 0;

            uint id;

            // 1) semantic registry
            if (AiSemanticRegistry.TryResolveAuraId(aura, out id))
                return id;

            // 2) direct lookup
            if (AiEnumRegistry.AuraNameToId.TryGetValue(aura.Trim(), out id))
                return id;

            // 3) not found → no aura
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


        private static uint? FindBestRuneCostId(int blood, int frost, int unholy)
        {
            var dbc = DBCManager.GetInstance().FindDbcForBinding("SpellRuneCost") as SpellRuneCost;
            if (dbc == null)
                return null;

            var boxes = dbc.GetAllBoxes();
            if (boxes == null || boxes.Count == 0)
                return null;

            foreach (var box in boxes)
            {
                // ID 0 is the default/no-cost row; skip it when searching.
                if (box.ID == 0)
                    continue;

                var name = box.Name ?? string.Empty;

                // Name format: "Cost {r1}, {r2}, {r3} Gain {runepowerGain} ID {id}"
                var idxCost = name.IndexOf("Cost ", StringComparison.OrdinalIgnoreCase);
                if (idxCost < 0)
                    continue;

                var afterCost = name.Substring(idxCost + "Cost ".Length);
                var idxGain = afterCost.IndexOf("Gain", StringComparison.OrdinalIgnoreCase);
                var costsPart = idxGain >= 0 ? afterCost.Substring(0, idxGain) : afterCost;

                var tokens = costsPart.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 3)
                    continue;

                if (!int.TryParse(tokens[0], out var c1)) continue;
                if (!int.TryParse(tokens[1], out var c2)) continue;
                if (!int.TryParse(tokens[2], out var c3)) continue;

                if (c1 == blood && c2 == frost && c3 == unholy)
                    return (uint)box.ID;
            }

            return null;
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
            if (!row.Table.Columns.Contains("SpellVisual1"))
                return;

            if (string.IsNullOrWhiteSpace(def.VisualName))
                return;

            uint visualId = PickBestVisualId(def.VisualName);
            if (visualId != 0)
                row["SpellVisual1"] = (int)visualId;

            if (def.VisualId.HasValue && row.Table.Columns.Contains("SpellVisual1"))
            {
                row["SpellVisual1"] = def.VisualId.Value;
            }
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

        private static void SafeSet(DataRow row, string col, object value)
        {
            if (row.Table.Columns.Contains(col))
                row[col] = value;
        }
    }
}
