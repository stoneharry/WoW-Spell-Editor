using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Data;
using SpellEditor.Sources.Config;

namespace SpellEditor.Sources.DBC
{
    class SpellDBC : AbstractDBC
    {
        private MainWindow main;

        public static string ErrorMessage = "";

        public bool LoadDBCFile(MainWindow window)
        {
            main = window;

            try
            {
                ReadDBCFile<Spell_DBC_Record>("DBC/Spell.dbc");
            }
            catch (Exception ex)
            {
                main.HandleErrorMessage(ex.Message);
                return false;
            }
            return true;
        }

        private void SaveDBCFile(Spell_DBC_RecordMap[] recordMap)
        {
            uint stringBlockOffset = 1;

            Dictionary<int, uint> offsetStorage = new Dictionary<int, uint>();
            Dictionary<uint, string> reverseStorage = new Dictionary<uint, string>();

            // Populate string <-> offset lookup maps, this could do with a refactor
            for (uint i = 0; i < Header.RecordCount; ++i)
            {
                for (uint j = 0; j < 9; ++j)
                {
                    // spell name
                    if (recordMap[i].spellName[j].Length == 0)
                        recordMap[i].record.SpellName[j] = 0;
                    else
                    {
                        int key = recordMap[i].spellName[j].GetHashCode();
                        if (offsetStorage.ContainsKey(key))
                            recordMap[i].record.SpellName[j] = offsetStorage[key];
                        else
                        {
                            recordMap[i].record.SpellName[j] = stringBlockOffset;
                            stringBlockOffset += (uint) Encoding.UTF8.GetByteCount(recordMap[i].spellName[j]) + 1;
                            offsetStorage.Add(key, recordMap[i].record.SpellName[j]);
                            reverseStorage.Add(recordMap[i].record.SpellName[j], recordMap[i].spellName[j]);
                        }
                    }
                    // spell rank
                    if (recordMap[i].spellRank[j].Length == 0)
                        recordMap[i].record.SpellRank[j] = 0;
                    else
                    {
                        int key = recordMap[i].spellRank[j].GetHashCode();
                        if (offsetStorage.ContainsKey(key))
                            recordMap[i].record.SpellRank[j] = offsetStorage[key];
                        else
                        {
                            recordMap[i].record.SpellRank[j] = stringBlockOffset;
                            stringBlockOffset += (uint) Encoding.UTF8.GetByteCount(recordMap[i].spellRank[j]) + 1;
                            offsetStorage.Add(key, recordMap[i].record.SpellRank[j]);
                            reverseStorage.Add(recordMap[i].record.SpellRank[j], recordMap[i].spellRank[j]);
                        }
                    }
                    // spell tooltip
                    if (recordMap[i].spellTool[j].Length == 0)
                        recordMap[i].record.SpellToolTip[j] = 0;
                    else
                    {
                        int key = recordMap[i].spellTool[j].GetHashCode();
                        if (offsetStorage.ContainsKey(key))
                            recordMap[i].record.SpellToolTip[j] = offsetStorage[key];
                        else
                        {
                            recordMap[i].record.SpellToolTip[j] = stringBlockOffset;
                            stringBlockOffset += (uint) Encoding.UTF8.GetByteCount(recordMap[i].spellTool[j]) + 1;
                            offsetStorage.Add(key, recordMap[i].record.SpellToolTip[j]);
                            reverseStorage.Add(recordMap[i].record.SpellToolTip[j], recordMap[i].spellTool[j]);
                        }
                    }
                    // spell description
                    if (recordMap[i].spellDesc[j].Length == 0)
                        recordMap[i].record.SpellDescription[j] = 0;
                    else
                    {
                        int key = recordMap[i].spellDesc[j].GetHashCode();
                        if (offsetStorage.ContainsKey(key))
                            recordMap[i].record.SpellDescription[j] = offsetStorage[key];
                        else
                        {
                            recordMap[i].record.SpellDescription[j] = stringBlockOffset;
                            stringBlockOffset += (uint) Encoding.UTF8.GetByteCount(recordMap[i].spellDesc[j]) + 1;
                            offsetStorage.Add(key, recordMap[i].record.SpellDescription[j]);
                            reverseStorage.Add(recordMap[i].record.SpellDescription[j], recordMap[i].spellDesc[j]);
                        }
                    }
                }
            }

            Header.StringBlockSize = (int) stringBlockOffset;

            // Write spell.dbc file
            string path = "Export/Spell.dbc";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path))
                File.Delete(path);
            using (FileStream fileStream = new FileStream("Export/Spell.dbc", FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    int count = Marshal.SizeOf(typeof(DBCHeader));
                    byte[] buffer = new byte[count];
                    GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    Marshal.StructureToPtr(Header, handle.AddrOfPinnedObject(), true);
                    writer.Write(buffer, 0, count);
                    handle.Free();

                    for (uint i = 0; i < Header.RecordCount; ++i)
                    {
                        count = Marshal.SizeOf(typeof(Spell_DBC_Record));
                        buffer = new byte[count];
                        handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                        Marshal.StructureToPtr(recordMap[i].record, handle.AddrOfPinnedObject(), true);
                        writer.Write(buffer, 0, count);
                        handle.Free();
                    }

                    uint[] offsetsStored = offsetStorage.Values.ToArray();

                    writer.Write(Encoding.UTF8.GetBytes("\0"));

                    for (int i = 0; i < offsetsStored.Length; ++i)
                        writer.Write(Encoding.UTF8.GetBytes(reverseStorage[offsetsStored[i]] + "\0"));
                }
            }
        }

		public Task ImportTOSQL(DBAdapter adapter, MainWindow.UpdateProgressFunc UpdateProgress)
        {
            return ImportToSQL<Spell_DBC_Record>(adapter, UpdateProgress, "ID");
        }

		public static Spell_DBC_Record GetRecordById(uint id,MainWindow mainWindows)
		{
			DataRowCollection Result = mainWindows.GetDBAdapter().query(string.Format("SELECT * FROM `{0}` WHERE `ID` = '{1}'", mainWindows.GetConfig().Table, id)).Rows;
			if (Result != null && Result.Count == 1)
				return GetRowToRecord(Result[0]);
			return new Spell_DBC_Record();
		}

        public static Spell_DBC_Record GetRowToRecord(DataRow row)
        {
            var record = new Spell_DBC_Record();
            var fields = record.GetType().GetFields();
            foreach (var f in fields)
            {
                if (!row.Table.Columns.Contains(f.Name)) {
                    continue;
                }
                switch (Type.GetTypeCode(f.FieldType))
                {
                    case TypeCode.UInt32:
                        {
                            f.SetValueForValueType(ref record, uint.Parse(row[f.Name].ToString()));
                            break;
                        }
                    case TypeCode.Int32:
                        {
                            f.SetValueForValueType(ref record, int.Parse(row[f.Name].ToString()));
                            break;
                        }
                    case TypeCode.Single:
                        {
                            f.SetValueForValueType(ref record, Single.Parse(row[f.Name].ToString()));
                            break;
                        }
                }
            }
            return record;
        }

		public Task Export(DBAdapter adapter, MainWindow.UpdateProgressFunc updateProgress)
        {
            return ExportToDBC(adapter, updateProgress, "ID", "Spell");
            return Task.Run(() =>
            {
				var rows = adapter.query(string.Format("SELECT * FROM `{0}` ORDER BY `ID`", adapter.Table)).Rows;
                uint numRows = uint.Parse(rows.Count.ToString());
                // Hardcode for 3.3.5a 12340
                Header = new DBCHeader();
                Header.FieldCount = 234;
                Header.Magic = 1128416343;
                Header.RecordCount = numRows;
                Header.RecordSize = 936;
                Header.StringBlockSize = 0;

                var recordMap = new Spell_DBC_RecordMap[numRows];
                for (int i = 0; i < numRows; ++i)
                {
                    recordMap[i] = new Spell_DBC_RecordMap();
                    if (i % 250 == 0)
                    {
                        // Visual studio says these casts are redundant but it does not work without them
                        double percent = (double) i / (double) numRows;
                        updateProgress(percent);
                    }
                    recordMap[i].record = new Spell_DBC_Record();
                    recordMap[i].spellName = new string[9];
                    recordMap[i].spellDesc = new string[9];
                    recordMap[i].spellRank = new string[9];
                    recordMap[i].spellTool = new string[9];
                    recordMap[i].record.SpellName = new uint[9];
                    recordMap[i].record.SpellDescription = new uint[9];
                    recordMap[i].record.SpellRank = new uint[9];
                    recordMap[i].record.SpellToolTip = new uint[9];
                    recordMap[i].record.SpellNameFlag = new uint[8];
                    recordMap[i].record.SpellDescriptionFlags = new uint[8];
                    recordMap[i].record.SpellRankFlags = new uint[8];
                    recordMap[i].record.SpellToolTipFlags = new uint[8];
                    var fields = recordMap[i].record.GetType().GetFields();
                    foreach (var f in fields)
                    {
                        switch (Type.GetTypeCode(f.FieldType))
                        {
                            case TypeCode.UInt32:
                                {
                                    f.SetValueForValueType(ref recordMap[i].record, uint.Parse(rows[i][f.Name].ToString()));
                                    break;
                                }
                            case TypeCode.Int32:
                                {
                                    f.SetValueForValueType(ref recordMap[i].record, int.Parse(rows[i][f.Name].ToString()));
                                    break;
                                }
                            case TypeCode.Single:
                                {
                                    f.SetValueForValueType(ref recordMap[i].record, float.Parse(rows[i][f.Name].ToString()));
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
                                                        for (int j = 0; j < attr.Count; ++j)
                                                            recordMap[i].spellName[j] = rows[i]["SpellName" + j].ToString();
                                                        break;
                                                    }
                                                case 2:
                                                    {
                                                        for (int j = 0; j < attr.Count; ++j)
                                                            recordMap[i].spellRank[j] = rows[i]["SpellRank" + j].ToString();
                                                        break;
                                                    }
                                                case 3:
                                                    {
                                                        for (int j = 0; j < attr.Count; ++j)
                                                            recordMap[i].spellDesc[j] = rows[i]["SpellDescription" + j].ToString();
                                                        break;
                                                    }
                                                case 4:
                                                    {
                                                        for (int j = 0; j < attr.Count; ++j)
                                                            recordMap[i].spellTool[j] = rows[i]["SpellToolTip" + j].ToString();
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
                                                        for (int j = 0; j < attr.Count; ++j)
                                                            recordMap[i].record.SpellNameFlag[j] = uint.Parse(rows[i]["SpellNameFlag" + j].ToString());
                                                        break;
                                                    }
                                                case 2:
                                                    {
                                                        for (int j = 0; j < attr.Count; ++j)
                                                            recordMap[i].record.SpellRankFlags[j] = uint.Parse(rows[i]["SpellRankFlags" + j].ToString());
                                                        break;
                                                    }
                                                case 3:
                                                    {
                                                        for (int j = 0; j < attr.Count; ++j)
                                                            recordMap[i].record.SpellDescriptionFlags[j] = uint.Parse(rows[i]["SpellDescriptionFlags" + j].ToString());
                                                        break;
                                                    }
                                                case 4:
                                                    {
                                                        for (int j = 0; j < attr.Count; ++j)
                                                            recordMap[i].record.SpellToolTipFlags[j] = uint.Parse(rows[i]["SpellToolTipFlags" + j].ToString());
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
                                throw new Exception("Unhandled type: " + Type.GetTypeCode(f.FieldType).ToString() + ", field: " + f.Name);
                        }
                    }
                }
                SaveDBCFile(recordMap);
            });
        }
    }

    //[CLSCompliant(true)]
    static class Hlp
    {
        public static void SetValueForValueType<T>(this FieldInfo field, ref T item, object value) where T : struct
        {
            field.SetValueDirect(__makeref(item), value);
        }
    }

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
        public uint ID;
        public uint Category;
        public uint Dispel;
        public uint Mechanic;
        public uint Attributes;
        public uint AttributesEx;
        public uint AttributesEx2;
        public uint AttributesEx3;
        public uint AttributesEx4;
        public uint AttributesEx5;
        public uint AttributesEx6;
        public uint AttributesEx7;
        public uint Stances;
        public uint Unknown1;
        public uint StancesNot;
        public uint Unknown2;
        public uint Targets;
        public uint TargetCreatureType;
        public uint RequiresSpellFocus;
        public uint FacingCasterFlags;
        public uint CasterAuraState;
        public uint TargetAuraState;
        public uint CasterAuraStateNot;
        public uint TargetAuraStateNot;
        public uint CasterAuraSpell;
        public uint TargetAuraSpell;
        public uint ExcludeCasterAuraSpell;
        public uint ExcludeTargetAuraSpell;
        public uint CastingTimeIndex;
        public uint RecoveryTime;
        public uint CategoryRecoveryTime;
        public uint InterruptFlags;
        public uint AuraInterruptFlags;
        public uint ChannelInterruptFlags;
        public uint ProcFlags;
        public uint ProcChance;
        public uint ProcCharges;
        public uint MaximumLevel;
        public uint BaseLevel;
        public uint SpellLevel;
        public uint DurationIndex;
        public uint PowerType;
        public uint ManaCost;
        public uint ManaCostPerLevel;
        public uint ManaPerSecond;
        public uint ManaPerSecondPerLevel;
        public uint RangeIndex;
        public float Speed;
        public uint ModalNextSpell;
        public uint StackAmount;
        public uint Totem1;
        public uint Totem2;
        public int Reagent1;
        public int Reagent2;
        public int Reagent3;
        public int Reagent4;
        public int Reagent5;
        public int Reagent6;
        public int Reagent7;
        public int Reagent8;
        public uint ReagentCount1;
        public uint ReagentCount2;
        public uint ReagentCount3;
        public uint ReagentCount4;
        public uint ReagentCount5;
        public uint ReagentCount6;
        public uint ReagentCount7;
        public uint ReagentCount8;
        public int EquippedItemClass;
        public int EquippedItemSubClassMask;
        public int EquippedItemInventoryTypeMask;
        public uint Effect1;
        public uint Effect2;
        public uint Effect3;
        public int EffectDieSides1;
        public int EffectDieSides2;
        public int EffectDieSides3;
        public float EffectRealPointsPerLevel1;
        public float EffectRealPointsPerLevel2;
        public float EffectRealPointsPerLevel3;
        public int EffectBasePoints1;
        public int EffectBasePoints2;
        public int EffectBasePoints3;
        public uint EffectMechanic1;
        public uint EffectMechanic2;
        public uint EffectMechanic3;
        public uint EffectImplicitTargetA1;
        public uint EffectImplicitTargetA2;
        public uint EffectImplicitTargetA3;
        public uint EffectImplicitTargetB1;
        public uint EffectImplicitTargetB2;
        public uint EffectImplicitTargetB3;
        public uint EffectRadiusIndex1;
        public uint EffectRadiusIndex2;
        public uint EffectRadiusIndex3;
        public uint EffectApplyAuraName1;
        public uint EffectApplyAuraName2;
        public uint EffectApplyAuraName3;
        public uint EffectAmplitude1;
        public uint EffectAmplitude2;
        public uint EffectAmplitude3;
        public float EffectMultipleValue1;
        public float EffectMultipleValue2;
        public float EffectMultipleValue3;
        public uint EffectChainTarget1;
        public uint EffectChainTarget2;
        public uint EffectChainTarget3;
        public uint EffectItemType1;
        public uint EffectItemType2;
        public uint EffectItemType3;
        public int EffectMiscValue1;
        public int EffectMiscValue2;
        public int EffectMiscValue3;
        public int EffectMiscValueB1;
        public int EffectMiscValueB2;
        public int EffectMiscValueB3;
        public uint EffectTriggerSpell1;
        public uint EffectTriggerSpell2;
        public uint EffectTriggerSpell3;
        public float EffectPointsPerComboPoint1;
        public float EffectPointsPerComboPoint2;
        public float EffectPointsPerComboPoint3;
        public uint EffectSpellClassMaskA1;
        public uint EffectSpellClassMaskA2;
        public uint EffectSpellClassMaskA3;
        public uint EffectSpellClassMaskB1;
        public uint EffectSpellClassMaskB2;
        public uint EffectSpellClassMaskB3;
        public uint EffectSpellClassMaskC1;
        public uint EffectSpellClassMaskC2;
        public uint EffectSpellClassMaskC3;
        public uint SpellVisual1;
        public uint SpellVisual2;
        public uint SpellIconID;
        public uint ActiveIconID;
        public uint SpellPriority;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        [HandleField(1, 1, 9)]
        public uint[] SpellName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        [HandleField(1, 2, 8)]
        public uint[] SpellNameFlag;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        [HandleField(2, 1, 9)]
        public uint[] SpellRank;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        [HandleField(2, 2, 8)]
        public uint[] SpellRankFlags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        [HandleField(3, 1, 9)]
        public uint[] SpellDescription;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        [HandleField(3, 2, 8)]
        public uint[] SpellDescriptionFlags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        [HandleField(4, 1, 9)]
        public uint[] SpellToolTip;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        [HandleField(4, 2, 8)]
        public uint[] SpellToolTipFlags;
        public uint ManaCostPercentage;
        public uint StartRecoveryCategory;
        public uint StartRecoveryTime;
        public uint MaximumTargetLevel;
        public uint SpellFamilyName;
        public uint SpellFamilyFlags;
        public uint SpellFamilyFlags1;
        public uint SpellFamilyFlags2;
        public uint MaximumAffectedTargets;
        public uint DamageClass;
        public uint PreventionType;
        public uint StanceBarOrder;
        public float EffectDamageMultiplier1;
        public float EffectDamageMultiplier2;
        public float EffectDamageMultiplier3;
        public uint MinimumFactionId;
        public uint MinimumReputation;
        public uint RequiredAuraVision;
        public uint TotemCategory1;
        public uint TotemCategory2;
        public uint AreaGroupID;
        public uint SchoolMask;
        public uint RuneCostID;
        public uint SpellMissileID;
        public uint PowerDisplayId;
        public float EffectBonusMultiplier1;
        public float EffectBonusMultiplier2;
        public float EffectBonusMultiplier3;
        public uint SpellDescriptionVariableID;
        public uint SpellDifficultyID;
    };
}
