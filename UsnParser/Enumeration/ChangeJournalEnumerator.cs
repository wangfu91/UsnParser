﻿using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UsnParser.Native;
using static UsnParser.Enumeration.BaseEnumerable;
using static UsnParser.Native.Kernel32;

namespace UsnParser.Enumeration
{
    internal unsafe class ChangeJournalEnumerator : BaseEnumerator
    {
        private readonly ChangeJournalEnumerationOptions _options;
        private long _nextStartUsn;
        private readonly ulong _usnJournalId;
        private readonly FindPredicate? _shouldIncludePredicate;

        public ChangeJournalEnumerator(SafeFileHandle volumeRootHandle, ulong usnJournalId, ChangeJournalEnumerationOptions options, FindPredicate? shouldIncludePredicate)
            : base(volumeRootHandle, options.BufferSize)
        {
            _usnJournalId = usnJournalId;
            _options = options;
            _nextStartUsn = options.StartUsn;
            _shouldIncludePredicate = shouldIncludePredicate;
        }

        private unsafe bool GetData()
        {
            var readData = new READ_USN_JOURNAL_DATA_V0
            {
                StartUsn = _nextStartUsn,
                ReasonMask = USN_REASON_MASK_ALL,
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
                    nint.Zero);

                if (!success)
                {
                    var error = Marshal.GetLastWin32Error();
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

        protected override bool ShouldIncludeEntry(UsnEntry entry) =>
            _shouldIncludePredicate?.Invoke(entry) ?? true;
    }
}
