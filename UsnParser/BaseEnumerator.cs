using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UsnParser.Native;

namespace UsnParser
{
    public unsafe abstract class BaseEnumerator : IEnumerator<UsnEntry>
    {
        protected bool _disposed;
        protected IntPtr _buffer;
        protected readonly int _bufferLength;
        protected readonly SafeFileHandle _volumeRootHandle;        
        protected uint _offset;
        protected uint _bytesRead;
        protected USN_RECORD_V2* _record;
        protected UsnEntry? _current;

        public UsnEntry Current => _current!;

        object IEnumerator.Current => Current;

        public BaseEnumerator(SafeFileHandle volumeRootHandle, int bufferSize)
        {
            _volumeRootHandle = volumeRootHandle;
            _bufferLength = bufferSize;
            _buffer = Marshal.AllocHGlobal(_bufferLength);
        }

        public bool MoveNext()
        {
            FindNextEntry();
            if (_record == null) return false;

            _current = new UsnEntry(_record);
            return true;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        protected unsafe abstract void FindNextEntry();

        // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~BaseEnumerator()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Dispose managed state (managed objects)
            }

            // Free unmanaged resources (unmanaged objects) and override finalizer
            // Set large fields to null
            if (_buffer != default)
            {
                Marshal.FreeHGlobal(_buffer);
                _buffer = default;
            }

            _disposed = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
