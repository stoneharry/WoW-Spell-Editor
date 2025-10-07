using System;
using System.Data;
using System.Data.SQLite;
using NLog;

namespace SpellEditor.Sources.Database
{
    public class SQLite : AbstractAdapter
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private SQLiteConnection _connection;

        public SQLite()
        {
            var connectionString = $"Data Source ={Environment.CurrentDirectory}\\{Config.Config.SQLiteFilename}.db";

            _connection = new SQLiteConnection(connectionString);
            _connection.Open();
        }

        public override void Dispose()
        {
            try
            {
                _connection?.Close();
            }
            finally
            {
                _connection?.Dispose();
            }
        }

        public override void CreateAllTablesFromBindings()
        {

            lock (_syncLock)
            {
                foreach (var createStatement in GetAllTableCreateStrings())
                {
                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.CommandText = createStatement;
                        Logger.Trace(cmd.CommandText);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public override DataTable Query(string query)
        {
            Logger.Trace(query);
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

        public override object QuerySingleValue(string query)
        {
            Logger.Trace(query);
            lock (_syncLock)
            {
                using (var adapter = new SQLiteDataAdapter(query, _connection))
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

        public override void CommitChanges(string query, DataTable dataTable)
        {
            if (Updating)
                return;
            Logger.Trace(query);

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

        public override void Execute(string p)
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
        /*
        public string GetTableCreateString(Binding.Binding binding)
        {
            StringBuilder str = new StringBuilder();
            str.Append(@"CREATE TABLE IF NOT EXISTS `{0}` (");
            foreach (var field in binding.Fields)
            {
                switch (field.Type)
                {
                    case BindingType.UINT:
                        str.Append($@"`{field.Name}` INTEGER(10) NOT NULL DEFAULT '0', ");
                        break;
                    case BindingType.INT:
                        str.Append($@"`{field.Name}` INTEGER(11) NOT NULL DEFAULT '0', ");
                        break;
                    case BindingType.UINT8:
                        str.Append($@"`{field.Name}` TINYINT NOT NULL DEFAULT '0', ");
                        break;
                    case BindingType.FLOAT:
                        str.Append($@"`{field.Name}` REAL NOT NULL DEFAULT '0', ");
                        break;
                    case BindingType.STRING_OFFSET:
                        str.Append($@"`{field.Name}` TEXT, ");
                        break;
                    default:
                        throw new Exception($"ERROR: Unhandled type: {field.Type} on field: {field.Name} on binding: {binding.Name}");

                }
            }
            var idField = binding.Fields.FirstOrDefault(record => record.Name.ToLower().Equals("id"));
            if (idField != null && binding.OrderOutput)
                str.Append($"PRIMARY KEY (`{idField.Name}`)");
            else
                str.Remove(str.Length - 2, 2);
            str.Append(");");
            return str.ToString();
        }
        */

        public override string EscapeString(string keyWord)
        {
            keyWord = keyWord.Replace("'", "''");
            return keyWord;
        }
    }
}
