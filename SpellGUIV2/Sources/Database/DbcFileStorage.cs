using SpellEditor.Sources.Binding;
using SpellEditor.Sources.DBC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpellEditor.Sources.DBC.AbstractDBC;

namespace SpellEditor.Sources.Database
{
    public class DbcFileStorage : IStorageAdapter
    {
        public Task Export(IDatabaseAdapter adapter, AbstractDBC dbc, MainWindow.UpdateProgressFunc updateProgress, string IdKey, string bindingName)
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

                body.Records = dbc.LoadRecords(adapter, bindingName, orderClause, updateProgress);
                var numRows = body.Records.Count();
                if (numRows == 0)
                    throw new Exception("No rows to export");

                dbc.UpdateHeader(new DBCHeader
                {
                    FieldCount = (uint)binding.Fields.Count(),
                    // Magic is always 'WDBC' https://wowdev.wiki/DBC
                    Magic = 1128416343,
                    RecordCount = (uint)numRows,
                    RecordSize = (uint)binding.CalcRecordSize(),
                    StringBlockSize = body.GenerateStringOffsetsMap(binding)
                });

                dbc.SaveDbcFile(updateProgress, body, binding);
            });
        }

        public Task Export(IDatabaseAdapter adapter, AbstractDBC dbc, MainWindow.UpdateProgressFunc updateProgress, string IdKey, string bindingName, DBCBodyToSerialize body)
        {
            return Task.Run(() =>
            {
                var binding = BindingManager.GetInstance().FindBinding(bindingName);
                if (binding == null)
                    throw new Exception("Binding not found: " + bindingName);

                var orderClause = "";
                if (binding.OrderOutput)
                {
                    orderClause = binding.Fields.FirstOrDefault(f => f.Name.Equals(IdKey)) != null ? $" ORDER BY `{IdKey}`" : "";
                }

                if (body.Records == null)
                    body.Records = dbc.LoadRecords(adapter, bindingName, orderClause, updateProgress);
                var numRows = body.Records.Count();
                if (numRows == 0)
                    throw new Exception("No rows to export");

                dbc.UpdateHeader(new DBCHeader
                {
                    FieldCount = (uint)binding.Fields.Count(),
                    // Magic is always 'WDBC' https://wowdev.wiki/DBC
                    Magic = 1128416343,
                    RecordCount = (uint)numRows,
                    RecordSize = (uint)binding.CalcRecordSize(),
                    StringBlockSize = body.GenerateStringOffsetsMap(binding)
                });

                dbc.SaveDbcFile(updateProgress, body, binding);
            });
        }

        public Task Import(IDatabaseAdapter adapter, AbstractDBC dbc, MainWindow.UpdateProgressFunc updateProgress, string IdKey, string bindingName)
        {
            return Task.Run(() =>
            {
                var binding = BindingManager.GetInstance().FindBinding(bindingName);

                adapter.Execute(string.Format(adapter.GetTableCreateString(binding), binding.Name.ToLower()));
                uint currentRecord = 0;
                uint count = dbc.Header.RecordCount;
                uint updateRate = count < 100 ? 100 : count / 100;
                uint index = 0;
                StringBuilder q = null;
                foreach (var recordMap in dbc.Body.RecordMaps)
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
                    currentRecord = recordMap.ContainsKey(IdKey) ? (uint)recordMap[IdKey] : 0;
                    q.Append("(");
                    foreach (var field in binding.Fields)
                    {
                        switch (field.Type)
                        {
                            case BindingType.INT:
                            case BindingType.UINT:
                            case BindingType.UINT8:
                                {
                                    q.Append(string.Format("'{0}', ", recordMap[field.Name]));
                                    break;
                                }
                            case BindingType.FLOAT:
                            case BindingType.DOUBLE:
                                {
                                    q.Append(string.Format("REPLACE('{0}', ',', '.'), ", recordMap[field.Name]));
                                    break;
                                }
                            case BindingType.STRING_OFFSET:
                                {
                                    var strOffset = (uint)recordMap[field.Name];
                                    var lookupResult = dbc.LookupStringOffset(strOffset);
                                    q.Append(string.Format("'{0}', ", adapter.EscapeString(lookupResult)));
                                    break;
                                }
                            case BindingType.UNKNOWN:
                                break;
                            default:
                                throw new Exception($"ERROR: Record[{currentRecord}] Unhandled type: {field.Type} on field: {field.Name}");
                        }
                    }
                    q.Remove(q.Length - 2, 2);
                    q.Append("), ");
                }
                if (q.Length > 0)
                {
                    q.Remove(q.Length - 2, 2);
                    adapter.Execute(q.ToString());
                }
                // We have attempted to import the Spell.dbc so clean up unneeded data
                // This will be recreated if the import process is started again
                dbc.CleanStringsMap();
            });
        }
    }
}
