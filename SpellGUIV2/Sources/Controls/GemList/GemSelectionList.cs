using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using SpellEditor.Sources.Constants;
using SpellEditor.Sources.Controls.Common;
using SpellEditor.Sources.Controls.SpellSelectList;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.Gem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static SpellEditor.Sources.Constants.GemType;

namespace SpellEditor.Sources.Controls
{
    public class GemSelectionList : ListBox
    {

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // These should not be hardcoded
        private static readonly uint _PrismaticRefLootId = 90010;
        private static readonly string _WorldTableName = "new_world";

        private int _ContentsCount;
        private int _ContentsIndex;
        private IDatabaseAdapter _Adapter;
        private readonly DataTable _Table = new DataTable();
        public bool Initialised = false;
        private Label _SelectedGemText;
        private ThreadSafeComboBox _GemTypeBox;
        private List<UIElement> _Elements;
        private Action<string> _ShowFlyoutMessage;
        private Func<string, string, MetroDialogSettings, Task<string>> _ShowInputAsync;
        // ItemCache -> SkillDiscovery
        private Dictionary<uint, SkillDiscovery> _DiscoveryLookup = new Dictionary<uint, SkillDiscovery>();

        private static readonly string _SelectColumnsString =
            "SELECT g.id, g.SpellItemEnchantmentRef, g.gemType, e.sRefName0, e.ItemCache, " +
                "s.id AS \"TriggerSpell\", s.EffectTriggerSpell1 AS \"TempLearnSpell\", " +
                "a.id AS \"Achievement\", c.id AS \"AchievementCriteria\", s2.SpellIconID ";

        private static readonly string _SelectMaxString =
            "SELECT max(g.id), max(g.SpellItemEnchantmentRef), max(e.ItemCache) ";

        private static readonly string _QueryCriteriaString = 
            "FROM gemproperties g " +
            "JOIN spellitemenchantment e on g.SpellItemEnchantmentRef = e.id " +
            "JOIN spell s ON e.objectId1 = s.id " +
            "JOIN spell s2 ON s.EffectTriggerSpell1 = s2.id " +
            "JOIN achievement_criteria c ON e.ItemCache = c.assetType " +
            "JOIN achievement a ON c.referredAchievement = a.id " +
            "WHERE e.ItemCache >= 70000";

        private static readonly string _SelectSkillDiscoveryString =
             "SELECT d.spellId, d.reqSpell, s.EffectItemType1 AS Item " +
            $"FROM {_WorldTableName}.skill_discovery_template d " +
             "JOIN spell s ON s.id = d.spellId " +
             "WHERE reqSpell IN (170000, 170001, 170002)";

        private static readonly string _QueryTableString = _SelectColumnsString + _QueryCriteriaString;
        private static readonly string _QueryMaxString = _SelectMaxString + _QueryCriteriaString;

        public void Initialise(Label selectedGemText, List<UIElement> elements, 
            Action<string> showFlyoutMessage, Func<string, string, MetroDialogSettings, Task<string>> showInputAsync)
        {
            if (!Initialised)
            {
                Initialised = true;
                _Elements = elements;
                _GemTypeBox = (ThreadSafeComboBox)elements[9];
                _SelectedGemText = selectedGemText;
                _ShowFlyoutMessage = showFlyoutMessage;
                _ShowInputAsync = showInputAsync;
                _Table.Columns.Add("id", typeof(uint));
                _Table.Columns.Add("SpellItemEnchantmentRef", typeof(uint));
                _Table.Columns.Add("gemType", typeof(uint));
                _Table.Columns.Add("sRefName0", typeof(string));
                _Table.Columns.Add("ItemCache", typeof(uint));
                _Table.Columns.Add("TriggerSpell", typeof(uint));
                _Table.Columns.Add("TempLearnSpell", typeof(uint));
                _Table.Columns.Add("Achievement", typeof(uint));
                _Table.Columns.Add("AchievementCriteria", typeof(uint));
                _Table.Columns.Add("SpellIconID", typeof(uint));
                PopulateGemSelect();
                _GemTypeBox.ItemsSource = GemTypeManager.Instance.GemTypes;
                SelectionChanged += GemSelectionList_SelectionChanged;
                ((Button)elements[0]).Click += SaveGemChangesClick;
                ((Button)elements[1]).Click += DuplicateGemClick;
                ((Button)elements[2]).Click += DeleteGemClick;
                var filterByColour = ((ThreadSafeComboBox)_Elements[10]);
                filterByColour.ItemsSource = GetFilterByColourSource();
                filterByColour.SelectionChanged += FilterGemColourChanged;
                var filterByDisc = ((ThreadSafeComboBox)_Elements[11]);
                filterByDisc.SelectionChanged += FilterDiscoverableChanged;
                var discOptions = new List<string>
                {
                    "No Filter",
                    "Discoverable",
                    "Not Discoverable"
                };
                filterByDisc.ItemsSource = discOptions;
            }
        }

        private List<string> GetFilterByColourSource()
        {
            var gems = GemTypeManager.Instance.GemTypes.Select(gem => gem.Name).ToList();
            gems.Insert(0, "No Filter");
            return gems;
        }

        private void GemSelectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null || sender.GetType() != typeof(GemSelectionList))
                return;

            var selected = (GemSelectionEntry)(e.AddedItems.Count > 0 ? e.AddedItems[0] : null);
            if (selected == null)
                return;

            _SelectedGemText.Content = selected.GemId;
            var gem = selected.GemTypeEntry.Name;
            _GemTypeBox.Text = gem ?? string.Empty;

            ((ThreadSafeTextBox)_Elements[7]).Text = selected.SpellItemEnchantmentEntry.Name;
            ((ThreadSafeTextBox)_Elements[3]).Text = selected.SpellItemEnchantmentEntry.ItemCache.Id.ToString();
            ((ThreadSafeTextBox)_Elements[4]).Text = selected.SpellItemEnchantmentEntry.TriggerSpell.Id.ToString();
            ((ThreadSafeTextBox)_Elements[5]).Text = selected.SpellItemEnchantmentEntry.TempLearnSpell.Id.ToString();
            ((ThreadSafeTextBox)_Elements[6]).Text = selected.AchievementEntry.Id.ToString() + " / " + selected.AchievementCriteriaEntry.Id;

            LoadSkillDiscoveryData();
            ((ThreadSafeTextBox)_Elements[8]).Text = _DiscoveryLookup.ContainsKey(selected.SpellItemEnchantmentEntry.ItemCache.Id) ? 
                _DiscoveryLookup[selected.SpellItemEnchantmentEntry.ItemCache.Id].Id.ToString() :
                string.Empty;

            _Elements.ForEach(element => element.IsEnabled = true);
            _GemTypeBox.IsEnabled = true;
        }

        private void LoadSkillDiscoveryData()
        {
            if (_DiscoveryLookup.Count > 0)
                return;

            lock (_DiscoveryLookup)
            {
                if (_DiscoveryLookup.Count > 0)
                    return;

                try
                {
                    using (var query = _Adapter.Query(_SelectSkillDiscoveryString))
                    {
                        foreach (DataRow row in query.Rows)
                        {
                            var spellId = uint.Parse(row[0].ToString());
                            var reqSpell = uint.Parse(row[1].ToString());
                            var item = uint.Parse(row[2].ToString());
                            _DiscoveryLookup.Add(item, new SkillDiscovery(spellId, reqSpell, item));
                        }
                    }
                }
                catch (Exception exception)
                {
                    Logger.Info(exception, "ERROR: " + exception.Message);
                    _ShowFlyoutMessage.Invoke("ERROR: {_WorldTableName}.skill_discovery_template could not be loaded: " + exception.Message);
                }
            }
        }

        private string DeriveName(string text) =>
            text.ToLower().StartsWith("teaches you ") ?
            text.Substring("teaches you".Length).Trim() :
            text;

        private void SaveGemChangesClick(object sender, RoutedEventArgs e)
        {
            if (sender != _Elements[0] || SelectedItem == null)
                return;

            if (!(SelectedItem is GemSelectionEntry entry))
                return;

            var spellIconId = _Adapter.QuerySingleValue($"SELECT SpellIconID FROM spell WHERE id = {((ThreadSafeTextBox)_Elements[5]).Text}").ToString();

            // Update UI and entry
            var result = _Table.Select("id = " + entry.GemId);
            var data = result.First();
            data.BeginEdit();
            data["gemType"] = GemTypeManager.Instance.LookupGemTypeByName(_GemTypeBox.Text).Type;
            data["TriggerSpell"] = ((ThreadSafeTextBox)_Elements[4]).Text;
            data["TempLearnSpell"] = ((ThreadSafeTextBox)_Elements[5]).Text;
            data["ItemCache"] = ((ThreadSafeTextBox)_Elements[3]).Text;
            data["sRefName0"] = ((ThreadSafeTextBox)_Elements[7]).Text;
            data["SpellIconID"] = spellIconId;
            data.EndEdit();
            entry.RefreshEntry(data);

            var name = DeriveName(data["sRefName0"].ToString());
            var discoverSpellName = "Gem of " + name;
            var triggerSpellName = discoverSpellName + " Trigger";

            // GemType
            _Adapter.Execute($"UPDATE gemproperties SET gemType = {entry.GemTypeEntry.Type} WHERE id = {entry.GemId}");

            // SpellItemEnchantmentRef
            _Adapter.Execute($"UPDATE spellitemenchantment SET objectId1 = {entry.SpellItemEnchantmentEntry.TriggerSpell.Id}," +
                $" ItemCache = {entry.SpellItemEnchantmentEntry.ItemCache.Id}," +
                $" sRefName0 = \"{entry.SpellItemEnchantmentEntry.Name}\"" +
                $" WHERE id = {entry.SpellItemEnchantmentEntry.Id}");

            // Update trigger spell pointing to which spell to temp learn
            _Adapter.Execute($"UPDATE spell SET EffectTriggerSpell1 = {entry.SpellItemEnchantmentEntry.TempLearnSpell.Id}, " +
                $"SpellName0 = \"{triggerSpellName}\" " +
                $"WHERE id = {entry.SpellItemEnchantmentEntry.TriggerSpell.Id}");
            
            // Discovery data
            if (_DiscoveryLookup.ContainsKey(entry.SpellItemEnchantmentEntry.ItemCache.Id))
            {
                // Update Discover Spell Name
                var skillSpellId = _DiscoveryLookup[entry.SpellItemEnchantmentEntry.ItemCache.Id].Id;
                _Adapter.Execute($"UPDATE spell SET SpellName0 = \"{discoverSpellName}\", " +
                    $"EffectItemType1 = {entry.SpellItemEnchantmentEntry.ItemCache.Id} " +
                    $"WHERE id = {skillSpellId}");

                // Update category for discovery spell
                _Adapter.Execute($"UPDATE {_WorldTableName}.skill_discovery_template SET reqSpell = {entry.GemTypeEntry.SkillDiscoverySpellId} " +
                    $"WHERE spellId = {skillSpellId}");
            }

            // Update Skill Line Ability based on gem colour
            if (0 == int.Parse(_Adapter.QuerySingleValue(
                $"SELECT COUNT(*) FROM skilllineability WHERE spellId = {entry.SpellItemEnchantmentEntry.TempLearnSpell.Id}").ToString()))
            {
                var newAbilityId = uint.Parse(_Adapter.QuerySingleValue($"SELECT MAX(id) FROM skilllineability").ToString()) + 1u;
                _Adapter.Execute($"INSERT INTO skilllineability VALUES " +
                    $"({newAbilityId}, {entry.GemTypeEntry.SkillId}, {entry.SpellItemEnchantmentEntry.TempLearnSpell.Id}, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0)");
            }
            else
            {
                _Adapter.Execute($"UPDATE skilllineability SET skillId = {entry.GemTypeEntry.SkillId} " +
                    $"WHERE spellId = {entry.SpellItemEnchantmentEntry.TempLearnSpell.Id}");
            }

            // Update Achievement
            _Adapter.Execute($"UPDATE achievement SET name1 = \"{discoverSpellName}\", " +
                $"categoryId = {entry.GemTypeEntry.AchievementCategory}, " +
                $"icon = {spellIconId} " +
                $"WHERE ID = {entry.AchievementEntry.Id}");

            // Update Achievement_Criteria
            _Adapter.Execute($"UPDATE achievement_criteria SET assetType = {entry.SpellItemEnchantmentEntry.ItemCache.Id} " +
                $"WHERE ID = {entry.AchievementCriteriaEntry.Id}");

            // Update item data (DBC and server side)
            _Adapter.Execute($"UPDATE item SET ItemDisplayInfo = {entry.GemTypeEntry.ItemDisplayId} " +
                $"WHERE itemID = {entry.SpellItemEnchantmentEntry.ItemCache.Id}");
            _Adapter.Execute($"UPDATE {_WorldTableName}.item_template SET displayId = {entry.GemTypeEntry.ItemDisplayId}, " +
                $"`name` = \"{discoverSpellName}\", " +
                $"GemProperties = {entry.GemId} " +
                $"WHERE entry = {entry.SpellItemEnchantmentEntry.ItemCache.Id}");

            // Update chest prismatic gem loot
            if (((GemTypeEnum)entry.GemTypeEntry.Type) == GemTypeEnum.Purple)
            {
                _Adapter.Execute($"REPLACE INTO {_WorldTableName}.reference_loot_template VALUES ({_PrismaticRefLootId}, {entry.SpellItemEnchantmentEntry.ItemCache}, 0, 0, 0, 1, 1, 1, 1, \"{discoverSpellName}\")");
            }
            else
            {
                _Adapter.Execute($"DELETE FROM {_WorldTableName}.reference_loot_template WHERE entry = {_PrismaticRefLootId} AND item = {entry.SpellItemEnchantmentEntry.ItemCache}");
            }

            _ShowFlyoutMessage.Invoke($"Saved gem: {entry.GemId} - {entry.SpellItemEnchantmentEntry.Name}");
        }

        private async void DuplicateGemClick(object sender, RoutedEventArgs e)
        {
            if (sender != _Elements[1] || SelectedItem == null)
                return;

            if (!(SelectedItem is GemSelectionEntry entry))
                return;

            var input = await _ShowInputAsync("Duplicate", $"Input \"y\" to duplicate [{entry.SpellItemEnchantmentEntry.Name}].", null);
            if (string.IsNullOrWhiteSpace(input) || !input.Contains("y"))
                return;

            var newGemId = uint.Parse(_Adapter.QuerySingleValue($"SELECT MAX(id) FROM gemproperties").ToString()) + 1u;
            var newEnchantId = uint.Parse(_Adapter.QuerySingleValue($"SELECT MAX(ID) FROM spellitemenchantment").ToString()) + 1u;
            var newItemId = uint.Parse(_Adapter.QuerySingleValue($"SELECT MAX(entry) FROM {_WorldTableName}.item_template WHERE entry < 100000").ToString()) + 1u;
            var newTriggerSpellId = uint.Parse(_Adapter.QuerySingleValue("SELECT MAX(ID) FROM spell").ToString()) + 1u;
            var newTempLearnSpellId = newTriggerSpellId + 1u;
            var newDiscoverSpellId = newTriggerSpellId + 2u;
            var discoverExists = _DiscoveryLookup.ContainsKey(entry.SpellItemEnchantmentEntry.ItemCache.Id);

            // GemProperties
            _Adapter.Execute(
                $"INSERT INTO gemproperties SELECT {newGemId}, {newEnchantId}, maxCount_inv, maxCount_item, gemType FROM gemproperties WHERE ID = {entry.GemId}");
            
            // SpellItemEnchantment
            _Adapter.Execute(
                $"INSERT INTO spellitemenchantment SELECT {newEnchantId}, charges, SpellDispelType1, SpellDispelType2, SpellDispelType3, minAmount1, minAmount2, minAmount3," +
                $" maxAmount1, maxAmount2, maxAmount3, {newTriggerSpellId}, objectId2, objectId3, sRefName0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, sRefNameFlags, ItemVisuals, Flags," +
                $" {newItemId}, SpellItemEnchantmentCondition, SkillLine, SkillLevel, requiredLevel " +
                $"FROM spellitemenchantment WHERE ID = {entry.SpellItemEnchantmentEntry.Id}");

            // Item server + dbc
            _Adapter.Execute(
                $"INSERT INTO {_WorldTableName}.item_template SELECT {newItemId},class,subclass,SoundOverrideSubclass,name,displayid,Quality,Flags,FlagsExtra,BuyCount,BuyPrice,SellPrice," +
                "InventoryType,AllowableClass,AllowableRace,ItemLevel,RequiredLevel,RequiredSkill,RequiredSkillRank,requiredspell,requiredhonorrank,RequiredCityRank,RequiredReputationFaction," +
                "RequiredReputationRank,maxcount,stackable,ContainerSlots,StatsCount,stat_type1,stat_value1,stat_type2,stat_value2,stat_type3,stat_value3,stat_type4,stat_value4,stat_type5," +
                "stat_value5,stat_type6,stat_value6,stat_type7,stat_value7,stat_type8,stat_value8,stat_type9,stat_value9,stat_type10,stat_value10,ScalingStatDistribution,ScalingStatValue," +
                "dmg_min1,dmg_max1,dmg_type1,dmg_min2,dmg_max2,dmg_type2,armor,holy_res,fire_res,nature_res,frost_res,shadow_res,arcane_res,delay,ammo_type,RangedModRange,spellid_1," +
                "spelltrigger_1,spellcharges_1,spellppmRate_1,spellcooldown_1,spellcategory_1,spellcategorycooldown_1,spellid_2,spelltrigger_2,spellcharges_2,spellppmRate_2,spellcooldown_2," +
                "spellcategory_2,spellcategorycooldown_2,spellid_3,spelltrigger_3,spellcharges_3,spellppmRate_3,spellcooldown_3,spellcategory_3,spellcategorycooldown_3,spellid_4,spelltrigger_4," +
                "spellcharges_4,spellppmRate_4,spellcooldown_4,spellcategory_4,spellcategorycooldown_4,spellid_5,spelltrigger_5,spellcharges_5,spellppmRate_5,spellcooldown_5,spellcategory_5," +
                "spellcategorycooldown_5,bonding,description,PageText,LanguageID,PageMaterial,startquest,lockid,Material,sheath,RandomProperty,RandomSuffix,block,itemset,MaxDurability," +
                "area,Map,BagFamily,TotemCategory,socketColor_1,socketContent_1,socketColor_2,socketContent_2,socketColor_3,socketContent_3,socketBonus,GemProperties,RequiredDisenchantSkill," +
                "ArmorDamageModifier,duration,ItemLimitCategory,HolidayId,ScriptName,DisenchantID,FoodType,minMoneyLoot,maxMoneyLoot,flagsCustom,VerifiedBuild " +
                $"FROM {_WorldTableName}.item_template " +
                $"WHERE entry = {entry.SpellItemEnchantmentEntry.ItemCache.Id}");
            _Adapter.Execute(
                $"INSERT INTO item SELECT entry, class, subclass, -1, Material, displayid, InventoryType, sheath FROM {_WorldTableName}.item_template " +
                $"WHERE entry = {newItemId}");

            // Duplicate spell data
            var spellCopyColumns = "Category,Dispel,Mechanic,Attributes,AttributesEx,AttributesEx2,AttributesEx3,AttributesEx4,AttributesEx5,AttributesEx6,AttributesEx7,Stances,Unknown1,StancesNot,Unknown2,Targets," +
                "TargetCreatureType,RequiresSpellFocus,FacingCasterFlags,CasterAuraState,TargetAuraState,CasterAuraStateNot,TargetAuraStateNot,CasterAuraSpell,TargetAuraSpell,ExcludeCasterAuraSpell," +
                "ExcludeTargetAuraSpell,CastingTimeIndex,RecoveryTime,CategoryRecoveryTime,InterruptFlags,AuraInterruptFlags,ChannelInterruptFlags,ProcFlags,ProcChance,ProcCharges,MaximumLevel," +
                "BaseLevel,SpellLevel,DurationIndex,PowerType,ManaCost,ManaCostPerLevel,ManaPerSecond,ManaPerSecondPerLevel,RangeIndex,Speed,ModalNextSpell,StackAmount,Totem1,Totem2,Reagent1," +
                "Reagent2,Reagent3,Reagent4,Reagent5,Reagent6,Reagent7,Reagent8,ReagentCount1,ReagentCount2,ReagentCount3,ReagentCount4,ReagentCount5,ReagentCount6,ReagentCount7,ReagentCount8," +
                "EquippedItemClass,EquippedItemSubClassMask,EquippedItemInventoryTypeMask,Effect1,Effect2,Effect3,EffectDieSides1,EffectDieSides2,EffectDieSides3,EffectRealPointsPerLevel1," +
                "EffectRealPointsPerLevel2,EffectRealPointsPerLevel3,EffectBasePoints1,EffectBasePoints2,EffectBasePoints3,EffectMechanic1,EffectMechanic2,EffectMechanic3,EffectImplicitTargetA1," +
                "EffectImplicitTargetA2,EffectImplicitTargetA3,EffectImplicitTargetB1,EffectImplicitTargetB2,EffectImplicitTargetB3,EffectRadiusIndex1,EffectRadiusIndex2,EffectRadiusIndex3," +
                "EffectApplyAuraName1,EffectApplyAuraName2,EffectApplyAuraName3,EffectAmplitude1,EffectAmplitude2,EffectAmplitude3,EffectMultipleValue1,EffectMultipleValue2,EffectMultipleValue3," +
                "EffectChainTarget1,EffectChainTarget2,EffectChainTarget3,EffectItemType1,EffectItemType2,EffectItemType3,EffectMiscValue1,EffectMiscValue2,EffectMiscValue3,EffectMiscValueB1," +
                "EffectMiscValueB2,EffectMiscValueB3,EffectTriggerSpell1,EffectTriggerSpell2,EffectTriggerSpell3,EffectPointsPerComboPoint1,EffectPointsPerComboPoint2,EffectPointsPerComboPoint3," +
                "EffectSpellClassMaskA1,EffectSpellClassMaskA2,EffectSpellClassMaskA3,EffectSpellClassMaskB1,EffectSpellClassMaskB2,EffectSpellClassMaskB3,EffectSpellClassMaskC1," +
                "EffectSpellClassMaskC2,EffectSpellClassMaskC3,SpellVisual1,SpellVisual2,SpellIconID,ActiveIconID,SpellPriority,SpellName0,SpellName1,SpellName2,SpellName3,SpellName4,SpellName5," +
                "SpellName6,SpellName7,SpellName8,SpellNameFlag0,SpellNameFlag1,SpellNameFlag2,SpellNameFlag3,SpellNameFlag4,SpellNameFlag5,SpellNameFlag6,SpellNameFlag7,SpellRank0,SpellRank1," +
                "SpellRank2,SpellRank3,SpellRank4,SpellRank5,SpellRank6,SpellRank7,SpellRank8,SpellRankFlags0,SpellRankFlags1,SpellRankFlags2,SpellRankFlags3,SpellRankFlags4,SpellRankFlags5," +
                "SpellRankFlags6,SpellRankFlags7,SpellDescription0,SpellDescription1,SpellDescription2,SpellDescription3,SpellDescription4,SpellDescription5,SpellDescription6,SpellDescription7," +
                "SpellDescription8,SpellDescriptionFlags0,SpellDescriptionFlags1,SpellDescriptionFlags2,SpellDescriptionFlags3,SpellDescriptionFlags4,SpellDescriptionFlags5,SpellDescriptionFlags6," +
                "SpellDescriptionFlags7,SpellToolTip0,SpellToolTip1,SpellToolTip2,SpellToolTip3,SpellToolTip4,SpellToolTip5,SpellToolTip6,SpellToolTip7,SpellToolTip8,SpellToolTipFlags0,SpellToolTipFlags1," +
                "SpellToolTipFlags2,SpellToolTipFlags3,SpellToolTipFlags4,SpellToolTipFlags5,SpellToolTipFlags6,SpellToolTipFlags7,ManaCostPercentage,StartRecoveryCategory,StartRecoveryTime,MaximumTargetLevel," +
                "SpellFamilyName,SpellFamilyFlags,SpellFamilyFlags1,SpellFamilyFlags2,MaximumAffectedTargets,DamageClass,PreventionType,StanceBarOrder,EffectDamageMultiplier1,EffectDamageMultiplier2," +
                "EffectDamageMultiplier3,MinimumFactionId,MinimumReputation,RequiredAuraVision,TotemCategory1,TotemCategory2,AreaGroupID,SchoolMask,RuneCostID,SpellMissileID,PowerDisplayId,EffectBonusMultiplier1," +
                "EffectBonusMultiplier2,EffectBonusMultiplier3,SpellDescriptionVariableID,SpellDifficultyID ";
            // Trigger Spell
            _Adapter.Execute(
                $"INSERT INTO spell SELECT {newTriggerSpellId}, " +
                spellCopyColumns.Replace("EffectTriggerSpell1", newTriggerSpellId.ToString()) +
                $"FROM spell WHERE ID = {entry.SpellItemEnchantmentEntry.TriggerSpell.Id}");
            // Temp Learn Spell
            _Adapter.Execute(
                $"INSERT INTO spell SELECT {newTempLearnSpellId}, " +
                spellCopyColumns +
                $"FROM spell WHERE ID = {entry.SpellItemEnchantmentEntry.TempLearnSpell.Id}");
            // Discover Spell
            if (discoverExists)
            {
                var skillId = _DiscoveryLookup[entry.SpellItemEnchantmentEntry.ItemCache.Id].Id;
                _Adapter.Execute(
                    $"INSERT INTO spell SELECT {newDiscoverSpellId}, " +
                    spellCopyColumns.Replace("EffectItemType1", newItemId.ToString()) +
                    $"FROM spell WHERE ID = {skillId}");

                _Adapter.Execute(
                    $"INSERT INTO {_WorldTableName}.skill_discovery_template SELECT {newDiscoverSpellId}, reqSpell, reqSkillValue, chance " +
                    $"FROM {_WorldTableName}.skill_discovery_template WHERE spellId = {skillId}");

                // UI Cache Update
                _DiscoveryLookup[newItemId] = new SkillDiscovery(
                    newDiscoverSpellId, 
                    _DiscoveryLookup[entry.SpellItemEnchantmentEntry.ItemCache.Id].ReqSpell,
                    newItemId);
            }

            // SkillLineAbility
            var newSkillLineAbility = uint.Parse(_Adapter.QuerySingleValue($"SELECT MAX(id) FROM skilllineability").ToString()) + 1u;
            _Adapter.Execute(
                $"INSERT INTO skilllineability SELECT {newSkillLineAbility}, " +
                "skillId,spellId,chrRaces,chrClasses,unk1,unk2,requiredSkillValue,spellIdParent,acquireMethod,skillGreyLevel,skillGreenLevel,characterPoints1,characterPoints2 " +
                $"FROM skilllineability WHERE id = {entry.SpellItemEnchantmentEntry.TempLearnSpell.Id}");

            // Update chest prismatic gem loot
            if (((GemTypeEnum)entry.GemTypeEntry.Type) == GemTypeEnum.Purple)
            {
                _Adapter.Execute($"INSERT INTO {_WorldTableName}.reference_loot_template VALUES ({_PrismaticRefLootId}, {newItemId}, 0, 0, 0, 1, 1, 1, 1, \"Gem of {input}\")");
            }

            // Achievement
            var newAchievementId = uint.Parse(_Adapter.QuerySingleValue($"SELECT MAX(ID) FROM achievement").ToString()) + 1u;
            _Adapter.Execute($"INSERT INTO achievement SELECT {newAchievementId}," +
                "requiredFaction,mapId,parentAchievement,name1,name2,name3,name4,name5,name6,name7,name8,name9,name10,name11,name12,name13,name14,name15,name16,nameFlags," +
                "description1,description2,description3,description4,description5,description6,description7,description8,description9,description10,description11," +
                "description12,description13,description14,description15,description16,descriptionFlags,categoryId,points,orderInCategory,flags,icon,titleReward1," +
                "titleReward2,titleReward3,titleReward4,titleReward5,titleReward6,titleReward7,titleReward8,titleReward9,titleReward10,titleReward11,titleReward12," +
                "titleReward13,titleReward14,titleReward15,titleReward16,titleRewardFlags,count,refAchievement " +
                $"FROM achievement WHERE ID = {entry.AchievementEntry.Id}");

            // Achievement_Criteria
            var newCriteriaId = uint.Parse(_Adapter.QuerySingleValue($"SELECT MAX(ID) FROM achievement_criteria").ToString()) + 1u;
            _Adapter.Execute($"INSERT INTO achievement_criteria SELECT {newCriteriaId},{newAchievementId}," +
                $"requiredType,{newItemId},assetCount,startEvent,startAsset,failEvent,failAsset,name1,name2,name3,name4,name5,name6,name7,name8,name9,name10,name11," +
                "name12,name13,name14,name15,name16,nameFlags,flags,timedType,timerStartEvent,timeLimit,showOrder " +
                $"FROM achievement_criteria WHERE ID = {entry.AchievementCriteriaEntry.Id}");

            // Refresh UI
            PopulateGemSelect(false);

            _ShowFlyoutMessage.Invoke($"New gem: {newGemId} with same spell name.");
        }

        private async void DeleteGemClick(object sender, RoutedEventArgs e)
        {
            if (sender != _Elements[2] || SelectedItem == null)
                return;

            if (!(SelectedItem is GemSelectionEntry entry))
                return;

            var input = await _ShowInputAsync("ARE YOU SURE?", $"Input \"hard\" to delete [{entry.SpellItemEnchantmentEntry.Name}] *COMPLETELY*. ", null);
            if (string.IsNullOrEmpty(input))
            {
                return;
            }
            // hard delete
            else if (input.Equals("hard"))
            {
                _Adapter.Execute($"DELETE FROM gemproperties WHERE id = {entry.GemId}");
                _Adapter.Execute($"DELETE FROM spellitemenchantment WHERE id = {entry.SpellItemEnchantmentEntry.Id}");
                _Adapter.Execute($"DELETE FROM achievement WHERE id = {entry.AchievementEntry.Id}");
                _Adapter.Execute($"DELETE FROM achievement_criteria WHERE id = {entry.AchievementCriteriaEntry.Id}");

                var itemId = entry.SpellItemEnchantmentEntry.ItemCache.Id;
                var triggerSpellId = entry.SpellItemEnchantmentEntry.TriggerSpell.Id;
                var tempLearnSpellId = entry.SpellItemEnchantmentEntry.TempLearnSpell.Id;
                _Adapter.Execute($"DELETE FROM spell WHERE id IN ({triggerSpellId}, {tempLearnSpellId})");
                _Adapter.Execute($"DELETE FROM item WHERE itemId = {itemId}");
                _Adapter.Execute($"DELETE FROM {_WorldTableName}.item_template WHERE entry = {itemId}");

                if (_DiscoveryLookup.ContainsKey(itemId))
                {
                    var spellId = _DiscoveryLookup[itemId].Id;

                    _Adapter.Execute($"DELETE FROM {_WorldTableName}.skill_discovery_template WHERE spellId = {spellId}");

                    // UI Cache update
                    _DiscoveryLookup.Remove(itemId);
                }

                // We leave SkillLineAbility in place, no harm

                // Prismatic Gem Loot
                _Adapter.Execute($"DELETE FROM {_WorldTableName}.reference_loot_template WHERE entry = {_PrismaticRefLootId} AND item = {itemId}");

                _ShowFlyoutMessage.Invoke($"Deleted gem: {entry.GemId} - {entry.SpellItemEnchantmentEntry.Name}");

                _Table.Select($"id = {entry.GemId}").First().Delete();
                _Table.AcceptChanges();
                // Refresh UI
                RefreshGemList();
            }
            // soft delete
            else if (input.Equals("soft"))
            {
                // TODO: Soft delete option, only removes skill_discovery data
                // Also disable the gem item? Trivial to do
                // Hard bit is having these soft-disabled gems still show up in this editor.
                // For now, not handling.
            }
        }

        private void FilterGemColourChanged(object sender, SelectionChangedEventArgs e)
        {
            var items = e.AddedItems;
            if (items.Count > 0)
            {
                var item = items[0].ToString();
                var typeId = item.ToLower().Contains("no filter") ? 
                    "-1" : 
                    GemTypeManager.Instance.LookupGemTypeByName(item).Type.ToString();

                ICollectionView view = CollectionViewSource.GetDefaultView(Items);
                view.Filter = o =>
                {
                    var panel = (StackPanel)o;
                    using (var enumerator = panel.GetChildObjects().GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            if (!(enumerator.Current is TextBlock block))
                                continue;

                            if (typeId.Equals("-1"))
                                return true;

                            var trimmed = block.Text.TrimStart();
                            var idString = trimmed.Substring(0, trimmed.IndexOf(' ')).Trim();
                            var row = _Table.Select("id = " + idString).First();
                            return row["gemType"].ToString().Equals(typeId);
                        }
                    }
                    return false;
                };
            }
        }

        private void FilterDiscoverableChanged(object sender, SelectionChangedEventArgs e)
        {
            var items = e.AddedItems;
            if (items.Count > 0)
            {
                var item = items[0].ToString();
                var mode = 0;
                if (item.Equals("Discoverable"))
                {
                    mode = 1;
                }
                else if (item.Equals("Not Discoverable"))
                {
                    mode = 2;
                }

                LoadSkillDiscoveryData();

                ICollectionView view = CollectionViewSource.GetDefaultView(Items);
                view.Filter = o =>
                {
                    var panel = (StackPanel)o;
                    using (var enumerator = panel.GetChildObjects().GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            if (mode == 0)
                                return true;

                            if (!(enumerator.Current is TextBlock block))
                                continue;

                            var trimmed = block.Text.TrimStart();
                            var idString = trimmed.Substring(0, trimmed.IndexOf(' ')).Trim();
                            var row = _Table.Select("id = " + idString).First();
                            var keyExists = _DiscoveryLookup.ContainsKey(uint.Parse(row["ItemCache"].ToString()));
                            return mode == 1 ? keyExists : !keyExists;
                        }
                    }
                    return false;
                };
            }
        }

        public bool IsInitialised() => Initialised;
        public bool HasAdapter() => _Adapter != null;

        public GemSelectionList SetAdapter(IDatabaseAdapter adapter)
        {
            _Adapter = adapter;
            return this;
        }

        public int GetLoadedRowCount() => _Table.Rows.Count;

        public void PopulateGemSelect(bool clearData = true)
        {
            if (_Adapter == null)
                return;
            if (_Table.Columns.Count == 0)
                return;

            using (var adapter = AdapterFactory.Instance.GetAdapter(false))
            {
                var watch = new Stopwatch();
                watch.Start();
                _ContentsIndex = 0;
                _ContentsCount = Items.Count;
                var worker = new GemListQueryWorker(adapter, watch) { WorkerReportsProgress = true };
                worker.ProgressChanged += _worker_ProgressChanged;

                worker.DoWork += delegate
                {
                    // Validate
                    if (worker.Adapter == null || !Config.Config.IsInit)
                        return;

                    // Clear Data
                    if (clearData)
                        _Table.Rows.Clear();

                    const uint pageSize = 5000;
                    uint lowerBounds = 0;
                    DataRowCollection results = GetGemData(lowerBounds, 100);
                    lowerBounds += 100;
                    // Edge case of empty table after truncating, need to send a event to the handler
                    if (results != null && results.Count == 0)
                    {
                        worker.ReportProgress(0, results);
                    }
                    while (results != null && results.Count != 0)
                    {
                        worker.ReportProgress(0, results);
                        results = GetGemData(lowerBounds, pageSize);
                        lowerBounds += pageSize;
                    }
                };
                worker.RunWorkerAsync();
                worker.RunWorkerCompleted += (sender, args) =>
                {
                    if (!(sender is GemListQueryWorker spellListQueryWorker))
                        return;

                    spellListQueryWorker.Watch.Stop();
                    Logger.Info($"Loaded gem selection list contents in {spellListQueryWorker.Watch.ElapsedMilliseconds}ms");
                };
            }
        }

        private void RefreshGemList()
        {
            // Update UI
            _ContentsIndex = 0;
            _ContentsCount = Items.Count;
            _Table.DefaultView.Sort = "id";
            // We have to call ToTable to return a new sorted data table
            // Returning the existing table will have new rows at the end of the collection
            var arg = new ProgressChangedEventArgs(100, _Table.DefaultView.ToTable().Rows);
            _worker_ProgressChanged(this, arg);
        }

        private void _worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            DataRowCollection collection = (DataRowCollection)e.UserState;
            int rowIndex = 0;
            // Reuse existing UI elements if they exist
            if (_ContentsIndex < _ContentsCount)
            {
                foreach (DataRow row in collection)
                {
                    if (_ContentsIndex == _ContentsCount || _ContentsIndex >= Items.Count)
                        break;

                    if (!(Items[_ContentsIndex] is GemSelectionEntry entry))
                        continue;

                    ++rowIndex;

                    entry.RefreshEntry(row);

                    ++_ContentsIndex;
                }
            }
            // Spawn any new UI elements required
            var newElements = new List<UIElement>();
            for (; rowIndex < collection.Count; ++rowIndex)
            {
                var row = collection[rowIndex];
                var entry = new GemSelectionEntry();
                entry.RefreshEntry(row);
                newElements.Add(entry);
                ++_ContentsIndex;
            }
            // Replace the item source directly, adding each item would raise a high amount of events
            var src = ItemsSource;
            var newSrc = new List<object>();
            if (src != null)
            {
                // This will also delete any listbox items we no longer need
                var enumerator = src.GetEnumerator();
                for (int i = 0; i < _ContentsIndex; ++i)
                {
                    if (!enumerator.MoveNext())
                        break;
                    newSrc.Add(enumerator.Current);
                }
            }

            newSrc.AddRange(newElements);
            ItemsSource = newSrc;
        }

        private DataRowCollection GetGemData(uint lowerBound, uint pageSize)
        {
            using (var gemData = _Adapter.Query(string.Format(_QueryTableString + 
                " ORDER BY g.id DESC LIMIT {0}, {1}", 
                lowerBound, pageSize)))
            {
                _Table.Merge(gemData, false, MissingSchemaAction.Add);
                _Table.AcceptChanges();

                return gemData.Rows;
            }
        }

        private class GemListQueryWorker : BackgroundWorker
        {
            public readonly IDatabaseAdapter Adapter;
            public readonly Stopwatch Watch;

            public GemListQueryWorker(IDatabaseAdapter adapter, Stopwatch watch)
            {
                Adapter = adapter;
                Watch = watch;
            }
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
