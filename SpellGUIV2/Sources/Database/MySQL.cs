using MySql.Data.MySqlClient;
using SpellEditor.Sources.Binding;
using System;
using System.Data;
using System.Linq;
using System.Text;

namespace SpellEditor.Sources.Database
{
    class MySQL : IDatabaseAdapter
    {
        private readonly object _syncLock = new object();
        private Config.Config _config;
        private MySqlConnection _connection;
        public bool Updating
        {
            get;
            set;
        }

        public MySQL(Config.Config config)
        {
            _config = config;

            string connectionString = "server={0};port={1};uid={2};pwd={3};Charset=utf8;";
                connectionString = string.Format(connectionString,
                config.Host, config.Port, config.User, config.Pass);

            _connection = new MySqlConnection();
            _connection.ConnectionString = connectionString;
            _connection.Open();
            // Create DB if not exists and use
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = string.Format("CREATE DATABASE IF NOT EXISTS `{0}`; USE `{0}`;", config.Database);
                cmd.ExecuteNonQuery();
            }
            // Create binding tables
            foreach (var binding in BindingManager.GetInstance().GetAllBindings())
            {
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = string.Format(GetTableCreateString(binding), binding.Name);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        ~MySQL()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
                _connection.Close();
        }

        public DataTable Query(string query)
        {
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

        public void CommitChanges(string query, DataTable dataTable)
        {
            if (Updating)
                return;
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
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = p;
                cmd.ExecuteNonQuery();
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
                        {
                            str.Append(string.Format(@"`{0}` int(10) unsigned NOT NULL DEFAULT '0', ", field.Name));
                            break;
                        }
                    case BindingType.INT:
                        {
                            str.Append(string.Format(@"`{0}` int(11) NOT NULL DEFAULT '0', ", field.Name));
                            break;
                        }
                    case BindingType.FLOAT:
                        {
                            str.Append(string.Format(@"`{0}` FLOAT NOT NULL DEFAULT '0', ", field.Name));
                            break;
                        }
                    case BindingType.STRING_OFFSET:
                        {
                            str.Append(string.Format(@"`{0}` TEXT CHARACTER SET utf8, ", field.Name));
                            break;
                        }
                    default:
                        throw new Exception($"ERROR: Unhandled type: {field.Type} on field: {field.Name} on binding: {binding.Name}");

                }
            }
            var idField = binding.Fields.Where(record => record.Name.ToLower().Equals("id")).FirstOrDefault();
            if (idField != null)
                str.Append($"PRIMARY KEY (`{idField.Name}`)) ");
            else
            {
                str = str.Remove(str.Length - 2, 2);
                str = str.Append(") ");
            } 
            str.Append("ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=COMPACT;");   
            return str.ToString();
        }

        public string EscapeString(string keyWord)
        {
            keyWord = keyWord.Replace("'", "''");
            return keyWord;
        }
    }
}
