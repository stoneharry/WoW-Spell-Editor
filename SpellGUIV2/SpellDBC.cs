using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.InteropServices;

namespace SpellGUIV2
{
    public class SpellDBC
    {
        private long FileSize;

        public SpellDBC_Header header;
        public SpellDBC_Body body;

        public bool loadDBCFile(string fileName)
        {
            try
            {
                FileStream fs = new FileStream(fileName, FileMode.Open);
                FileSize = fs.Length;

                // Read header
                int count = Marshal.SizeOf(typeof(SpellDBC_Header));
                byte[] readBuffer = new byte[count];
                BinaryReader reader = new BinaryReader(fs);
                readBuffer = reader.ReadBytes(count);
                GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                header = (SpellDBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDBC_Header));
                handle.Free();

                // Prepare body
                body.records = new SpellDBC_RecordMap[header.record_count];

                // Read body
                for (UInt32 i = 0; i < header.record_count; ++i)
                {
                    count = Marshal.SizeOf(typeof(SpellDBC_Record));
                    readBuffer = new byte[count];
                    readBuffer = reader.ReadBytes(count);
                    handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                    body.records[i].record = (SpellDBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(SpellDBC_Record));
                    handle.Free();               
                }

                string string_block;
                Dictionary<UInt32, VirtualStrTableEntry> strings = new Dictionary<UInt32, VirtualStrTableEntry>();;

                // Read string block
                string_block = Encoding.UTF8.GetString(reader.ReadBytes(header.string_block_size));

                // Turn the string block into something readable
                string temp = "";
                UInt32 lastString = 0;
                //for (UInt32 i = 0; i < header.string_block_size; ++i)
                UInt32 counter = 0;
                Int32 length = new System.Globalization.StringInfo(string_block).LengthInTextElements;
                while (counter < length)
                {
                    var t = string_block[(int)counter];
                    if (t == '\0')
                    {
                        VirtualStrTableEntry n = new VirtualStrTableEntry();
                        n.value = temp;
                        n.newValue = 0;
                        strings.Add(lastString, n);
                        lastString += (UInt32) Encoding.UTF8.GetByteCount(temp) + 1;
                        temp = "";
                    }
                    else
                    {
                        temp += t;
                    }
                    ++counter;
                }

                // Clean up for bad computers GC
                string_block = null;

                for (int i = 0; i < body.records.Length; ++i)
                {
                    body.records[i].spellName = new string[9];
                    body.records[i].spellRank = new string[9];
                    body.records[i].spellDesc = new string[9];
                    body.records[i].spellTool = new string[9];
                    for (int j = 0; j < 9; ++j)
                    {
                        body.records[i].spellName[j] = strings[body.records[i].record.SpellName[j]].value;
                        body.records[i].spellRank[j] = strings[body.records[i].record.Rank[j]].value;
                        body.records[i].spellDesc[j] = strings[body.records[i].record.Description[j]].value;
                        body.records[i].spellTool[j] = strings[body.records[i].record.ToolTip[j]].value;
                    }
                }

                reader.Close();
                fs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }

            return true;
        }


        public bool SaveDBCFile(string fileName)
        {
            try
            {
                Dictionary<int, UInt32> offsetStorage = new Dictionary<int, UInt32>();
                Dictionary<UInt32, string> reverseStorage = new Dictionary<UInt32, string>();
                // Generate some string offsets...
                UInt32 stringBlockOffset = 1; // first character is always \0

                for (UInt32 i = 0; i < header.record_count; ++i)
                {
                    for (UInt32 j = 0; j < 9; ++j)
                    {
                        // Name
                        if (body.records[i].spellName[j].Length == 0)
                            body.records[i].record.SpellName[j] = 0;
                        else
                        {
                            int key = body.records[i].spellName[j].GetHashCode();
                            if (offsetStorage.ContainsKey(key))
                                body.records[i].record.SpellName[j] = offsetStorage[key];
                            else
                            {
                                body.records[i].record.SpellName[j] = stringBlockOffset;
                                stringBlockOffset += (UInt32)Encoding.UTF8.GetByteCount(body.records[i].spellName[j]) + 1;
                                offsetStorage.Add(key, body.records[i].record.SpellName[j]);
                                reverseStorage.Add(body.records[i].record.SpellName[j], body.records[i].spellName[j]);
                            }
                        }
                        // Rank
                        if (body.records[i].spellRank[j].Length == 0)
                            body.records[i].record.Rank[j] = 0;
                        else
                        {
                            int key = body.records[i].spellRank[j].GetHashCode();
                            if (offsetStorage.ContainsKey(key))
                                body.records[i].record.Rank[j] = offsetStorage[key];
                            else
                            {
                                body.records[i].record.Rank[j] = stringBlockOffset;
                                stringBlockOffset += (UInt32)Encoding.UTF8.GetByteCount(body.records[i].spellRank[j]) + 1;
                                offsetStorage.Add(key, body.records[i].record.Rank[j]);
                                reverseStorage.Add(body.records[i].record.Rank[j], body.records[i].spellRank[j]);
                            }
                        }
                        // Tooltip
                        if (body.records[i].spellTool[j].Length == 0)
                            body.records[i].record.ToolTip[j] = 0;
                        else
                        {
                            int key = body.records[i].spellTool[j].GetHashCode();
                            if (offsetStorage.ContainsKey(key))
                                body.records[i].record.ToolTip[j] = offsetStorage[key];
                            else
                            {
                                body.records[i].record.ToolTip[j] = stringBlockOffset;
                                stringBlockOffset += (UInt32)Encoding.UTF8.GetByteCount(body.records[i].spellTool[j]) + 1;
                                offsetStorage.Add(key, body.records[i].record.ToolTip[j]);
                                reverseStorage.Add(body.records[i].record.ToolTip[j], body.records[i].spellTool[j]);
                            }
                        }
                        // Desc
                        if (body.records[i].spellDesc[j].Length == 0)
                            body.records[i].record.Description[j] = 0;
                        else
                        {
                            int key = body.records[i].spellDesc[j].GetHashCode();
                            if (offsetStorage.ContainsKey(key))
                                body.records[i].record.Description[j] = offsetStorage[key];
                            else
                            {
                                body.records[i].record.Description[j] = stringBlockOffset;
                                stringBlockOffset += (UInt32)Encoding.UTF8.GetByteCount(body.records[i].spellDesc[j]) + 1;
                                offsetStorage.Add(key, body.records[i].record.Description[j]);
                                reverseStorage.Add(body.records[i].record.Description[j], body.records[i].spellDesc[j]);
                            }
                        }
                    }
                }

                header.string_block_size = (int)stringBlockOffset;

                if (File.Exists(fileName))
                    File.Delete(fileName);
                FileStream fs = new FileStream(fileName, FileMode.Create);
                BinaryWriter writer = new BinaryWriter(fs);

                // Write header
                int count = Marshal.SizeOf(typeof(SpellDBC_Header));
                byte[] buffer = new byte[count];
                GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                Marshal.StructureToPtr(header, gcHandle.AddrOfPinnedObject(), true);
                writer.Write(buffer, 0, count);
                gcHandle.Free();

                // Write records
                for (UInt32 i = 0; i < header.record_count; ++i)
                {
                    // Write main body
                    count = Marshal.SizeOf(typeof(SpellDBC_Record));
                    buffer = new byte[count];
                    gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    Marshal.StructureToPtr(body.records[i].record, gcHandle.AddrOfPinnedObject(), true);
                    writer.Write(buffer, 0, count);
                    gcHandle.Free();
                }

                UInt32[] offsets_stored = offsetStorage.Values.ToArray<UInt32>();
                // Write string block
                writer.Write(Encoding.UTF8.GetBytes("\0"));
                for (int i = 0; i < offsets_stored.Length; ++i)
                {
                    writer.Write(Encoding.UTF8.GetBytes(reverseStorage[offsets_stored[i]] + "\0"));
                }
                
                writer.Close();
                fs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }

            return true;
        }
    }

    // Class instead of struct allows passing by reference
    public class VirtualStrTableEntry
    {
        public string value;
        public UInt32 newValue;
    };

    public struct SpellDBC_Header
    {
        public UInt32 magic;
        public UInt32 record_count;
        public UInt32 field_count;
        public UInt32 record_size;
        public Int32 string_block_size;
    };

    public struct SpellDBC_Body
    {
        public SpellDBC_RecordMap[] records;
    };

    public struct SpellDBC_RecordMap
    {
        public SpellDBC_Record record;
        public string[] spellName;
        public string[] spellRank;
        public string[] spellDesc;
        public string[] spellTool;
    };

    public struct SpellDBC_Record
    {
        public UInt32 Id; //	m_ID; 	
        public UInt32 Category; //	m_category; 	
        public UInt32 Dispel; //	m_dispelType; 	
        public UInt32 Mechanic; //	m_mechanic; 	
        public UInt32 Attributes; //	m_attribute; 	
        public UInt32 AttributesEx; //	m_attributesEx; 	
        public UInt32 AttributesEx2; //	m_attributesExB; 	
        public UInt32 AttributesEx3; //	m_attributesExC; 	
        public UInt32 AttributesEx4; //	m_attributesExD; 	
        public UInt32 AttributesEx5; //	m_attributesExE; 	
        public UInt32 AttributesEx6; //	m_attributesExF; 	
        public UInt32 AttributesEx7; //	3.2.0	(0x20	-	totems,	0x4	-
        public UInt32 Stances; //	m_shapeshiftMask; 	
        public UInt32 unk_320_2; //	3.2.0; 	
        public UInt32 StancesNot; //	m_shapeshiftExclude; 	
        public UInt32 unk_320_3; //	3.2.0; 	
        public UInt32 Targets; //	m_targets; 	
        public UInt32 TargetCreatureType; //	m_targetCreatureType; 	
        public UInt32 RequiresSpellFocus; //	m_requiresSpellFocus; 	
        public UInt32 FacingCasterFlags; //	m_facingCasterFlags; 	
        public UInt32 CasterAuraState; //	m_casterAuraState; 	
        public UInt32 TargetAuraState; //	m_targetAuraState; 	
        public UInt32 CasterAuraStateNot; //	m_excludeCasterAuraState; 	
        public UInt32 TargetAuraStateNot; //	m_excludeTargetAuraState; 	
        public UInt32 casterAuraSpell; //	m_casterAuraSpell; 	
        public UInt32 targetAuraSpell; //	m_targetAuraSpell; 	
        public UInt32 excludeCasterAuraSpell; //	m_excludeCasterAuraSpell; 	
        public UInt32 excludeTargetAuraSpell; //	m_excludeTargetAuraSpell; 	
        public UInt32 CastingTimeIndex; //	m_castingTimeIndex; 	
        public UInt32 RecoveryTime; //	m_recoveryTime; 	
        public UInt32 CategoryRecoveryTime; //	m_categoryRecoveryTime; 	
        public UInt32 InterruptFlags; //	m_interruptFlags; 	
        public UInt32 AuraInterruptFlags; //	m_auraInterruptFlags; 	
        public UInt32 ChannelInterruptFlags; //	m_channelInterruptFlags; 	
        public UInt32 procFlags; //	m_procTypeMask; 	
        public UInt32 procChance; //	m_procChance; 	
        public UInt32 procCharges; //	m_procCharges; 	
        public UInt32 maxLevel; //	m_maxLevel; 	
        public UInt32 baseLevel; //	m_baseLevel; 	
        public UInt32 spellLevel; //	m_spellLevel; 	
        public UInt32 DurationIndex; //	m_durationIndex; 	
        public UInt32 powerType; //	m_powerType; 	
        public UInt32 manaCost; //	m_manaCost; 	
        public UInt32 manaCostPerlevel; //	m_manaCostPerLevel; 	
        public UInt32 manaPerSecond; //	m_manaPerSecond; 	
        public UInt32 manaPerSecondPerLevel; //	m_manaPerSecondPerLeve; 	
        public UInt32 rangeIndex; //	m_rangeIndex; 	
        public float speed; //	m_speed; 	
        public UInt32 modalNextSpell; //	m_modalNextSpell; 	
        public UInt32 StackAmount; //	m_cumulativeAura; 	
        public UInt32 Totem1; //; 		
        public UInt32 Totem2; //	m_totem; 	
        public Int32 Reagent1; //; 		
        public Int32 Reagent2; //; 		
        public Int32 Reagent3; //; 		
        public Int32 Reagent4; //; 		
        public Int32 Reagent5; //; 		
        public Int32 Reagent6; //; 		
        public Int32 Reagent7; //; 		
        public Int32 Reagent8; //	m_reagent; 	
        public UInt32 ReagentCount1; //; 		
        public UInt32 ReagentCount2; //; 		
        public UInt32 ReagentCount3; //; 		
        public UInt32 ReagentCount4; //; 		
        public UInt32 ReagentCount5; //; 		
        public UInt32 ReagentCount6; //; 		
        public UInt32 ReagentCount7; //; 		
        public UInt32 ReagentCount8; //	m_reagentCount; 	
        public Int32 EquippedItemClass; //	m_equippedItemClass	(value); 
        public Int32 EquippedItemSubClassMask; //	m_equippedItemSubclass	(mask); 
        public Int32 EquippedItemInventoryTypeMask; //	m_equippedItemInvTypes	(mask); 
        public UInt32 Effect1; //; 		
        public UInt32 Effect2; //; 		
        public UInt32 Effect3; //	m_effect; 	
        public Int32 EffectDieSides1; //; 		
        public Int32 EffectDieSides2; //; 		
        public Int32 EffectDieSides3; //	m_effectDieSides; 	
        public float EffectRealPointsPerLevel1; //; 		
        public float EffectRealPointsPerLevel2; //; 		
        public float EffectRealPointsPerLevel3; //	m_effectRealPointsPerLevel; 	
        public Int32 EffectBasePoints1; //; 		
        public Int32 EffectBasePoints2; //; 		
        public Int32 EffectBasePoints3; //	m_effectBasePoints	(don't	must	be	used	in
        public UInt32 EffectMechanic1; //; 		
        public UInt32 EffectMechanic2; //; 		
        public UInt32 EffectMechanic3; //	m_effectMechanic; 	
        public UInt32 EffectImplicitTargetA1; //; 		
        public UInt32 EffectImplicitTargetA2; //; 		
        public UInt32 EffectImplicitTargetA3; //	m_implicitTargetA; 	
        public UInt32 EffectImplicitTargetB1; //; 		
        public UInt32 EffectImplicitTargetB2; //; 		
        public UInt32 EffectImplicitTargetB3; //	m_implicitTargetB; 	
        public UInt32 EffectRadiusIndex1; //; 		
        public UInt32 EffectRadiusIndex2; //; 		
        public UInt32 EffectRadiusIndex3; //	m_effectRadiusIndex	-	spellradius.dbc			
        public UInt32 EffectApplyAuraName1; //; 		
        public UInt32 EffectApplyAuraName2; //; 		
        public UInt32 EffectApplyAuraName3; //	m_effectAura; 	
        public UInt32 EffectAmplitude1; //; 		
        public UInt32 EffectAmplitude2; //; 		
        public UInt32 EffectAmplitude3; //	m_effectAuraPeriod; 	
        public float EffectMultipleValue1; //; 		
        public float EffectMultipleValue2; //; 		
        public float EffectMultipleValue3; //	m_effectAmplitude; 	
        public UInt32 EffectChainTarget1; //; 		
        public UInt32 EffectChainTarget2; //; 		
        public UInt32 EffectChainTarget3; //	m_effectChainTargets; 	
        public UInt32 EffectItemType1; //; 		
        public UInt32 EffectItemType2; //; 		
        public UInt32 EffectItemType3; //	m_effectItemType; 	
        public Int32 EffectMiscValue1; //; 		
        public Int32 EffectMiscValue2; //; 		
        public Int32 EffectMiscValue3; //	m_effectMiscValue; 	
        public Int32 EffectMiscValueB1; //; 		
        public Int32 EffectMiscValueB2; //; 		
        public Int32 EffectMiscValueB3; //	m_effectMiscValueB; 	
        public UInt32 EffectTriggerSpell1; //; 		
        public UInt32 EffectTriggerSpell2; //; 		
        public UInt32 EffectTriggerSpell3; //	m_effectTriggerSpell; 	
        public float EffectPointsPerComboPoint1; //; 		
        public float EffectPointsPerComboPoint2; //; 		
        public float EffectPointsPerComboPoint3; //	m_effectPointsPerCombo; 	
        public UInt32 EffectSpellClassMaskA1; //; 		
        public UInt32 EffectSpellClassMaskA2; //; 		
        public UInt32 EffectSpellClassMaskA3; //	m_effectSpellClassMaskA,	effect	0			
        public UInt32 EffectSpellClassMaskB1; //; 		
        public UInt32 EffectSpellClassMaskB2; //; 		
        public UInt32 EffectSpellClassMaskB3; //	m_effectSpellClassMaskB,	effect	1			
        public UInt32 EffectSpellClassMaskC1; //; 		
        public UInt32 EffectSpellClassMaskC2; //; 		
        public UInt32 EffectSpellClassMaskC3; //	m_effectSpellClassMaskC,	effect	2			
        public UInt32 SpellVisual1; //; 		
        public UInt32 SpellVisual2; //	m_spellVisualID; 	
        public UInt32 SpellIconID; //	m_spellIconID; 	
        public UInt32 activeIconID; //	m_activeIconID; 	
        public UInt32 spellPriority; //	m_spellPriority; 	
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public UInt32[] SpellName; //	m_name_lang;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public UInt32[] SpellNameFlag; //;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public UInt32[] Rank; //	m_nameSubtext_lang;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public UInt32[] RankFlags; //;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public UInt32[] Description; //	m_description_lang; 	
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public UInt32[] DescriptionFlags; //;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public UInt32[] ToolTip; //	m_auraDescription_lang; 	
	    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public UInt32[] ToolTipFlags; //;
        public UInt32 ManaCostPercentage; //	m_manaCostPct; 	
        public UInt32 StartRecoveryCategory; //	m_startRecoveryCategory; 	
        public UInt32 StartRecoveryTime; //	m_startRecoveryTime; 	
        public UInt32 MaxTargetLevel; //	m_maxTargetLevel; 	
        public UInt32 SpellFamilyName; //	m_spellClassSet; 	
        public UInt32 SpellFamilyFlags; //	m_spellClassMask	NOTE:	size	is	12	bytes!!!
        public UInt32 SpellFamilyFlags_UNKNOWN;
        public UInt32 SpellFamilyFlags2; //	addition	to	m_spellClassMask			
        public UInt32 MaxAffectedTargets; //	m_maxTargets; 	
        public UInt32 DmgClass; //	m_defenseType; 	
        public UInt32 PreventionType; //	m_preventionType; 	
        public UInt32 StanceBarOrder; //	m_stanceBarOrder; 	
        public float DmgMultiplier1; //; 		
        public float DmgMultiplier2; //; 		
        public float DmgMultiplier3; //	m_effectChainAmplitude; 	
        public UInt32 MinFactionId; //	m_minFactionID; 	
        public UInt32 MinReputation; //	m_minReputation; 	
        public UInt32 RequiredAuraVision; //	m_requiredAuraVision; 	
        public UInt32 TotemCategory1; //; 		
        public UInt32 TotemCategory2; //	m_requiredTotemCategoryID; 	
        public Int32 AreaGroupId; //	m_requiredAreaGroupId; 	
        public UInt32 SchoolMask; //	m_schoolMask; 	
        public UInt32 runeCostID; //	m_runeCostID; 	
        public UInt32 spellMissileID; //	m_spellMissileID; 	
        public UInt32 PowerDisplayId; //	PowerDisplay.dbc,	new	in	3.1		
        public float EffectBonusMultiplier1; //; 		
        public float EffectBonusMultiplier2; //; 		
        public float EffectBonusMultiplier3; //	3.2.0; 	
        public UInt32 spellDescriptionVariableID; //	3.2.0; 	
        public UInt32 SpellDifficultyId; //	3.3.0; 		
    };
}
