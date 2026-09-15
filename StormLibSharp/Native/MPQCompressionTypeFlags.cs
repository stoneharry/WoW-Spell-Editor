using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StormLibSharp
{
    /// <summary>
    /// Compression types for multiple compressions
    /// </summary>
    [Flags]
    public enum MpqCompressionTypeFlags : uint
    {
        /// <summary>
        /// Huffmann compression (used on WAVE files only)
        /// </summary>
        MPQ_COMPRESSION_HUFFMANN = 0x01,

        /// <summary>
        /// ZLIB compression
        /// </summary>
        MPQ_COMPRESSION_ZLIB = 0x02,

        /// <summary>
        ///  PKWARE DCL compression
        /// </summary>
        MPQ_COMPRESSION_PKWARE = 0x08,

        /// <summary>
        /// BZIP2 compression (added in Warcraft III)
        /// </summary>
        MPQ_COMPRESSION_BZIP2 = 0x10,

        /// <summary>
        /// Sparse compression (added in Starcraft 2)
        /// </summary>
        MPQ_COMPRESSION_SPARSE = 0x20,

        /// <summary>
        /// IMA ADPCM compression (mono)
        /// </summary>
        MPQ_COMPRESSION_ADPCM_MONO = 0x40,

        /// <summary>
        /// IMA ADPCM compression (stereo)
        /// </summary>
        MPQ_COMPRESSION_ADPCM_STEREO = 0x80,

        /// <summary>
        /// LZMA compression. Added in Starcraft 2. This value is NOT a combination of flags.
        /// </summary>
        MPQ_COMPRESSION_LZMA = 0x12,

        /// <summary>
        /// Same compression
        /// </summary>
        MPQ_COMPRESSION_NEXT_SAME = 0xFFFFFFFF
    }
}
