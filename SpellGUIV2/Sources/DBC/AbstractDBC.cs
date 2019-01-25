using SpellEditor.Sources.Binding;
using SpellEditor.Sources.Config;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    public abstract class AbstractDBC
    {
        protected DBCHeader Header;
        protected DBCBody Body = new DBCBody();
        protected DBCReader Reader;

        protected void ReadDBCFile<RecordType>(string filePath)
        {
            Reader = new DBCReader(filePath);
            Header = Reader.ReadDBCHeader();
            Reader.ReadDBCRecords<RecordType>(Body, Marshal.SizeOf(typeof(RecordType)));
            Reader.ReadStringBlock();
        }

        public Dictionary<string, object> LookupRecord(uint ID) => LookupRecord(ID, "ID");
        public Dictionary<string, object> LookupRecord(uint ID, string IDKey)
        {
            foreach (Dictionary<string, object> entry in Body.RecordMaps)
            {
                if (!entry.ContainsKey(IDKey))
                    continue;
                if ((uint) entry[IDKey] == ID)
                    return entry;
            }
            return null;
        }

        public Task ImportToSQL<RecordStruct>(DBAdapter adapter, MainWindow.UpdateProgressFunc UpdateProgress, string IdKey)
        {
            return Task.Run(() =>
            {
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
                        q.Append(string.Format("INSERT INTO `{0}` VALUES ", adapter.Table));
                    }
                    if (++index % updateRate == 0)
                    {
                        // Visual studio says these casts are redundant but it does not work without them
                        double percent = (double)index / (double)count;
                        UpdateProgress(percent);
                    }
                    currentRecord = (uint)recordMap[IdKey];
                    q.Append("(");
                    foreach (var f in typeof(RecordStruct).GetFields())
                    {
                        switch (Type.GetTypeCode(f.FieldType))
                        {
                            case TypeCode.UInt32:
                            case TypeCode.Int32:
                                {
                                    q.Append(string.Format("'{0}', ", recordMap[f.Name]));
                                    break;
                                }
                            case TypeCode.Single:
                                {
                                    q.Append(string.Format("REPLACE('{0}', ',', '.'), ", recordMap[f.Name]));
                                    break;
                                }
                            case TypeCode.Object:
                                {
                                    var attr = f.GetCustomAttribute<HandleField>();
                                    if (attr != null)
                                    {
                                        if (attr.Method == 1)
                                        {
                                            uint[] array = (uint[])recordMap[f.Name];
                                            for (int i = 0; i < array.Length; ++i)
                                            {
                                                var lookupResult = Reader.LookupStringOffset(array[i]);
                                                q.Append(string.Format("\'{0}\', ", SQLite.SQLite.EscapeString(lookupResult)));
                                            }
                                            break;
                                        }
                                        else if (attr.Method == 2)
                                        {
                                            uint[] array = (uint[])recordMap[f.Name];
                                            for (int i = 0; i < array.Length; ++i)
                                                q.Append(string.Format("\'{0}\', ", array[i]));
                                            break;
                                        }
                                    }
                                    goto default;
                                }
                            default:
                                throw new Exception($"ERROR: Record[{currentRecord}] Unhandled type: {f.FieldType} on field: {f.Name}");
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

        public Task ExportToDBC(DBAdapter adapter, MainWindow.UpdateProgressFunc updateProgress, string IdKey, string bindingName)
        {
            return Task.Run(() =>
            {
                var binding = BindingManager.GetInstance().FindBinding(bindingName);
                if (binding == null)
                    throw new Exception("Binding not found: " + bindingName);

                var orderClause = IdKey.Length > 0 ? $" ORDER BY `{IdKey}`" : "";
                var rows = adapter.query(string.Format($"SELECT * FROM `{adapter.Table}`{orderClause}")).Rows;
                uint numRows = uint.Parse(rows.Count.ToString());
                // Hardcode for 3.3.5a 12340
                Header = new DBCHeader();
                Header.FieldCount = 234;
                Header.Magic = 1128416343;
                Header.RecordCount = numRows;
                Header.RecordSize = 936;
                Header.StringBlockSize = 0;

                var body = new DBCBodyToSerialize();
                body.Records = new List<DataRow>((int)Header.RecordCount);
                for (int i = 0; i < numRows; ++i)
                {
                    body.Records.Add(rows[i]);
                }
                SaveDbcFile(updateProgress, body, binding);
            });
        }

        protected void SaveDbcFile(MainWindow.UpdateProgressFunc updateProgress, DBCBodyToSerialize body, Binding.Binding binding)
        {
            Header.StringBlockSize = 0;//(int)stringBlockOffset;

            string path = $"Export/{binding.Name}.dbc";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path))
                File.Delete(path);
            using (FileStream fileStream = new FileStream(path, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    int count = Marshal.SizeOf(typeof(DBCHeader));
                    byte[] buffer = new byte[count];
                    GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    Marshal.StructureToPtr(Header, handle.AddrOfPinnedObject(), true);
                    writer.Write(buffer, 0, count);
                    handle.Free();

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
                            else
                                throw new Exception($"Unknwon type: {entry.Type} on entry {entry.Name} binding {binding.Name}");
                        }
                    }

                    /*uint[] offsetsStored = offsetStorage.Values.ToArray();

                    writer.Write(Encoding.UTF8.GetBytes("\0"));

                    for (int i = 0; i < offsetsStored.Length; ++i)
                        writer.Write(Encoding.UTF8.GetBytes(reverseStorage[offsetsStored[i]] + "\0"));*/
                }
            }
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
            // Colum Name -> String Value
            public Dictionary<string, string[]> StringMaps;
        };

        public class DBCBodyToSerialize
        {
            public List<DataRow> Records;
        }

        public class VirtualStrTableEntry
        {
            public string Value;
            public uint NewValue;
        };
    }
}
