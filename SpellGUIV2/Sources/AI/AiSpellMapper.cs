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

        /// <summary>
        /// Maps human-readable mechanic-name strings into WoW Mechanic enum IDs
        /// for use in EffectMiscValue for mechanic-based auras.
        /// </summary>
        private static readonly Dictionary<string, int> MechanicMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "charm",        1 },
            { "disorient",    2 },
            { "disarm",       3 },
            { "distract",     4 },
            { "fear",         5 },
            { "grip",         6 },
            { "root",         7 },
            { "silence",      8 },
            { "sleep",        9 },
            { "snare",        10 },
            { "stun",         11 },
            { "freeze",       12 },
            { "knockback",    13 },
            { "bleed",        15 },
            { "bandage",      16 },
            { "polymorph",    17 },
            { "banish",       18 },
            { "shield",       19 },
            { "shackle",      20 },
            { "mount",        21 },
            { "invisibility", 22 },
            { "interrupt",    24 },
            { "daze",         27 },
            { "sap",          28 },
            { "charge",       31 }
        };

        /// <summary>
        /// Maps user-friendly proc keywords to WotLK ProcFlags
        /// stored in EffectMiscValueB for proc auras.
        /// </summary>
        private static readonly Dictionary<string, uint> ProcFlagMap = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
        {
            { "hittaken",      0x00000001 }, // PROC_FLAG_TAKEN_MELEE_HIT
            { "crittaken",     0x00000002 }, // PROC_FLAG_TAKEN_MELEE_CRIT
            { "miss",          0x00000004 }, // PROC_FLAG_TAKEN_MELEE_MISS
            { "dodge",         0x00000008 }, // PROC_FLAG_TAKEN_MELEE_DODGE
            { "parry",         0x00000010 }, // PROC_FLAG_TAKEN_MELEE_PARRY
            { "block",         0x00000020 }, // PROC_FLAG_TAKEN_MELEE_BLOCK
            { "hit",           0x00000040 }, // PROC_FLAG_MELEE_HIT
            { "crit",          0x00000080 }, // PROC_FLAG_MELEE_CRIT

            { "rangedhit",     0x00000100 }, // PROC_FLAG_RANGED_HIT
            { "rangedcrit",    0x00000200 }, // PROC_FLAG_RANGED_CRIT

            { "spellhit",      0x00000400 }, // PROC_FLAG_SPELL_HIT
            { "spellcrit",     0x00000800 }, // PROC_FLAG_SPELL_CRIT

            { "periodic",      0x00001000 }, // PROC_FLAG_PERIODIC_DAMAGE
            { "heal",          0x00002000 }, // PROC_FLAG_PERIODIC_HEAL
            { "cast",          0x00004000 }, // PROC_FLAG_DONE_SPELL_CAST

            { "taken",         0x00008000 }, // PROC_FLAG_TAKEN_DAMAGE
            { "done",          0x00010000 }, // PROC_FLAG_DONE_DAMAGE
        };

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
                // AURA INFERENCE (if effect type is known aura-like)
                // --------------------------------------------------------
                if (eff.Aura == null && !string.IsNullOrWhiteSpace(eff.Type))
                {
                    string typeLower = eff.Type.Trim().ToLowerInvariant();
                    switch (typeLower)
                    {
                        // Hard CC
                        case "stun":
                            eff.Aura = "ModStun";
                            break;
                        case "root":
                        case "immobilize":
                            eff.Aura = "ModRoot";
                            break;
                        case "slow":
                            eff.Aura = "ModDecreaseSpeed";
                            break;
                        case "silence":
                            eff.Aura = "ModSilence";
                            break;
                        case "fear":
                            eff.Aura = "ModFear";
                            break;
                        case "charm":
                            eff.Aura = "ModCharm";
                            break;

                        // Periodic helpers
                        case "periodicdamage":
                        case "dot":
                            eff.Aura = "PeriodicDamage";
                            break;

                        case "periodicheal":
                        case "hot":
                            eff.Aura = "PeriodicHeal";
                            break;

                        // Generic buff / debuff
                        case "buff":
                            eff.Aura = "ModStat";          // generic stat buff
                            break;
                        case "debuff":
                            eff.Aura = "ModDamageTaken";   // generic debuff
                            break;

                        // Shields / absorbs
                        case "absorb":
                        case "absorbdamage":
                            eff.Aura = "SchoolAbsorb";
                            break;
                        case "damageshield":
                            eff.Aura = "DamageShield";
                            break;
                        case "manashield":
                            eff.Aura = "ManaShield";
                            break;

                        // Primary stats / rating-style helpers
                        case "modstat":
                        case "modstrength":
                        case "modagility":
                        case "modstamina":
                        case "modintellect":
                        case "modspirit":
                            eff.Aura = "ModStat";
                            break;

                        case "modattackpower":
                            eff.Aura = "ModAttackPower";
                            break;
                        case "modrangedattackpower":
                            eff.Aura = "ModRangedAttackPower";
                            break;
                        case "modspellpower":
                            eff.Aura = "ModSpellPower";
                            break;
                        case "modarmor":
                            eff.Aura = "ModArmor";
                            break;
                        case "modresistance":
                            eff.Aura = "ModResistance";
                            break;
                        case "moddamagedone":
                            eff.Aura = "ModDamageDone";
                            break;
                        case "moddamagetaken":
                            eff.Aura = "ModDamageTaken";
                            break;
                        case "modcritchance":
                            eff.Aura = "ModCritChance";
                            break;
                        case "modhaste":
                            eff.Aura = "ModHaste";
                            break;
                        case "modhit":
                        case "modhitchance":
                            eff.Aura = "ModHitChance";
                            break;

                        // Regen helpers
                        case "modmanaregen":
                            eff.Aura = "ModManaRegen";
                            break;
                        case "modhealthregen":
                            eff.Aura = "ModHealthRegen";
                            break;

                        // Movement speed
                        case "modincreasespeed":
                        case "speedincrease":
                            eff.Aura = "ModIncreaseSpeed";
                            break;
                        case "moddecreasespeed":
                        case "speeddecrease":
                            eff.Aura = "ModDecreaseSpeed";
                            break;

                        // --- combat ratings & extended buffs ---

                        // Combat ratings (defensive)
                        case "moddodgepercent":
                            eff.Aura = "ModDodgePercent";
                            break;
                        case "modparrypercent":
                            eff.Aura = "ModParryPercent";
                            break;
                        case "modblockpercent":
                            eff.Aura = "ModBlockPercent";
                            break;
                        case "modblockvalue":
                            eff.Aura = "ModBlockValue";
                            break;

                        // Offensive ratings
                        case "modarmorpenetration":
                            eff.Aura = "ModArmorPenetration";
                            break;
                        case "modexpertise":
                            eff.Aura = "ModExpertise";
                            break;

                        // Damage / healing bonuses
                        case "modspelldamagedone":
                            eff.Aura = "ModSpellDamageDone";
                            break;
                        case "moddamagepercentdone":
                            eff.Aura = "ModDamagePercentDone";
                            break;
                        case "modhealingdone":
                            eff.Aura = "ModHealingDone";
                            break;
                        case "modhealingtakenpct":
                            eff.Aura = "ModHealingTakenPct";
                            break;
                        case "moddamagetakenpct":
                            eff.Aura = "ModDamageTakenPct";
                            break;

                        // Utility / movement / misc
                        case "modswimspeed":
                            eff.Aura = "ModSwimSpeed";
                            break;
                        case "modflightspeed":
                            eff.Aura = "ModFlightSpeed";
                            break;
                        case "modmountedspeedpct":
                        case "modmountspeed":
                            eff.Aura = "ModMountedSpeedPct";
                            break;
                        case "waterbreathing":
                            eff.Aura = "WaterBreathing";
                            break;
                        case "waterwalking":
                            eff.Aura = "WaterWalking";
                            break;
                        case "levitate":
                            eff.Aura = "Levitate";
                            break;
                        case "feigndeath":
                            eff.Aura = "FeignDeath";
                            break;

                        // Mechanic immunities / mechanic modifiers
                        case "stunimmune":
                            eff.Aura = "ModMechanicImmune";
                            eff.MiscValue = MechanicMap["stun"];
                            break;

                        case "fearimmune":
                            eff.Aura = "ModMechanicImmune";
                            eff.MiscValue = MechanicMap["fear"];
                            break;

                        case "rootimmune":
                            eff.Aura = "ModMechanicImmune";
                            eff.MiscValue = MechanicMap["root"];
                            break;

                        case "silenceimmune":
                            eff.Aura = "ModMechanicImmune";
                            eff.MiscValue = MechanicMap["silence"];
                            break;

                        case "knockbackimmune":
                            eff.Aura = "ModMechanicImmune";
                            eff.MiscValue = MechanicMap["knockback"];
                            break;

                        // Duration modifiers
                        case "stunduration":
                            eff.Aura = "ModMechanicDuration";
                            eff.MiscValue = MechanicMap["stun"];
                            break;

                        case "fearduration":
                            eff.Aura = "ModMechanicDuration";
                            eff.MiscValue = MechanicMap["fear"];
                            break;

                        case "rootduration":
                            eff.Aura = "ModMechanicDuration";
                            eff.MiscValue = MechanicMap["root"];
                            break;

                        // Damage taken modifiers by mechanic
                        case "damagefromstun":
                            eff.Aura = "ModMechanicDamageTaken";
                            eff.MiscValue = MechanicMap["stun"];
                            break;

                        case "damagefromfear":
                            eff.Aura = "ModMechanicDamageTaken";
                            eff.MiscValue = MechanicMap["fear"];
                            break;

                        // --------------------------------------------------------
                        // DAMAGE REDIRECTION / SPLIT DAMAGE
                        // --------------------------------------------------------
                        case "splitdamage":
                        case "redirectdamage":
                            // Base aura: redirect damage (SPELL_AURA_SPLIT_DAMAGE_PCT = 73)
                            // No conflict with existing auras; your code will auto-apply APPLY_AURA effect.
                            eff.Aura = "SplitDamagePct";

                            // If the AI specifies a school, we let ComputeEffectMiscValue map it.
                            // If not, default to FullMask.
                            if (!eff.MiscValue.HasValue)
                                eff.MiscValue = 127; // SpellSchoolMask.All

                            break;

                        // --------------------------------------------------------
                        // GROUP / RAID BUFFS
                        // --------------------------------------------------------
                        case "increasehealthpercent":
                        case "incmaxhealthpct":
                            eff.Aura = "ModIncreaseHealthPercent";
                            break;

                        case "increasemaxhealth":
                            eff.Aura = "ModIncreaseMaxHealth";
                            break;

                        case "increasemana":
                        case "increasemaxmana":
                            eff.Aura = "ModIncreaseMaxPower";
                            eff.MiscValue = 0;
                            break;

                        case "increaseenergy":
                            eff.Aura = "ModIncreaseMaxPower";
                            eff.MiscValue = 3;
                            break;

                        case "increasespirit":
                            eff.Aura = "ModStat";
                            eff.MiscValue = 4;
                            break;

                        case "increasestamina":
                            eff.Aura = "ModStat";
                            eff.MiscValue =2;
                            break;

                        case "increaseintellect":
                            eff.Aura = "ModStat";
                            eff.MiscValue = 3;
                            break;

                        case "increaseagility":
                            eff.Aura = "ModStat";
                            eff.MiscValue = 1;
                            break;

                        case "increasestrength":
                            eff.Aura = "ModStat";
                            eff.MiscValue = 0;
                            break;


                        // --------------------------------------------------------
                        // PROC AURAS (AI Type → aura + proc mask)
                        // --------------------------------------------------------
                        case "proconhit":
                            eff.Aura = "ProcTriggerSpell";
                            eff.MiscValueB = (int)ProcFlagMap["hit"];
                            break;

                        case "proconcrit":
                            eff.Aura = "ProcTriggerSpell";
                            eff.MiscValueB = (int)ProcFlagMap["crit"];
                            break;

                        case "procondodge":
                            eff.Aura = "ProcTriggerSpell";
                            eff.MiscValueB = (int)ProcFlagMap["dodge"];
                            break;

                        case "proconparry":
                            eff.Aura = "ProcTriggerSpell";
                            eff.MiscValueB = (int)ProcFlagMap["parry"];
                            break;

                        case "proconblock":
                            eff.Aura = "ProcTriggerSpell";
                            eff.MiscValueB = (int)ProcFlagMap["block"];
                            break;

                        case "proconspellhit":
                            eff.Aura = "ProcTriggerSpell";
                            eff.MiscValueB = (int)ProcFlagMap["spellhit"];
                            break;

                        case "proconspellcrit":
                            eff.Aura = "ProcTriggerSpell";
                            eff.MiscValueB = (int)ProcFlagMap["spellcrit"];
                            break;

                        case "proconhittaken":
                            eff.Aura = "ProcTriggerSpell";
                            eff.MiscValueB = (int)ProcFlagMap["hittaken"];
                            break;

                        case "proconcrittaken":
                            eff.Aura = "ProcTriggerSpell";
                            eff.MiscValueB = (int)ProcFlagMap["crittaken"];
                            break;

                        case "proconperiodic":
                            eff.Aura = "ProcTriggerSpell";
                            eff.MiscValueB = (int)ProcFlagMap["periodic"];
                            break;

                        case "proconcast":
                            eff.Aura = "ProcTriggerSpell";
                            eff.MiscValueB = (int)ProcFlagMap["cast"];
                            break;

                        // --------------------------------------------------------
                        // DISPEL (DispelType-based removal)
                        // --------------------------------------------------------
                        case "dispelmagic":
                            eff.Type = "Dispel";
                            eff.MiscValue = 1; // Magic
                            break;

                        case "dispelcurse":
                            eff.Type = "Dispel";
                            eff.MiscValue = 2; // Curse
                            break;

                        case "dispeldisease":
                            eff.Type = "Dispel";
                            eff.MiscValue = 3; // Disease
                            break;

                        case "dispelpoison":
                            eff.Type = "Dispel";
                            eff.MiscValue = 4; // Poison
                            break;

                        case "dispelenrage":
                            eff.Type = "Dispel";
                            eff.MiscValue = 7; // Enrage (WotLK uses 7)
                            break;

                        // --------------------------------------------------------
                        // DISPEL MECHANIC (removes specific CC categories)
                        // --------------------------------------------------------
                        case "dispelstun":
                            eff.Type = "DispelMechanic";
                            eff.MiscValue = MechanicMap["stun"];
                            break;

                        case "dispelfear":
                            eff.Type = "DispelMechanic";
                            eff.MiscValue = MechanicMap["fear"];
                            break;

                        case "dispelroot":
                            eff.Type = "DispelMechanic";
                            eff.MiscValue = MechanicMap["root"];
                            break;

                        case "dispelsilence":
                            eff.Type = "DispelMechanic";
                            eff.MiscValue = MechanicMap["silence"];
                            break;

                        case "dispelsnare":
                            eff.Type = "DispelMechanic";
                            eff.MiscValue = MechanicMap["snare"];
                            break;

                        // --------------------------------------------------------
                        // SCHOOL IMMUNITY AURAS
                        // --------------------------------------------------------
                        case "immunefire":
                            eff.Aura = "ModSchoolImmunity";
                            eff.MiscValue = (int)MapSchool("Fire");
                            break;

                        case "immunefrost":
                            eff.Aura = "ModSchoolImmunity";
                            eff.MiscValue = (int)MapSchool("Frost");
                            break;

                        case "immunenature":
                            eff.Aura = "ModSchoolImmunity";
                            eff.MiscValue = (int)MapSchool("Nature");
                            break;

                        case "immuneholy":
                            eff.Aura = "ModSchoolImmunity";
                            eff.MiscValue = (int)MapSchool("Holy");
                            break;

                        case "immuneshadow":
                            eff.Aura = "ModSchoolImmunity";
                            eff.MiscValue = (int)MapSchool("Shadow");
                            break;

                        case "immunearcane":
                            eff.Aura = "ModSchoolImmunity";
                            eff.MiscValue = (int)MapSchool("Arcane");
                            break;

                        case "immunephysical":
                            eff.Aura = "ModSchoolImmunity";
                            eff.MiscValue = (int)MapSchool("Physical");
                            break;

                        // --------------------------------------------------------
                        // TriggerSpell aura helpers (Type-level hinting)
                        // --------------------------------------------------------
                        case "triggerspell":
                        case "trigger":
                        case "cast":
                            // Usually paired with TriggerSpellId
                            eff.Type = "TriggerSpell";
                            // No aura needed (effect does the work)
                            break;

                        // --------------------------------------------------------
                        // Dummy behavior helpers
                        // --------------------------------------------------------
                        case "dummy":
                        case "script":
                        case "vehicle":
                            eff.Type = "Dummy";
                            // no aura, effect = DUMMY
                            break;

                        case "triggerfire":
                            eff.Type = "TriggerSpell";
                            break;

                        // --------------------------------------------------------
                        // AREA AURA INFERENCE
                        // --------------------------------------------------------
                        case "areaauraparty":
                            eff.Type = "AreaAuraParty";
                            if (string.IsNullOrWhiteSpace(eff.Target))
                                eff.Target = "Area";
                            // Aura must be provided explicitly by the AI OR via registry
                            break;

                        case "areaaurafriend":
                            eff.Type = "AreaAuraFriend";
                            if (string.IsNullOrWhiteSpace(eff.Target))
                                eff.Target = "Area";
                            break;

                        case "areaauraenemy":
                            eff.Type = "AreaAuraEnemy";
                            if (string.IsNullOrWhiteSpace(eff.Target))
                                eff.Target = "Area";
                            break;

                        case "areaaurapet":
                            eff.Type = "AreaAuraPet";
                            if (string.IsNullOrWhiteSpace(eff.Target))
                                eff.Target = "Area";
                            break;

                        case "areaauraowner":
                            eff.Type = "AreaAuraOwner";
                            if (string.IsNullOrWhiteSpace(eff.Target))
                                eff.Target = "Area";
                            break;

                        case "areaauraraid":
                            eff.Type = "AreaAuraRaid";
                            if (string.IsNullOrWhiteSpace(eff.Target))
                                eff.Target = "Area";
                            break;

                        // --------------------------------------------------------
                        // THREAT AURAS
                        // --------------------------------------------------------
                        case "modthreat":
                            eff.Aura = "ModThreat";
                            break;

                        case "modthreatpct":
                            eff.Aura = "ModThreatPercent";
                            break;

                        // --------------------------------------------------------
                        // PUSHBACK / CASTING PROTECTION
                        // --------------------------------------------------------
                        case "pushbackreduction":
                            eff.Aura = "ModPushback";
                            break;

                        case "interruptresist":
                            eff.Aura = "ModCastingSpeedNotStack";
                            break;

                        // --------------------------------------------------------
                        // SPELL REFLECTION
                        // --------------------------------------------------------
                        case "reflectspell":
                            eff.Aura = "ModSpellReflection";
                            eff.MiscValue = 127; // SpellSchoolMask.All;
                            break;

                        case "reflectschool":
                            eff.Aura = "ModSpellReflection";
                            // MiscValue derived from School
                            break;

                        // --------------------------------------------------------
                        // POWER BURN / PERIODIC LEECH
                        // --------------------------------------------------------
                        case "periodicleech":
                            eff.Aura = "PeriodicLeech";
                            break;

                        case "powerburn":
                            eff.Aura = "PeriodicPowerBurn";
                            break;

                        // --------------------------------------------------------
                        // STEALTH / INVISIBILITY
                        // --------------------------------------------------------
                        case "stealth":
                            eff.Aura = "ModStealth";
                            break;

                        case "stealthlevel":
                            eff.Aura = "ModStealthLevel";
                            break;

                        case "detect":
                            eff.Aura = "ModDetect";
                            break;

                        case "invisible":
                            eff.Aura = "ModInvisibility";
                            break;

                        case "invisibilitydetect":
                            eff.Aura = "ModInvisibilityDetection";
                            break;

                        // --------------------------------------------------------
                        // TRACKING + SHAPESHIFT
                        // --------------------------------------------------------
                        case "trackcreatures":
                            eff.Aura = "TrackCreatures";
                            break;

                        case "trackresources":
                            eff.Aura = "TrackResources";
                            break;

                        case "shapeshift":
                            eff.Aura = "ModShapeshift";
                            break;

                        // --------------------------------------------------------
                        // TALENT DAMAGE MODIFIERS
                        // --------------------------------------------------------
                        case "modcritdamage":
                            eff.Aura = "ModCritDamageBonus";
                            break;

                        case "modspellcritdamage":
                            eff.Aura = "ModSpellCritDamageBonus";
                            break;

                        case "modoffhanddamagepct":
                            eff.Aura = "ModOffhandDamagePercent";
                            break;

                        // --------------------------------------------------------
                        // WEAPON SKILL
                        // --------------------------------------------------------
                        case "modweaponskill":
                            eff.Aura = "ModWeaponSkill";
                            break;

                        case "modweaponcritpercent":
                            eff.Aura = "ModWeaponCriticalPercent";
                            break;

                        // --------------------------------------------------------
                        // PET / VEHICLE CONTROL
                        // --------------------------------------------------------
                        case "controlpet":
                            eff.Aura = "ControlPet";
                            break;

                        case "modpetdamage":
                            eff.Aura = "ModPetDamageDone";
                            break;

                        case "modpetspeed":
                            eff.Aura = "ModVehicleSpeed";
                            break;

                        case "modpetpower":
                            eff.Aura = "ModVehiclePower";
                            break;

                        // --------------------------------------------------------
                        // ENRAGE / RAGE GENERATION
                        // --------------------------------------------------------
                        case "enrage":
                            eff.Aura = "ModRageFromDamageTaken";
                            break;

                        case "modragegeneration":
                            eff.Aura = "ModRageGeneration";
                            break;

                        // --------------------------------------------------------
                        // MAX HEALTH / POWER MODS
                        // --------------------------------------------------------
                        case "increasehealthflat":
                            eff.Aura = "IncreaseMaxHealth";
                            break;

                        case "increasemanaflat":
                            eff.Aura = "IncreaseMaxPower";
                            eff.MiscValue = 0;
                            break;

                        // --------------------------------------------------------
                        // PROC AURAS – EXTENDED SET
                        // --------------------------------------------------------
                        case "proctriggerdamage":
                            eff.Aura = "ProcTriggerDamage";
                            break;

                        case "proctriggerspellwithvalue":
                            eff.Aura = "ProcTriggerSpellWithValue";
                            break;

                        case "procevent":
                            eff.Aura = "ProcEvent";
                            break;

                        case "proccopy":
                        case "proctriggerspellcopy":
                            eff.Aura = "ProcTriggerSpellCopy";
                            break;

                        // --------------------------------------------------------
                        // TALENT / CREATURE FAMILY DAMAGE MODIFIERS
                        // --------------------------------------------------------
                        case "moddamagevscreature":
                            eff.Aura = "ModDamageDoneVersusCreature";
                            break;

                        case "modcritvscreature":
                            eff.Aura = "ModCritPercentVersusCreature";
                            break;

                        case "modspelldamagevscreature":
                            eff.Aura = "ModSpellDamageVersusCreature";
                            break;

                        case "modrangedap":
                            eff.Aura = "ModRangedAttackPower";
                            break;

                        case "modrangedappct":
                            eff.Aura = "ModRangedAttackPowerPercent";
                            break;

                        // --------------------------------------------------------
                        // DAMAGE SHIELD (DIRECT / SCHOOL-BASED)
                        // --------------------------------------------------------
                        case "damageshieldschool":
                            eff.Aura = "DamageShieldSchool";
                            // School mask via ComputeEffectMiscValue
                            break;

                        case "procdamageshield":
                            eff.Aura = "ProcDamageShield";
                            break;

                        // --------------------------------------------------------
                        // VEHICLE / POSSESSION / TOTEM HANDLERS
                        // --------------------------------------------------------
                        case "controlvehicle":
                            eff.Aura = "ControlVehicle";
                            break;

                        case "ridevehicle":
                            eff.Aura = "RideVehicle";
                            break;

                        case "possess":
                            eff.Aura = "Possess";
                            break;

                        case "totemearth":
                            eff.Aura = "TotemEffectEarth";
                            break;

                        case "totemair":
                            eff.Aura = "TotemEffectAir";
                            break;

                        case "totemfire":
                            eff.Aura = "TotemEffectFire";
                            break;

                        case "totemwater":
                            eff.Aura = "TotemEffectWater";
                            break;

                        // --------------------------------------------------------
                        // HEAL OVER TIME / INTERRUPT / CAST PROTECTION AURAS
                        // --------------------------------------------------------
                        case "interruptregen":
                            eff.Aura = "InterruptRegen";
                            break;

                        case "intervalheal":
                            eff.Aura = "PeriodicIntervalHeal";
                            break;

                        // --------------------------------------------------------
                        // TELEPORT + MOVEMENT AURAS
                        // --------------------------------------------------------
                        case "teleportaura":
                            eff.Aura = "ModTeleport";
                            break;

                        case "slowfall":
                            eff.Aura = "FeatherFall";
                            break;

                        // --------------------------------------------------------
                        // IMMUNITY / SANCTUARY
                        // --------------------------------------------------------
                        case "sanctuary":
                            eff.Aura = "Sanctuary";
                            break;

                        // --------------------------------------------------------
                        // PERSISTENT AREA AURAS
                        // --------------------------------------------------------
                        case "persistentareaaura":
                        case "groundaoe":
                            // Aura applied by persistent area effect (Blizzard-like)
                            eff.Aura = "PeriodicDummy";
                            break;

                        // --------------------------------------------------------
                        // SCRIPT-RELATED AURAS
                        // --------------------------------------------------------
                        case "scriptaura":
                        case "scriptstate":
                            eff.Aura = "PeriodicTriggerSpell";
                            break;

                        // --------------------------------------------------------
                        // ENCHANTING / ITEM BUFF AURAS
                        // --------------------------------------------------------
                        case "itemenchant":
                        case "enchantbuff":
                            eff.Aura = "EnchantItemTemp";
                            break;

                        // --------------------------------------------------------
                        // SKINNING / HARVEST AURAS
                        // --------------------------------------------------------
                        case "harvest":
                        case "skinningaura":
                            eff.Aura = "ModSkinning";
                            break;

                        // --------------------------------------------------------
                        // DISMISS / CONTROL AURAS
                        // --------------------------------------------------------
                        case "dismisspet":
                        case "unsummonpet":
                            eff.Aura = "DismissPet";
                            break;

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

                // Persistent area effects sometimes need an amplitude.
                // If it's a periodic aura without amplitude, default to 1000ms.
                if (eff.Type != null && eff.Type.Contains("persistent"))
                {
                    if (!ampSec.HasValue)
                        SafeSet(row, $"EffectAmplitude{slot}", 1000);
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
            // 0) Explicit overrides from AI definition -----------------------------
            if (eff != null)
            {
                // If the AI explicitly set MiscValue, use it as-is.
                if (eff.MiscValue.HasValue)
                    return eff.MiscValue.Value;

                // Summon / kill-credit style effects: allow CreatureId to drive MiscValue.
                // For example:
                //  - SPELL_EFFECT_SUMMON / SPELL_EFFECT_SUMMON_TYPE / etc.
                //  - SPELL_EFFECT_KILL_CREDIT (misc = creature entry)
                if (eff.CreatureId.HasValue)
                    return eff.CreatureId.Value;
            }

            string aura = eff?.Aura ?? string.Empty;
            string type = eff?.Type ?? string.Empty;

            string auraLower = aura.Trim().ToLowerInvariant();
            string typeLower = type.Trim().ToLowerInvariant();

            // 1) Power-type based effects / auras:
            //    - SPELL_EFFECT_ENERGIZE / POWER_DRAIN / POWER_LEECH-style
            //    - SPELL_AURA_PERIODIC_ENERGIZE / PERIODIC_*_LEECH
            //    These use MiscValue as a Powers enum.
            if (!string.IsNullOrWhiteSpace(def.PowerType))
            {
                bool isPowerEffect =
                    typeLower == "energize" ||
                    typeLower == "powerburn" ||
                    typeLower == "drainmana" ||
                    typeLower == "leechmana" ||
                    typeLower == "drainhealth" ||
                    typeLower == "leechhealth";

                bool isPowerAura =
                    auraLower == "periodicenergize" ||
                    auraLower == "periodicmanaleech" ||
                    auraLower == "periodichealthleech" ||
                    auraLower == "periodicleech" ||
                    auraLower == "powerburn" ||
                    auraLower == "obsmodpower";

                if (isPowerEffect || isPowerAura)
                {
                    var powerMisc = MapPowerTypeToMisc(def.PowerType);
                    if (powerMisc.HasValue)
                        return powerMisc.Value;
                }
            }

            // 2) Stat auras: primary stats and "all stats"
            //    MiscValue is a Stats enum index:
            //      0 = STR, 1 = AGI, 2 = STA, 3 = INT, 4 = SPI, -1 = all stats
            if (!string.IsNullOrEmpty(auraLower))
            {
                switch (auraLower)
                {
                    case "modstrength": return 0; // STAT_STRENGTH
                    case "modagility": return 1; // STAT_AGILITY
                    case "modstamina": return 2; // STAT_STAMINA
                    case "modintellect": return 3; // STAT_INTELLECT
                    case "modspirit": return 4; // STAT_SPIRIT
                }

                if (auraLower == "modstat" || auraLower == "modtotalstatpercentage")
                    return -1; // all stats
            }

            // 3) School-mask based auras: use the spell's SchoolMask as MiscValue.
            //    These use MiscValue as a SpellSchoolMask in SpellAuraEffects.cpp:
            //      - ModSchoolMaskDamage / ModSchoolMaskResistance
            //      - DamageShield / SchoolAbsorb / Absorb* / TotalAbsorb / PeriodicAbsorb
            //      - Some resistance auras using school masks
            //      - ModSpellDamageDone / ModDamagePercentDone (school-based bonuses)
            if (!string.IsNullOrWhiteSpace(def.School) && !string.IsNullOrEmpty(auraLower))
            {
                bool isSchoolDamageOrAbsorb =
                    auraLower == "periodicdamage" ||
                    auraLower == "schooldamage" ||
                    auraLower == "schoolabsorb" ||
                    auraLower == "periodicabsorb" ||
                    auraLower == "absorbdamage" ||
                    auraLower == "absorbmagic" ||
                    auraLower == "absorbschool" ||
                    auraLower == "damageshield" ||
                    auraLower == "totalabsorb" ||
                    auraLower == "modschoolmaskdamage" ||
                    auraLower == "modschoolmaskresistance" ||
                    auraLower == "modspelldamagedone" ||
                    auraLower == "moddamagepercentdone";

                bool isSchoolResist =
                    auraLower == "modresistance" ||
                    auraLower == "modbaseresistance" ||
                    auraLower == "modresistanceexclusive" ||
                    auraLower == "modbaseresistancepct";

                if (isSchoolDamageOrAbsorb || isSchoolResist)
                    return (int)MapSchool(def.School);
            }

            // -----------------------------------------------------------
            // 4) Mechanic-based auras
            // -----------------------------------------------------------

            if (!string.IsNullOrEmpty(auraLower))
            {
                if (auraLower == "modmechanicimmune" ||
                    auraLower == "modmechanicduration" ||
                    auraLower == "modmechanicdamagetaken")
                {
                    // If eff.MiscValue was explicitly set by inference, use that.
                    if (eff.MiscValue.HasValue)
                        return eff.MiscValue.Value;

                    // Otherwise, attempt to auto-map based on Type
                    // ("stun", "fear", "root", "silence", etc)
                    if (!string.IsNullOrEmpty(typeLower))
                    {
                        foreach (var kvp in MechanicMap)
                        {
                            if (typeLower.Contains(kvp.Key))
                                return kvp.Value;
                        }
                    }

                    // No mechanic could be inferred → return null
                    return null;
                }
            }

            // -----------------------------------------------------------
            // 5) Dispel (Effect = DISPEL)
            //     MiscValue = DispelType (Magic/Curse/Disease/Poison/Enrage)
            // -----------------------------------------------------------
            if (typeLower == "dispel" && eff.MiscValue.HasValue)
            {
                return eff.MiscValue.Value;
            }

            // -----------------------------------------------------------
            // 6) DispelMechanic (Effect = DISPEL_MECHANIC)
            //     MiscValue = MechanicID
            // -----------------------------------------------------------
            if (typeLower == "dispelmechanic" && eff.MiscValue.HasValue)
            {
                return eff.MiscValue.Value;
            }

            // -----------------------------------------------------------
            // 7) School Immunity (ModSchoolImmunity)
            //     MiscValue = school mask
            // -----------------------------------------------------------
            if (auraLower == "modschoolimmunity")
            {
                // If manually provided (via inference above), use it.
                if (eff.MiscValue.HasValue)
                    return eff.MiscValue.Value;

                // If not provided, attempt to derive from spell School
                if (!string.IsNullOrWhiteSpace(def.School))
                    return (int)MapSchool(def.School);
            }

            // -----------------------------------------------------------
            // 8) Damage redirection (SplitDamagePct)
            //     MiscValue = SchoolMask to redirect
            // -----------------------------------------------------------
            if (auraLower == "splitdamagepct")
            {
                // If inference already set a value, use it
                if (eff.MiscValue.HasValue)
                    return eff.MiscValue.Value;

                // Otherwise derive from spell's School if present
                if (!string.IsNullOrWhiteSpace(def.School))
                    return (int)MapSchool(def.School);

                // Default: ALL schools
                return 127; // SpellSchoolMask.All
            }

            if (auraLower == "periodicpowerburn")
            {
                return MapPowerTypeToMisc(def.PowerType);
            }


            // Unknown, no misc
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
            // --------------------------------------------------------------------
            // 0) Explicit override from the AI (always wins)
            // --------------------------------------------------------------------
            if (eff.MiscValueB.HasValue)
                return eff.MiscValueB.Value;

            string typeLower = eff.Type?.Trim().ToLowerInvariant() ?? "";
            string auraLower = eff.Aura?.Trim().ToLowerInvariant() ?? "";

            // --------------------------------------------------------------------
            // 1) PROC AURAS (ProcTriggerSpell)
            //     → EffectMiscValueB = ProcFlag mask
            // --------------------------------------------------------------------
            if (auraLower == "proctriggerspell")
            {
                // If TYPE inference already set a proc flag into MiscValueB, keep it.
                foreach (var kvp in ProcFlagMap)
                {
                    if (typeLower.Contains(kvp.Key))
                        return (int)kvp.Value;
                }

                // No inference possible → silently return null
                return null;
            }

            // --------------------------------------------------------------------
            // 2) POWER DRAINS / LEECHES / ENERGIZE
            //     Some of these effects historically use both MiscValue and MiscValueB
            //     as the PowerType (ex: drain mana = power 0).
            // --------------------------------------------------------------------
            if (typeLower == "drainmana" ||
                typeLower == "leechmana" ||
                typeLower == "energize" ||
                typeLower == "drainhealth" ||
                typeLower == "leechhealth")
            {
                // Return power-type as MiscValueB
                return MapPowerTypeToMisc(def.PowerType);
            }

            // periodic power effects (auras)
            if (auraLower == "periodicenergize" ||
                auraLower == "periodicmanaleech" ||
                auraLower == "periodicleech" ||
                auraLower == "obsmodpower")
            {
                return MapPowerTypeToMisc(def.PowerType);
            }

            // --------------------------------------------------------------------
            // 3) Nothing else requires MiscValueB
            // --------------------------------------------------------------------
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

            // Null / empty → simple fallbacks
            if (string.IsNullOrWhiteSpace(type))
            {
                // If there's an aura, it's APPLY_AURA
                if (!string.IsNullOrWhiteSpace(aura))
                    return 6; // SPELL_EFFECT_APPLY_AURA

                // Otherwise assume generic SCHOOL_DAMAGE
                return 2; // SPELL_EFFECT_SCHOOL_DAMAGE
            }

            string t = type.Trim().ToLowerInvariant();
            uint id;

            // ------------------------------------------------------------
            // 1) Semantic registry first (master alias system)
            // ------------------------------------------------------------
            if (AiSemanticRegistry.TryResolveEffectId(type, aura, out id))
                return id;

            // ------------------------------------------------------------
            // 2) Weapon Damage explicit handling
            // ------------------------------------------------------------
            if (t == "weapondamage" ||
                t == "applyweapondamage" ||
                t == "meleedamage" ||
                t == "swingdamage")
            {
                return 1; // SPELL_EFFECT_WEAPON_DAMAGE
            }

            // ------------------------------------------------------------
            // 3) Summon family
            //    We try to hit the enum names if they exist, otherwise
            //    fall back to the generic SUMMON (28).
            // ------------------------------------------------------------

            // Generic summon (creature/guardian/etc.)
            if (t == "summon" || t == "summoncreature" || t == "summonunit")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON", out id))
                    return id;

                return 28; // SPELL_EFFECT_SUMMON (safe WotLK value)
            }

            // Pet / demon
            if (t == "summonpet" || t == "callpet" || t == "summondemon")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_PET", out id))
                    return id;

                // Fallback: generic summon
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON", out id))
                    return id;

                return 28;
            }

            // Guardian / companion
            if (t == "summonguardian" || t == "summoncompanion")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_GUARDIAN", out id))
                    return id;

                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON", out id))
                    return id;

                return 28;
            }

            // Wild summon (temporary guardian in many DBs)
            if (t == "summonwild" || t == "summonwildguardian")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_WILD", out id))
                    return id;

                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_GUARDIAN", out id))
                    return id;

                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON", out id))
                    return id;

                return 28;
            }

            // Totems – use SLOT1-4 variants when explicitly requested
            if (t == "summontotem" || t == "summontotem1")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_TOTEM_SLOT1", out id))
                    return id;
            }

            if (t == "summontotem2")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_TOTEM_SLOT2", out id))
                    return id;
            }

            if (t == "summontotem3")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_TOTEM_SLOT3", out id))
                    return id;
            }

            if (t == "summontotem4")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_TOTEM_SLOT4", out id))
                    return id;
            }

            // Generic "totem" alias → try slot 1, then generic SUMMON
            if (t == "totem")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_TOTEM_SLOT1", out id))
                    return id;

                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON", out id))
                    return id;

                return 28;
            }

            // Vehicles
            if (t == "summonvehicle" || t == "vehicle")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_VEHICLE", out id))
                    return id;

                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON", out id))
                    return id;

                return 28;
            }

            // ------------------------------------------------------------
            // 4) Teleports / Blink
            // ------------------------------------------------------------
            if (t == "teleport" ||
                t == "teleportself" ||
                t == "teleporttarget" ||
                t == "blink")
            {
                // Standard WotLK effect name is TELEPORT_UNITS
                if (AiEnumRegistry.EffectNameToId.TryGetValue("TELEPORT_UNITS", out id))
                    return id;
            }

            // ------------------------------------------------------------
            // 5) Interrupt
            // ------------------------------------------------------------
            if (t == "interrupt" || t == "interruptcast")
            {
                // If the DB enum exists, prefer that
                if (AiEnumRegistry.EffectNameToId.TryGetValue("INTERRUPT_CAST", out id))
                    return id;

                return 19; // SPELL_EFFECT_INTERRUPT_CAST (known WotLK value)
            }

            // ------------------------------------------------------------
            // 6) Threat
            // ------------------------------------------------------------
            if (t == "threat" || t == "addthreat" || t == "modthreat")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("THREAT", out id))
                    return id;

                return 5; // SPELL_EFFECT_THREAT
            }

            // ------------------------------------------------------------
            // 7) Kill credit (quest helpers)
            // ------------------------------------------------------------
            if (t == "killcredit" || t == "killcreditpersonal" || t == "credit")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("KILL_CREDIT", out id))
                    return id;

                if (AiEnumRegistry.EffectNameToId.TryGetValue("KILL_CREDIT2", out id))
                    return id;
            }

            // ------------------------------------------------------------
            // 8) Open Lock / chest interaction
            // ------------------------------------------------------------
            if (t == "openlock" || t == "unlock" || t == "picklock")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("OPEN_LOCK", out id))
                    return id;
            }

            // Jump movement
            if (t == "jump" || t == "jumpforward")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("JUMP", out id))
                    return id;
            }

            // Jump to destination (often teleport-like)
            if (t == "jumpdest" || t == "jumpdestination")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("JUMP_DEST", out id))
                    return id;
            }

            // Teleport Player / Teleport Graveyard
            if (t == "teleportplayer")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("TELEPORT_PLAYER", out id))
                    return id;
            }

            if (t == "teleportgraveyard" || t == "graveteleport")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("TELEPORT_GRAVEYARD", out id))
                    return id;
            }

            // Bind location
            if (t == "bind" || t == "sethearth" || t == "bindlocation")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("BIND", out id))
                    return id;
            }

            // Self-resurrect
            if (t == "selfres" || t == "selfresurrect")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SELF_RESURRECT", out id))
                    return id;
            }

            // Script Effect (non-dummy)
            if (t == "scripteffect" || t == "scriptspecial")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SCRIPT_EFFECT", out id))
                    return id;
            }

            // ------------------------------------------------------------
            // Niche and advanced EFFECT types
            // ------------------------------------------------------------

            // Charge (Warrior charge/intercept)
            if (t == "charge" || t == "chargemove" || t == "intercept")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("CHARGE", out id))
                    return id;
            }

            // Charge destination movement (Death Grip jump)
            if (t == "chargedest" || t == "chargejump")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("CHARGE_DEST", out id))
                    return id;
            }

            // Jump / JumpDest
            if (t == "jump" || t == "jumpforward")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("JUMP", out id))
                    return id;
            }
            if (t == "jumpdest" || t == "jumpdestination")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("JUMP_DEST", out id))
                    return id;
            }

            // TeleportPlayer
            if (t == "teleportplayer")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("TELEPORT_PLAYER", out id))
                    return id;
            }

            // TeleportGraveyard
            if (t == "teleportgraveyard" || t == "graveteleport")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("TELEPORT_GRAVEYARD", out id))
                    return id;
            }

            // Persistent Area Aura (e.g. Blizzard, Rain of Fire)
            if (t == "persistentarea" || t == "persistentareaaura")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("PERSISTENT_AREA_AURA", out id))
                    return id;
            }

            // ScriptEffect (server executes SpellScript effect)
            if (t == "scripteffect" || t == "script")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SCRIPT_EFFECT", out id))
                    return id;
            }

            // Create Item
            if (t == "createitem" || t == "makeitem")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("CREATE_ITEM", out id))
                    return id;
            }

            // Enchant Item
            if (t == "enchantitem" || t == "applyenchant")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("ENCHANT_ITEM", out id))
                    return id;
            }

            // Skinning
            if (t == "skinning")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SKINNING", out id))
                    return id;
            }

            // Dismiss pet
            if (t == "dismisspet" || t == "unsummonpet")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("DISMISS_PET", out id))
                    return id;
            }

            // Instakill
            if (t == "instakill" || t == "kill" || t == "slay")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("INSTAKILL", out id))
                    return id;
            }

            // Summon Object / Object Slot
            if (t == "summonobject")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_OBJECT", out id))
                    return id;
            }
            if (t == "summonobjectslot" || t == "summonobjectslots")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_OBJECT_SLOT", out id))
                    return id;
            }

            // Activate Object
            if (t == "activateobject")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("ACTIVATE_OBJECT", out id))
                    return id;
            }

            // ------------------------------------------------------------
            // Niche and advanced EFFECT types
            // ------------------------------------------------------------

            // Charge (Warrior charge/intercept)
            if (t == "charge" || t == "chargemove" || t == "intercept")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("CHARGE", out id))
                    return id;
            }

            // Charge destination movement (Death Grip jump)
            if (t == "chargedest" || t == "chargejump")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("CHARGE_DEST", out id))
                    return id;
            }

            // Jump / JumpDest
            if (t == "jump" || t == "jumpforward")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("JUMP", out id))
                    return id;
            }
            if (t == "jumpdest" || t == "jumpdestination")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("JUMP_DEST", out id))
                    return id;
            }

            // TeleportPlayer
            if (t == "teleportplayer")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("TELEPORT_PLAYER", out id))
                    return id;
            }

            // TeleportGraveyard
            if (t == "teleportgraveyard" || t == "graveteleport")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("TELEPORT_GRAVEYARD", out id))
                    return id;
            }

            // Persistent Area Aura (e.g. Blizzard, Rain of Fire)
            if (t == "persistentarea" || t == "persistentareaaura")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("PERSISTENT_AREA_AURA", out id))
                    return id;
            }

            // ScriptEffect (server executes SpellScript effect)
            if (t == "scripteffect" || t == "script")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SCRIPT_EFFECT", out id))
                    return id;
            }

            // Create Item
            if (t == "createitem" || t == "makeitem")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("CREATE_ITEM", out id))
                    return id;
            }

            // Enchant Item
            if (t == "enchantitem" || t == "applyenchant")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("ENCHANT_ITEM", out id))
                    return id;
            }

            // Skinning
            if (t == "skinning")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SKINNING", out id))
                    return id;
            }

            // Dismiss pet
            if (t == "dismisspet" || t == "unsummonpet")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("DISMISS_PET", out id))
                    return id;
            }

            // Instakill
            if (t == "instakill" || t == "kill" || t == "slay")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("INSTAKILL", out id))
                    return id;
            }

            // Summon Object / Object Slot
            if (t == "summonobject")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_OBJECT", out id))
                    return id;
            }
            if (t == "summonobjectslot" || t == "summonobjectslots")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("SUMMON_OBJECT_SLOT", out id))
                    return id;
            }

            // Activate Object
            if (t == "activateobject")
            {
                if (AiEnumRegistry.EffectNameToId.TryGetValue("ACTIVATE_OBJECT", out id))
                    return id;
            }

            // ------------------------------------------------------------
            // 9) Default effect-name resolution from client enums
            //    (accept raw names like "SCHOOL_DAMAGE", "APPLY_AURA", etc.)
            // ------------------------------------------------------------
            if (AiEnumRegistry.EffectNameToId.TryGetValue(type.Trim(), out id))
                return id;

            // ------------------------------------------------------------
            // 10) Aura present → default APPLY_AURA
            // ------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(aura))
                return 6; // SPELL_EFFECT_APPLY_AURA

            // ------------------------------------------------------------
            // 11) Final fallback → SCHOOL_DAMAGE
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

                string h = hint.ToLowerInvariant().Trim();

                double best = double.MinValue;
                uint bestId = 0;

                foreach (var icon in iconDbc.Lookups)
                {
                    if (string.IsNullOrWhiteSpace(icon.Name))
                        continue;

                    string iconName = icon.Name.ToLowerInvariant();

                    double score = SimpleSubstringScore(iconName, h);

                    if (score > best)
                    {
                        best = score;
                        bestId = icon.ID;
                    }
                }

                return bestId;
            }
            catch
            {
                return 0;
            }
        }

        private static double SimpleSubstringScore(string iconName, string hint)
        {
            if (string.IsNullOrWhiteSpace(iconName) || string.IsNullOrWhiteSpace(hint))
                return 0.0;

            string name = iconName.ToLowerInvariant();
            string h = hint.ToLowerInvariant();

            double score = 0;

            // 1) Full phrase match (rare but high-value)
            if (name.Contains(h))
                score += 50;

            // 2) Token match
            var tokens = h.Split(new[] { ' ', '-', '_', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token.Length <= 1)
                    continue;

                if (name.Contains(token))
                    score += 20;

                // Small partial token match boost
                foreach (var part in SplitToken(token))
                {
                    if (name.Contains(part))
                        score += 8;
                }
            }

            return score;
        }

        private static IEnumerable<string> SplitToken(string token)
        {
            // Break complex words like "firestrike" into fire + strike
            var parts = new List<string>();

            for (int i = 1; i < token.Length - 1; i++)
            {
                var left = token.Substring(0, i);
                var right = token.Substring(i);

                if (left.Length >= 3)
                    parts.Add(left);

                if (right.Length >= 3)
                    parts.Add(right);
            }

            return parts;
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
