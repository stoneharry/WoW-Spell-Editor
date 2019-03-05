using System;
using System.Data;
using System.Data.SQLite;
using System.Text;
using SpellEditor.Sources.Binding;
using System.Linq;

namespace SpellEditor.Sources.Database
{
    class SQLite : IDatabaseAdapter
    {
        private readonly object _syncLock = new object();
        private Config.Config _config;
        private SQLiteConnection _connection = null;
        public bool Updating
        {
            get;
            set;
        }

        public SQLite(Config.Config config)
        {
            _config = config;

            var connectionString = $"Data Source ={Environment.CurrentDirectory}\\SpellEditor.db";

            _connection = new SQLiteConnection(connectionString);
            _connection.Open();
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

        public DataTable Query(string query)
        {
            lock (_syncLock)
            {
                using (var adapter = new SQLiteDataAdapter(query, _connection))
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
                using (var adapter = new SQLiteDataAdapter())
                {
                    using (var mcb = new SQLiteCommandBuilder(adapter))
                    {
                        mcb.ConflictOption = ConflictOption.OverwriteChanges;
                        adapter.SelectCommand = new SQLiteCommand(query, _connection);
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
                            str.Append(string.Format(@"`{0}` INTEGER(10) NOT NULL DEFAULT '0', ", field.Name));
                            break;
                        }
                    case BindingType.INT:
                        {
                            str.Append(string.Format(@"`{0}` INTEGER(11) NOT NULL DEFAULT '0', ", field.Name));
                            break;
                        }
                    case BindingType.FLOAT:
                        {
                            str.Append(string.Format(@"`{0}` REAL NOT NULL DEFAULT '0', ", field.Name));
                            break;
                        }
                    case BindingType.STRING_OFFSET:
                        {
                            str.Append(string.Format(@"`{0}` TEXT, ", field.Name));
                            break;
                        }
                    default:
                        throw new Exception($"ERROR: Unhandled type: {field.Type} on field: {field.Name} on binding: {binding.Name}");

                }
            }
            var idField = binding.Fields.Where(record => record.Name.ToLower().Equals("id")).FirstOrDefault();
            if (idField != null)
                str.Append($"PRIMARY KEY (`{idField.Name}`)");
            else
                str.Remove(str.Length - 2, 2);
            str.Append(");");
            return str.ToString();
        }

        public string EscapeString(string keyWord)
        {
            keyWord = keyWord.Replace("'", "''");
            return keyWord;
        }
    }
}
