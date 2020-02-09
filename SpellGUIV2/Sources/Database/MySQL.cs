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
        private readonly MySqlConnection _connection;
        public bool Updating { get; set; }

        public MySQL()
        {
            string connectionString = $"server={Config.Config.Host};port={Config.Config.Port};uid={Config.Config.User};pwd={Config.Config.Pass};Charset=utf8;";

            _connection = new MySqlConnection {ConnectionString = connectionString};
            _connection.Open();
            // Create DB if not exists and use
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = string.Format("CREATE DATABASE IF NOT EXISTS `{0}`; USE `{0}`;", Config.Config.Database);
                cmd.ExecuteNonQuery();
            }
        }

        ~MySQL()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
                _connection.Close();
        }

        public void CreateAllTablesFromBindings()
        {
            foreach (var binding in BindingManager.GetInstance().GetAllBindings())
            {
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = string.Format(GetTableCreateString(binding), binding.Name);
                    cmd.ExecuteNonQuery();
                }
            }
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
                        str.Append($@"`{field.Name}` int(10) unsigned NOT NULL DEFAULT '0', ");
                        break;
                    case BindingType.INT:
                        str.Append($@"`{field.Name}` int(11) NOT NULL DEFAULT '0', ");
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
