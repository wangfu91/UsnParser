using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
        private bool _lastRecordFound;
        private ulong _startFileReferenceNumber;
        private uint _offset;
        private uint _bytesRead;
        private readonly long _highUsn;
        private USN_RECORD_V2* _record;
        private UsnEntry _current;

        public UsnEntry Current => _current;

        object IEnumerator.Current => Current;

        public MasterFileTableEnumerator(SafeFileHandle volumeRootHandle, USN_JOURNAL_DATA_V0 changeJournal)
        {
            _volumeRootHandle = volumeRootHandle;
            _highUsn = changeJournal.NextUsn;
            _bufferLength = 256 * 1024;
            _buffer = Marshal.AllocHGlobal(_bufferLength);
            ZeroMemory(_buffer, _bufferLength);
        }

        private unsafe bool GetData()
        {
            var mftEnumData = new MFT_ENUM_DATA_V0
            {
                StartFileReferenceNumber = _startFileReferenceNumber,
                LowUsn = 0,
                HighUsn = _highUsn
            };

            var mftEnumDataSize = Marshal.SizeOf(mftEnumData);
            var mftDataBuffer = Marshal.AllocHGlobal(mftEnumDataSize);
            ZeroMemory(mftDataBuffer, mftEnumDataSize);
            Marshal.StructureToPtr(mftEnumData, mftDataBuffer, true);

            var result = DeviceIoControl(
               _volumeRootHandle,
               FSCTL_ENUM_USN_DATA,
               mftDataBuffer,
               mftEnumDataSize,
               _buffer,
               _bufferLength,
               out _bytesRead,
               IntPtr.Zero);

            return result;
        }

        private unsafe void FindNextRecord()
        {
            if (_record != null && _offset < _bytesRead)
            {
                _record = (USN_RECORD_V2*)((byte*)_record + _record->RecordLength);
            }

            if (_record != null) return;

            // We need read more data
            if (GetData())
            {
                _offset = sizeof(long);
                _record = (USN_RECORD_V2*)(_buffer + _offset);
                _offset += _record->RecordLength;
            }
        }

        public bool MoveNext()
        {
            FindNextRecord();
            if (_record == null) return false;

            _current = new UsnEntry(_record);
            _startFileReferenceNumber = _current.FileReferenceNumber;
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
                    // TODO: dispose managed state (managed objects)
                    _lastRecordFound = true;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
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
