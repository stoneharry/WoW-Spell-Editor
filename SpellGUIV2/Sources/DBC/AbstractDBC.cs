using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SpellEditor.Sources.DBC
{
    public abstract class AbstractDBC
    {
        protected DBC_Header Header;
        protected DBC_Body Body = new DBC_Body();
        protected DBCReader reader;

        protected void ReadDBCFile<RecordType>(string filePath)
        {
            reader = new DBCReader(filePath);
            Header = reader.ReadDBCHeader();
            reader.ReadDBCRecords<RecordType>(Body, Marshal.SizeOf(typeof(RecordType)));
            reader.ReadStringBlock();
        }

        public struct DBC_Header
        {
            public uint Magic;
            public uint RecordCount;
            public uint FieldCount;
            public uint RecordSize;
            public int StringBlockSize;
        };

        public class DBC_Body
        {
            public Dictionary<string, object>[] RecordMaps;
        };

        public class VirtualStrTableEntry
        {
            public string Value;
            public uint NewValue;
        };
    }
}
