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

            // ---------------------------------------------------------------------
            // TELEPORT / MOVEMENT UTILITY
            // ---------------------------------------------------------------------
            if (TryLookupEffect("TELEPORT_UNITS", out id))
            {
                EffectNameToId[NormalizeKey("Teleport")] = id;
                EffectNameToId[NormalizeKey("Blink")] = id;
                EffectNameToId[NormalizeKey("TeleportSelf")] = id;
                EffectNameToId[NormalizeKey("TeleportTarget")] = id;
            }

            if (TryLookupEffect("TELEPORT_PLAYER", out id))
            {
                EffectNameToId[NormalizeKey("TeleportPlayer")] = id;
            }

            if (TryLookupEffect("TELEPORT_GRAVEYARD", out id))
            {
                EffectNameToId[NormalizeKey("TeleportGraveyard")] = id;
            }

            if (TryLookupEffect("JUMP", out id))
            {
                EffectNameToId[NormalizeKey("Jump")] = id;
                EffectNameToId[NormalizeKey("JumpForward")] = id;
            }

            if (TryLookupEffect("JUMP_DEST", out id))
            {
                EffectNameToId[NormalizeKey("JumpDest")] = id;
            }

            if (TryLookupEffect("CHARGE", out id))
            {
                EffectNameToId[NormalizeKey("Charge")] = id;
            }

            if (TryLookupEffect("CHARGE_DEST", out id))
            {
                EffectNameToId[NormalizeKey("ChargeDest")] = id;
            }

            // ---------------------------------------------------------------------
            // PERSISTENT AREA AURAS / SCRIPT EFFECTS
            // ---------------------------------------------------------------------
            if (TryLookupEffect("PERSISTENT_AREA_AURA", out id))
            {
                EffectNameToId[NormalizeKey("PersistentArea")] = id;
                EffectNameToId[NormalizeKey("PersistentAreaAura")] = id;
                EffectNameToId[NormalizeKey("GroundAura")] = id;
                EffectNameToId[NormalizeKey("AreaTrigger")] = id;
            }

            if (TryLookupEffect("SCRIPT_EFFECT", out id))
            {
                EffectNameToId[NormalizeKey("ScriptEffect")] = id;
                EffectNameToId[NormalizeKey("Script")] = id;
                EffectNameToId[NormalizeKey("CustomEffect")] = id;
            }

            // ---------------------------------------------------------------------
            // PET / CREATURE / KILL EFFECTS
            // ---------------------------------------------------------------------
            if (TryLookupEffect("DISMISS_PET", out id))
            {
                EffectNameToId[NormalizeKey("DismissPet")] = id;
                EffectNameToId[NormalizeKey("UnsummonPet")] = id;
            }

            if (TryLookupEffect("INSTAKILL", out id))
            {
                EffectNameToId[NormalizeKey("Instakill")] = id;
                EffectNameToId[NormalizeKey("Kill")] = id;
                EffectNameToId[NormalizeKey("Slay")] = id;
            }

            // ---------------------------------------------------------------------
            // GAMEOBJECT / INTERACTION
            // ---------------------------------------------------------------------
            if (TryLookupEffect("SUMMON_OBJECT", out id))
            {
                EffectNameToId[NormalizeKey("SummonObject")] = id;
            }

            if (TryLookupEffect("SUMMON_OBJECT_SLOT", out id))
            {
                EffectNameToId[NormalizeKey("SummonObjectSlot")] = id;
                EffectNameToId[NormalizeKey("SummonObjectSlots")] = id;
            }

            if (TryLookupEffect("OPEN_LOCK", out id))
            {
                EffectNameToId[NormalizeKey("OpenLock")] = id;
                EffectNameToId[NormalizeKey("Unlock")] = id;
                EffectNameToId[NormalizeKey("PickLock")] = id;
            }

            if (TryLookupEffect("ACTIVATE_OBJECT", out id))
            {
                EffectNameToId[NormalizeKey("ActivateObject")] = id;
            }

            // ---------------------------------------------------------------------
            // AREA AURA (APPLY_AREA_AURA_*) helpers – used by AiSpellMapper
            // ---------------------------------------------------------------------
            // Note: AiSpellMapper sets eff.Type = "AreaAuraParty" / "AreaAuraFriend"
            // etc. These aliases let TryResolveEffectId handle them directly.
            if (TryLookupEffect("APPLY_AREA_AURA_PARTY", out id) ||
                TryLookupEffect("Apply Area Aura Party", out id))
            {
                EffectNameToId[NormalizeKey("AreaAuraParty")] = id;
            }

            if (TryLookupEffect("APPLY_AREA_AURA_FRIEND", out id) ||
                TryLookupEffect("Apply Area Aura Friend", out id))
            {
                EffectNameToId[NormalizeKey("AreaAuraFriend")] = id;
            }

            if (TryLookupEffect("APPLY_AREA_AURA_ENEMY", out id) ||
                TryLookupEffect("Apply Area Aura Enemy", out id))
            {
                EffectNameToId[NormalizeKey("AreaAuraEnemy")] = id;
            }

            if (TryLookupEffect("APPLY_AREA_AURA_PET", out id) ||
                TryLookupEffect("Apply Area Aura Pet", out id))
            {
                EffectNameToId[NormalizeKey("AreaAuraPet")] = id;
            }

            if (TryLookupEffect("APPLY_AREA_AURA_OWNER", out id) ||
                TryLookupEffect("Apply Area Aura Owner", out id))
            {
                EffectNameToId[NormalizeKey("AreaAuraOwner")] = id;
            }

            if (TryLookupEffect("APPLY_AREA_AURA_RAID", out id) ||
                TryLookupEffect("Apply Area Aura Raid", out id))
            {
                EffectNameToId[NormalizeKey("AreaAuraRaid")] = id;
            }


            // ---------------------------------------------------------------------
            // KILL CREDIT helpers – mirror AiSpellMapper's manual handling
            // ---------------------------------------------------------------------
            if (TryLookupEffect("KILL_CREDIT", out id) ||
                TryLookupEffect("KILL_CREDIT2", out id))
            {
                // Generic catch-all
                EffectNameToId[NormalizeKey("KillCredit")] = id;
                EffectNameToId[NormalizeKey("KillCreditPersonal")] = id;
                EffectNameToId[NormalizeKey("Credit")] = id;
            }

            // ---------------------------------------------------------------------
            // BIND / SELF-RESURRECT helpers
            // ---------------------------------------------------------------------
            if (TryLookupEffect("BIND", out id) || TryLookupEffect("Bind", out id))
            {
                EffectNameToId[NormalizeKey("Bind")] = id;
                EffectNameToId[NormalizeKey("BindLocation")] = id;
                EffectNameToId[NormalizeKey("SetHearth")] = id;
            }

            if (TryLookupEffect("SELF_RESURRECT", out id) ||
                TryLookupEffect("Self Resurrect", out id))
            {
                EffectNameToId[NormalizeKey("SelfResurrect")] = id;
            }

            // ---------------------------------------------------------------------
            // CREATE / ENCHANT ITEM helpers (already partly handled, but align with
            // AiSpellMapper's extra AI type strings)
            // ---------------------------------------------------------------------
            if (TryLookupEffect("CREATE_ITEM", out id) ||
                TryLookupEffect("Create Item", out id))
            {
                EffectNameToId[NormalizeKey("CreateItem")] = id;
                EffectNameToId[NormalizeKey("MakeItem")] = id;
            }

            if (TryLookupEffect("ENCHANT_ITEM", out id) ||
                TryLookupEffect("Enchant Item", out id))
            {
                EffectNameToId[NormalizeKey("EnchantItem")] = id;
                EffectNameToId[NormalizeKey("ApplyEnchant")] = id;
            }

            // ---------------------------------------------------------------------
            // SKINNING / DISMISS PET / INSTAKILL – align with AiSpellMapper
            // ---------------------------------------------------------------------
            if (TryLookupEffect("SKINNING", out id) || TryLookupEffect("Skinning", out id))
            {
                EffectNameToId[NormalizeKey("Skinning")] = id;
            }

            if (TryLookupEffect("DISMISS_PET", out id) ||
                TryLookupEffect("Dismiss Pet", out id))
            {
                EffectNameToId[NormalizeKey("DismissPet")] = id;
                EffectNameToId[NormalizeKey("UnsummonPet")] = id;
            }

            if (TryLookupEffect("INSTAKILL", out id) ||
                TryLookupEffect("Instakill", out id))
            {
                EffectNameToId[NormalizeKey("Instakill")] = id;
                EffectNameToId[NormalizeKey("Kill")] = id;
                EffectNameToId[NormalizeKey("Slay")] = id;
            }

            // ---------------------------------------------------------------------
            // SUMMON helpers – complement your existing Summon aliases
            // ---------------------------------------------------------------------
            if (TryLookupEffect("SUMMON_VEHICLE", out id) ||
                TryLookupEffect("Summon Vehicle", out id))
            {
                EffectNameToId[NormalizeKey("SummonVehicle")] = id;
                EffectNameToId[NormalizeKey("Vehicle")] = id;
            }

            if (TryLookupEffect("SUMMON_TOTEM", out id) ||
                TryLookupEffect("Summon Totem", out id) ||
                TryLookupEffect("SUMMON_TOTEM_SLOT1", out id))
            {
                EffectNameToId[NormalizeKey("SummonTotem")] = id;
                EffectNameToId[NormalizeKey("SummonTotemSlot1")] = id;
            }

            // Summon Object helpers (if not already covered above)
            if (TryLookupEffect("SUMMON_OBJECT", out id) ||
                TryLookupEffect("Summon Object", out id))
            {
                EffectNameToId[NormalizeKey("SummonObject")] = id;
            }

            if (TryLookupEffect("SUMMON_OBJECT_SLOT", out id) ||
                TryLookupEffect("Summon Object Slot", out id))
            {
                EffectNameToId[NormalizeKey("SummonObjectSlot")] = id;
                EffectNameToId[NormalizeKey("SummonObjectSlots")] = id;
            }

            if (TryLookupEffect("ACTIVATE_OBJECT", out id) ||
                TryLookupEffect("Activate Object", out id))
            {
                EffectNameToId[NormalizeKey("ActivateObject")] = id;
            }

            // ---------------------------------------------------------------------
            // AREA AURA (APPLY_AREA_AURA_*) helpers – used by AiSpellMapper
            // ---------------------------------------------------------------------
            // Note: AiSpellMapper sets eff.Type = "AreaAuraParty" / "AreaAuraFriend"
            // etc. These aliases let TryResolveEffectId handle them directly.
            if (TryLookupEffect("APPLY_AREA_AURA_PARTY", out id) ||
                TryLookupEffect("Apply Area Aura Party", out id))
            {
                EffectNameToId[NormalizeKey("AreaAuraParty")] = id;
            }

            if (TryLookupEffect("APPLY_AREA_AURA_FRIEND", out id) ||
                TryLookupEffect("Apply Area Aura Friend", out id))
            {
                EffectNameToId[NormalizeKey("AreaAuraFriend")] = id;
            }

            if (TryLookupEffect("APPLY_AREA_AURA_ENEMY", out id) ||
                TryLookupEffect("Apply Area Aura Enemy", out id))
            {
                EffectNameToId[NormalizeKey("AreaAuraEnemy")] = id;
            }

            if (TryLookupEffect("APPLY_AREA_AURA_PET", out id) ||
                TryLookupEffect("Apply Area Aura Pet", out id))
            {
                EffectNameToId[NormalizeKey("AreaAuraPet")] = id;
            }

            if (TryLookupEffect("APPLY_AREA_AURA_OWNER", out id) ||
                TryLookupEffect("Apply Area Aura Owner", out id))
            {
                EffectNameToId[NormalizeKey("AreaAuraOwner")] = id;
            }

            if (TryLookupEffect("APPLY_AREA_AURA_RAID", out id) ||
                TryLookupEffect("Apply Area Aura Raid", out id))
            {
                EffectNameToId[NormalizeKey("AreaAuraRaid")] = id;
            }


            // ---------------------------------------------------------------------
            // KILL CREDIT helpers – mirror AiSpellMapper's manual handling
            // ---------------------------------------------------------------------
            if (TryLookupEffect("KILL_CREDIT", out id) ||
                TryLookupEffect("KILL_CREDIT2", out id))
            {
                // Generic catch-all
                EffectNameToId[NormalizeKey("KillCredit")] = id;
                EffectNameToId[NormalizeKey("KillCreditPersonal")] = id;
                EffectNameToId[NormalizeKey("Credit")] = id;
            }

            // ---------------------------------------------------------------------
            // BIND / SELF-RESURRECT helpers
            // ---------------------------------------------------------------------
            if (TryLookupEffect("BIND", out id) || TryLookupEffect("Bind", out id))
            {
                EffectNameToId[NormalizeKey("Bind")] = id;
                EffectNameToId[NormalizeKey("BindLocation")] = id;
                EffectNameToId[NormalizeKey("SetHearth")] = id;
            }

            if (TryLookupEffect("SELF_RESURRECT", out id) ||
                TryLookupEffect("Self Resurrect", out id))
            {
                EffectNameToId[NormalizeKey("SelfResurrect")] = id;
            }

            // ---------------------------------------------------------------------
            // CREATE / ENCHANT ITEM helpers (already partly handled, but align with
            // AiSpellMapper's extra AI type strings)
            // ---------------------------------------------------------------------
            if (TryLookupEffect("CREATE_ITEM", out id) ||
                TryLookupEffect("Create Item", out id))
            {
                EffectNameToId[NormalizeKey("CreateItem")] = id;
                EffectNameToId[NormalizeKey("MakeItem")] = id;
            }

            if (TryLookupEffect("ENCHANT_ITEM", out id) ||
                TryLookupEffect("Enchant Item", out id))
            {
                EffectNameToId[NormalizeKey("EnchantItem")] = id;
                EffectNameToId[NormalizeKey("ApplyEnchant")] = id;
            }

            // ---------------------------------------------------------------------
            // SKINNING / DISMISS PET / INSTAKILL – align with AiSpellMapper
            // ---------------------------------------------------------------------
            if (TryLookupEffect("SKINNING", out id) || TryLookupEffect("Skinning", out id))
            {
                EffectNameToId[NormalizeKey("Skinning")] = id;
            }

            if (TryLookupEffect("DISMISS_PET", out id) ||
                TryLookupEffect("Dismiss Pet", out id))
            {
                EffectNameToId[NormalizeKey("DismissPet")] = id;
                EffectNameToId[NormalizeKey("UnsummonPet")] = id;
            }

            if (TryLookupEffect("INSTAKILL", out id) ||
                TryLookupEffect("Instakill", out id))
            {
                EffectNameToId[NormalizeKey("Instakill")] = id;
                EffectNameToId[NormalizeKey("Kill")] = id;
                EffectNameToId[NormalizeKey("Slay")] = id;
            }

            // ---------------------------------------------------------------------
            // SUMMON helpers – complement your existing Summon aliases
            // ---------------------------------------------------------------------
            if (TryLookupEffect("SUMMON_VEHICLE", out id) ||
                TryLookupEffect("Summon Vehicle", out id))
            {
                EffectNameToId[NormalizeKey("SummonVehicle")] = id;
                EffectNameToId[NormalizeKey("Vehicle")] = id;
            }

            if (TryLookupEffect("SUMMON_TOTEM", out id) ||
                TryLookupEffect("Summon Totem", out id) ||
                TryLookupEffect("SUMMON_TOTEM_SLOT1", out id))
            {
                EffectNameToId[NormalizeKey("SummonTotem")] = id;
                EffectNameToId[NormalizeKey("SummonTotemSlot1")] = id;
            }

            // Summon Object helpers (if not already covered above)
            if (TryLookupEffect("SUMMON_OBJECT", out id) ||
                TryLookupEffect("Summon Object", out id))
            {
                EffectNameToId[NormalizeKey("SummonObject")] = id;
            }

            if (TryLookupEffect("SUMMON_OBJECT_SLOT", out id) ||
                TryLookupEffect("Summon Object Slot", out id))
            {
                EffectNameToId[NormalizeKey("SummonObjectSlot")] = id;
                EffectNameToId[NormalizeKey("SummonObjectSlots")] = id;
            }

            if (TryLookupEffect("ACTIVATE_OBJECT", out id) ||
                TryLookupEffect("Activate Object", out id))
            {
                EffectNameToId[NormalizeKey("ActivateObject")] = id;
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

            // ---------------------------------------------------------------------
            // SCHOOL ABSORB / SHIELD / DAMAGE BONUSES
            // ---------------------------------------------------------------------
            if (TryLookupAura("School Absorb", out id))
            {
                AuraNameToId[NormalizeKey("SchoolAbsorb")] = id;
                AuraNameToId[NormalizeKey("PeriodicAbsorb")] = id;
                AuraNameToId[NormalizeKey("AbsorbShield")] = id;
            }

            if (TryLookupAura("Damage Shield", out id))
            {
                AuraNameToId[NormalizeKey("DamageShield")] = id;
            }

            if (TryLookupAura("Mod Spell Damage Done", out id))
            {
                AuraNameToId[NormalizeKey("SpellDamageBuff")] = id;
                AuraNameToId[NormalizeKey("DamageBuff")] = id;
            }

            if (TryLookupAura("Mod Spell Healing Done", out id))
            {
                AuraNameToId[NormalizeKey("HealingBuff")] = id;
            }

            // ---------------------------------------------------------------------
            // SCHOOL IMMUNITIES
            // ---------------------------------------------------------------------
            if (TryLookupAura("Mod School Immunity", out id))
            {
                AuraNameToId[NormalizeKey("SchoolImmunity")] = id;
                AuraNameToId[NormalizeKey("ElementalImmunity")] = id;
            }

            // ---------------------------------------------------------------------
            // MECHANIC IMMUNITIES / DURATION / DAMAGE TAKEN
            // ---------------------------------------------------------------------
            if (TryLookupAura("Mechanic Immunity", out id))
            {
                AuraNameToId[NormalizeKey("MechanicImmune")] = id;
            }

            if (TryLookupAura("Mechanic Duration Mod", out id))
            {
                AuraNameToId[NormalizeKey("MechanicDurationMod")] = id;
            }

            if (TryLookupAura("Mod Mechanic Damage Taken", out id))
            {
                AuraNameToId[NormalizeKey("MechanicDamageTakenMod")] = id;
            }

            // ----------------------------
            // Shields / Absorbs
            // ----------------------------
            if (TryLookupAura("School Absorb", out id))
            {
                AuraNameToId[NormalizeKey("SchoolAbsorb")] = id;
                AuraNameToId[NormalizeKey("Absorb")] = id;
                AuraNameToId[NormalizeKey("Shield")] = id;
            }

            if (TryLookupAura("Damage Shield", out id))
            {
                AuraNameToId[NormalizeKey("DamageShield")] = id;
            }

            if (TryLookupAura("Mana Shield", out id))
            {
                AuraNameToId[NormalizeKey("ManaShield")] = id;
            }

            if (TryLookupAura("Split Damage Pct", out id))
            {
                AuraNameToId[NormalizeKey("SplitDamagePct")] = id;
                AuraNameToId[NormalizeKey("RedirectDamage")] = id;
            }

            // ----------------------------
            // Stat Buffs / Rating Auras
            // ----------------------------
            string[] statAuras = new[]
            {
                "Mod Stat", "Mod Attack Power", "Mod Ranged Attack Power", "Mod Spell Power",
                "Mod Armor", "Mod Resistance", "Mod Damage Done", "Mod Damage Taken",
                "Mod Crit Chance", "Mod Haste", "Mod Hit Chance", "Mod Mana Regen",
                "Mod Health Regen", "Mod Increase Speed", "Mod Dodge Percent",
                "Mod Parry Percent", "Mod Block Percent", "Mod Block Value",
                "Mod Armor Penetration", "Mod Expertise", "Mod Spell Damage Done",
                "Mod Damage Percent Done", "Mod Healing Done", "Mod Healing Taken Percent",
                "Mod Damage Taken Percent"
            };

            foreach (var name in statAuras)
            {
                if (TryLookupAura(name, out id))
                    AuraNameToId[NormalizeKey(name)] = id;
            }

            // ----------------------------
            // Proc Auras
            // ----------------------------
            string[] procAuras = new[]
            {
                "Proc Trigger Spell", "Proc Trigger Miss", "Proc Trigger Critical",
                "Proc Trigger Periodic", "Proc Trigger Spell With Value",
                "Periodic Trigger Damage"
            };

            foreach (var name in procAuras)
            {
                if (TryLookupAura(name, out id))
                    AuraNameToId[NormalizeKey(name.Replace(" ", ""))] = id;
            }

            // ----------------------------
            // Mechanic Immunity / Duration
            // ----------------------------
            if (TryLookupAura("Mod Mechanic Immune", out id))
            {
                AuraNameToId[NormalizeKey("ModMechanicImmune")] = id;
            }

            if (TryLookupAura("Mod Mechanic Duration", out id))
            {
                AuraNameToId[NormalizeKey("ModMechanicDuration")] = id;
            }

            if (TryLookupAura("Mod Mechanic Damage Taken", out id))
            {
                AuraNameToId[NormalizeKey("ModMechanicDamageTaken")] = id;
            }

            // ----------------------------
            // School Immunity
            // ----------------------------
            if (TryLookupAura("Mod School Immunity", out id))
            {
                AuraNameToId[NormalizeKey("ModSchoolImmunity")] = id;
            }

            // ----------------------------
            // Movement / Utility
            // ----------------------------
            string[] utility = new[]
            {
                "Mod Swim Speed", "Mod Flight Speed", "Mod Mounted Speed Percent",
                "Water Breathing", "Water Walking", "Levitate", "Feign Death"
            };

            foreach (var name in utility)
            {
                if (TryLookupAura(name, out id))
                    AuraNameToId[NormalizeKey(name.Replace(" ", ""))] = id;
            }

            // ----------------------------
            // Stealth / Detection
            // ----------------------------
            string[] stealth = new[]
            {
                "Mod Stealth", "Mod Stealth Level", "Mod Detect",
                "Mod Invisibility", "Mod Invisibility Detection"
            };

            foreach (var name in stealth)
            {
                if (TryLookupAura(name, out id))
                    AuraNameToId[NormalizeKey(name.Replace(" ", ""))] = id;
            }

            // ----------------------------
            // Tracking / Shapeshift
            // ----------------------------
            if (TryLookupAura("Track Creatures", out id))
                AuraNameToId[NormalizeKey("TrackCreatures")] = id;

            if (TryLookupAura("Track Resources", out id))
                AuraNameToId[NormalizeKey("TrackResources")] = id;

            if (TryLookupAura("Mod Shapeshift", out id))
                AuraNameToId[NormalizeKey("ModShapeshift")] = id;

            // ----------------------------
            // Weapons / Offhand / Crit
            // ----------------------------
            if (TryLookupAura("Mod Weapon Skill", out id))
                AuraNameToId[NormalizeKey("ModWeaponSkill")] = id;

            if (TryLookupAura("Mod Weapon Critical Percent", out id))
                AuraNameToId[NormalizeKey("ModWeaponCriticalPercent")] = id;

            if (TryLookupAura("Mod Offhand Damage Percent", out id))
                AuraNameToId[NormalizeKey("ModOffhandDamagePercent")] = id;

            // ----------------------------
            // Pet / Vehicle
            // ----------------------------
            if (TryLookupAura("Control Pet", out id))
                AuraNameToId[NormalizeKey("ControlPet")] = id;

            if (TryLookupAura("Mod Pet Damage Done", out id))
                AuraNameToId[NormalizeKey("ModPetDamageDone")] = id;

            if (TryLookupAura("Mod Vehicle Speed", out id))
                AuraNameToId[NormalizeKey("ModVehicleSpeed")] = id;

            if (TryLookupAura("Mod Vehicle Power", out id))
                AuraNameToId[NormalizeKey("ModVehiclePower")] = id;

            // ----------------------------
            // Max Health / Power
            // ----------------------------
            if (TryLookupAura("Mod Increase Health Percent", out id))
                AuraNameToId[NormalizeKey("ModIncreaseHealthPercent")] = id;

            if (TryLookupAura("Mod Increase Max Health", out id))
                AuraNameToId[NormalizeKey("ModIncreaseMaxHealth")] = id;

            if (TryLookupAura("Increase Max Health", out id))
                AuraNameToId[NormalizeKey("IncreaseMaxHealth")] = id;

            if (TryLookupAura("Increase Max Power", out id))
                AuraNameToId[NormalizeKey("IncreaseMaxPower")] = id;

            // ---------------------------------------------------------------------
            // MAX HEALTH / POWER – high-level helpers used by AiSpellMapper
            // ---------------------------------------------------------------------
            if (TryLookupAura("Mod Increase Health Percent", out id) ||
                TryLookupAura("Increase Health %", out id))
            {
                AuraNameToId[NormalizeKey("ModIncreaseHealthPercent")] = id;
                AuraNameToId[NormalizeKey("IncreaseHealthPercent")] = id;
            }

            if (TryLookupAura("Mod Increase Health", out id) ||
                TryLookupAura("Increase Health", out id))
            {
                AuraNameToId[NormalizeKey("ModIncreaseMaxHealth")] = id;
                AuraNameToId[NormalizeKey("IncreaseMaxHealth")] = id;
            }

            if (TryLookupAura("Mod Increase Power", out id) ||
                TryLookupAura("Increase Power", out id) ||
                TryLookupAura("Mod Increase Max Power", out id))
            {
                AuraNameToId[NormalizeKey("ModIncreaseMaxPower")] = id;
                AuraNameToId[NormalizeKey("IncreaseMaxPower")] = id;
            }

            // ---------------------------------------------------------------------
            // PERIODIC LEECH / POWER BURN – unify several AI spell types
            // ---------------------------------------------------------------------
            if (TryLookupAura("Periodic Mana Leech", out id) ||
                TryLookupAura("Periodic Health Leech", out id))
            {
                AuraNameToId[NormalizeKey("PeriodicLeech")] = id;
                AuraNameToId[NormalizeKey("PeriodicManaLeech")] = id;
                AuraNameToId[NormalizeKey("PeriodicHealthLeech")] = id;
            }

            if (TryLookupAura("Periodic Power Burn", out id) ||
                TryLookupAura("Periodic PowerBurn", out id))
            {
                AuraNameToId[NormalizeKey("PeriodicPowerBurn")] = id;
            }

            // ---------------------------------------------------------------------
            // INTERRUPT / PUSHBACK / CASTING PROTECTION
            // ---------------------------------------------------------------------
            if (TryLookupAura("Interrupt Regeneration", out id) ||
                TryLookupAura("Interrupt Regen", out id))
            {
                AuraNameToId[NormalizeKey("InterruptRegen")] = id;
            }

            if (TryLookupAura("Mod Casting Speed Not Stack", out id) ||
                TryLookupAura("Mod Casting Speed (Not Stack)", out id))
            {
                AuraNameToId[NormalizeKey("ModCastingSpeedNotStack")] = id;
            }

            if (TryLookupAura("Mod Pushback", out id) ||
                TryLookupAura("Mod Spell Pushback", out id))
            {
                AuraNameToId[NormalizeKey("ModPushback")] = id;
            }

            // ---------------------------------------------------------------------
            // PROC AURAS – extended set
            // ---------------------------------------------------------------------
            if (TryLookupAura("Proc Trigger Spell", out id))
            {
                AuraNameToId[NormalizeKey("ProcTriggerSpell")] = id;
            }

            if (TryLookupAura("Proc Trigger Spell With Value", out id))
            {
                AuraNameToId[NormalizeKey("ProcTriggerSpellWithValue")] = id;
            }

            if (TryLookupAura("Proc Trigger Damage", out id))
            {
                AuraNameToId[NormalizeKey("ProcTriggerDamage")] = id;
            }

            if (TryLookupAura("Proc Event", out id))
            {
                AuraNameToId[NormalizeKey("ProcEvent")] = id;
            }

            if (TryLookupAura("Proc Trigger Spell Copy", out id) ||
                TryLookupAura("Proc Trigger Spell (Copy)", out id))
            {
                AuraNameToId[NormalizeKey("ProcTriggerSpellCopy")] = id;
            }

            // ---------------------------------------------------------------------
            // DAMAGE SHIELD (DIRECT + SCHOOL VARIANTS)
            // ---------------------------------------------------------------------
            if (TryLookupAura("Damage Shield School", out id))
            {
                AuraNameToId[NormalizeKey("DamageShieldSchool")] = id;
            }

            if (TryLookupAura("Proc Damage Shield", out id))
            {
                AuraNameToId[NormalizeKey("ProcDamageShield")] = id;
            }

            // ---------------------------------------------------------------------
            // VEHICLE / POSSESSION / PET CONTROL – aura side
            // ---------------------------------------------------------------------
            if (TryLookupAura("Control Vehicle", out id))
            {
                AuraNameToId[NormalizeKey("ControlVehicle")] = id;
            }

            if (TryLookupAura("Ride Vehicle", out id))
            {
                AuraNameToId[NormalizeKey("RideVehicle")] = id;
            }

            if (TryLookupAura("Possess", out id))
            {
                AuraNameToId[NormalizeKey("Possess")] = id;
            }

            if (TryLookupAura("Control Pet", out id))
            {
                AuraNameToId[NormalizeKey("ControlPet")] = id;
            }

            if (TryLookupAura("Mod Pet Damage Done", out id) ||
                TryLookupAura("Mod Pet Damage", out id))
            {
                AuraNameToId[NormalizeKey("ModPetDamageDone")] = id;
            }

            if (TryLookupAura("Mod Vehicle Speed", out id) ||
                TryLookupAura("Mod Pet Speed", out id))
            {
                AuraNameToId[NormalizeKey("ModVehicleSpeed")] = id;
            }

            if (TryLookupAura("Mod Vehicle Power", out id) ||
                TryLookupAura("Mod Pet Power", out id))
            {
                AuraNameToId[NormalizeKey("ModVehiclePower")] = id;
            }

            // ---------------------------------------------------------------------
            // ENRAGE / RAGE GENERATION
            // ---------------------------------------------------------------------
            if (TryLookupAura("Mod Rage From Damage Taken", out id))
            {
                AuraNameToId[NormalizeKey("ModRageFromDamageTaken")] = id;
            }

            if (TryLookupAura("Mod Rage Generation", out id))
            {
                AuraNameToId[NormalizeKey("ModRageGeneration")] = id;
            }

            // ---------------------------------------------------------------------
            // TOTEM EFFECT HELPERS
            // ---------------------------------------------------------------------
            if (TryLookupAura("Totem Effect Earth", out id))
            {
                AuraNameToId[NormalizeKey("TotemEffectEarth")] = id;
            }

            if (TryLookupAura("Totem Effect Air", out id))
            {
                AuraNameToId[NormalizeKey("TotemEffectAir")] = id;
            }

            if (TryLookupAura("Totem Effect Fire", out id))
            {
                AuraNameToId[NormalizeKey("TotemEffectFire")] = id;
            }

            if (TryLookupAura("Totem Effect Water", out id))
            {
                AuraNameToId[NormalizeKey("TotemEffectWater")] = id;
            }

            // ---------------------------------------------------------------------
            // ENCHANT / SANCTUARY / INTERVAL HEAL – misc helpers
            // ---------------------------------------------------------------------
            if (TryLookupAura("Enchant Item Temp", out id) ||
                TryLookupAura("Enchant Item Temporary", out id))
            {
                AuraNameToId[NormalizeKey("EnchantItemTemp")] = id;
            }

            if (TryLookupAura("Sanctuary", out id))
            {
                AuraNameToId[NormalizeKey("Sanctuary")] = id;
            }

            // PeriodicIntervalHeal: use same ID as Periodic Heal if no dedicated aura
            if (TryLookupAura("Periodic Heal", out id) ||
                TryLookupAura("PeriodicHealing", out id))
            {
                AuraNameToId[NormalizeKey("PeriodicIntervalHeal")] = id;
            }

            // ---------------------------------------------------------------------
            // MAX HEALTH / POWER – high-level helpers used by AiSpellMapper
            // ---------------------------------------------------------------------
            if (TryLookupAura("Mod Increase Health Percent", out id) ||
                TryLookupAura("Increase Health %", out id))
            {
                AuraNameToId[NormalizeKey("ModIncreaseHealthPercent")] = id;
                AuraNameToId[NormalizeKey("IncreaseHealthPercent")] = id;
            }

            if (TryLookupAura("Mod Increase Health", out id) ||
                TryLookupAura("Increase Health", out id))
            {
                AuraNameToId[NormalizeKey("ModIncreaseMaxHealth")] = id;
                AuraNameToId[NormalizeKey("IncreaseMaxHealth")] = id;
            }

            if (TryLookupAura("Mod Increase Power", out id) ||
                TryLookupAura("Increase Power", out id) ||
                TryLookupAura("Mod Increase Max Power", out id))
            {
                AuraNameToId[NormalizeKey("ModIncreaseMaxPower")] = id;
                AuraNameToId[NormalizeKey("IncreaseMaxPower")] = id;
            }

            // ---------------------------------------------------------------------
            // PERIODIC LEECH / POWER BURN – unify several AI spell types
            // ---------------------------------------------------------------------
            if (TryLookupAura("Periodic Mana Leech", out id) ||
                TryLookupAura("Periodic Health Leech", out id))
            {
                AuraNameToId[NormalizeKey("PeriodicLeech")] = id;
                AuraNameToId[NormalizeKey("PeriodicManaLeech")] = id;
                AuraNameToId[NormalizeKey("PeriodicHealthLeech")] = id;
            }

            if (TryLookupAura("Periodic Power Burn", out id) ||
                TryLookupAura("Periodic PowerBurn", out id))
            {
                AuraNameToId[NormalizeKey("PeriodicPowerBurn")] = id;
            }

            // ---------------------------------------------------------------------
            // INTERRUPT / PUSHBACK / CASTING PROTECTION
            // ---------------------------------------------------------------------
            if (TryLookupAura("Interrupt Regeneration", out id) ||
                TryLookupAura("Interrupt Regen", out id))
            {
                AuraNameToId[NormalizeKey("InterruptRegen")] = id;
            }

            if (TryLookupAura("Mod Casting Speed Not Stack", out id) ||
                TryLookupAura("Mod Casting Speed (Not Stack)", out id))
            {
                AuraNameToId[NormalizeKey("ModCastingSpeedNotStack")] = id;
            }

            if (TryLookupAura("Mod Pushback", out id) ||
                TryLookupAura("Mod Spell Pushback", out id))
            {
                AuraNameToId[NormalizeKey("ModPushback")] = id;
            }

            // ---------------------------------------------------------------------
            // PROC AURAS – extended set
            // ---------------------------------------------------------------------
            if (TryLookupAura("Proc Trigger Spell", out id))
            {
                AuraNameToId[NormalizeKey("ProcTriggerSpell")] = id;
            }

            if (TryLookupAura("Proc Trigger Spell With Value", out id))
            {
                AuraNameToId[NormalizeKey("ProcTriggerSpellWithValue")] = id;
            }

            if (TryLookupAura("Proc Trigger Damage", out id))
            {
                AuraNameToId[NormalizeKey("ProcTriggerDamage")] = id;
            }

            if (TryLookupAura("Proc Event", out id))
            {
                AuraNameToId[NormalizeKey("ProcEvent")] = id;
            }

            if (TryLookupAura("Proc Trigger Spell Copy", out id) ||
                TryLookupAura("Proc Trigger Spell (Copy)", out id))
            {
                AuraNameToId[NormalizeKey("ProcTriggerSpellCopy")] = id;
            }

            // ---------------------------------------------------------------------
            // DAMAGE SHIELD (DIRECT + SCHOOL VARIANTS)
            // ---------------------------------------------------------------------
            if (TryLookupAura("Damage Shield School", out id))
            {
                AuraNameToId[NormalizeKey("DamageShieldSchool")] = id;
            }

            if (TryLookupAura("Proc Damage Shield", out id))
            {
                AuraNameToId[NormalizeKey("ProcDamageShield")] = id;
            }

            // ---------------------------------------------------------------------
            // VEHICLE / POSSESSION / PET CONTROL – aura side
            // ---------------------------------------------------------------------
            if (TryLookupAura("Control Vehicle", out id))
            {
                AuraNameToId[NormalizeKey("ControlVehicle")] = id;
            }

            if (TryLookupAura("Ride Vehicle", out id))
            {
                AuraNameToId[NormalizeKey("RideVehicle")] = id;
            }

            if (TryLookupAura("Possess", out id))
            {
                AuraNameToId[NormalizeKey("Possess")] = id;
            }

            if (TryLookupAura("Control Pet", out id))
            {
                AuraNameToId[NormalizeKey("ControlPet")] = id;
            }

            if (TryLookupAura("Mod Pet Damage Done", out id) ||
                TryLookupAura("Mod Pet Damage", out id))
            {
                AuraNameToId[NormalizeKey("ModPetDamageDone")] = id;
            }

            if (TryLookupAura("Mod Vehicle Speed", out id) ||
                TryLookupAura("Mod Pet Speed", out id))
            {
                AuraNameToId[NormalizeKey("ModVehicleSpeed")] = id;
            }

            if (TryLookupAura("Mod Vehicle Power", out id) ||
                TryLookupAura("Mod Pet Power", out id))
            {
                AuraNameToId[NormalizeKey("ModVehiclePower")] = id;
            }

            // ---------------------------------------------------------------------
            // ENRAGE / RAGE GENERATION
            // ---------------------------------------------------------------------
            if (TryLookupAura("Mod Rage From Damage Taken", out id))
            {
                AuraNameToId[NormalizeKey("ModRageFromDamageTaken")] = id;
            }

            if (TryLookupAura("Mod Rage Generation", out id))
            {
                AuraNameToId[NormalizeKey("ModRageGeneration")] = id;
            }

            // ---------------------------------------------------------------------
            // TOTEM EFFECT HELPERS
            // ---------------------------------------------------------------------
            if (TryLookupAura("Totem Effect Earth", out id))
            {
                AuraNameToId[NormalizeKey("TotemEffectEarth")] = id;
            }

            if (TryLookupAura("Totem Effect Air", out id))
            {
                AuraNameToId[NormalizeKey("TotemEffectAir")] = id;
            }

            if (TryLookupAura("Totem Effect Fire", out id))
            {
                AuraNameToId[NormalizeKey("TotemEffectFire")] = id;
            }

            if (TryLookupAura("Totem Effect Water", out id))
            {
                AuraNameToId[NormalizeKey("TotemEffectWater")] = id;
            }

            // ---------------------------------------------------------------------
            // ENCHANT / SANCTUARY / INTERVAL HEAL – misc helpers
            // ---------------------------------------------------------------------
            if (TryLookupAura("Enchant Item Temp", out id) ||
                TryLookupAura("Enchant Item Temporary", out id))
            {
                AuraNameToId[NormalizeKey("EnchantItemTemp")] = id;
            }

            if (TryLookupAura("Sanctuary", out id))
            {
                AuraNameToId[NormalizeKey("Sanctuary")] = id;
            }

            // PeriodicIntervalHeal: use same ID as Periodic Heal if no dedicated aura
            if (TryLookupAura("Periodic Heal", out id) ||
                TryLookupAura("PeriodicHealing", out id))
            {
                AuraNameToId[NormalizeKey("PeriodicIntervalHeal")] = id;
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
            if (lower == "shield" || lower == "absorb")
            {
                if (TryLookupAura("School Absorb", out id))
                    return true;
            }

            return false;
        }
    }
}
