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
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    public abstract class AbstractDBC : IGraphicUserInterfaceDBC
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public DBCHeader Header { get; private set; }
        public DBCBody Body { get; private set; }

        private string _filePath;
        private Dictionary<uint, VirtualStrTableEntry> _stringsMap;

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
            var reader = new DBCReader(_filePath);
            // Header
            headerWatch.Start();
            Header = reader.ReadDBCHeader();
            headerWatch.Stop();
            // Body
            bodyWatch.Start();
            Body = reader.ReadDBCRecords(name);
            bodyWatch.Stop();
            // Strings
            stringWatch.Start();
            _stringsMap = reader.ReadStringBlock();
            stringWatch.Stop();
            // Total
            var totalElapsed = stringWatch.ElapsedMilliseconds + bodyWatch.ElapsedMilliseconds + headerWatch.ElapsedMilliseconds;
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

        public string LookupStringOffset(uint offset)
        {
            if (_stringsMap == null)
                return "";
            if (!_stringsMap.ContainsKey(offset))
            {
                var errorMsg = $"ERROR: Unknown string offset {offset}. This value will be replaced by the spell editor!";
                Logger.Error(errorMsg, new KeyNotFoundException(errorMsg));
                return $"Unknown String: {offset}";
            }
            return _stringsMap[offset].Value;
        }

        public void CleanStringsMap()
        {
            _stringsMap = null;
        }

        public void CleanBody()
        {
            Body = new DBCBody();
        }

        public void UpdateHeader(DBCHeader _header)
        {
            Header = _header;
        }

        public Task ImportTo(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc UpdateProgress, string IdKey, string bindingName, ImportExportType _type)
        {
            return StorageFactory.Instance.GetStorageAdapter(_type).Import(adapter, this, UpdateProgress, IdKey, bindingName);
        }

        public Task ExportTo(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress, string IdKey, string bindingName, ImportExportType _type)
        {
            return StorageFactory.Instance.GetStorageAdapter(_type).Export(adapter, this, updateProgress, IdKey, bindingName);
        }

        public List<Dictionary<string, object>> LoadRecords(IDatabaseAdapter adapter, string bindingName, string orderClause, MainWindow.UpdateProgressFunc updateProgress)
        {
            const int pageSize = 2500;
            int totalCount;
            using (var queryData = adapter.Query($"SELECT COUNT(*) FROM `{bindingName.ToLower()}`"))
            {
                totalCount = int.Parse(queryData.Rows[0][0].ToString());
            }
            int lowerBounds = 0;
            int loadCount;
            var results = new List<Dictionary<string, object>>(totalCount);
            do
            {
                var page = LoadRecordPage(lowerBounds, pageSize, adapter, bindingName, orderClause);
                loadCount = page.Count;
                results.AddRange(page);

                lowerBounds += pageSize;

                // Visual studio says these casts are redundant but it does not work without them
                double percent = ((double)Math.Min(totalCount, lowerBounds) / (double)totalCount);
                // Report 0 .. 0.8 only
                updateProgress(percent * 0.8);
            }
            while (loadCount > 0);

            return results;
        }

        protected List<Dictionary<string, object>> LoadRecordPage(int lowerBounds, int pageSize, IDatabaseAdapter adapter, string bindingName, string orderClause)
        {
            var records = new List<Dictionary<string, object>>(pageSize);
            using (var queryData = adapter.Query($"SELECT * FROM `{bindingName.ToLower()}`{orderClause} LIMIT {lowerBounds}, {pageSize}"))
            {
                foreach (DataRow row in queryData.Rows)
                {
                    records.Add(ConvertDataRowToDictionary(row));
                }
            }
            return records;
        }

        public void SaveDbcFile(MainWindow.UpdateProgressFunc updateProgress, DBCBodyToSerialize body, Binding.Binding binding)
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
                    string areaName = LookupStringOffset(strOffset);
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

        protected string GetStringForField(string fieldName, Dictionary<string, object> record)
        {
            string name = "";

            if (!record.ContainsKey(fieldName))
            {
                if (record.ContainsKey(fieldName + 1))
                    fieldName = fieldName + 1;
                else
                    return "";
            }

            uint strOffset = (uint)record[fieldName];
            if (strOffset > 0)
            {
                name = LookupStringOffset(strOffset);
            }

            return name;
        }

        private Dictionary<string, object> ConvertDataRowToDictionary(DataRow dataRow)
        {
            var record = new Dictionary<string, object>(dataRow.Table.Columns.Count);
            foreach (DataColumn column in dataRow.Table.Columns)
            {
                record.Add(column.ColumnName, dataRow[column]);
            }
            return record;
        }

        public bool HasData()
        {
            return Body.RecordMaps != null && Body.RecordMaps.Count() > 0;
        }

        public virtual void LoadGraphicUserInterface()
        {
            // NOOP default implementation
        }

        public struct DBCHeader
        {
            public uint Magic;
            public uint RecordCount;
            public uint FieldCount;
            public uint RecordSize;
            public int StringBlockSize;
        };

        public struct DBCBody
        {
            // Column Name -> Column Value
            public Dictionary<string, object>[] RecordMaps;
        }

        public struct VirtualStrTableEntry
        {
            public string Value;
            public uint NewValue;
        };
    }
}
