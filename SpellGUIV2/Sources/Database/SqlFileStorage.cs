using SpellEditor.Sources.Binding;
using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.Database
{
    public class SqlFileStorage : IStorageAdapter
    {
        private static object _lock = new object();
        private static bool initialised = false;
        private static Dictionary<string, Dictionary<string, string>> _tableColumns = new Dictionary<string, Dictionary<string, string>>();
        private static Dictionary<string, string> _tableNames = new Dictionary<string, string>();

        public Task Export(IDatabaseAdapter adapter, AbstractDBC dbc, MainWindow.UpdateProgressFunc updateProgress, string IdKey, string bindingName)
        {
            return Task.Run(() =>
            {
                // Initialise
                Initialise();

                // Setup export data
                var binding = BindingManager.GetInstance().FindBinding(bindingName);
                if (binding == null)
                    throw new Exception("Binding not found: " + bindingName);
                var body = new DBCBodyToSerialize();

                var orderClause = "";
                if (binding.OrderOutput)
                {
                    orderClause = binding.Fields.FirstOrDefault(f => f.Name.Equals(IdKey)) != null ? $" ORDER BY `{IdKey}`" : "";
                }

                // Load SQL data
                body.Records = dbc.LoadRecords(adapter, bindingName, orderClause, updateProgress);
                var numRows = body.Records.Count();
                if (numRows == 0)
                    throw new Exception("No rows to export");

                // Build text file of insert queries
                var export = new StringBuilder();
                var progressI = 0;
                foreach (var row in body.Records)
                {
                    var rowStr = new StringBuilder();
                    // INSERT INTO table (
                    rowStr.Append($"INSERT INTO `{binding.Name.ToLower()}` (");
                    var fieldCount = binding.Fields.Count();
                    string[] values = new string[fieldCount];
                    var fieldI = 0;
                    foreach (var field in binding.Fields)
                    {
                        ++fieldI;
                        // Append each column name
                        rowStr.Append($"`{field.Name}`" + (fieldI < fieldCount ? ", " : ") "));
                        // save value to write after
                        values[fieldI - 1] = GetValue(field, row[field.Name]);
                    }
                    // Now insert values
                    rowStr.Append("VALUES (");
                    fieldI = 0;
                    foreach (var value in values)
                    {
                        ++fieldI;
                        rowStr.Append(value + (fieldI < fieldCount ? ", " : ");"));
                    }
                    export.AppendLine(rowStr.ToString());
                    // Update progress
                    if (++progressI % 250 == 0)
                    {
                        double percent = (double)progressI / (double)numRows;
                        // Report 0.8 .. 0.9 only
                        updateProgress((percent * 0.1) + 0.8);
                    }
                }

                ExportTableToSql(bindingName, "Export", updateProgress, export.ToString());
            });
        }

        private void ExportTableToSql(string tableName, string path, MainWindow.UpdateProgressFunc func, string script)
        {
            var dbTableName = tableName.ToLower();

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

            // Write to file logic
            var bytes = Encoding.UTF8.GetBytes(script.ToString());
            // Try to GC collect immediately
            script = null;
            using (var fileStream = new FileStream($"{path}/{tableName}.sql", FileMode.Create))
            {
                fileStream.Write(bytes, 0, bytes.Length);
            }

            func?.Invoke(1.0);
        }

        private string GetValue(BindingEntry field, object value)
        {
            switch (field.Type)
            {
                case BindingType.STRING_OFFSET:
                    return $"\"{value}\"";
                default:
                    return value.ToString();
            }
        }

        public Task Import(IDatabaseAdapter adapter, AbstractDBC dbc, MainWindow.UpdateProgressFunc updateProgress, string IdKey, string bindingName)
        {
            throw new NotImplementedException();
        }

        private void Initialise()
        {
            // Initialise mappings if not yet
            if (!initialised)
            {
                lock (_lock)
                {
                    if (!initialised)
                    {
                        CreateDatabasesTablesColumns();
                        initialised = true;
                    }
                }
            }
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
