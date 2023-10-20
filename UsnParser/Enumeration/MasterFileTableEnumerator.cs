using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UsnParser.Native;
using static UsnParser.Enumeration.BaseEnumerable;
using static UsnParser.Native.Kernel32;

namespace UsnParser.Enumeration
{
    internal unsafe class MasterFileTableEnumerator : BaseEnumerator
    {
        private ulong _nextStartFileId;
        private readonly long _highUsn;
        private readonly MasterFileTableEnumerationOptions _options;
        private readonly FindPredicate? _shouldIncludePredicate;

        public MasterFileTableEnumerator(SafeFileHandle volumeRootHandle, long highUsn, MasterFileTableEnumerationOptions options, FindPredicate? shouldIncludePredicate)
            : base(volumeRootHandle, options.BufferSize)
        {
            _highUsn = highUsn;
            _options = options;
            _shouldIncludePredicate = shouldIncludePredicate;
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
                Marshal.FreeHGlobal(mftDataBuffer);
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

        protected override bool ShouldIncludeEntry(UsnEntry entry) =>
            _shouldIncludePredicate?.Invoke(entry) ?? true;
    }
}
