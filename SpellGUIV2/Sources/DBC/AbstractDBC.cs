using System.Collections.Generic;
using System.Runtime.InteropServices;

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
            public Dictionary<string, object>[] RecordMaps;
        };

        public class VirtualStrTableEntry
        {
            public string Value;
            public uint NewValue;
        };
    }
}
