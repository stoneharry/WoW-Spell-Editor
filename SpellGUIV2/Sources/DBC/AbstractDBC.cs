using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellEditor.Sources.DBC
{
    abstract class AbstractDBC
    {
        public struct DBC_Header
        {
            public uint Magic;
            public uint RecordCount;
            public uint FieldCount;
            public uint RecordSize;
            public int StringBlockSize;
        };

        public class VirtualStrTableEntry
        {
            public string Value;
            public uint NewValue;
        };
    }
}
