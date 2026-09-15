using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StormLibSharp.Native
{
    /// <summary>
    /// Flags enumeration for SFileAddFile and SFileAddFileEx.
    /// </summary>
    [Flags]
    internal enum SFileAddFileFlags : uint
    {
        /// <summary>
        /// Implode method (By PKWARE Data Compression Library)
        /// </summary>
        MPQ_FILE_IMPLODE = 0x00000100,

        /// <summary>
        /// Compress methods (By multiple methods)
        /// </summary>
        MPQ_FILE_COMPRESS = 0x00000200,

        /// <summary>
        /// Indicates whether file is encrypted 
        /// </summary>
        MPQ_FILE_ENCRYPTED = 0x00010000,

        /// <summary>
        /// File decryption key has to be fixed
        /// </summary>
        MPQ_FILE_FIX_KEY = 0x00020000,

        /// <summary>
        /// The file is a patch file. Raw file data begin with TPatchInfo structure
        /// </summary>
        MPQ_FILE_PATCH_FILE = 0x00100000,

        /// <summary>
        /// File is stored as a single unit, rather than split into sectors (Thx, Quantam)
        /// </summary>
        MPQ_FILE_SINGLE_UNIT = 0x01000000,

        /// <summary>
        /// File is a deletion marker. Used in MPQ patches, indicating that the file no longer exists.
        /// </summary>
        MPQ_FILE_DELETE_MARKER = 0x02000000,

        /// <summary>
        /// File has checksums for each sector.
        /// </summary>
        MPQ_FILE_SECTOR_CRC = 0x04000000
    }
}
