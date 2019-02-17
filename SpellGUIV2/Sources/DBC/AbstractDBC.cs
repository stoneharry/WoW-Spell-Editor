using SpellEditor.Sources.Binding;
using SpellEditor.Sources.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    public abstract class AbstractDBC
    {
        private string _filePath;
        protected DBCHeader Header;
        protected DBCBody Body;
        protected DBCReader Reader;

        protected void ReadDBCFile(string filePath)
        {
            _filePath = filePath;
            ReloadContents();
        }

        public void ReloadContents()
        {
            var bodyWatch = new Stopwatch();
            var stringWatch = new Stopwatch();
            bodyWatch.Start();
            Body = new DBCBody();
            Reader = new DBCReader(_filePath);
            var name = Path.GetFileNameWithoutExtension(_filePath);
            var binding = BindingManager.GetInstance().FindBinding(name);
            if (binding == null)
                throw new Exception($"Binding not found: {name}.txt");
            Header = Reader.ReadDBCHeader();
            Reader.ReadDBCRecords(Body, name);
            bodyWatch.Stop();
            stringWatch.Start();
            Reader.ReadStringBlock();
            stringWatch.Stop();
            var totalElapsed = stringWatch.ElapsedMilliseconds + bodyWatch.ElapsedMilliseconds;
            Console.WriteLine(
                $"Loaded {name}.dbc into memory in {totalElapsed}ms. Records: {bodyWatch.ElapsedMilliseconds}ms, strings: {stringWatch.ElapsedMilliseconds}ms");
        }

        public Dictionary<string, object> LookupRecord(uint ID) => LookupRecord(ID, "ID");
        public Dictionary<string, object> LookupRecord(uint ID, string IDKey)
        {
            foreach (Dictionary<string, object> entry in Body.RecordMaps)
            {
                if (!entry.ContainsKey(IDKey))
                    continue;
                if ((uint)entry[IDKey] == ID)
                    return entry;
            }
            return null;
        }

        public Task ImportToSql(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc UpdateProgress, string IdKey, string bindingName)
        {
            return Task.Run(() =>
            {
                var binding = BindingManager.GetInstance().FindBinding(bindingName);
                adapter.Execute(string.Format(adapter.GetTableCreateString(binding), binding.Name));
                uint currentRecord = 0;
                uint count = Header.RecordCount;
                uint updateRate = count < 100 ? 100 : count / 100;
                uint index = 0;
                StringBuilder q = null;
                foreach (var recordMap in Body.RecordMaps)
                {
                    // This might be needed? Disabled unless bugs are reported around this
                    //if (r.record.ID == 0)
                    //  continue;
                    if (index == 0 || index % 250 == 0)
                    {
                        if (q != null)
                        {
                            q.Remove(q.Length - 2, 2);
                            adapter.Execute(q.ToString());
                        }
                        q = new StringBuilder();
                        q.Append(string.Format("INSERT INTO `{0}` VALUES ", bindingName));
                    }
                    if (++index % updateRate == 0)
                    {
                        // Visual studio says these casts are redundant but it does not work without them
                        double percent = (double)index / (double)count;
                        UpdateProgress(percent);
                    }
                    currentRecord = recordMap.ContainsKey(IdKey) ? (uint)recordMap[IdKey] : 0;
                    q.Append("(");
                    foreach (var field in binding.Fields)
                    {
                        switch (field.Type)
                        {
                            case BindingType.INT:
                            case BindingType.UINT:
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
                                    var lookupResult = Reader.LookupStringOffset(strOffset);
                                    q.Append(string.Format("\'{0}\', ", adapter.EscapeString(lookupResult)));
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
                Reader.CleanStringsMap();
            });
        }

        public Task ExportToDbc(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress, string IdKey, string bindingName)
        {
            return Task.Run(() =>
            {
                var binding = BindingManager.GetInstance().FindBinding(bindingName);
                if (binding == null)
                    throw new Exception("Binding not found: " + bindingName);

                var orderClause = binding.Fields.FirstOrDefault(f => f.Name.Equals(IdKey)) != null ? $" ORDER BY `{IdKey}`" : "";
                var rows = adapter.Query(string.Format($"SELECT * FROM `{bindingName}`{orderClause}")).Rows;
                uint numRows = uint.Parse(rows.Count.ToString());
                // Hardcode for 3.3.5a 12340
                Header = new DBCHeader();
                Header.FieldCount = (uint)binding.Fields.Count();
                Header.Magic = 1128416343;
                Header.RecordCount = numRows;
                Header.RecordSize = (uint)binding.CalcRecordSize();
                Header.StringBlockSize = 0;

                var body = new DBCBodyToSerialize();
                body.Records = new List<DataRow>((int)Header.RecordCount);
                for (int i = 0; i < numRows; ++i)
                    body.Records.Add(rows[i]);
                Header.StringBlockSize = body.GenerateStringOffsetsMap(binding);
                SaveDbcFile(updateProgress, body, binding);
            });
        }

        protected void SaveDbcFile(MainWindow.UpdateProgressFunc updateProgress, DBCBodyToSerialize body, Binding.Binding binding)
        {
            string path = $"Export/{binding.Name}.dbc";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path))
                File.Delete(path);
            using (FileStream fileStream = new FileStream(path, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    // Write the header file
                    int count = Marshal.SizeOf(typeof(DBCHeader));
                    byte[] buffer = new byte[count];
                    GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    Marshal.StructureToPtr(Header, handle.AddrOfPinnedObject(), true);
                    writer.Write(buffer, 0, count);
                    handle.Free();
                    // Write each record
                    for (int i = 0; i < Header.RecordCount; ++i)
                    {
                        if (i % 250 == 0)
                        {
                            // Visual studio says these casts are redundant but it does not work without them
                            double percent = (double)i / (double)Header.RecordCount;
                            updateProgress(percent);
                        }
                        var record = body.Records[i];
                        foreach (var entry in binding.Fields)
                        {
                            if (!record.Table.Columns.Contains(entry.Name))
                                throw new Exception($"Column binding not found {entry.Name} in table, using binding {binding.Name}.txt");
                            var data = record[entry.Name].ToString();
                            if (entry.Type == BindingType.INT)
                            {
                                if (int.TryParse(data, out int value))
                                    writer.Write(value);
                                else
                                    writer.Write(0);
                            }
                            else if (entry.Type == BindingType.UINT)
                            {
                                if (uint.TryParse(data, out uint value))
                                    writer.Write(value);
                                else
                                    writer.Write(0u);
                            }
                            else if (entry.Type == BindingType.FLOAT)
                            {
                                if (float.TryParse(data, out float value))
                                    writer.Write(value);
                                else
                                    writer.Write(0f);
                            }
                            else if (entry.Type == BindingType.DOUBLE)
                            {
                                if (double.TryParse(data, out double value))
                                    writer.Write(value);
                                else
                                    writer.Write(0d);
                            }
                            else if (entry.Type == BindingType.STRING_OFFSET)
                            {
                                writer.Write(data.Length == 0 ? 0 : body.OffsetStorage[data.GetHashCode()]);
                            }
                            else
                                throw new Exception($"Unknwon type: {entry.Type} on entry {entry.Name} binding {binding.Name}");
                        }
                    }
                    // Write string block
                    int[] offsetsStored = body.OffsetStorage.Values.ToArray();
                    writer.Write(Encoding.UTF8.GetBytes("\0"));
                    for (int i = 0; i < offsetsStored.Length; ++i)
                        writer.Write(Encoding.UTF8.GetBytes(body.ReverseStorage[offsetsStored[i]] + "\0"));
                }
            }
        }

        public bool HasData()
        {
            return Body != null && Body.RecordMaps != null && Body.RecordMaps.Count() > 0;
        }

        public struct DBCHeader
        {
            public uint Magic;
            public uint RecordCount;
            public uint FieldCount;
            public uint RecordSize;
            public int StringBlockSize;
        };

        public class DBCBody
        {
            // Column Name -> Column Value
            public Dictionary<string, object>[] RecordMaps;
        };

        protected class DBCBodyToSerialize
        {
            public List<DataRow> Records;
            public Dictionary<int, int> OffsetStorage;
            public Dictionary<int, string> ReverseStorage;

            // Returns new header stringBlockOffset
            public int GenerateStringOffsetsMap(Binding.Binding binding)
            {
                // Start at 1 as 0 is hardcoded as '\0'
                int stringBlockOffset = 1;
                // Performance gain by collecting the fields to iterate first
                var fields = binding.Fields.Where(field => field.Type == BindingType.STRING_OFFSET).ToArray();
                OffsetStorage = new Dictionary<int, int>();
                ReverseStorage = new Dictionary<int, string>();
                // Populate string <-> offset lookup maps
                for (int i = 0; i < Records.Count; ++i)
                {
                    foreach (var entry in fields)
                    {
                        string str = Records[i][entry.Name].ToString();
                        if (str.Length == 0)
                            continue;
                        var key = str.GetHashCode();
                        if (!OffsetStorage.ContainsKey(key))
                        {
                            OffsetStorage.Add(key, stringBlockOffset);
                            ReverseStorage.Add(stringBlockOffset, str);
                            stringBlockOffset += Encoding.UTF8.GetByteCount(str) + 1;
                        }
                    }
                }
                return stringBlockOffset;
            }
        }

        public class VirtualStrTableEntry
        {
            public string Value;
            public uint NewValue;
        };
    }
}
