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
    internal unsafe class ChangeJournalEnumerator : BaseEnumerator, IEnumerator<UsnEntry>
    {
        private readonly ChangeJournalEnumerationOptions _options;

        public UsnEntry Current => _current;

        object? IEnumerator.Current => Current;

        public ChangeJournalEnumerator(SafeFileHandle volumeRootHandle, USN_JOURNAL_DATA_V0 changeJournal, ChangeJournalEnumerationOptions? options = null)
            :base(volumeRootHandle, changeJournal, options)
        {
            _volumeRootHandle = volumeRootHandle;
            _usnJournalId = changeJournal.UsnJournalID;
            _options = options ?? ChangeJournalEnumerationOptions.Default;
            _nextStartUsn = _options.StartUsn;
            _bufferLength = _options.BufferSize;
            _buffer = Marshal.AllocHGlobal(_bufferLength);
        }

        private unsafe bool GetData()
        {
            var readData = new READ_USN_JOURNAL_DATA_V0
            {
                StartUsn = _nextStartUsn,
                ReasonMask = USN_REASON_MASK,
                ReturnOnlyOnClose = _options.ReturnOnlyOnClose ? 1u : 0u,
                Timeout = _options.Timeout,
                BytesToWaitFor = _options.BytesToWaitFor,
                UsnJournalID = _usnJournalId
            };
            var readDataSize = Marshal.SizeOf(readData);
            var readDataBuffer = Marshal.AllocHGlobal(readDataSize);
            try
            {
                Marshal.StructureToPtr(readData, readDataBuffer, true);
                var success = DeviceIoControl(_volumeRootHandle,
                    FSCTL_READ_USN_JOURNAL,
                    readDataBuffer,
                    readDataSize,
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
                Marshal.FreeHGlobal(readDataBuffer);
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
                // The USN returned as the first item in the output buffer is the USN of the next record number to be retrieved.
                // Use this value to continue reading records from the end boundary forward.
                _nextStartUsn = *(long*)_buffer;
                _offset = sizeof(long);
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
        ~ChangeJournalEnumerator()
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
