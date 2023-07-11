using NLog;

using SpellEditor.Sources.Binding;
using SpellEditor.Sources.Database;
using SpellEditor.Sources.VersionControl;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    public abstract class AbstractDBC
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
            var name = Path.GetFileNameWithoutExtension(_filePath);
            var binding = BindingManager.GetInstance().FindBinding(name);
            if (binding == null)
                throw new Exception($"Binding not found: {name}.txt");
            var headerWatch = new Stopwatch();
            var bodyWatch = new Stopwatch();
            var stringWatch = new Stopwatch();
            Body = new DBCBody();
            Reader = new DBCReader(_filePath);
            // Header
            headerWatch.Start();
            Header = Reader.ReadDBCHeader();
            headerWatch.Stop();
            // Body
            bodyWatch.Start();
            Reader.ReadDBCRecords(Body, name);
            bodyWatch.Stop();
            // Strings
            stringWatch.Start();
            Reader.ReadStringBlock();
            stringWatch.Stop();
            // Total
            var totalElapsed = stringWatch.ElapsedMilliseconds + bodyWatch.ElapsedMilliseconds;
            Logger.Info(
                $"Loaded {name}.dbc into memory in {totalElapsed}ms.\n" +
                $"\tHeader: {headerWatch.ElapsedMilliseconds}ms\n" +
                $"\tRecords: {bodyWatch.ElapsedMilliseconds}ms\n" +
                $"\tStrings: {stringWatch.ElapsedMilliseconds}ms");
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

        public Task ImportTo(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc UpdateProgress, string IdKey, string bindingName, ImportExportType _type)
        {
            return StorageFactory.Instance.GetStorageAdapter(_type).Import(adapter, UpdateProgress, IdKey, bindingName);
        }

        public Task ExportTo(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress, string IdKey, string bindingName, ImportExportType _type)
        {
            return StorageFactory.Instance.GetStorageAdapter(_type).Export(adapter, updateProgress, IdKey, bindingName);
        }

        protected List<Dictionary<string, object>> LoadRecords(IDatabaseAdapter adapter, string bindingName, string orderClause, MainWindow.UpdateProgressFunc updateProgress)
        {
            const int pageSize = 1000;
            int totalCount;
            using (var queryData = adapter.Query($"SELECT COUNT(*) FROM `{bindingName.ToLower()}`"))
            {
                totalCount = int.Parse(queryData.Rows[0][0].ToString());
            }
            var lowerBounds = 0;
            var results = LoadRecordPage(lowerBounds, pageSize, adapter, bindingName, orderClause);
            var loadCount = results.Count;
            while (loadCount > 0)
            {
                lowerBounds += pageSize;
                // Visual studio says these casts are redundant but it does not work without them
                double percent = ((double)Math.Min(totalCount, lowerBounds) / (double)totalCount);
                // Report 0 .. 0.8 only
                updateProgress(percent * 0.8);

                var page = LoadRecordPage(lowerBounds, pageSize, adapter, bindingName, orderClause);
                loadCount = page.Count;
                page.ForEach(results.Add);
            }
            return results;
        }

        protected List<Dictionary<string, object>> LoadRecordPage(int lowerBounds, int pageSize, IDatabaseAdapter adapter, string bindingName, string orderClause)
        {
            var records = new List<Dictionary<string, object>>();
            using (var queryData = adapter.Query($"SELECT * FROM `{bindingName.ToLower()}`{orderClause} LIMIT {lowerBounds}, {pageSize}"))
            {
                foreach (DataRow row in queryData.Rows)
                {
                    records.Add(ConvertDataRowToDictionary(row));
                }
            }
            return records;
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
                        if (updateProgress != null && i % 250 == 0)
                        {
                            // Visual studio says these casts are redundant but it does not work without them
                            double percent = (double)i / (double)Header.RecordCount;
                            // Report 0.8 .. 1.0 only
                            updateProgress((percent * 0.2) + 0.8);
                        }
                        var record = body.Records[i];
                        foreach (var entry in binding.Fields)
                        {
                            if (!record.Keys.Contains(entry.Name))
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
                            else if (entry.Type == BindingType.UINT8)
                            {
                                if (byte.TryParse(data, out byte value))
                                    writer.Write(value);
                                else
                                    writer.Write(byte.MinValue);
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
                                throw new Exception($"Unknown type: {entry.Type} on entry {entry.Name} binding {binding.Name}");
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

        protected string GetAllLocaleStringsForField(string fieldName, Dictionary<string, object> record)
        {
            uint numLocales = WoWVersionManager.GetInstance().SelectedVersion().NumLocales;
            string name = "";
            bool notFirstItem = false;
            for (int i = 1; i <= numLocales; ++i)
            {
                if (!record.ContainsKey(fieldName + i))
                {
                    continue;
                }
                uint strOffset = (uint)record[fieldName + i];
                if (strOffset > 0)
                {
                    string areaName = Reader.LookupStringOffset(strOffset);
                    if (areaName.Length > 0)
                    {
                        name += areaName;
                        if (notFirstItem && i != numLocales)
                            name += ", ";
                        else
                            notFirstItem = true;
                    }
                }
            }
            return name;
        }

        private Dictionary<string, object> ConvertDataRowToDictionary(DataRow dataRow)
        {
            var record = new Dictionary<string, object>();
            foreach (DataColumn column in dataRow.Table.Columns)
            {
                record.Add(column.ColumnName, dataRow[column]);
            }
            return record;
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
            public List<Dictionary<string, object>> Records;
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
                        var record = Records.ElementAt(i);
                        string str = record[entry.Name].ToString();
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
