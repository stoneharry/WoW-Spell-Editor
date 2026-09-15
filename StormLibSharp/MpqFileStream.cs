using StormLibSharp.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace StormLibSharp
{
    public class MpqFileStream : Stream
    {
        private MpqFileSafeHandle _handle;
        private FileAccess _accessType;
        private MpqArchive _owner;

        internal MpqFileStream(MpqFileSafeHandle handle, FileAccess accessType, MpqArchive owner)
        {
            _handle = handle;
            _accessType = accessType;
            _owner = owner;
        }

        private void VerifyHandle()
        {
            if (_handle == null || _handle.IsInvalid || _handle.IsClosed)
                throw new ObjectDisposedException("MpqFileStream");
        }

        public override bool CanRead
        {
            get { VerifyHandle(); return true; }
        }

        public override bool CanSeek
        {
            get { VerifyHandle(); return true; }
        }

        public override bool CanWrite
        {
            get { VerifyHandle(); return _accessType != FileAccess.Read; }
        }

        public override void Flush()
        {
            VerifyHandle();

            _owner.Flush();
        }

        public override long Length
        {
            get 
            {
                VerifyHandle();

                uint high = 0;
                uint low = NativeMethods.SFileGetFileSize(_handle, ref high);

                ulong val = (high << 32) | low;
                return unchecked((long)val);
            }
        }

        public override long Position
        {
            get
            {
                VerifyHandle();

                return NativeMethods.SFileGetFilePointer(_handle);
            }
            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        public override unsafe int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset > buffer.Length || (offset + count) > buffer.Length)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            VerifyHandle();

            bool success;
            uint read;
            fixed (byte* pb = &buffer[offset])
            {
                NativeOverlapped overlapped = default(NativeOverlapped);
                success = NativeMethods.SFileReadFile(_handle, new IntPtr(pb), unchecked((uint)count), out read, ref overlapped);
            }

            if (!success)
            {
                int lastError = Win32Methods.GetLastError();
                if (lastError != 38) // EOF
                    throw new Win32Exception(lastError);
            }

            return unchecked((int)read);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            VerifyHandle();

            uint low, high;
            low = unchecked((uint)(offset & 0xffffffffu));
            high = unchecked((uint)(offset >> 32));
            return NativeMethods.SFileSetFilePointer(_handle, low, ref high, (uint)origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override unsafe void Write(byte[] buffer, int offset, int count)
        {
            VerifyHandle();

            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset > buffer.Length || (offset + count) > buffer.Length)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            VerifyHandle();

            bool success;
            fixed (byte* pb = &buffer[offset])
            {
                success = NativeMethods.SFileWriteFile(_handle, new IntPtr(pb), unchecked((uint)count), 0u);
            }

            if (!success)
                throw new Win32Exception();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (_handle != null && !_handle.IsInvalid)
                {
                    _handle.Close();
                    _handle = null;
                }

                if (_owner != null)
                {
                    _owner.RemoveOwnedFile(this);
                    _owner = null;
                }
            }
        }

        // TODO: Seems like the right place for SFileGetFileInfo, but will need to determine
        // what value add these features have except for sophisticated debugging purposes 
        // (like in Ladis' MPQ Editor app).

        public int ChecksumCrc32
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public byte[] GetMd5Hash()
        {
            throw new NotImplementedException();
        }
    }
}
