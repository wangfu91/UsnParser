using DotNet.Globbing;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using UsnParser.Native;
using Microsoft.Win32.SafeHandles;
using static UsnParser.Native.Kernel32;
using static UsnParser.Native.Ntdll;
using FileAccess = UsnParser.Native.FileAccess;
using UsnParser.Enumeration;
using static UsnParser.Enumeration.BaseEnumerable;

namespace UsnParser
{
    public class UsnJournal : IDisposable
    {
        private readonly DriveInfo _driveInfo;
        private readonly bool _isChangeJournalSupported;
        private readonly SafeFileHandle _volumeRootHandle;

        public string VolumeName { get; }

        public USN_JOURNAL_DATA_V0 JournalInfo { get; private set; }

        public UsnJournal(DriveInfo driveInfo)
        {
            _driveInfo = driveInfo;
            VolumeName = driveInfo.Name;

            _isChangeJournalSupported = _driveInfo.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase)
                || _driveInfo.DriveFormat.Equals("ReFs", StringComparison.OrdinalIgnoreCase);

            if (!_isChangeJournalSupported)
            {
                throw new Exception($"{_driveInfo.Name} is not an NTFS or ReFS volume, which does not support USN change journal.");
            }

            _volumeRootHandle = GetVolumeRootHandle();
            Init();
        }

        private void Init()
        {
            if (_volumeRootHandle.IsInvalid)
                throw new Win32Exception((int)Win32Error.ERROR_INVALID_HANDLE);

            try
            {
                JournalInfo = QueryUsnJournalInfo();
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == (int)Win32Error.ERROR_JOURNAL_NOT_ACTIVE)
                {
                    var shouldCreate = Prompt.GetYesNo(
                            $"The change journal of volume {VolumeName} is not active, active it now?",
                            defaultAnswer: true);

                    if (shouldCreate)
                    {
                        // Set default max size to 32MB, default allocation delta to 8MB.
                        CreateUsnJournal(0x2000000, 0x800000);
                        JournalInfo = QueryUsnJournalInfo();
                        return;
                    }
                }

                throw;
            }
        }

        private unsafe void CreateUsnJournal(ulong maxSize, ulong allocationDelta)
        {
            var createData = new CREATE_USN_JOURNAL_DATA
            {
                MaximumSize = maxSize,
                AllocationDelta = allocationDelta
            };

            var createDataSize = sizeof(CREATE_USN_JOURNAL_DATA);
            var createDataBuffer = Marshal.AllocHGlobal(createDataSize);
            try
            {
                ZeroMemory(createDataBuffer, createDataSize);
                *(CREATE_USN_JOURNAL_DATA*)createDataBuffer = createData;
                var bSuccess = DeviceIoControl(
                   _volumeRootHandle,
                   FSCTL_CREATE_USN_JOURNAL,
                   createDataBuffer,
                   createDataSize,
                   IntPtr.Zero,
                   0,
                   out _,
                   IntPtr.Zero);
                if (!bSuccess)
                {
                    var lastError = Marshal.GetLastWin32Error();
                    throw new Win32Exception(lastError);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(createDataBuffer);
            }
        }

        public IEnumerable<UsnEntry> EnumerateMasterFileTable(long highUsn, FilterOptions filterOptions)
        {
            // Note: 
            // In ReFS there is no MFT and subsequently no MFT entries.
            // http://www.resilientfilesystem.co.uk/refs-master-file-table

            var options = MasterFileTableEnumerationOptions.Default;
            return new MasterFileTableEnumerable(_volumeRootHandle, highUsn, options, Filter(filterOptions));
        }

        private static FindPredicate Filter(FilterOptions filterOptions)
        {
            return usnEntry =>
            {
                if (filterOptions.FileOnly && usnEntry.IsFolder) return false;
                if (filterOptions.DirectoryOnly && !usnEntry.IsFolder) return false;

                if (string.IsNullOrWhiteSpace(filterOptions.Keyword)) return true;

                var globOptions = new GlobOptions { Evaluation = { CaseInsensitive = filterOptions.IgnoreCase } };
                var glob = Glob.Parse(filterOptions.Keyword, globOptions);
                return glob.IsMatch(usnEntry.FileName);
            };
        }

        public IEnumerable<UsnEntry> MonitorLiveUsn(ulong usnJournalId, long startUsn, FilterOptions filterOptions)
        {
            var options = new ChangeJournalEnumerationOptions
            {
                BytesToWaitFor = 1,
                Timeout = 0,
                ReturnOnlyOnClose = false,
                StartUsn = startUsn,
            };
            return new ChangeJournalEnumerable(_volumeRootHandle, usnJournalId, options, Filter(filterOptions));
        }

        public IEnumerable<UsnEntry> EnumerateUsnEntries(ulong usnJournalId, FilterOptions filterOptions)
        {
            var options = new ChangeJournalEnumerationOptions
            {
                BytesToWaitFor = 0,
                Timeout = 0,
                ReturnOnlyOnClose = false,
                StartUsn = 0,
            };
            return new ChangeJournalEnumerable(_volumeRootHandle, usnJournalId, options, Filter(filterOptions));
        }

        private SafeFileHandle GetVolumeRootHandle()
        {
            var vol = string.Concat(@"\\.\", _driveInfo.Name.TrimEnd('\\'));

            var rootHandle = CreateFile(
                vol,
                FileAccess.GENERIC_READ | FileAccess.GENERIC_WRITE,
                FileShare.ReadWrite,
                default,
                FileMode.Open,
                0,
                default
                );

            if (rootHandle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return rootHandle;
        }

        private unsafe USN_JOURNAL_DATA_V0 QueryUsnJournalInfo()
        {
            var journalDataSize = sizeof(USN_JOURNAL_DATA_V0);
            IntPtr usnJournalStatePtr = Marshal.AllocHGlobal(journalDataSize);
            try
            {
                var bSuccess = DeviceIoControl(
                    _volumeRootHandle,
                    FSCTL_QUERY_USN_JOURNAL,
                    IntPtr.Zero,
                    0,
                    usnJournalStatePtr,
                    journalDataSize,
                    out var _,
                    IntPtr.Zero);

                if (!bSuccess)
                {
                    var lastError = Marshal.GetLastWin32Error();
                    throw new Win32Exception(lastError);
                }

                return Marshal.PtrToStructure<USN_JOURNAL_DATA_V0>(usnJournalStatePtr);
            }
            finally
            {
                Marshal.FreeHGlobal(usnJournalStatePtr);
            }
        }

        public unsafe bool TryGetPathFromFileId(ulong frn, out string? path)
        {
            if (!_isChangeJournalSupported)
                throw new Exception($"{_driveInfo.Name} is not an NTFS volume.");

            if (_volumeRootHandle.IsInvalid)
                throw new Win32Exception((int)Win32Error.ERROR_INVALID_HANDLE);

            path = null;
            if (frn == 0) return false;

            long allocationSize = 0;
            var objAttributes = new OBJECT_ATTRIBUTES();
            var ioStatusBlock = new IO_STATUS_BLOCK();

            var buffer = Marshal.AllocHGlobal(4096);
            var refPtr = Marshal.AllocHGlobal(8);
            var objAttIntPtr = Marshal.AllocHGlobal(sizeof(OBJECT_ATTRIBUTES));

            // Pointer >> fileId.
            Marshal.WriteInt64(refPtr, (long)frn);

            UNICODE_STRING unicodeString;
            unicodeString.Length = 8;
            unicodeString.MaximumLength = 8;
            unicodeString.Buffer = refPtr;
            *(UNICODE_STRING*)objAttIntPtr = unicodeString;

            // InitializeObjectAttributes.
            objAttributes.length = (uint)sizeof(OBJECT_ATTRIBUTES);
            objAttributes.objectName = objAttIntPtr;
            objAttributes.rootDirectory = _volumeRootHandle.DangerousGetHandle();
            objAttributes.attributes = (int)ObjectAttribute.OBJ_CASE_INSENSITIVE;
            try
            {
                var bSuccess = NtCreateFile(
                    out var hFile,
                    FileAccess.GENERIC_READ | FileAccess.GENERIC_WRITE,
                    objAttributes,
                    out ioStatusBlock,
                    allocationSize,
                    0,
                    FileShare.ReadWrite,
                    NtFileMode.FILE_OPEN,
                    NtFileCreateOptions.FILE_OPEN_BY_FILE_ID | NtFileCreateOptions.FILE_OPEN_FOR_BACKUP_INTENT,
                    IntPtr.Zero,
                    0);

                using (hFile)
                {
                    if (bSuccess == 0)
                    {
                        bSuccess = NtQueryInformationFile(hFile, ioStatusBlock, buffer, 4096,
                            FILE_INFORMATION_CLASS.FileNameInformation);

                        if (bSuccess == 0)
                        {
                            // The first 4 bytes are the name length.
                            var nameLength = Marshal.ReadInt32(buffer, 0);

                            // The next bytes are the name.
                            path = Marshal.PtrToStringUni(new IntPtr(buffer.ToInt64() + 4), nameLength / 2);

                            return true;
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
                Marshal.FreeHGlobal(objAttIntPtr);
                Marshal.FreeHGlobal(refPtr);
            }

            return false;
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                _volumeRootHandle.Dispose();
        }

        #endregion
    }
}

