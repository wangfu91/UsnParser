using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UsnParser.Native;
using static UsnParser.Native.Kernel32;

namespace UsnParser
{
    internal unsafe class MasterFileTableEnumerator : IEnumerator<UsnEntry>
    {
        private bool _disposed;
        private IntPtr _buffer;
        private readonly int _bufferLength;
        private readonly SafeFileHandle _volumeRootHandle;
        private uint _offset;
        private uint _bytesRead;
        private ulong _nextStartFileId;
        private readonly long _highUsn;
        private USN_RECORD_V2* _record;
        private UsnEntry _current;
        private readonly MasterFileTableEnumerationOptions _options;

        public UsnEntry Current => _current;

        object IEnumerator.Current => Current;

        public MasterFileTableEnumerator(SafeFileHandle volumeRootHandle, USN_JOURNAL_DATA_V0 changeJournalData, MasterFileTableEnumerationOptions? options)
        {
            _volumeRootHandle = volumeRootHandle;
            _highUsn = changeJournalData.NextUsn;
            _options = options ?? MasterFileTableEnumerationOptions.Default;
            _bufferLength = _options.BufferSize;
            _buffer = Marshal.AllocHGlobal(_bufferLength);
        }

        private unsafe bool GetData()
        {
            // To enumerate files on a volume, use the FSCTL_ENUM_USN_DATA operation one or more times.
            // On the first call, set the starting point, the StartFileReferenceNumber member of the MFT_ENUM_DATA structure, to (DWORDLONG)0.
            var mftEnumData = new MFT_ENUM_DATA_V0
            {
                StartFileReferenceNumber = _nextStartFileId,
                LowUsn = 0,
                HighUsn = _highUsn
            };
            var mftEnumDataSize = Marshal.SizeOf(mftEnumData);
            var mftDataBuffer = Marshal.AllocHGlobal(mftEnumDataSize);

            try
            {
                Marshal.StructureToPtr(mftEnumData, mftDataBuffer, true);

                var success = DeviceIoControl(
                   _volumeRootHandle,
                   FSCTL_ENUM_USN_DATA,
                   mftDataBuffer,
                   mftEnumDataSize,
                   _buffer,
                   _bufferLength,
                   out _bytesRead,
                   IntPtr.Zero);

                if (!success)
                {
                    var error = Marshal.GetLastPInvokeError();
                    if (error != (int)Win32Error.ERROR_HANDLE_EOF)
                    {
                        throw new Win32Exception(error);
                    }
                }

                return success;
            }
            finally
            {
                Marshal.FreeHGlobal(mftDataBuffer);
            }
        }

        private unsafe void FindNextRecord()
        {
            if (_record != null && _offset < _bytesRead)
            {
                _record = (USN_RECORD_V2*)((byte*)_record + _record->RecordLength);
                _offset += _record->RecordLength;
                return;
            }

            // We need read more data
            if (GetData())
            {
                // Each call to FSCTL_ENUM_USN_DATA retrieves the starting point for the subsequent call as the first entry in the output buffer.
                _nextStartFileId = *(ulong*)_buffer;
                _offset = sizeof(ulong);
                if (_offset < _bytesRead)
                {
                    _record = (USN_RECORD_V2*)(_buffer + _offset);
                    _offset += _record->RecordLength;
                    return;
                }
            }

            // EOF, no more records
            _record = default;
        }

        public bool MoveNext()
        {
            FindNextRecord();
            if (_record == null) return false;

            _current = new UsnEntry(_record);
            return true;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                if (_buffer != default)
                {
                    Marshal.FreeHGlobal(_buffer);
                }

                _buffer = default;

                _disposed = true;
            }
        }

        // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~MasterFileTableEnumerator()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
