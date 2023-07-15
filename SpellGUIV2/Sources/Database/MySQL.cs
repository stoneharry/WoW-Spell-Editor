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
using System.Security.Policy;

namespace SpellEditor.Sources.Database
{
    public class MySQL : IDatabaseAdapter, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly object _syncLock = new object();
        private readonly MySqlConnection _connection;
        private Dictionary<string, Dictionary<string, string>> _tableColumns = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, string> _tableNames = new Dictionary<string, string>();
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

        public void ExportTableToSql(string tableName, string path, int? taskId, MainWindow.UpdateProgressFunc func)
        {
            var script = new StringBuilder();
            var dbTableName = tableName.ToLower();

            using (var cmd = new MySqlCommand())
            {
                using (var mb = new MySqlBackup(cmd))
                {
                    MySqlConnection conn = null;
                    lock (_syncLock)
                    {
                        conn = (MySqlConnection)_connection.Clone();
                    }
                    conn.Open();
                    conn.ChangeDatabase(Config.Config.Database);
                    cmd.Connection = conn;
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
                    mb.ExportProgressChanged += (sender, args) =>
                    {
                        var currentRowIndexInCurrentTable = args.CurrentRowIndexInCurrentTable;
                        var totalRowsInCurrentTable = args.TotalRowsInCurrentTable;
                        var progress =  0.9 * currentRowIndexInCurrentTable / totalRowsInCurrentTable;
                        if (taskId != null)
                        {
                            progress += (double)taskId;
                        }
                        func?.Invoke(progress);
                    };
                    script.AppendLine(mb.ExportToString());
                    conn.Close();
                }
            }

            // Then we replace the dbTableName names
            if (_tableNames.TryGetValue(dbTableName, out var tableNameValue))
            {
                script.Replace($"INTO `{dbTableName}`", $"INTO `{tableNameValue}`");
                script.Replace($"TABLE `{dbTableName}`", $"TABLE `{tableNameValue}`");
            }

            // Surprisingly, this is fast enough
            if (_tableColumns.TryGetValue(dbTableName, out var tableColumns))
            {
                foreach (var entry in tableColumns)
                {
                    script.Replace($"`{entry.Key}`", $"`{entry.Value}`");
                }
            }

            func?.Invoke(0.95);

            var bytes = Encoding.UTF8.GetBytes(script.ToString());
            var fileStream = new FileStream($"{path}/{tableName}.sql", FileMode.Create);
            fileStream.Write(bytes, 0, bytes.Length);
            fileStream.Close();

            func?.Invoke(1.0);
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
            var sqlMapperDir = Config.Config.SqlMapperDirectory;

            var tables = Directory.GetFiles(sqlMapperDir, ".tables", SearchOption.TopDirectoryOnly);
            var files = Directory.GetFiles(sqlMapperDir, "*.txt", SearchOption.TopDirectoryOnly);

            if (tables.Length == 1)
            {
                using (var reader = new StreamReader(tables[0]))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var split = line.ToLower().Split('=');
                        if (split.Length != 2)
                            continue;
                        _tableNames[split[0]] = split[1];
                    }
                }
            }

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                _tableColumns[fileName] = new Dictionary<string, string>();
                using (var reader = new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var split = line.Split('=');
                        if (split.Length != 2)
                        {
                            continue;
                        }
                        _tableColumns[fileName][split[0]] = split[1];
                    }
                }
            }
        }

    }
}
