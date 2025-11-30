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

            // --------------------------------------------------------------------
            // NEW ALIASES – wired to actual SpellEffect handlers in SpellEffects.cpp
            // --------------------------------------------------------------------

            // THREAT (EffectModifyThreatPercent / SpellEffect THREAT)
            if (TryLookupEffect("THREAT", out id))
            {
                EffectNameToId[NormalizeKey("Threat")] = id;
            }

            // WEAPON_DAMAGE / WEAPON_DAMAGE_NOSCHOOL / WEAPON_PERCENT_DAMAGE
            // Used for "ApplyWeaponDamage" semantic type
            if (TryLookupEffect("WEAPON_DAMAGE", out id) ||
                TryLookupEffect("WEAPON_DAMAGE_NOSCHOOL", out id) ||
                TryLookupEffect("WEAPON_PERCENT_DAMAGE", out id))
            {
                EffectNameToId[NormalizeKey("ApplyWeaponDamage")] = id;
                EffectNameToId[NormalizeKey("WeaponStrike")] = id;
            }

            // SUMMON – generic summon creature/totem semantics
            if (TryLookupEffect("SUMMON", out id))
            {
                EffectNameToId[NormalizeKey("Summon")] = id;
                EffectNameToId[NormalizeKey("SummonCreature")] = id;
            }

            // INTERRUPT – EffectInterruptCast in SpellEffects.cpp
            if (TryLookupEffect("INTERRUPT_CAST", out id) || TryLookupEffect("INTERRUPT", out id))
            {
                EffectNameToId[NormalizeKey("Interrupt")] = id;
            }

            // DRAIN / LEECH helpers – these are aura-based mechanically,
            // but we allow semantic effect types to map to the correct effect entry.
            if (TryLookupEffect("DAMAGE_FROM_MAX_HEALTH_PCT", out id) ||
                TryLookupEffect("HEALTH_LEECH", out id))
            {
                EffectNameToId[NormalizeKey("DrainHealth")] = id;
                EffectNameToId[NormalizeKey("LeechHealth")] = id;
            }

            if (TryLookupEffect("POWER_DRAIN", out id) ||
                TryLookupEffect("POWER_LEECH", out id))
            {
                EffectNameToId[NormalizeKey("DrainMana")] = id;
                EffectNameToId[NormalizeKey("LeechMana")] = id;
            }

            // RESURRECT – self/full resurrect helpers
            if (TryLookupEffect("RESURRECT", out id) ||
                TryLookupEffect("RESURRECT_NEW", out id) ||
                TryLookupEffect("SELF_RESURRECT", out id))
            {
                EffectNameToId[NormalizeKey("Resurrect")] = id;
                EffectNameToId[NormalizeKey("SelfResurrect")] = id;
            }

            // OPEN_LOCK – lockpicking / chest / door
            if (TryLookupEffect("OPEN_LOCK", out id))
            {
                EffectNameToId[NormalizeKey("OpenLock")] = id;
            }

            // ENCHANT_ITEM / ENCHANT_ITEM_TEMPORARY – weapon enchants
            if (TryLookupEffect("ENCHANT_ITEM", out id) ||
                TryLookupEffect("ENCHANT_ITEM_TEMPORARY", out id))
            {
                EffectNameToId[NormalizeKey("EnchantItem")] = id;
                EffectNameToId[NormalizeKey("WeaponEnchant")] = id;
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

            // PERIODIC_MANA_LEECH
            if (TryLookupAura("Periodic Mana Leech", out id))
            {
                AuraNameToId[NormalizeKey("PeriodicManaLeech")] = id;
                AuraNameToId[NormalizeKey("ManaLeech")] = id;
                AuraNameToId[NormalizeKey("PeriodicManaDrain")] = id;
            }

            // PERIODIC_HEALTH_LEECH
            if (TryLookupAura("Periodic Health Leech", out id))
            {
                AuraNameToId[NormalizeKey("PeriodicHealthLeech")] = id;
                AuraNameToId[NormalizeKey("HealthLeech")] = id;
                AuraNameToId[NormalizeKey("LifeLeech")] = id;
            }

            // PERIODIC_ABSORB (if present in enUS.xaml – safe no-op if not)
            if (TryLookupAura("Periodic Absorb", out id))
            {
                AuraNameToId[NormalizeKey("PeriodicAbsorb")] = id;
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

            // --------------------------------------------------------------------
            // NEW AURA ALIASES – align with AI-Prompt aura names and SpellAuraEffects.cpp
            // --------------------------------------------------------------------
            // --------------------------------------------------------------------
            // EXTENDED WOTLK AURA SUPPORT (NON-SCRIPTED, FULLY DBC-SAFE)
            // --------------------------------------------------------------------

            //
            // 1) Combat ratings & avoidance
            //
            if (TryLookupAura("Mod Dodge Percent", out id))
            {
                AuraNameToId[NormalizeKey("ModDodgePercent")] = id;
            }
            if (TryLookupAura("Mod Parry Percent", out id))
            {
                AuraNameToId[NormalizeKey("ModParryPercent")] = id;
            }
            if (TryLookupAura("Mod Block Percent", out id))
            {
                AuraNameToId[NormalizeKey("ModBlockPercent")] = id;
            }
            if (TryLookupAura("Mod Critical Chance", out id) ||
                TryLookupAura("Mod Crit Percent", out id))
            {
                AuraNameToId[NormalizeKey("ModCritPercent")] = id;
            }
            if (TryLookupAura("Mod Spell Crit Chance", out id) ||
                TryLookupAura("Mod Spell Crit Percent", out id))
            {
                AuraNameToId[NormalizeKey("ModSpellCritChance")] = id;
            }
            if (TryLookupAura("Mod Expertise", out id))
            {
                AuraNameToId[NormalizeKey("ModExpertise")] = id;
            }
            if (TryLookupAura("Mod Armor Penetration", out id))
            {
                AuraNameToId[NormalizeKey("ModArmorPenetration")] = id;
            }

            //
            // 2) Damage, healing & spell bonuses
            //
            if (TryLookupAura("Mod Damage Percent Done", out id))
            {
                AuraNameToId[NormalizeKey("ModDamagePercentDone")] = id;
            }
            if (TryLookupAura("Mod Healing Done", out id))
            {
                AuraNameToId[NormalizeKey("ModHealingDone")] = id;
            }
            if (TryLookupAura("Mod Spell Damage Done", out id) ||
                TryLookupAura("Mod Spell Damage Done School", out id))
            {
                AuraNameToId[NormalizeKey("ModSpellDamageDone")] = id;
            }
            if (TryLookupAura("Periodic Trigger Damage", out id))
            {
                AuraNameToId[NormalizeKey("ProcTriggerDamage")] = id;
            }

            //
            // 3) School-based resistances
            //
            string[] schools =
            {
    "Holy", "Fire", "Nature", "Frost", "Shadow", "Arcane"
};
            foreach (var s in schools)
            {
                if (TryLookupAura($"Mod {s} Resistance", out id))
                    AuraNameToId[NormalizeKey($"Mod{s}Resistance")] = id;
            }

            //
            // 4) Proc Auras
            //
            if (TryLookupAura("Proc Trigger Spell", out id))
            {
                AuraNameToId[NormalizeKey("ProcTriggerSpell")] = id;
            }
            if (TryLookupAura("Proc Trigger Miss", out id))
            {
                AuraNameToId[NormalizeKey("ProcTriggerMiss")] = id;
            }
            if (TryLookupAura("Proc Trigger Critical", out id))
            {
                AuraNameToId[NormalizeKey("ProcTriggerCritical")] = id;
            }
            if (TryLookupAura("Proc Trigger Periodic", out id))
            {
                AuraNameToId[NormalizeKey("ProcTriggerPeriodic")] = id;
            }

            //
            // 5) Power and resource regeneration auras
            //
            if (TryLookupAura("Mod Rage Generation", out id))
            {
                AuraNameToId[NormalizeKey("ModRageRegen")] = id;
            }
            if (TryLookupAura("Mod Energy Regen", out id))
            {
                AuraNameToId[NormalizeKey("ModEnergyRegen")] = id;
            }
            if (TryLookupAura("Mod Runic Power Regen", out id))
            {
                AuraNameToId[NormalizeKey("ModRunicPowerRegen")] = id;
            }
            if (TryLookupAura("Mod Runes Regen", out id))
            {
                AuraNameToId[NormalizeKey("ModRuneRegen")] = id;
            }

            //
            // 6) Defense, mitigation & reductions
            //
            if (TryLookupAura("Mod Damage Taken Percent", out id))
            {
                AuraNameToId[NormalizeKey("ModDamageTakenPct")] = id;
            }
            if (TryLookupAura("Mod Healing Taken Percent", out id))
            {
                AuraNameToId[NormalizeKey("ModHealingTakenPct")] = id;
            }
            if (TryLookupAura("Mod Block Value", out id))
            {
                AuraNameToId[NormalizeKey("ModBlockValue")] = id;
            }

            //
            // 7) Utility & movement
            //
            if (TryLookupAura("Mod Swim Speed", out id))
            {
                AuraNameToId[NormalizeKey("ModSwimSpeed")] = id;
            }
            if (TryLookupAura("Mod Flight Speed", out id) ||
                TryLookupAura("Flight Speed", out id))
            {
                AuraNameToId[NormalizeKey("ModFlightSpeed")] = id;
            }
            if (TryLookupAura("Mod Stealth Detect", out id))
            {
                AuraNameToId[NormalizeKey("ModStealthDetect")] = id;
            }
            if (TryLookupAura("Mod Invisibility Detect", out id))
            {
                AuraNameToId[NormalizeKey("ModInvisibilityDetect")] = id;
            }
            if (TryLookupAura("Mod Mounted Speed Percent", out id))
            {
                AuraNameToId[NormalizeKey("ModMountedSpeedPct")] = id;
            }
            if (TryLookupAura("Mod Jump", out id))
            {
                AuraNameToId[NormalizeKey("ModJump")] = id;
            }

            //
            // 8) Absorbs & shields
            //
            if (TryLookupAura("Absorb Damage", out id))
            {
                AuraNameToId[NormalizeKey("AbsorbDamage")] = id;
            }
            if (TryLookupAura("Absorb Magic", out id))
            {
                AuraNameToId[NormalizeKey("AbsorbMagic")] = id;
            }
            if (TryLookupAura("Absorb School", out id))
            {
                AuraNameToId[NormalizeKey("AbsorbSchool")] = id;
            }
            if (TryLookupAura("Periodic Absorb", out id))
            {
                AuraNameToId[NormalizeKey("PeriodicAbsorb")] = id;
            }

            // MOVEMENT / CONTROL --------------------------------------------------

            // FEAR
            if (TryLookupAura("Fear", out id) || TryLookupAura("Mod Fear", out id))
            {
                AuraNameToId[NormalizeKey("ModFear")] = id;
            }

            // CHARM
            if (TryLookupAura("Charm", out id) || TryLookupAura("Mod Charm", out id))
            {
                AuraNameToId[NormalizeKey("ModCharm")] = id;
            }

            // CONFUSE / DISORIENT
            if (TryLookupAura("Confuse", out id) || TryLookupAura("Mod Confuse", out id) ||
                TryLookupAura("Disorient", out id))
            {
                AuraNameToId[NormalizeKey("ModConfuse")] = id;
            }

            // PACIFY (can’t attack)
            if (TryLookupAura("Pacify", out id) || TryLookupAura("Mod Pacify", out id))
            {
                AuraNameToId[NormalizeKey("ModPacify")] = id;
            }

            // SILENCE
            if (TryLookupAura("Silence", out id) || TryLookupAura("Mod Silence", out id) ||
                TryLookupAura("Silenced", out id))
            {
                AuraNameToId[NormalizeKey("ModSilence")] = id;
            }

            // MOVEMENT SPEED UP
            if (TryLookupAura("Mod Increase Speed", out id) || TryLookupAura("Increase Speed", out id))
            {
                AuraNameToId[NormalizeKey("ModIncreaseSpeed")] = id;
            }

            // COMBAT ADJUSTMENT ---------------------------------------------------

            if (TryLookupAura("Mod Threat", out id))
            {
                AuraNameToId[NormalizeKey("ModThreat")] = id;
            }

            if (TryLookupAura("Mod Taunt", out id) || TryLookupAura("Taunt", out id))
            {
                AuraNameToId[NormalizeKey("ModTaunt")] = id;
            }

            if (TryLookupAura("Mod Damage Done", out id))
            {
                AuraNameToId[NormalizeKey("ModDamageDone")] = id;
            }

            if (TryLookupAura("Mod Damage Taken", out id))
            {
                AuraNameToId[NormalizeKey("ModDamageTaken")] = id;
            }

            if (TryLookupAura("Mod Attack Power", out id))
            {
                AuraNameToId[NormalizeKey("ModAttackPower")] = id;
            }

            if (TryLookupAura("Mod Ranged Attack Power", out id))
            {
                AuraNameToId[NormalizeKey("ModRangedAttackPower")] = id;
            }

            if (TryLookupAura("Mod Spell Power", out id) ||
                TryLookupAura("Mod Spell Damage Done", out id))
            {
                AuraNameToId[NormalizeKey("ModSpellPower")] = id;
            }

            if (TryLookupAura("Mod Armor", out id))
            {
                AuraNameToId[NormalizeKey("ModArmor")] = id;
            }

            if (TryLookupAura("Mod Resistance", out id))
            {
                AuraNameToId[NormalizeKey("ModResistance")] = id;
            }

            if (TryLookupAura("Mod Crit Chance", out id) ||
                TryLookupAura("Mod Crit %", out id))
            {
                AuraNameToId[NormalizeKey("ModCritChance")] = id;
            }

            if (TryLookupAura("Mod Haste", out id) ||
                TryLookupAura("Mod Melee Haste", out id) ||
                TryLookupAura("Mod Ranged Haste", out id) ||
                TryLookupAura("Mod Casting Speed", out id))
            {
                AuraNameToId[NormalizeKey("ModHaste")] = id;
            }

            if (TryLookupAura("Mod Hit Chance", out id) ||
                TryLookupAura("Mod Hit %", out id))
            {
                AuraNameToId[NormalizeKey("ModHitChance")] = id;
            }

            // RESOURCE & REGEN ----------------------------------------------------

            if (TryLookupAura("Mod Mana Regen", out id) ||
                TryLookupAura("Mod Power Regen", out id))
            {
                AuraNameToId[NormalizeKey("ModManaRegen")] = id;
                AuraNameToId[NormalizeKey("ModPowerRegen")] = id;
            }

            if (TryLookupAura("Mod Health Regen", out id))
            {
                AuraNameToId[NormalizeKey("ModHealthRegen")] = id;
            }

            if (TryLookupAura("Mod Max Health", out id))
            {
                AuraNameToId[NormalizeKey("ModMaxHealth")] = id;
            }

            if (TryLookupAura("Mod Max Power", out id) ||
                TryLookupAura("Mod Max Mana", out id))
            {
                AuraNameToId[NormalizeKey("ModMaxMana")] = id;
            }

            // ABSORPTION / SHIELDS -----------------------------------------------

            if (TryLookupAura("Damage Shield", out id))
            {
                AuraNameToId[NormalizeKey("DamageShield")] = id;
            }

            if (TryLookupAura("School Absorb", out id) ||
                TryLookupAura("Absorb School", out id))
            {
                AuraNameToId[NormalizeKey("SchoolAbsorb")] = id;
            }

            if (TryLookupAura("Total Absorb", out id) ||
                TryLookupAura("Absorb All", out id))
            {
                AuraNameToId[NormalizeKey("TotalAbsorb")] = id;
            }

            if (TryLookupAura("Mana Shield", out id))
            {
                AuraNameToId[NormalizeKey("ManaShield")] = id;
            }

            // MISC BUFF / DEBUFF --------------------------------------------------

            if (TryLookupAura("Mod Stat", out id) ||
                TryLookupAura("Mod All Stats", out id))
            {
                AuraNameToId[NormalizeKey("ModStat")] = id;
            }

            if (TryLookupAura("Mod Skill", out id))
            {
                AuraNameToId[NormalizeKey("ModSkill")] = id;
            }

            if (TryLookupAura("Mod Stealth", out id) ||
                TryLookupAura("Stealth", out id))
            {
                AuraNameToId[NormalizeKey("ModStealth")] = id;
            }

            if (TryLookupAura("Mod Invisibility", out id) ||
                TryLookupAura("Invisibility", out id))
            {
                AuraNameToId[NormalizeKey("ModInvisibility")] = id;
            }

            if (TryLookupAura("Mod Scale", out id))
            {
                AuraNameToId[NormalizeKey("ModScale")] = id;
            }

            if (TryLookupAura("Mounted Speed", out id) ||
                TryLookupAura("Mod Mounted Speed", out id))
            {
                AuraNameToId[NormalizeKey("ModMountSpeed")] = id;
            }

            if (TryLookupAura("Water Breathing", out id))
            {
                AuraNameToId[NormalizeKey("WaterBreathing")] = id;
            }

            if (TryLookupAura("Water Walking", out id) ||
                TryLookupAura("Water Walk", out id))
            {
                AuraNameToId[NormalizeKey("WaterWalking")] = id;
            }

            if (TryLookupAura("Feather Fall", out id) ||
                TryLookupAura("Levitate", out id) ||
                TryLookupAura("Slow Fall", out id))
            {
                AuraNameToId[NormalizeKey("Levitate")] = id;
            }

            if (TryLookupAura("Feign Death", out id))
            {
                AuraNameToId[NormalizeKey("FeignDeath")] = id;
            }

            if (TryLookupAura("Mod Aggro Radius", out id) ||
                TryLookupAura("Mod Aggro Range", out id))
            {
                AuraNameToId[NormalizeKey("ModAggroRadius")] = id;
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
