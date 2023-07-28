﻿using MySql.Data.MySqlClient;
using SpellEditor.Sources.Binding;
using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using NLog;

namespace SpellEditor.Sources.Database
{
    public class MySQL : IDatabaseAdapter, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly object _syncLock = new object();
        private readonly MySqlConnection _connection;
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
                finally
                {
                    _connection.Dispose();
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
    }
}
