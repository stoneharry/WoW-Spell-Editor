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
        private string filePath;
        private long fileSize;
        private long filePosition;
        private DBCHeader Header;
        private Dictionary<uint, VirtualStrTableEntry> stringsMap;

        public DBCReader(string filePath)
        {
            this.filePath = filePath;
        }

        public string LookupStringOffset(uint offset)
        {
            return stringsMap[offset].Value;
        }

        public void CleanStringsMap()
        {
            stringsMap = null;
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
                Console.WriteLine(e);
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
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                fileSize = fileStream.Length;
                int count = Marshal.SizeOf(typeof(DBCHeader));
                byte[] readBuffer = new byte[count];
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    readBuffer = reader.ReadBytes(count);
                    filePosition = reader.BaseStream.Position;
                    header = ReadStruct<DBCHeader>(reader, readBuffer);
                }
            }
            this.Header = header;
            return header;
        }

        /**
         * Reads all the records from the DBC file. It puts each record in a
         * array of key value pairs inside the body. The key value pairs are
         * column name to column value.
         */
        public void ReadDBCRecords<RecordStruct>(DBCBody body, int recordSize)
        {
            if (Header.RecordSize != recordSize)
                throw new Exception($"The DBC [{ filePath }] is not supported! It's version is not 3.3.5a 12340, expected record size [{ Header.RecordSize }] got [{ recordSize }].");

            body.RecordMaps = new Dictionary<string, object>[Header.RecordCount];
            for (int i = 0; i < Header.RecordCount; ++i)
                body.RecordMaps[i] = new Dictionary<string, object>((int) Header.FieldCount);
            byte[] readBuffer;
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    reader.BaseStream.Position = filePosition;
                    for (uint i = 0; i < Header.RecordCount; ++i)
                    {
                        readBuffer = new byte[recordSize];
                        readBuffer = reader.ReadBytes(recordSize);

                        RecordStruct record = ReadStruct<RecordStruct>(reader, readBuffer);

                        var entry = body.RecordMaps[i];
                        foreach (var field in typeof(RecordStruct).GetFields())
                            entry.Add(field.Name, field.GetValue(record));
                    }
                    filePosition = reader.BaseStream.Position;
                }
            }
        }
        public void ReadDBCRecords(DBCBody body, int recordSize, string bindingName)
        {
            var binding = BindingManager.GetInstance().FindBinding(bindingName);
            if (binding == null)
                throw new Exception($"Binding not found: {bindingName}.txt");
            if (Header.RecordSize != recordSize)
                throw new Exception($"The DBC [{ filePath }] is not supported! It's version is not 3.3.5a 12340, expected record size [{ Header.RecordSize }] got [{ recordSize }].");

            body.RecordMaps = new Dictionary<string, object>[Header.RecordCount];
            for (int i = 0; i < Header.RecordCount; ++i)
                body.RecordMaps[i] = new Dictionary<string, object>((int)Header.FieldCount);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    reader.BaseStream.Position = filePosition;
                    for (uint i = 0; i < Header.RecordCount; ++i)
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
                    filePosition = reader.BaseStream.Position;
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
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(fileStream))
                {
                    reader.BaseStream.Position = filePosition;
                    stringsMap = new Dictionary<uint, VirtualStrTableEntry>();

                    StringBlock = Encoding.UTF8.GetString(reader.ReadBytes(Header.StringBlockSize));
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

                            stringsMap.Add(lastString, n);

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
