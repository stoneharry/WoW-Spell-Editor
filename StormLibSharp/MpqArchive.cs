using StormLibSharp.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace StormLibSharp
{
    public class MpqArchive : IDisposable
    {
        private MpqArchiveSafeHandle _handle;
        private List<MpqFileStream> _openFiles = new List<MpqFileStream>();
        private FileAccess _accessType;
        private List<MpqArchiveCompactingEventHandler> _compactCallbacks = new List<MpqArchiveCompactingEventHandler>();
        private SFILE_COMPACT_CALLBACK _compactCallback;

        #region Constructors / Factories
        public MpqArchive(string filePath, FileAccess accessType)
        {
            _accessType = accessType;
            SFileOpenArchiveFlags flags = SFileOpenArchiveFlags.TypeIsFile;
            if (accessType == FileAccess.Read)
                flags |= SFileOpenArchiveFlags.AccessReadOnly;
            else
                flags |= SFileOpenArchiveFlags.AccessReadWriteShare;

            // constant 2 = SFILE_OPEN_HARD_DISK_FILE
            if (!NativeMethods.SFileOpenArchive(filePath, 2, flags, out _handle))
                throw new Win32Exception(); // Implicitly calls GetLastError
        }

        public MpqArchive(MemoryMappedFile file, FileAccess accessType)
        {
            _accessType = accessType;
            string fileName = Win32Methods.GetFileNameOfMemoryMappedFile(file);
            if (fileName == null)
                throw new ArgumentException("Could not retrieve the name of the file to initialize.");

            SFileOpenArchiveFlags flags = SFileOpenArchiveFlags.TypeIsMemoryMapped;
            if (accessType == FileAccess.Read)
                flags |= SFileOpenArchiveFlags.AccessReadOnly;
            else
                flags |= SFileOpenArchiveFlags.AccessReadWriteShare;

            // constant 2 = SFILE_OPEN_HARD_DISK_FILE
            if (!NativeMethods.SFileOpenArchive(fileName, 2, flags, out _handle))
                throw new Win32Exception(); // Implicitly calls GetLastError
        }

        private MpqArchive(string filePath, MpqArchiveVersion version, MpqFileStreamAttributes listfileAttributes, MpqFileStreamAttributes attributesFileAttributes, int maxFileCount)
        {
            if (maxFileCount < 0)
                throw new ArgumentException("maxFileCount");

            SFileOpenArchiveFlags flags = SFileOpenArchiveFlags.TypeIsFile | SFileOpenArchiveFlags.AccessReadWriteShare;
            flags |= (SFileOpenArchiveFlags)version;

            //SFILE_CREATE_MPQ create = new SFILE_CREATE_MPQ()
            //{
            //    cbSize = unchecked((uint)Marshal.SizeOf(typeof(SFILE_CREATE_MPQ))),
            //    dwMaxFileCount = unchecked((uint)maxFileCount),
            //    dwMpqVersion = (uint)version,
            //    dwFileFlags1 = (uint)listfileAttributes,
            //    dwFileFlags2 = (uint)attributesFileAttributes,
            //    dwStreamFlags = (uint)flags,
            //};

            //if (!NativeMethods.SFileCreateArchive2(filePath, ref create, out _handle))
            //    throw new Win32Exception();
            if (!NativeMethods.SFileCreateArchive(filePath, (uint)flags, (uint)maxFileCount, out _handle))
                throw new Win32Exception();
        }

        public static MpqArchive CreateNew(string mpqPath, MpqArchiveVersion version)
        {
            return CreateNew(mpqPath, version, MpqFileStreamAttributes.None, MpqFileStreamAttributes.None, int.MaxValue);
        }

        public static MpqArchive CreateNew(string mpqPath, MpqArchiveVersion version, MpqFileStreamAttributes listfileAttributes,
            MpqFileStreamAttributes attributesFileAttributes, int maxFileCount)
        {
            return new MpqArchive(mpqPath, version, listfileAttributes, attributesFileAttributes, maxFileCount);
        }
        #endregion

        #region Properties
        // TODO: Move to common location.
        // This is a global setting, not per-archive setting.

        //public int Locale
        //{
        //    get
        //    {
        //        throw new NotImplementedException();
        //    }
        //    set
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        public long MaxFileCount
        {
            get
            {
                VerifyHandle();
                return NativeMethods.SFileGetMaxFileCount(_handle);
            }
            set
            {
                if (value < 0 || value > uint.MaxValue)
                    throw new ArgumentException("value");
                VerifyHandle();

                if (!NativeMethods.SFileSetMaxFileCount(_handle, unchecked((uint)value)))
                    throw new Win32Exception();
            }
        }

        private void VerifyHandle()
        {
            if (_handle == null || _handle.IsInvalid)
                throw new ObjectDisposedException("MpqArchive");
        }

        public bool IsPatchedArchive
        {
            get
            {
                VerifyHandle();
                return NativeMethods.SFileIsPatchedArchive(_handle);
            }
        }
        #endregion

        public void Flush()
        {
            VerifyHandle();
            if (!NativeMethods.SFileFlushArchive(_handle))
                throw new Win32Exception();
        }

        public int AddListFile(string listfileContents)
        {
            VerifyHandle();
            return NativeMethods.SFileAddListFile(_handle, listfileContents);
        }

        public void AddFileFromDisk(string filePath, string archiveName)
        {
            VerifyHandle();

            if (!NativeMethods.SFileAddFile(_handle, filePath, archiveName, 0))
                throw new Win32Exception();
        }

        public void AddFileFromDiskWithCompression(string filePath, string archiveName, MpqCompressionTypeFlags compressionFlags)
        {
            VerifyHandle();

            if(!NativeMethods.SFileAddFileEx(_handle, filePath, archiveName, (uint)SFileAddFileFlags.MPQ_FILE_COMPRESS, (uint)compressionFlags, (uint)compressionFlags))
                throw new Win32Exception();
        }

        public void Compact(string listfile)
        {
            VerifyHandle();
            if (!NativeMethods.SFileCompactArchive(_handle, listfile, false))
                throw new Win32Exception();
        }

        private void _OnCompact(IntPtr pvUserData, uint dwWorkType, ulong bytesProcessed, ulong totalBytes)
        {
            MpqArchiveCompactingEventArgs args = new MpqArchiveCompactingEventArgs(dwWorkType, bytesProcessed, totalBytes);
            OnCompacting(args);
        }

        protected virtual void OnCompacting(MpqArchiveCompactingEventArgs e)
        {
            foreach (var cb in _compactCallbacks)
            {
                cb(this, e);
            }
        }

        public event MpqArchiveCompactingEventHandler Compacting
        {
            add
            {
                VerifyHandle();
                _compactCallback = _OnCompact;
                if (!NativeMethods.SFileSetCompactCallback(_handle, _compactCallback, IntPtr.Zero))
                    throw new Win32Exception();

                _compactCallbacks.Add(value);
            }
            remove
            {
                _compactCallbacks.Remove(value);

                VerifyHandle();
                if (_compactCallbacks.Count == 0)
                {
                    if (!NativeMethods.SFileSetCompactCallback(_handle, null, IntPtr.Zero))
                    {
                        // Don't do anything here.  Remove shouldn't fail hard.
                    }
                }
            }
        }

        // TODO: Determine if SFileGetAttributes/SFileSetAttributes/SFileUpdateFileAttributes deserves a projection.
        // It's unclear - these seem to affect the (attributes) file but I can't figure out exactly what that means.

        public void AddPatchArchive(string patchPath)
        {
            VerifyHandle();

            if (!NativeMethods.SFileOpenPatchArchive(_handle, patchPath, null, 0))
                throw new Win32Exception();
        }

        public void AddPatchArchives(IEnumerable<string> patchPaths)
        {
            if (patchPaths == null)
                throw new ArgumentNullException("patchPaths");

            VerifyHandle();

            foreach (string path in patchPaths)
            {
                // Don't sublet to AddPatchArchive to avoid having to repeatedly call VerifyHandle()
                if (!NativeMethods.SFileOpenPatchArchive(_handle, path, null, 0))
                    throw new Win32Exception();
            }
        }

        public bool HasFile(string fileToFind)
        {
            VerifyHandle();

            return NativeMethods.SFileHasFile(_handle, fileToFind);
        }

        public MpqFileStream OpenFile(string fileName)
        {
            VerifyHandle();

            MpqFileSafeHandle fileHandle;
            if (!NativeMethods.SFileOpenFileEx(_handle, fileName, 0, out fileHandle))
                throw new Win32Exception();

            MpqFileStream fs = new MpqFileStream(fileHandle, _accessType, this);
            _openFiles.Add(fs);
            return fs;
        }

        public void ExtractFile(string fileToExtract, string destinationPath)
        {
            VerifyHandle();

            if (!NativeMethods.SFileExtractFile(_handle, fileToExtract, destinationPath, 0))
                throw new Win32Exception();
        }

        public MpqFileVerificationResults VerifyFile(string fileToVerify)
        {
            VerifyHandle();

            return (MpqFileVerificationResults)NativeMethods.SFileVerifyFile(_handle, fileToVerify, 0);
        }

        // TODO: Consider SFileVerifyRawData

        public MpqArchiveVerificationResult VerifyArchive()
        {
            VerifyHandle();

            return (MpqArchiveVerificationResult)NativeMethods.SFileVerifyArchive(_handle);
        }


        #region IDisposable implementation
        public void Dispose()
        {
            Dispose(true);
        }

        ~MpqArchive()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Release owned files first.
                if (_openFiles != null)
                {
                    foreach (var file in _openFiles)
                    {
                        file.Dispose();
                    }

                    _openFiles.Clear();
                    _openFiles = null;
                }

                // Release
                if (_handle != null && !_handle.IsInvalid)
                {
                    _handle.Close();
                    _handle = null;
                }
            }
        }

        internal void RemoveOwnedFile(MpqFileStream file)
        {
            _openFiles.Remove(file);
        }

        #endregion
    }

    public enum MpqArchiveVersion
    {
        Version1 = 0,
        Version2 = 0x01000000,
        Version3 = 0x02000000,
        Version4 = 0x03000000,
    }

    [Flags]
    public enum MpqFileStreamAttributes
    {
        None = 0x0,
    }

    [Flags]
    public enum MpqFileVerificationResults
    {
        /// <summary>
        /// There were no errors with the file.
        /// </summary>
        Verified = 0,
        /// <summary>
        /// Failed to open the file
        /// </summary>
        Error = 0x1,
        /// <summary>
        /// Failed to read all data from the file
        /// </summary>
        ReadError = 0x2,
        /// <summary>
        /// File has sector CRC
        /// </summary>
        HasSectorCrc = 0x4,
        /// <summary>
        /// Sector CRC check failed
        /// </summary>
        SectorCrcError = 0x8,
        /// <summary>
        /// File has CRC32
        /// </summary>
        HasChecksum = 0x10,
        /// <summary>
        /// CRC32 check failed
        /// </summary>
        ChecksumError = 0x20,
        /// <summary>
        /// File has data MD5
        /// </summary>
        HasMd5 = 0x40,
        /// <summary>
        /// MD5 check failed
        /// </summary>
        Md5Error = 0x80,
        /// <summary>
        /// File has raw data MD5
        /// </summary>
        HasRawMd5 = 0x100,
        /// <summary>
        /// Raw MD5 check failed
        /// </summary>
        RawMd5Error = 0x200,
    }

    public enum MpqArchiveVerificationResult
    {
        /// <summary>
        /// There is no signature in the MPQ
        /// </summary>
        NoSignature = 0,
        /// <summary>
        /// There was an error during verifying signature (like no memory)
        /// </summary>
        VerificationFailed = 1,
        /// <summary>
        /// There is a weak signature and sign check passed
        /// </summary>
        WeakSignatureVerified = 2,
        /// <summary>
        /// There is a weak signature but sign check failed
        /// </summary>
        WeakSignatureFailed = 3,
        /// <summary>
        /// There is a strong signature and sign check passed
        /// </summary>
        StrongSignatureVerified = 4,
        /// <summary>
        /// There is a strong signature but sign check failed
        /// </summary>
        StrongSignatureFailed = 5,
    }
}
