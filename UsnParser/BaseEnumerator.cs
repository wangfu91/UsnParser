using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using UsnParser.Native;

namespace UsnParser
{
    public unsafe abstract class BaseEnumerator : IDisposable
    {
        protected bool _disposed;
        protected IntPtr _buffer;
        protected readonly int _bufferLength;
        protected readonly SafeFileHandle _volumeRootHandle;
        protected readonly ulong _usnJournalId;
        protected uint _offset;
        protected uint _bytesRead;
        protected long _nextStartUsn;
        protected USN_RECORD_V2* _record;
        protected UsnEntry _current;
        protected BaseEnumerationOptions _options;

        public UsnEntry Current => _current;

        public BaseEnumerator(SafeFileHandle volumeRootHandle, USN_JOURNAL_DATA_V0 changeJournal, BaseEnumerationOptions? options = null)
        {
            _volumeRootHandle = volumeRootHandle;
            _usnJournalId = changeJournal.UsnJournalID;
            _options = options ?? BaseEnumerationOptions.Default;
            _bufferLength = _options.BufferSize;
            _buffer = Marshal.AllocHGlobal(_bufferLength);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BaseEnumerator()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (_buffer != default)
            {
                Marshal.FreeHGlobal(_buffer);
                _buffer = default;
            }

            _disposed = true;
        }
    }
}
