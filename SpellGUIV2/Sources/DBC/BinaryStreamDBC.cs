using MySqlX.XDevAPI.Common;
using SpellEditor.Sources.Database;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using static SpellEditor.Sources.DBC.AbstractDBC;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace SpellEditor.Sources.DBC
{
    public class BinaryStreamDBCWriter
    {
        private readonly AbstractDBC _DBC;
        private readonly Binding.Binding _Binding;
        private readonly string _Path;
        private long _WriteOffset;
        private ConcurrentQueue<Dictionary<string, object>> _RecordQueue;

        public BinaryStreamDBCWriter(AbstractDBC dbc, Binding.Binding binding)
        {
            _DBC = dbc;
            _Binding = binding;
            _WriteOffset = 0;
            _Path = $"Export/{_Binding.Name}.dbc";
            _RecordQueue = new ConcurrentQueue<Dictionary<string, object>>();
        }

        public void RunExportJob(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress)
        {
            InitialiseFileForWrite();

            // Initial header data to write
            var header = new DBCHeader
            {
                FieldCount = (uint)_Binding.Fields.Count(),
                // Magic is always 'WDBC' https://wowdev.wiki/DBC
                Magic = 1128416343,
                RecordCount = 0,
                RecordSize = (uint)_Binding.CalcRecordSize(),
                StringBlockSize = 0
            };
            _WriteOffset = WriteHeader(header);

            var orderClause = "";
            if (_Binding.OrderOutput)
            {
                orderClause = $" ORDER BY `{_Binding.Fields[0].Name}`";
            }

            var streamJob = StartStreamRecordsJob(adapter, updateProgress, orderClause);
            while (!streamJob.IsCompleted || _RecordQueue.TryPeek(out var peekResult))
            {
                while (_RecordQueue.TryDequeue(out var record))
                {
                    WriteRecord(record);
                }
            }
        }

        public Task StartStreamRecordsJob(IDatabaseAdapter adapter, MainWindow.UpdateProgressFunc updateProgress, string orderClause)
        {
            return Task.Run(() =>
            {
                const int pageSize = 1000;
                int totalCount;
                using (var queryData = adapter.Query($"SELECT COUNT(*) FROM `{_Binding.Name.ToLower()}`"))
                {
                    totalCount = int.Parse(queryData.Rows[0][0].ToString());
                }
                var lowerBounds = 0;
                var loadCount = 1;
                while (loadCount > 0)
                {
                    var page = _DBC.LoadRecordPage(lowerBounds, pageSize, adapter, _Binding.Name, orderClause);
                    lowerBounds += pageSize;
                    loadCount = page.Count;
                    page.ForEach(_RecordQueue.Enqueue);
                    // Visual studio says these casts are redundant but it does not work without them
                    double percent = (double)Math.Min(totalCount, lowerBounds) / (double)totalCount;
                    updateProgress(percent);
                }
            });
        }

        public void InitialiseFileForWrite()
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_Path));
            if (File.Exists(_Path))
                File.Delete(_Path);
        }

        public long WriteHeader(AbstractDBC.DBCHeader header)
        {
            using (FileStream fileStream = new FileStream(_Path, FileMode.OpenOrCreate))
            {
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    writer.Write(header.Magic);
                    writer.Write(header.RecordCount);
                    writer.Write(header.FieldCount);
                    writer.Write(header.RecordSize);
                    writer.Write(header.StringBlockSize);
                    return fileStream.Position;
                }
            }
        }

        public void WriteRecord(Dictionary<string, object> record)
        {
            using (FileStream fileStream = new FileStream(_Path, FileMode.OpenOrCreate))
            {
                fileStream.Position = _WriteOffset;
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    foreach (var entry in _Binding.Fields)
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
                _WriteOffset = fileStream.Position;
            }
        }
    }
}
