using SpellEditor.Sources.Binding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpellEditor.Sources.Database
{
    public class CsvFileStorage : IStorageAdapter
    {
        public Task Export(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress, string IdKey, string bindingName)
        {
            return Task.Run(() =>
            {
                var binding = BindingManager.GetInstance().FindBinding(bindingName);
                if (binding == null)
                    throw new Exception("Binding not found: " + bindingName);
                var body = new DBCBodyToSerialize();

                var orderClause = "";
                if (binding.OrderOutput)
                {
                    orderClause = binding.Fields.FirstOrDefault(f => f.Name.Equals(IdKey)) != null ? $" ORDER BY `{IdKey}`" : "";
                }

                body.Records = LoadRecords(adapter, bindingName, orderClause, updateProgress);
                var numRows = body.Records.Count();
                if (numRows == 0)
                    throw new Exception("No rows to export");

                string path = $"Export/{binding.Name}.csv";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (File.Exists(path))
                    File.Delete(path);

                var toWrite = new List<string>(body.Records.Count + 1)
                {
                    // Write header
                    string.Join(",", binding.Fields.Select(f => f.Name).Select(f => EscapeCsv(f)).ToList())
                };
                body.Records.ForEach(record =>
                {
                    List<string> fields = new List<string>(binding.Fields.Length);
                    foreach (var field in binding.Fields)
                    {
                        switch (field.Type)
                        {
                            case BindingType.STRING_OFFSET:
                                {
                                    var str = EscapeCsv(record[field.Name].ToString());
                                    if (str.Length == 0)
                                        str = "\"\"";
                                    fields.Add(str);
                                    break;
                                }
                            default:
                                fields.Add(record[field.Name].ToString());
                                break;

                        }
                    }
                    toWrite.Add(string.Join(",", fields));
                });

                File.WriteAllText(path, string.Join("\n", toWrite));
            });
        }

        public Task Import(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress, string idKey, string bindingName)
        {
            return Task.Run(() =>
            {
                var binding = BindingManager.GetInstance().FindBinding(bindingName);
                adapter.Execute(string.Format(adapter.GetTableCreateString(binding), binding.Name.ToLower()));

                var path = $"{Config.Config.DbcDirectory}\\{binding.Name}.csv";
                if (!File.Exists(path))
                    throw new Exception("Cannot find file: " + path);

                var records = File.ReadAllLines(path);
                records = records.Skip(1).ToArray(); // skip header

                int count = records.Length;
                int updateRate = count < 100 ? 100 : count / 100;
                int index = 0;
                StringBuilder q = null;
                foreach (var record in records)
                {
                    if (index == 0 || index % 250 == 0)
                    {
                        if (q != null)
                        {
                            q.Remove(q.Length - 2, 2);
                            adapter.Execute(q.ToString());
                        }
                        q = new StringBuilder();
                        q.Append(string.Format("INSERT INTO `{0}` VALUES ", bindingName.ToLower()));
                    }
                    if (++index % updateRate == 0)
                    {
                        // Visual studio says these casts are redundant but it does not work without them
                        double percent = (double)index / (double)count;
                        updateProgress(percent);
                    }
                    q.Append("(");
                    q.Append(record.Replace("\\r", "\r").Replace("\\n", "\n"));
                    q.Append("), ");
                }
                if (q.Length > 0)
                {
                    q.Remove(q.Length - 2, 2);
                    adapter.Execute(q.ToString());
                }
            });
        }

        private string EscapeCsv(string field)
        {
            var returnVal = field;
            if (Regex.Matches(field, "([A-Z]|[a-z]|\n|\\\"|\'| |&|~|!|£|$|%|\\^|&|\\*|\\(|\\)|`|$)+").Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("\"");
                foreach (char nextChar in field)
                {
                    sb.Append(nextChar);
                    if (nextChar == '"')
                        sb.Append("\"");
                }
                sb.Append("\"");
                returnVal = sb.ToString();
            }
            returnVal = returnVal.Replace("\n", "\\n").Replace("\r", "\\r");
            return returnVal;
        }
    }
}
