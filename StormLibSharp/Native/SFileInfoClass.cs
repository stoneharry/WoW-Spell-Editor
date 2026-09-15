using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StormLibSharp.Native
{
    internal enum SFileInfoClass
    {
        // Info classes for archives
        SFileMpqFileName,                       // Name of the archive file (TCHAR [])
        SFileMpqStreamBitmap,                   // Array of bits, each bit means availability of one block (BYTE [])
        SFileMpqUserDataOffset,                 // Offset of the user data header (ULONGLONG)
        SFileMpqUserDataHeader,                 // Raw (unfixed) user data header (TMPQUserData)
        SFileMpqUserData,                       // MPQ USer data, without the header (BYTE [])
        SFileMpqHeaderOffset,                   // Offset of the MPQ header (ULONGLONG)
        SFileMpqHeaderSize,                     // Fixed size of the MPQ header
        SFileMpqHeader,                         // Raw (unfixed) archive header (TMPQHeader)
        SFileMpqHetTableOffset,                 // Offset of the HET table, relative to MPQ header (ULONGLONG)
        SFileMpqHetTableSize,                   // Compressed size of the HET table (ULONGLONG)
        SFileMpqHetHeader,                      // HET table header (TMPQHetHeader)
        SFileMpqHetTable,                       // HET table as pointer. Must be freed using SFileFreeFileInfo
        SFileMpqBetTableOffset,                 // Offset of the BET table, relative to MPQ header (ULONGLONG)
        SFileMpqBetTableSize,                   // Compressed size of the BET table (ULONGLONG)
        SFileMpqBetHeader,                      // BET table header, followed by the flags (TMPQBetHeader + DWORD[])
        SFileMpqBetTable,                       // BET table as pointer. Must be freed using SFileFreeFileInfo
        SFileMpqHashTableOffset,                // Hash table offset, relative to MPQ header (ULONGLONG)
        SFileMpqHashTableSize64,                // Compressed size of the hash table (ULONGLONG)
        SFileMpqHashTableSize,                  // Size of the hash table, in entries (DWORD)
        SFileMpqHashTable,                      // Raw (unfixed) hash table (TMPQBlock [])
        SFileMpqBlockTableOffset,               // Block table offset, relative to MPQ header (ULONGLONG)
        SFileMpqBlockTableSize64,               // Compressed size of the block table (ULONGLONG)
        SFileMpqBlockTableSize,                 // Size of the block table, in entries (DWORD)
        SFileMpqBlockTable,                     // Raw (unfixed) block table (TMPQBlock [])
        SFileMpqHiBlockTableOffset,             // Hi-block table offset, relative to MPQ header (ULONGLONG)
        SFileMpqHiBlockTableSize64,             // Compressed size of the hi-block table (ULONGLONG)
        SFileMpqHiBlockTable,                   // The hi-block table (USHORT [])
        SFileMpqSignatures,                     // Signatures present in the MPQ (DWORD)
        SFileMpqStrongSignatureOffset,          // Byte offset of the strong signature, relative to begin of the file (ULONGLONG)
        SFileMpqStrongSignatureSize,            // Size of the strong signature (DWORD)
        SFileMpqStrongSignature,                // The strong signature (BYTE [])
        SFileMpqArchiveSize64,                  // Archive size from the header (ULONGLONG)
        SFileMpqArchiveSize,                    // Archive size from the header (DWORD)
        SFileMpqMaxFileCount,                   // Max number of files in the archive (DWORD)
        SFileMpqFileTableSize,                  // Number of entries in the file table (DWORD)
        SFileMpqSectorSize,                     // Sector size (DWORD)
        SFileMpqNumberOfFiles,                  // Number of files (DWORD)
        SFileMpqRawChunkSize,                   // Size of the raw data chunk for MD5
        SFileMpqStreamFlags,                    // Stream flags (DWORD)
        SFileMpqIsReadOnly,                     // Nonzero if the MPQ is read only (DWORD)

        // Info classes for files
        SFileInfoPatchChain,                    // Chain of patches where the file is (TCHAR [])
        SFileInfoFileEntry,                     // The file entry for the file (TFileEntry)
        SFileInfoHashEntry,                     // Hash table entry for the file (TMPQHash)
        SFileInfoHashIndex,                     // Index of the hash table entry (DWORD)
        SFileInfoNameHash1,                     // The first name hash in the hash table (DWORD)
        SFileInfoNameHash2,                     // The second name hash in the hash table (DWORD)
        SFileInfoNameHash3,                     // 64-bit file name hash for the HET/BET tables (ULONGLONG)
        SFileInfoLocale,                        // File locale (DWORD)
        SFileInfoFileIndex,                     // Block index (DWORD)
        SFileInfoByteOffset,                    // File position in the archive (ULONGLONG)
        SFileInfoFileTime,                      // File time (ULONGLONG)
        SFileInfoFileSize,                      // Size of the file (DWORD)
        SFileInfoCompressedSize,                // Compressed file size (DWORD)
        SFileInfoFlags,                         // File flags from (DWORD)
        SFileInfoEncryptionKey,                 // File encryption key
        SFileInfoEncryptionKeyRaw,              // Unfixed value of the file key
    }
}
