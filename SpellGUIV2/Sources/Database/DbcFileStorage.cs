using SpellEditor.Sources.Binding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

                var orderClause = "";
                if (binding.OrderOutput)
                {
                    orderClause = binding.Fields.FirstOrDefault(f => f.Name.Equals(IdKey)) != null ? $" ORDER BY `{IdKey}`" : "";
                }

                var header = WriteDbcFile(adapter, binding, orderClause, updateProgress);
                dbc.UpdateHeader(header);
            });
        }

        // Streams the table straight into the DBC file in a single ordered query
        private static DBCHeader WriteDbcFile(IDatabaseAdapter adapter, Binding.Binding binding, string orderClause, MainWindow.UpdateProgressFunc updateProgress)
        {
            var fields = binding.Fields;
            var types = fields.Select(f => f.Type).ToArray();
            for (int i = 0; i < types.Length; ++i)
            {
                switch (types[i])
                {
                    case BindingType.INT:
                    case BindingType.UINT:
                    case BindingType.UINT8:
                    case BindingType.FLOAT:
                    case BindingType.DOUBLE:
                    case BindingType.STRING_OFFSET:
                        break;
                    default:
                        throw new Exception($"Unknown type: {types[i]} on entry {fields[i].Name} binding {binding.Name}");
                }
            }

            var tableName = binding.Name.ToLower();
            int totalCount = 0;
            if (updateProgress != null)
                totalCount = Convert.ToInt32(adapter.QuerySingleValue($"SELECT COUNT(*) FROM `{tableName}`"));

            var path = $"Export/{binding.Name}.dbc";
            var tempPath = path + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var header = new DBCHeader
            {
                FieldCount = (uint)fields.Length,
                // Magic is always 'WDBC' https://wowdev.wiki/DBC
                Magic = 1128416343,
                RecordSize = (uint)binding.CalcRecordSize()
            };

            // Offset 0 is the empty string
            var stringOffsets = new Dictionary<string, int>(StringComparer.Ordinal);
            var stringBlock = new MemoryStream();
            stringBlock.WriteByte(0);
            var utf8 = new UTF8Encoding(false);

            using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20))
            using (var writer = new BinaryWriter(fileStream))
            {
                // Header is rewritten once the counts are known
                writer.Write(new byte[HeaderSize]);

                var columns = string.Join(", ", fields.Select(f => $"`{f.Name}`"));
                var query = $"SELECT {columns} FROM `{tableName}`{orderClause}";
                uint rows = 0;
                try
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
                                            if (!stringOffsets.TryGetValue(str, out int offset))
                                            {
                                                offset = (int)stringBlock.Length;
                                                stringOffsets.Add(str, offset);
                                                var bytes = utf8.GetBytes(str);
                                                stringBlock.Write(bytes, 0, bytes.Length);
                                                stringBlock.WriteByte(0);
                                            }
                                            writer.Write(offset);
                                            break;
                                        }
                                }
                            }
                            ++rows;
                            if (updateProgress != null && totalCount > 0 && rows % 250 == 0)
                                updateProgress(Math.Min(1D, (double)rows / totalCount));
                        }
                    });
                }
                catch (Exception e)
                {
                    writer.Dispose();
                    File.Delete(tempPath);
                    throw new Exception($"Failed to export {binding.Name}.dbc using binding {binding.Name}.txt: {e.Message}", e);
                }

                if (rows == 0)
                {
                    writer.Dispose();
                    File.Delete(tempPath);
                    throw new Exception("No rows to export");
                }

                writer.Flush();
                stringBlock.WriteTo(fileStream);
                header.RecordCount = rows;
                header.StringBlockSize = (int)stringBlock.Length;

                writer.Seek(0, SeekOrigin.Begin);
                writer.Write(header.Magic);
                writer.Write(header.RecordCount);
                writer.Write(header.FieldCount);
                writer.Write(header.RecordSize);
                writer.Write(header.StringBlockSize);
            }

            if (File.Exists(path))
                File.Delete(path);
            File.Move(tempPath, path);
            updateProgress?.Invoke(1D);
            return header;
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
