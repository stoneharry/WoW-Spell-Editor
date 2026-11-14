using SpellEditor.Sources.Binding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpellEditor.Sources.DBC;
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

                var ordered = binding.OrderOutput && binding.Fields.Any(f => f.Name.Equals(IdKey));
                var header = WriteDbcFile(adapter, binding, IdKey, ordered, updateProgress);
                dbc.UpdateHeader(header);
            });
        }

        // Tables at least this large are read as parallel key ranges on extra connections
        private const int ParallelRowThreshold = 20000;
        private const int RowsPerChunk = 8000;
        private const int MaxChunks = 8;

        private class Chunk
        {
            public string Where = "";
            public int ExpectedRows;
            public MemoryStream Records;
            public int Rows;
            // String slots hold (index into Strings + 1) until the merge assigns real offsets
            public List<string> Strings = new List<string>();
        }

        private static DBCHeader WriteDbcFile(IDatabaseAdapter adapter, Binding.Binding binding, string idKey, bool ordered, MainWindow.UpdateProgressFunc updateProgress)
        {
            var fields = binding.Fields;
            var types = fields.Select(f => f.Type).ToArray();
            var recordSize = binding.CalcRecordSize();
            var stringPositions = new List<int>();
            int position = 0;
            for (int i = 0; i < types.Length; ++i)
            {
                switch (types[i])
                {
                    case BindingType.INT:
                    case BindingType.UINT:
                    case BindingType.FLOAT:
                        position += 4;
                        break;
                    case BindingType.STRING_OFFSET:
                        stringPositions.Add(position);
                        position += 4;
                        break;
                    case BindingType.UINT8:
                        position += 1;
                        break;
                    case BindingType.DOUBLE:
                        position += 8;
                        break;
                    default:
                        throw new Exception($"Unknown type: {types[i]} on entry {fields[i].Name} binding {binding.Name}");
                }
            }

            var tableName = binding.Name.ToLower();
            var columns = string.Join(", ", fields.Select(f => $"`{f.Name}`"));
            var orderClause = ordered ? $" ORDER BY `{idKey}`" : "";

            Chunk[] chunks;
            int totalCount;
            try
            {
                totalCount = Convert.ToInt32(adapter.QuerySingleValue($"SELECT COUNT(*) FROM `{tableName}`"));
                chunks = PlanChunks(adapter, tableName, idKey, ordered, totalCount);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to export {binding.Name}.dbc using binding {binding.Name}.txt: {e.Message}", e);
            }
            if (totalCount == 0)
                throw new Exception("No rows to export");

            var rowsRead = new int[1];
            try
            {
                // Chunks are only planned for MySQL, whose QueryReader is safe to call concurrently
                var tasks = new List<Task>();
                for (int i = 1; i < chunks.Length; ++i)
                {
                    var index = i;
                    tasks.Add(Task.Run(() => ReadChunk(adapter, chunks[index], $"SELECT {columns} FROM `{tableName}`{chunks[index].Where}{orderClause}", types, recordSize, rowsRead, null, totalCount)));
                }
                // The first chunk runs on the calling task so progress reports keep its task id
                ReadChunk(adapter, chunks[0], $"SELECT {columns} FROM `{tableName}`{chunks[0].Where}{orderClause}", types, recordSize, rowsRead, updateProgress, totalCount);
                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                var inner = (e as AggregateException)?.Flatten().InnerExceptions.FirstOrDefault() ?? e;
                throw new Exception($"Failed to export {binding.Name}.dbc using binding {binding.Name}.txt: {inner.Message}", inner);
            }

            var rows = chunks.Sum(c => c.Rows);
            if (rows == 0)
                throw new Exception("No rows to export");

            var header = new DBCHeader
            {
                FieldCount = (uint)fields.Length,
                // Magic is always 'WDBC' https://wowdev.wiki/DBC
                Magic = 1128416343,
                RecordCount = (uint)rows,
                RecordSize = (uint)recordSize
            };

            var path = $"Export/{binding.Name}.dbc";
            var tempPath = path + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // Offset 0 is the empty string; chunks are merged in order so strings keep first-seen order
            var stringOffsets = new Dictionary<string, int>(StringComparer.Ordinal);
            var stringBlock = new MemoryStream();
            stringBlock.WriteByte(0);
            var utf8 = new UTF8Encoding(false);

            using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20))
            {
                fileStream.Write(new byte[HeaderSize], 0, HeaderSize);
                foreach (var chunk in chunks)
                {
                    var map = new int[chunk.Strings.Count + 1];
                    for (int i = 0; i < chunk.Strings.Count; ++i)
                    {
                        var str = chunk.Strings[i];
                        if (!stringOffsets.TryGetValue(str, out int offset))
                        {
                            offset = (int)stringBlock.Length;
                            stringOffsets.Add(str, offset);
                            var bytes = utf8.GetBytes(str);
                            stringBlock.Write(bytes, 0, bytes.Length);
                            stringBlock.WriteByte(0);
                        }
                        map[i + 1] = offset;
                    }

                    var buffer = chunk.Records.GetBuffer();
                    if (stringPositions.Count > 0)
                    {
                        for (int r = 0; r < chunk.Rows; ++r)
                        {
                            int rowStart = r * recordSize;
                            foreach (var stringPosition in stringPositions)
                            {
                                int p = rowStart + stringPosition;
                                int value = map[BitConverter.ToInt32(buffer, p)];
                                buffer[p] = (byte)value;
                                buffer[p + 1] = (byte)(value >> 8);
                                buffer[p + 2] = (byte)(value >> 16);
                                buffer[p + 3] = (byte)(value >> 24);
                            }
                        }
                    }
                    fileStream.Write(buffer, 0, (int)chunk.Records.Length);
                    chunk.Records = null;
                }

                stringBlock.WriteTo(fileStream);
                header.StringBlockSize = (int)stringBlock.Length;

                fileStream.Seek(0, SeekOrigin.Begin);
                using (var writer = new BinaryWriter(fileStream, utf8, true))
                {
                    writer.Write(header.Magic);
                    writer.Write(header.RecordCount);
                    writer.Write(header.FieldCount);
                    writer.Write(header.RecordSize);
                    writer.Write(header.StringBlockSize);
                }
            }

            if (File.Exists(path))
                File.Delete(path);
            File.Move(tempPath, path);
            updateProgress?.Invoke(1D);
            return header;
        }

        // Splits large ordered MySQL tables into key ranges of roughly equal row counts
        private static Chunk[] PlanChunks(IDatabaseAdapter adapter, string tableName, string idKey, bool ordered, int totalCount)
        {
            int chunkCount = Math.Min(MaxChunks, Math.Max(2, Environment.ProcessorCount));
            chunkCount = Math.Min(chunkCount, (totalCount + RowsPerChunk - 1) / RowsPerChunk);
            if (!ordered || !(adapter is MySQL) || totalCount < ParallelRowThreshold || chunkCount < 2)
                return new[] { new Chunk { ExpectedRows = totalCount } };

            var keys = new List<long>(totalCount);
            var integral = true;
            adapter.QueryReader($"SELECT `{idKey}` FROM `{tableName}` WHERE `{idKey}` IS NOT NULL ORDER BY `{idKey}`", reader =>
            {
                while (reader.Read())
                {
                    var value = reader.GetValue(0);
                    if (!(value is int || value is uint || value is long || value is short || value is ushort || value is byte || value is sbyte))
                    {
                        integral = false;
                        return;
                    }
                    keys.Add(Convert.ToInt64(value));
                }
            });
            if (!integral || keys.Count < ParallelRowThreshold)
                return new[] { new Chunk { ExpectedRows = totalCount } };

            // Boundaries are distinct key values so duplicate keys never straddle two chunks
            var bounds = new List<long>();
            for (int i = 1; i < chunkCount; ++i)
            {
                var bound = keys[(int)((long)keys.Count * i / chunkCount)];
                if (bounds.Count == 0 || bound > bounds[bounds.Count - 1])
                    bounds.Add(bound);
            }

            var key = $"`{idKey}`";
            var chunks = new Chunk[bounds.Count + 1];
            for (int i = 0; i < chunks.Length; ++i)
            {
                string where;
                if (i == 0)
                    where = $" WHERE ({key} < {bounds[0]} OR {key} IS NULL)";
                else if (i == chunks.Length - 1)
                    where = $" WHERE {key} >= {bounds[i - 1]}";
                else
                    where = $" WHERE {key} >= {bounds[i - 1]} AND {key} < {bounds[i]}";
                chunks[i] = new Chunk { Where = where, ExpectedRows = keys.Count / chunks.Length + 1 };
            }
            return chunks;
        }

        private static void ReadChunk(IDatabaseAdapter adapter, Chunk chunk, string query, BindingType[] types, int recordSize,
            int[] rowsRead, MainWindow.UpdateProgressFunc updateProgress, int totalCount)
        {
            chunk.Records = new MemoryStream(Math.Max(1, chunk.ExpectedRows) * recordSize);
            var localStrings = new Dictionary<string, int>(StringComparer.Ordinal);
            var strings = chunk.Strings;
            using (var writer = new BinaryWriter(chunk.Records, Encoding.UTF8, true))
            {
                adapter.QueryReader(query, reader =>
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < types.Length; ++i)
                        {
                            var value = reader.GetValue(i);
                            switch (types[i])
                            {
                                case BindingType.INT:
                                    writer.Write(ToInt(value));
                                    break;
                                case BindingType.UINT:
                                    writer.Write(ToUInt(value));
                                    break;
                                case BindingType.UINT8:
                                    writer.Write(ToByte(value));
                                    break;
                                case BindingType.FLOAT:
                                    writer.Write(ToFloat(value));
                                    break;
                                case BindingType.DOUBLE:
                                    writer.Write(ToDouble(value));
                                    break;
                                case BindingType.STRING_OFFSET:
                                    {
                                        var str = value as string ?? value.ToString();
                                        if (str.Length == 0)
                                        {
                                            writer.Write(0);
                                            break;
                                        }
                                        if (!localStrings.TryGetValue(str, out int index))
                                        {
                                            strings.Add(str);
                                            index = strings.Count;
                                            localStrings.Add(str, index);
                                        }
                                        writer.Write(index);
                                        break;
                                    }
                            }
                        }
                        ++chunk.Rows;
                        if (chunk.Rows % 250 == 0)
                        {
                            int total = Interlocked.Add(ref rowsRead[0], 250);
                            if (updateProgress != null && totalCount > 0)
                                updateProgress(Math.Min(1D, (double)total / totalCount));
                        }
                    }
                });
            }
        }

        private const int HeaderSize = 20;

        // Typed fast paths; anything else keeps the old ToString + TryParse behaviour
        private static int ToInt(object value)
        {
            switch (value)
            {
                case int i: return i;
                case uint u: return u <= int.MaxValue ? (int)u : 0;
                case long l: return l >= int.MinValue && l <= int.MaxValue ? (int)l : 0;
                case short s: return s;
                case ushort us: return us;
                case byte b: return b;
                case sbyte sb: return sb;
                default: return int.TryParse(value.ToString(), out int result) ? result : 0;
            }
        }

        private static uint ToUInt(object value)
        {
            switch (value)
            {
                case uint u: return u;
                case int i: return i >= 0 ? (uint)i : 0u;
                case long l: return l >= 0 && l <= uint.MaxValue ? (uint)l : 0u;
                case ushort us: return us;
                case byte b: return b;
                case short s: return s >= 0 ? (uint)s : 0u;
                case sbyte sb: return sb >= 0 ? (uint)sb : 0u;
                default: return uint.TryParse(value.ToString(), out uint result) ? result : 0u;
            }
        }

        private static byte ToByte(object value)
        {
            switch (value)
            {
                case byte b: return b;
                case sbyte sb: return sb >= 0 ? (byte)sb : byte.MinValue;
                case int i: return i >= 0 && i <= byte.MaxValue ? (byte)i : byte.MinValue;
                case uint u: return u <= byte.MaxValue ? (byte)u : byte.MinValue;
                case long l: return l >= 0 && l <= byte.MaxValue ? (byte)l : byte.MinValue;
                case short s: return s >= 0 && s <= byte.MaxValue ? (byte)s : byte.MinValue;
                case ushort us: return us <= byte.MaxValue ? (byte)us : byte.MinValue;
                default: return byte.TryParse(value.ToString(), out byte result) ? result : byte.MinValue;
            }
        }

        private static float ToFloat(object value)
        {
            if (value is float f)
                return f;
            return float.TryParse(value.ToString(), out float result) ? result : 0f;
        }

        private static double ToDouble(object value)
        {
            if (value is double d)
                return d;
            return double.TryParse(value.ToString(), out double result) ? result : 0d;
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
