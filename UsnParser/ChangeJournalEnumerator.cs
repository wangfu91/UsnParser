using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UsnParser.Native;
using static UsnParser.Native.Kernel32;

namespace UsnParser
{
    internal unsafe class ChangeJournalEnumerator : BaseEnumerator
    {
        private readonly ChangeJournalEnumerationOptions _options;
        private long _nextStartUsn;

        public ChangeJournalEnumerator(SafeFileHandle volumeRootHandle, USN_JOURNAL_DATA_V0 changeJournal, ChangeJournalEnumerationOptions options)
            : base(volumeRootHandle, changeJournal, options.BufferSize)
        {
            _options = options;
            _nextStartUsn = options.StartUsn;
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

        protected override unsafe void FindNextEntry()
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
    }
}
