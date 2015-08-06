using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using MySql.Data.MySqlClient;
using System.Windows.Threading;

namespace SpellEditor.Sources.DBC
{
    class SpellDBC
    {
        // Begin Window
        private MainWindow main;
        // End Window

        // Begin DBCs
        public DBC_Header header;
        public Spell_DBC_Body body; // TODO: Make this private
        // End DBCs

        // Begin Files
        private long fileSize;
        // End Files

        public bool LoadDBCFile(MainWindow window)
        {
            main = window;

            try
            {
                FileStream fileStream = new FileStream("DBC/Spell.dbc", FileMode.Open);
                fileSize = fileStream.Length;
                int count = Marshal.SizeOf(typeof(DBC_Header));
                byte[] readBuffer = new byte[count];
                BinaryReader reader = new BinaryReader(fileStream);
                readBuffer = reader.ReadBytes(count);
                GCHandle handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                header = (DBC_Header)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(DBC_Header));
                handle.Free();
                body.records = new Spell_DBC_RecordMap[header.RecordCount];

                count = Marshal.SizeOf(typeof(Spell_DBC_Record));
                if (header.RecordSize != count)
                    throw new Exception("This Spell DBC version is not supported! It is not 3.3.5a.");

                for (UInt32 i = 0; i < header.RecordCount; ++i)
                {
                    readBuffer = new byte[count];
                    readBuffer = reader.ReadBytes(count);
                    handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                    body.records[i].record = (Spell_DBC_Record)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Spell_DBC_Record));
                    handle.Free();
                }

                string StringBlock;

                Dictionary<UInt32, VirtualStrTableEntry> strings = new Dictionary<UInt32, VirtualStrTableEntry>();

                StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(header.StringBlockSize));

                string temp = "";

                UInt32 lastString = 0;
                UInt32 counter = 0;
                Int32 length = new System.Globalization.StringInfo(StringBlock).LengthInTextElements;

                while (counter < length)
                {
                    var t = StringBlock[(int)counter];

                    if (t == '\0')
                    {
                        VirtualStrTableEntry n = new VirtualStrTableEntry();

                        n.Value = temp;
                        n.NewValue = 0;

                        strings.Add(lastString, n);

                        lastString += (UInt32)Encoding.UTF8.GetByteCount(temp) + 1;

                        temp = "";
                    }

                    else { temp += t; }

                    ++counter;
                }

                StringBlock = null;

                for (int i = 0; i < body.records.Length; ++i)
                {
                    body.records[i].spellName = new string[9];
                    body.records[i].spellRank = new string[9];
                    body.records[i].spellDesc = new string[9];
                    body.records[i].spellTool = new string[9];

                    for (int j = 0; j < 9; ++j)
                    {
                        body.records[i].spellName[j] = strings[body.records[i].record.SpellName[j]].Value;
                        body.records[i].spellRank[j] = strings[body.records[i].record.SpellRank[j]].Value;
                        body.records[i].spellDesc[j] = strings[body.records[i].record.SpellDescription[j]].Value;
                        body.records[i].spellTool[j] = strings[body.records[i].record.SpellToolTip[j]].Value;
                    }
                }

                reader.Close();
                fileStream.Close();
            }

            catch (Exception ex)
            {
                main.HandleErrorMessage(ex.Message);

                return false;
            }

            return true;
        }

        public bool SaveDBCFile()
        {
            try
            {
                UInt32 stringBlockOffset = 1;

                Dictionary<int, UInt32> offsetStorage = new Dictionary<int, UInt32>();
                Dictionary<UInt32, string> reverseStorage = new Dictionary<UInt32, string>();

                for (UInt32 i = 0; i < header.RecordCount; ++i)
                {
                    for (UInt32 j = 0; j < 9; ++j)
                    {
                        if (body.records[i].spellName[j].Length == 0) { body.records[i].record.SpellName[j] = 0; }
                        else
                        {
                            int key = body.records[i].spellName[j].GetHashCode();

                            if (offsetStorage.ContainsKey(key)) { body.records[i].record.SpellName[j] = offsetStorage[key]; }
                            else
                            {
                                body.records[i].record.SpellName[j] = stringBlockOffset;
                                stringBlockOffset += (UInt32)Encoding.UTF8.GetByteCount(body.records[i].spellName[j]) + 1;
                                offsetStorage.Add(key, body.records[i].record.SpellName[j]);
                                reverseStorage.Add(body.records[i].record.SpellName[j], body.records[i].spellName[j]);
                            }
                        }

                        if (body.records[i].spellRank[j].Length == 0) { body.records[i].record.SpellRank[j] = 0; }
                        else
                        {
                            int key = body.records[i].spellRank[j].GetHashCode();

                            if (offsetStorage.ContainsKey(key)) { body.records[i].record.SpellRank[j] = offsetStorage[key]; }
                            else
                            {
                                body.records[i].record.SpellRank[j] = stringBlockOffset;
                                stringBlockOffset += (UInt32)Encoding.UTF8.GetByteCount(body.records[i].spellRank[j]) + 1;
                                offsetStorage.Add(key, body.records[i].record.SpellRank[j]);
                                reverseStorage.Add(body.records[i].record.SpellRank[j], body.records[i].spellRank[j]);
                            }
                        }

                        if (body.records[i].spellTool[j].Length == 0) { body.records[i].record.SpellToolTip[j] = 0; }
                        else
                        {
                            int key = body.records[i].spellTool[j].GetHashCode();

                            if (offsetStorage.ContainsKey(key)) { body.records[i].record.SpellToolTip[j] = offsetStorage[key]; }
                            else
                            {
                                body.records[i].record.SpellToolTip[j] = stringBlockOffset;
                                stringBlockOffset += (UInt32)Encoding.UTF8.GetByteCount(body.records[i].spellTool[j]) + 1;
                                offsetStorage.Add(key, body.records[i].record.SpellToolTip[j]);
                                reverseStorage.Add(body.records[i].record.SpellToolTip[j], body.records[i].spellTool[j]);
                            }
                        }

                        if (body.records[i].spellDesc[j].Length == 0) { body.records[i].record.SpellDescription[j] = 0; }
                        else
                        {
                            int key = body.records[i].spellDesc[j].GetHashCode();

                            if (offsetStorage.ContainsKey(key)) { body.records[i].record.SpellDescription[j] = offsetStorage[key]; }
                            else
                            {
                                body.records[i].record.SpellDescription[j] = stringBlockOffset;
                                stringBlockOffset += (UInt32)Encoding.UTF8.GetByteCount(body.records[i].spellDesc[j]) + 1;
                                offsetStorage.Add(key, body.records[i].record.SpellDescription[j]);
                                reverseStorage.Add(body.records[i].record.SpellDescription[j], body.records[i].spellDesc[j]);
                            }
                        }
                    }
                }

                header.StringBlockSize = (int)stringBlockOffset;

                if (File.Exists("DBC/Spell.dbc")) { File.Delete("DBC/Spell.dbc"); }

                FileStream fileStream = new FileStream("DBC/Spell.dbc", FileMode.Create);
                BinaryWriter writer = new BinaryWriter(fileStream);
                int count = Marshal.SizeOf(typeof(DBC_Header));
                byte[] buffer = new byte[count];
                GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                Marshal.StructureToPtr(header, handle.AddrOfPinnedObject(), true);
                writer.Write(buffer, 0, count);
                handle.Free();

                for (UInt32 i = 0; i < header.RecordCount; ++i)
                {
                    count = Marshal.SizeOf(typeof(Spell_DBC_Record));
                    buffer = new byte[count];
                    handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    Marshal.StructureToPtr(body.records[i].record, handle.AddrOfPinnedObject(), true);
                    writer.Write(buffer, 0, count);
                    handle.Free();
                }

                UInt32[] offsetsStored = offsetStorage.Values.ToArray<UInt32>();

                writer.Write(Encoding.UTF8.GetBytes("\0"));

                for (int i = 0; i < offsetsStored.Length; ++i) { writer.Write(Encoding.UTF8.GetBytes(reverseStorage[offsetsStored[i]] + "\0")); }

                writer.Close();
                fileStream.Close();
            }

            catch (Exception ex)
            {
                main.HandleErrorMessage(ex.Message);

                return false;
            }

            return true;
        }

        public Task import(MySQL.MySQL mySQL, SpellEditor.MainWindow.UpdateProgressFunc UpdateProgress)
        {
            return Task.Run(() => 
            {
                UInt32 count = header.RecordCount;
                UInt32 index = 0;
                StringBuilder q = null;
                foreach (Spell_DBC_RecordMap r in body.records)
                {
                    if (index == 0 || index % 250 == 0)
                    {
                        if (q != null)
                        {
                            q.Remove(q.Length - 2, 2);
                            mySQL.execute(q.ToString());
                        }
                        q = new StringBuilder();
                        q.Append(String.Format("INSERT INTO `{0}` VALUES ", mySQL.Table));
                    }
                    if (++index % 1000 == 0)
                    {
                        double percent = (double)index / (double)count;
                        UpdateProgress(percent);
                    }
                    q.Append("(");
                    foreach (var f in r.record.GetType().GetFields())
                    {
                        switch (Type.GetTypeCode(f.FieldType))
                        {
                            case TypeCode.UInt32:
                            case TypeCode.Int32:
                            case TypeCode.Single:
                                {
                                    q.Append(String.Format("'{0}', ", f.GetValue(r.record)));
                                    break;
                                }
                            case TypeCode.Object:
                                {
                                    var attr = f.GetCustomAttribute<HandleField>();
                                    if (attr != null)
                                    {
                                        if (attr.Method == 1)
                                        {
                                            switch (attr.Type)
                                            {
                                                case 1:
                                                    {
                                                        for (int i = 0; i < attr.Count; ++i)
                                                            q.Append(String.Format("\"{0}\", ", MySqlHelper.EscapeString(r.spellName[i])));
                                                        break;
                                                    }
                                                case 2:
                                                    {
                                                        for (int i = 0; i < attr.Count; ++i)
                                                            q.Append(String.Format("\"{0}\", ", MySqlHelper.EscapeString(r.spellRank[i])));
                                                        break;
                                                    }
                                                case 3:
                                                    {
                                                        for (int i = 0; i < attr.Count; ++i)
                                                            q.Append(String.Format("\"{0}\", ", MySqlHelper.EscapeString(r.spellDesc[i])));
                                                        break;
                                                    }
                                                case 4:
                                                    {
                                                        for (int i = 0; i < attr.Count; ++i)
                                                            q.Append(String.Format("\"{0}\", ", MySqlHelper.EscapeString(r.spellTool[i])));
                                                        break;
                                                    }
                                                default:
                                                    throw new Exception("ERROR: Unhandled type: " + f.FieldType + " on field: " + f.Name + " TYPE: " + attr.Type);
                                            }
                                            break;
                                        }
                                        else if (attr.Method == 2)
                                        {
                                            switch (attr.Type)
                                            {
                                                case 1:
                                                    {
                                                        for (int i = 0; i < attr.Count; ++i)
                                                            q.Append(String.Format("\"{0}\", ", r.record.SpellNameFlag[i]));
                                                        break;
                                                    }
                                                case 2:
                                                    {
                                                        for (int i = 0; i < attr.Count; ++i)
                                                            q.Append(String.Format("\"{0}\", ", r.record.SpellRankFlags[i]));
                                                        break;
                                                    }
                                                case 3:
                                                    {
                                                        for (int i = 0; i < attr.Count; ++i)
                                                            q.Append(String.Format("\"{0}\", ", r.record.SpellDescriptionFlags[i]));
                                                        break;
                                                    }
                                                case 4:
                                                    {
                                                        for (int i = 0; i < attr.Count; ++i)
                                                            q.Append(String.Format("\"{0}\", ", r.record.SpellToolTipFlags[i]));
                                                        break;
                                                    }
                                                default:
                                                    throw new Exception("ERROR: Unhandled type: " + f.FieldType + " on field: " + f.Name + " TYPE: " + attr.Type);
                                            }
                                            break;
                                        }
                                    }
                                    goto default;
                                }
                            default:
                                throw new Exception("ERROR: Unhandled type: " + f.FieldType + " on field: " + f.Name);
                        }
                    }
                    q.Remove(q.Length - 2, 2);
                    q.Append("), ");
                }
                if (q.Length > 0)
                {
                    q.Remove(q.Length - 2, 2);
                    mySQL.execute(q.ToString());
                }
            });
        }
    }

    public struct Spell_DBC_Body
    {
        public Spell_DBC_RecordMap[] records;
    };

    [Serializable()]
    public struct Spell_DBC_RecordMap
    {
        public Spell_DBC_Record record;
        public string[] spellName;
        public string[] spellRank;
        public string[] spellDesc;
        public string[] spellTool;
    };

    [AttributeUsage(AttributeTargets.Field)]
    class HandleField : System.Attribute
    {
        public int Method { get; private set; }
        public int Count { get; private set; }
        public int Type { get; private set; }

        public HandleField(int type, int method, int count)
        {
            this.Type = type;
            this.Method = method;
            this.Count = count;
        }
    }

    [Serializable()]
    public struct Spell_DBC_Record
    {
        public UInt32 ID;
        public UInt32 Category;
        public UInt32 Dispel;
        public UInt32 Mechanic;
        public UInt32 Attributes;
        public UInt32 AttributesEx;
        public UInt32 AttributesEx2;
        public UInt32 AttributesEx3;
        public UInt32 AttributesEx4;
        public UInt32 AttributesEx5;
        public UInt32 AttributesEx6;
        public UInt32 AttributesEx7;
        public UInt32 Stances;
        public UInt32 Unknown1;
        public UInt32 StancesNot;
        public UInt32 Unknown2;
        public UInt32 Targets;
        public UInt32 TargetCreatureType;
        public UInt32 RequiresSpellFocus;
        public UInt32 FacingCasterFlags;
        public UInt32 CasterAuraState;
        public UInt32 TargetAuraState;
        public UInt32 CasterAuraStateNot;
        public UInt32 TargetAuraStateNot;
        public UInt32 CasterAuraSpell;
        public UInt32 TargetAuraSpell;
        public UInt32 ExcludeCasterAuraSpell;
        public UInt32 ExcludeTargetAuraSpell;
        public UInt32 CastingTimeIndex;
        public UInt32 RecoveryTime;
        public UInt32 CategoryRecoveryTime;
        public UInt32 InterruptFlags;
        public UInt32 AuraInterruptFlags;
        public UInt32 ChannelInterruptFlags;
        public UInt32 ProcFlags;
        public UInt32 ProcChance;
        public UInt32 ProcCharges;
        public UInt32 MaximumLevel;
        public UInt32 BaseLevel;
        public UInt32 SpellLevel;
        public UInt32 DurationIndex;
        public UInt32 PowerType;
        public UInt32 ManaCost;
        public UInt32 ManaCostPerLevel;
        public UInt32 ManaPerSecond;
        public UInt32 ManaPerSecondPerLevel;
        public UInt32 RangeIndex;
        public float Speed;
        public UInt32 ModalNextSpell;
        public UInt32 StackAmount;
        public UInt32 Totem1;
        public UInt32 Totem2;
        public Int32 Reagent1;
        public Int32 Reagent2;
        public Int32 Reagent3;
        public Int32 Reagent4;
        public Int32 Reagent5;
        public Int32 Reagent6;
        public Int32 Reagent7;
        public Int32 Reagent8;
        public UInt32 ReagentCount1;
        public UInt32 ReagentCount2;
        public UInt32 ReagentCount3;
        public UInt32 ReagentCount4;
        public UInt32 ReagentCount5;
        public UInt32 ReagentCount6;
        public UInt32 ReagentCount7;
        public UInt32 ReagentCount8;
        public Int32 EquippedItemClass;
        public Int32 EquippedItemSubClassMask;
        public Int32 EquippedItemInventoryTypeMask;
        public UInt32 Effect1;
        public UInt32 Effect2;
        public UInt32 Effect3;
        public Int32 EffectDieSides1;
        public Int32 EffectDieSides2;
        public Int32 EffectDieSides3;
        public float EffectRealPointsPerLevel1;
        public float EffectRealPointsPerLevel2;
        public float EffectRealPointsPerLevel3;
        public Int32 EffectBasePoints1;
        public Int32 EffectBasePoints2;
        public Int32 EffectBasePoints3;
        public UInt32 EffectMechanic1;
        public UInt32 EffectMechanic2;
        public UInt32 EffectMechanic3;
        public UInt32 EffectImplicitTargetA1;
        public UInt32 EffectImplicitTargetA2;
        public UInt32 EffectImplicitTargetA3;
        public UInt32 EffectImplicitTargetB1;
        public UInt32 EffectImplicitTargetB2;
        public UInt32 EffectImplicitTargetB3;
        public UInt32 EffectRadiusIndex1;
        public UInt32 EffectRadiusIndex2;
        public UInt32 EffectRadiusIndex3;
        public UInt32 EffectApplyAuraName1;
        public UInt32 EffectApplyAuraName2;
        public UInt32 EffectApplyAuraName3;
        public UInt32 EffectAmplitude1;
        public UInt32 EffectAmplitude2;
        public UInt32 EffectAmplitude3;
        public float EffectMultipleValue1;
        public float EffectMultipleValue2;
        public float EffectMultipleValue3;
        public UInt32 EffectChainTarget1;
        public UInt32 EffectChainTarget2;
        public UInt32 EffectChainTarget3;
        public UInt32 EffectItemType1;
        public UInt32 EffectItemType2;
        public UInt32 EffectItemType3;
        public Int32 EffectMiscValue1;
        public Int32 EffectMiscValue2;
        public Int32 EffectMiscValue3;
        public Int32 EffectMiscValueB1;
        public Int32 EffectMiscValueB2;
        public Int32 EffectMiscValueB3;
        public UInt32 EffectTriggerSpell1;
        public UInt32 EffectTriggerSpell2;
        public UInt32 EffectTriggerSpell3;
        public float EffectPointsPerComboPoint1;
        public float EffectPointsPerComboPoint2;
        public float EffectPointsPerComboPoint3;
        public UInt32 EffectSpellClassMaskA1;
        public UInt32 EffectSpellClassMaskA2;
        public UInt32 EffectSpellClassMaskA3;
        public UInt32 EffectSpellClassMaskB1;
        public UInt32 EffectSpellClassMaskB2;
        public UInt32 EffectSpellClassMaskB3;
        public UInt32 EffectSpellClassMaskC1;
        public UInt32 EffectSpellClassMaskC2;
        public UInt32 EffectSpellClassMaskC3;
        public UInt32 SpellVisual1;
        public UInt32 SpellVisual2;
        public UInt32 SpellIconID;
        public UInt32 ActiveIconID;
        public UInt32 SpellPriority;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        [HandleField(1, 1, 9)]
        public UInt32[] SpellName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        [HandleField(1, 2, 8)]
        public UInt32[] SpellNameFlag;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        [HandleField(2, 1, 9)]
        public UInt32[] SpellRank;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        [HandleField(2, 2, 8)]
        public UInt32[] SpellRankFlags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        [HandleField(3, 1, 9)]
        public UInt32[] SpellDescription;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        [HandleField(3, 2, 8)]
        public UInt32[] SpellDescriptionFlags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        [HandleField(4, 1, 9)]
        public UInt32[] SpellToolTip;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        [HandleField(4, 2, 8)]
        public UInt32[] SpellToolTipFlags;
        public UInt32 ManaCostPercentage;
        public UInt32 StartRecoveryCategory;
        public UInt32 StartRecoveryTime;
        public UInt32 MaximumTargetLevel;
        public UInt32 SpellFamilyName;
        public UInt32 SpellFamilyFlags;
        public UInt32 SpellFamilyFlags1;
        public UInt32 SpellFamilyFlags2;
        public UInt32 MaximumAffectedTargets;
        public UInt32 DamageClass;
        public UInt32 PreventionType;
        public UInt32 StanceBarOrder;
        public float EffectDamageMultiplier1;
        public float EffectDamageMultiplier2;
        public float EffectDamageMultiplier3;
        public UInt32 MinimumFactionId;
        public UInt32 MinimumReputation;
        public UInt32 RequiredAuraVision;
        public UInt32 TotemCategory1;
        public UInt32 TotemCategory2;
        public UInt32 AreaGroupID;
        public UInt32 SchoolMask;
        public UInt32 RuneCostID;
        public UInt32 SpellMissileID;
        public UInt32 PowerDisplayId;
        public float EffectBonusMultiplier1;
        public float EffectBonusMultiplier2;
        public float EffectBonusMultiplier3;
        public UInt32 SpellDescriptionVariableID;
        public UInt32 SpellDifficultyID;
    };
}
