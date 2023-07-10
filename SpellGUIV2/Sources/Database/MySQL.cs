using MySql.Data.MySqlClient;
using SpellEditor.Sources.Binding;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Documents;
using NLog;

namespace SpellEditor.Sources.Database
{
    public class MySQL : IDatabaseAdapter, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly object _syncLock = new object();
        private readonly MySqlConnection _connection;
        private Dictionary<string, List<Tuple<string, string>>> _tableColumns = new Dictionary<string, List<Tuple<string, string>>>();
        private Timer _heartbeat;
        public bool Updating { get; set; }

        public MySQL(bool initialiseDatabase)
        {
            string connectionString = $"server={Config.Config.Host};port={Config.Config.Port};uid={Config.Config.User};pwd={Config.Config.Pass};Charset=utf8mb4;";

            _connection = new MySqlConnection { ConnectionString = connectionString };
            _connection.Open();

            if (initialiseDatabase)
            {
                // Create DB if not exists and use
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = string.Format("CREATE DATABASE IF NOT EXISTS `{0}`; USE `{0}`;", Config.Config.Database);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                // Use DB
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = string.Format("USE `{0}`;", Config.Config.Database);
                    cmd.ExecuteNonQuery();
                }
            }

            CreateDatabasesTablesColumns();

            // Heartbeat keeps the connection alive, otherwise it can be killed by remote for inactivity
            // Object reference needs to be held to prevent garbage collection.
            _heartbeat = CreateKeepAliveTimer(TimeSpan.FromMinutes(2));
        }

        public void Dispose()
        {
            _heartbeat?.Dispose();
            _heartbeat = null;
            if (_connection != null && _connection.State != ConnectionState.Closed)
            {
                try
                {
                    _connection.Close();
                }
                catch (Exception exception)
                {
                    Logger.Error(exception);
                }
            }
        }

        public void CreateAllTablesFromBindings()
        {
            lock (_syncLock)
            {
                foreach (var binding in BindingManager.GetInstance().GetAllBindings())
                {
                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.CommandText = string.Format(GetTableCreateString(binding), binding.Name.ToLower());
                        Logger.Trace(cmd.CommandText);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public DataTable Query(string query)
        {
            Logger.Trace(query);
            lock (_syncLock)
            {
                using (var adapter = new MySqlDataAdapter(query, _connection))
                {
                    using (var dataSet = new DataSet())
                    {
                        adapter.SelectCommand.CommandTimeout = 0;
                        adapter.Fill(dataSet);
                        return dataSet.Tables[0];
                    }
                }
            }
        }

        public void ExportTableToSql(string tableName, string path = "Export", int? taskId = null, MainWindow.UpdateProgressFunc func = null)
        {
            var script = new StringBuilder();
            var dbTableName = tableName.ToLower();

            lock (_syncLock)
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    using (MySqlBackup mb = new MySqlBackup(cmd))
                    {
                        cmd.Connection = _connection;
                        var tableList = new List<string>()
                        {
                            dbTableName
                        };
                        mb.ExportInfo.TablesToBeExportedList = tableList;
                        mb.ExportInfo.ExportTableStructure = false;
                        mb.ExportInfo.ExportRows = true;
                        mb.ExportInfo.EnableComment = false;
                        mb.ExportInfo.RowsExportMode = RowsDataExportMode.Replace;
                        mb.ExportInfo.GetTotalRowsMode = GetTotalRowsMethod.InformationSchema;
                        if (func != null)
                        {
                            mb.ExportProgressChanged += (sender, args) =>
                            {
                                var currentRowIndexInCurrentTable = (int)args.CurrentRowIndexInCurrentTable;
                                var totalRowsInCurrentTable = (int)args.TotalRowsInCurrentTable;
                                var progress = 0.8 * (double)currentRowIndexInCurrentTable / (double)totalRowsInCurrentTable;
                                func(progress);
                            };
                        }
                        script.AppendLine(mb.ExportToString());
                    }
                }
            }

            // TODO(Harry): This shouldn't be hardcoded
            script.Replace($"INTO `{dbTableName}`", $"INTO `{dbTableName}_dbc`");
            script.Replace($"TABLE `{dbTableName}`", $"TABLE `{dbTableName}_dbc`");

            // TODO(Harry): Should come from a file instead of hardcoding
            if (dbTableName.Equals("spell"))
            {
                FormatSpellTableScript(script);
            }

            // Not the fastest way, but it works
            if (_tableColumns.TryGetValue(dbTableName, out var tableColumn))
            {
                tableColumn.Aggregate(script, (current, column) => current.Replace(column.Item1, column.Item2));
            }

            if (func != null)
            {
                func(0.9);
            }

            var bytes = Encoding.UTF8.GetBytes(script.ToString());
            FileStream fileStream = new FileStream($"{path}/{tableName}.sql", FileMode.Create);
            fileStream.Write(bytes, 0, bytes.Length);
            fileStream.Close();
        }

        public object QuerySingleValue(string query)
        {
            Logger.Trace(query);
            lock (_syncLock)
            {
                using (var adapter = new MySqlDataAdapter(query, _connection))
                {
                    using (var dataSet = new DataSet())
                    {
                        adapter.SelectCommand.CommandTimeout = 0;
                        adapter.Fill(dataSet);
                        var table = dataSet.Tables[0];
                        return table.Rows.Count > 0 ? table.Rows[0][0] : null;
                    }
                }
            }
        }

        public void CommitChanges(string query, DataTable dataTable)
        {
            if (Updating)
                return;

            Logger.Trace(query);
            lock (_syncLock)
            {
                using (var adapter = new MySqlDataAdapter())
                {
                    using (var mcb = new MySqlCommandBuilder(adapter))
                    {
                        mcb.ConflictOption = ConflictOption.OverwriteChanges;
                        adapter.SelectCommand = new MySqlCommand(query, _connection);
                        adapter.Update(dataTable);
                        dataTable.AcceptChanges();
                    }
                }
            }
        }

        public void Execute(string p)
        {
            if (Updating)
                return;

            Logger.Trace(p);
            lock (_syncLock)
            {
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = p;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public string GetTableCreateString(Binding.Binding binding)
        {
            StringBuilder str = new StringBuilder();
            str.Append(@"CREATE TABLE IF NOT EXISTS `{0}` (");
            foreach (var field in binding.Fields)
            {
                switch (field.Type)
                {
                    case BindingType.UINT:
                        str.Append($@"`{field.Name}` int(10) unsigned NOT NULL DEFAULT '0', ");
                        break;
                    case BindingType.INT:
                        str.Append($@"`{field.Name}` int(11) NOT NULL DEFAULT '0', ");
                        break;
                    case BindingType.UINT8:
                        str.Append($@"`{field.Name}` tinyint unsigned NOT NULL DEFAULT '0', ");
                        break;
                    case BindingType.FLOAT:
                        str.Append($@"`{field.Name}` FLOAT NOT NULL DEFAULT '0', ");
                        break;
                    case BindingType.STRING_OFFSET:
                        str.Append($@"`{field.Name}` TEXT CHARACTER SET utf8, ");
                        break;
                    default:
                        throw new Exception($"ERROR: Unhandled type: {field.Type} on field: {field.Name} on binding: {binding.Name}");

                }
            }

            var idField = binding.Fields.FirstOrDefault(record => record.Name.ToLower().Equals("id"));
            if (idField != null && binding.OrderOutput)
                str.Append($"PRIMARY KEY (`{idField.Name}`)) ");
            else
            {
                str = str.Remove(str.Length - 2, 2);
                str = str.Append(") ");
            }
            str.Append("ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 ROW_FORMAT=COMPRESSED KEY_BLOCK_SIZE=8;");
            return str.ToString();
        }

        public string EscapeString(string keyWord)
        {
            keyWord = keyWord.Replace("'", "''");
            keyWord = keyWord.Replace("\\", "\\\\");
            return keyWord;
        }

        private Timer CreateKeepAliveTimer(TimeSpan interval)
        {
            return new Timer(
                (e) => Execute("SELECT 1"),
                null,
                interval,
                interval);
        }

        private void CreateDatabasesTablesColumns()
        {
            _tableColumns["spell"] = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("Dispel","DispelType"),
                new Tuple<string, string>("Stances","ShapeshiftMask"),
                new Tuple<string, string>("Unknown1","unk_320_2"),
                new Tuple<string, string>("ShapeshiftMaskNot","ShapeshiftExclude"),
                new Tuple<string, string>("Unknown2","unk_320_3"),
                new Tuple<string, string>("CasterAuraStateNot","ExcludeCasterAuraState"),
                new Tuple<string, string>("TargetAuraStateNot","ExcludeTargetAuraState"),
                new Tuple<string, string>("ProcFlags","ProcTypeMask"),
                new Tuple<string, string>("MaximumLevel","MaxLevel"),
                new Tuple<string, string>("StackAmount","CumulativeAura"),
                new Tuple<string, string>("Totem1","Totem_1"),
                new Tuple<string, string>("Totem2","Totem_2"),
                new Tuple<string, string>("Reagent1","Reagent_1"),
                new Tuple<string, string>("Reagent2","Reagent_2"),
                new Tuple<string, string>("Reagent3","Reagent_3"),
                new Tuple<string, string>("Reagent4","Reagent_4"),
                new Tuple<string, string>("Reagent5","Reagent_5"),
                new Tuple<string, string>("Reagent6","Reagent_6"),
                new Tuple<string, string>("Reagent7","Reagent_7"),
                new Tuple<string, string>("Reagent8","Reagent_8"),
                new Tuple<string, string>("Effect1","Effect_1"),
                new Tuple<string, string>("Effect2","Effect_2"),
                new Tuple<string, string>("Effect3","Effect_3"),
                new Tuple<string, string>("EffectMiscValue1","EffectMiscValue_1"),
                new Tuple<string, string>("EffectMiscValue2","EffectMiscValue_2"),
                new Tuple<string, string>("EffectMiscValue3","EffectMiscValue_3"),
                new Tuple<string, string>("EquippedItemSubClassMask","EquippedItemSubclass"),
                new Tuple<string, string>("EquippedItemInventoryTypeMask","EquippedItemInvTypes"),

                new Tuple<string, string>("SpellName0","Name_Lang_enUS"),
                new Tuple<string, string>("SpellName1","Name_Lang_enGB"),
                new Tuple<string, string>("SpellName2","Name_Lang_koKR"),
                new Tuple<string, string>("SpellName3","Name_Lang_frFR"),
                new Tuple<string, string>("SpellName4","Name_Lang_deDE"),
                new Tuple<string, string>("SpellName5","Name_Lang_enCN"),
                new Tuple<string, string>("SpellName6","Name_Lang_zhCN"),
                new Tuple<string, string>("SpellName7","Name_Lang_enTW"),
                new Tuple<string, string>("SpellName8","Name_Lang_zhTW"),
                new Tuple<string, string>("SpellNameFlag0","Name_Lang_esES"),
                new Tuple<string, string>("SpellNameFlag1","Name_Lang_esMX"),
                new Tuple<string, string>("SpellNameFlag2","Name_Lang_ruRU"),
                new Tuple<string, string>("SpellNameFlag3","Name_Lang_ptPT"),
                new Tuple<string, string>("SpellNameFlag4","Name_Lang_ptBR"),
                new Tuple<string, string>("SpellNameFlag5","Name_Lang_itIT"),
                new Tuple<string, string>("SpellNameFlag6","Name_Lang_Unk"),
                new Tuple<string, string>("SpellNameFlag7","Name_Lang_Mask"),

                new Tuple<string, string>("SpellRank0","NameSubtext_Lang_enUS"),
                new Tuple<string, string>("SpellRank1","NameSubtext_Lang_enGB"),
                new Tuple<string, string>("SpellRank2","NameSubtext_Lang_koKR"),
                new Tuple<string, string>("SpellRank3","NameSubtext_Lang_frFR"),
                new Tuple<string, string>("SpellRank4","NameSubtext_Lang_deDE"),
                new Tuple<string, string>("SpellRank5","NameSubtext_Lang_enCN"),
                new Tuple<string, string>("SpellRank6","NameSubtext_Lang_zhCN"),
                new Tuple<string, string>("SpellRank7","NameSubtext_Lang_enTW"),
                new Tuple<string, string>("SpellRank8","NameSubtext_Lang_zhTW"),
                new Tuple<string, string>("SpellRankFlags0","NameSubtext_Lang_esES"),
                new Tuple<string, string>("SpellRankFlags1","NameSubtext_Lang_esMX"),
                new Tuple<string, string>("SpellRankFlags2","NameSubtext_Lang_ruRU"),
                new Tuple<string, string>("SpellRankFlags3","NameSubtext_Lang_ptPT"),
                new Tuple<string, string>("SpellRankFlags4","NameSubtext_Lang_ptBR"),
                new Tuple<string, string>("SpellRankFlags5","NameSubtext_Lang_itIT"),
                new Tuple<string, string>("SpellRankFlags6","NameSubtext_Lang_Unk"),
                new Tuple<string, string>("SpellRankFlags7","NameSubtext_Lang_Mask"),
                                                        
                new Tuple<string, string>("SpellDescription0","Description_Lang_enUS"),
                new Tuple<string, string>("SpellDescription1","Description_Lang_enGB"),
                new Tuple<string, string>("SpellDescription2","Description_Lang_koKR"),
                new Tuple<string, string>("SpellDescription3","Description_Lang_frFR"),
                new Tuple<string, string>("SpellDescription4","Description_Lang_deDE"),
                new Tuple<string, string>("SpellDescription5","Description_Lang_enCN"),
                new Tuple<string, string>("SpellDescription6","Description_Lang_zhCN"),
                new Tuple<string, string>("SpellDescription7","Description_Lang_enTW"),
                new Tuple<string, string>("SpellDescription8","Description_Lang_zhTW"),
                new Tuple<string, string>("SpellDescriptionFlags0","Description_Lang_esES"),
                new Tuple<string, string>("SpellDescriptionFlags1","Description_Lang_esMX"),
                new Tuple<string, string>("SpellDescriptionFlags2","Description_Lang_ruRU"),
                new Tuple<string, string>("SpellDescriptionFlags3","Description_Lang_ptPT"),
                new Tuple<string, string>("SpellDescriptionFlags4","Description_Lang_ptBR"),
                new Tuple<string, string>("SpellDescriptionFlags5","Description_Lang_itIT"),
                new Tuple<string, string>("SpellDescriptionFlags6","Description_Lang_Unk"),
                new Tuple<string, string>("SpellDescriptionFlags7","Description_Lang_Mask"),

                new Tuple<string, string>("SpellToolTip0","AuraDescription_Lang_enUS"),
                new Tuple<string, string>("SpellToolTip1","AuraDescription_Lang_enGB"),
                new Tuple<string, string>("SpellToolTip2","AuraDescription_Lang_koKR"),
                new Tuple<string, string>("SpellToolTip3","AuraDescription_Lang_frFR"),
                new Tuple<string, string>("SpellToolTip4","AuraDescription_Lang_deDE"),
                new Tuple<string, string>("SpellToolTip5","AuraDescription_Lang_enCN"),
                new Tuple<string, string>("SpellToolTip6","AuraDescription_Lang_zhCN"),
                new Tuple<string, string>("SpellToolTip7","AuraDescription_Lang_enTW"),
                new Tuple<string, string>("SpellToolTip8","AuraDescription_Lang_zhTW"),
                new Tuple<string, string>("SpellToolTipFlags0","AuraDescription_Lang_esES"),
                new Tuple<string, string>("SpellToolTipFlags1","AuraDescription_Lang_esMX"),
                new Tuple<string, string>("SpellToolTipFlags2","AuraDescription_Lang_ruRU"),
                new Tuple<string, string>("SpellToolTipFlags3","AuraDescription_Lang_ptPT"),
                new Tuple<string, string>("SpellToolTipFlags4","AuraDescription_Lang_ptBR"),
                new Tuple<string, string>("SpellToolTipFlags5","AuraDescription_Lang_itIT"),
                new Tuple<string, string>("SpellToolTipFlags6","AuraDescription_Lang_Unk"),
                new Tuple<string, string>("SpellToolTipFlags7","AuraDescription_Lang_Mask"),

                new Tuple<string, string>("AreaGroupID","RequiredAreasID"),
                new Tuple<string, string>("MinimumFactionId","MinFactionID"),
                new Tuple<string, string>("MinimumReputation","MinReputation"),
                new Tuple<string, string>("DamageClass","DefenseType"),
                new Tuple<string, string>("MaximumAffectedTargets","MaxTargets"),
                new Tuple<string, string>("SpellFamilyName","SpellClassSet"),
                new Tuple<string, string>("MaximumTargetLevel","MaxTargetLevel"),
                new Tuple<string, string>("ManaCostPercentage","ManaCostPct"),

                new Tuple<string, string>("SpellFamilyFlags1","SpellClassMask_2"),
                new Tuple<string, string>("SpellFamilyFlags2","SpellClassMask_3"),
                new Tuple<string, string>("SpellFamilyFlags","SpellClassMask_1"),
            };

            _tableColumns["spellvisual"] = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("MissileFollowDropSpeed","MissileFollowGroundDropSpeed"),
                new Tuple<string, string>("MissileFollowApproach","MissileFollowGroundApproach"),
            };

        }

        private void FormatSpellTableScript(StringBuilder script)
        {
            script.Replace("ReagentCount", "ReagentCount_");
            script.Replace("EffectDieSides", "EffectDieSides_");
            script.Replace("EffectRealPointsPerLevel", "EffectRealPointsPerLevel_");
            script.Replace("EffectBasePoints", "EffectBasePoints_");
            script.Replace("EffectMechanic", "EffectMechanic_");
            script.Replace("EffectImplicitTargetA", "ImplicitTargetA_");
            script.Replace("EffectImplicitTargetB", "ImplicitTargetB_");
            script.Replace("EffectRadiusIndex", "EffectRadiusIndex_");
            script.Replace("EffectAura", "EffectAura_");
            script.Replace("EffectAuraPeriod", "EffectAuraPeriod_");
            script.Replace("EffectMultipleValue", "EffectMultipleValue_");
            script.Replace("EffectChainTargets", "EffectChainTargets_");
            script.Replace("EffectItemType", "EffectItemType_");
            script.Replace("EffectMiscValueB", "EffectMiscValueB_");
            script.Replace("EffectTriggerSpell", "EffectTriggerSpell_");
            script.Replace("EffectPointsPerComboPoint", "EffectPointsPerCombo_");
            script.Replace("EffectSpellClassMaskA", "EffectSpellClassMaskA_");
            script.Replace("EffectSpellClassMaskB", "EffectSpellClassMaskB_");
            script.Replace("EffectSpellClassMaskC", "EffectSpellClassMaskC_");
            script.Replace("EffectApplyAuraName", "EffectAura_");
            script.Replace("EffectAmplitude", "EffectAuraPeriod_");
            script.Replace("EffectChainTarget", "EffectChainTargets_");
            script.Replace("SpellVisual", "SpellVisualID_");
            script.Replace("EffectBonusMultiplier", "EffectBonusMultiplier_");
            script.Replace("TotemCategory", "RequiredTotemCategoryID_");
            script.Replace("EffectDamageMultiplier", "EffectChainAmplitude_");

        }
    }
}
