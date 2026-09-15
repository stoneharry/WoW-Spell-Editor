using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StormLibSharp.Native
{
    [Flags]
    internal enum SFileOpenArchiveFlags : uint
    {
        None = 0,
        TypeIsFile = None,
        TypeIsMemoryMapped = 1,
        TypeIsHttp = 2,

        AccessReadOnly = 0x100,
        AccessReadWriteShare = 0x200,
        AccessUseBitmap = 0x400,

        DontOpenListfile = 0x10000,
        DontOpenAttributes = 0x20000,
        DontSearchHeader = 0x40000,
        ForceVersion1 = 0x80000,
        CheckSectorCRC = 0x100000,
    }
}
