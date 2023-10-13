﻿using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UsnParser.Native;
using static UsnParser.Native.Kernel32;

namespace UsnParser
{
    internal unsafe class ChangeJournalEnumerator : IEnumerator<UsnEntry>
    {
        private bool _disposed;
        private IntPtr _buffer;
        private readonly int _bufferLength;
        private readonly SafeFileHandle _volumeRootHandle;
        private readonly ulong _usnJournalId;
        private uint _offset;
        private uint _bytesRead;
        private long _nextStartUsn;
        private USN_RECORD_V2* _record;
        private UsnEntry _current;

        public UsnEntry Current => _current;

        object IEnumerator.Current => Current;

        public ChangeJournalEnumerator(SafeFileHandle volumeRootHandle, USN_JOURNAL_DATA_V0 changeJournal)
        {
            _volumeRootHandle = volumeRootHandle;
            _bufferLength = 256 * 1024;
            _buffer = Marshal.AllocHGlobal(_bufferLength);
            _usnJournalId = changeJournal.UsnJournalID;
        }

        private unsafe bool GetData()
        {
            var readData = new READ_USN_JOURNAL_DATA_V0
            {
                StartUsn = _nextStartUsn,
                ReasonMask = USN_REASON_MASK,
                ReturnOnlyOnClose = 0,
                Timeout = 0,
                BytesToWaitFor = 0,
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
                _record = (USN_RECORD_V2*)(_buffer + _offset);
                _offset += _record->RecordLength;
            }
            else
            {
                // EOF, no more records
                _record = default;
            }
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
