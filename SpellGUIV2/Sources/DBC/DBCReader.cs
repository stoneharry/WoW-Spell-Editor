using NLog;
using SpellEditor.Sources.Binding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static SpellEditor.Sources.DBC.AbstractDBC;

namespace SpellEditor.Sources.DBC
{
    public class DBCReader
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private string _filePath;
        private long _fileSize;
        private long _filePosition;
        private DBCHeader _header;
        private Dictionary<uint, VirtualStrTableEntry> _stringsMap;

        public DBCReader(string filePath)
        {
            _filePath = filePath;
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

        /**
         * Reads a DBC record from a given binary reader.
         */
        private Struct ReadStruct<Struct>(BinaryReader reader, byte[] readBuffer)
        {
            Struct structure;
            GCHandle handle;
            try
            {
                handle = GCHandle.Alloc(readBuffer, GCHandleType.Pinned);
                structure = (Struct)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Struct));
            }
            catch (Exception e)
            {
                Logger.Info(e);
                throw new Exception(e.Message);
            }
            if (handle != null)
                handle.Free();
            return structure;
        }

        /**
         * Reads the DBC Header, saving it to the class and returning it
         */
        public DBCHeader ReadDBCHeader()
        {
            DBCHeader header;
            using (FileStream fileStream = new FileStream(_filePath, FileMode.Open))
            {
                _fileSize = fileStream.Length;
                int count = Marshal.SizeOf(typeof(DBCHeader));
                byte[] readBuffer = new byte[count];
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    readBuffer = reader.ReadBytes(count);
                    _filePosition = reader.BaseStream.Position;
                    header = ReadStruct<DBCHeader>(reader, readBuffer);
                }
            }
            _header = header;
            return header;
        }

        /**
         * Reads all the records from the DBC file. It puts each record in a
         * array of key value pairs inside the body. The key value pairs are
         * column name to column value.
         */
        public void ReadDBCRecords(DBCBody body, string bindingName)
        {
            var binding = BindingManager.GetInstance().FindBinding(bindingName);
            if (binding == null)
                throw new Exception($"Binding not found: {bindingName}.txt");
            if (_header.RecordSize != binding.CalcRecordSize())
                throw new Exception($"Binding [{_filePath}] fields size does not match the DBC header record size; expected record size [{binding.CalcRecordSize()}] got [{_header.RecordSize}].");
            if (_header.FieldCount != binding.CalcFieldCount())
                throw new Exception($"Binding [{_filePath}] field count does not match the DBC field count; expected [{binding.CalcFieldCount()}] got [{_header.FieldCount}].");

            body.RecordMaps = new Dictionary<string, object>[_header.RecordCount];
            for (int i = 0; i < _header.RecordCount; ++i)
                body.RecordMaps[i] = new Dictionary<string, object>((int)_header.FieldCount);
            using (FileStream fileStream = new FileStream(_filePath, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    reader.BaseStream.Position = _filePosition;
                    for (uint i = 0; i < _header.RecordCount; ++i)
                    {
                        var entry = body.RecordMaps[i];
                        foreach (var field in binding.Fields)
                        {
                            switch (field.Type)
                            {
                                case BindingType.INT:
                                    {
                                        entry.Add(field.Name, reader.ReadInt32());
                                        break;
                                    }
                                case BindingType.STRING_OFFSET:
                                case BindingType.UINT:
                                    {
                                        entry.Add(field.Name, reader.ReadUInt32());
                                        break;
                                    }
                                case BindingType.UINT8:
                                    {
                                        entry.Add(field.Name, reader.ReadByte());
                                        break;
                                    }
                                case BindingType.FLOAT:
                                    {
                                        entry.Add(field.Name, reader.ReadSingle());
                                        break;
                                    }
                                case BindingType.DOUBLE:
                                    {
                                        entry.Add(field.Name, reader.ReadDouble());
                                        break;
                                    }
                                default:
                                    throw new Exception($"Found unkown field type for column {field.Name} type {field.Type} in binding {binding.Name}");
                            }
                        }      
                    }
                    _filePosition = reader.BaseStream.Position;
                }
            }
        }

        /**
        * Reads the string block from the DBC file and saves it to the stringsMap
        * The position is saved into the map value so that spell records can
        * reverse lookup strings.
        */
        public void ReadStringBlock()
        {
            string StringBlock;
            using (FileStream fileStream = new FileStream(_filePath, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    reader.BaseStream.Position = _filePosition;
                    _stringsMap = new Dictionary<uint, VirtualStrTableEntry>();

                    StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(_header.StringBlockSize));
                    string temp = "";

                    uint lastString = 0;
                    uint counter = 0;
                    int length = new System.Globalization.StringInfo(StringBlock).LengthInTextElements;
                    while (counter < length)
                    {
                        var t = StringBlock[(int) counter];
                        if (t == '\0')
                        {
                            VirtualStrTableEntry n = new VirtualStrTableEntry();

                            n.Value = temp;
                            n.NewValue = 0;

                            _stringsMap.Add(lastString, n);

                            lastString += (uint)Encoding.UTF8.GetByteCount(temp) + 1;

                            temp = "";
                        }
                        else
                        {
                            temp += t;
                        }
                        ++counter;
                    }
                }
            }
        }
    }
}
