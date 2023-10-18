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

namespace UsnParser
{
    public class UsnJournal : IDisposable
    {
        private readonly DriveInfo _driveInfo;
        private readonly bool _isUsnSupported;
        private readonly SafeFileHandle _usnJournalRootHandle;

        public string VolumeName { get; }

        public UsnJournal(DriveInfo driveInfo)
        {
            _driveInfo = driveInfo;
            VolumeName = driveInfo.Name;

            _isUsnSupported = _driveInfo.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase)
                || _driveInfo.DriveFormat.Equals("ReFs", StringComparison.OrdinalIgnoreCase);

            if (!_isUsnSupported)
            {
                throw new Exception($"{_driveInfo.Name} is not an NTFS or ReFS volume, which does not support USN change journal.");
            }

            _usnJournalRootHandle = GetRootHandle();
        }

        public unsafe void CreateUsnJournal(ulong maxSize, ulong allocationDelta)
        {
            if (!_isUsnSupported)
                throw new Exception($"{_driveInfo.Name} is not an NTFS volume.");

            if (_usnJournalRootHandle.IsInvalid)
                throw new Win32Exception((int)Win32Error.ERROR_INVALID_HANDLE);

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
                   _usnJournalRootHandle,
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

        public IEnumerable<UsnEntry> EnumerateUsnEntries(string keyword, FilterOption filterOption)
        {
            var usnState = QueryUsnJournal();

            // Note: 
            // In ReFS there is no MFT and subsequently no MFT entries.
            // http://www.resilientfilesystem.co.uk/refs-master-file-table

            var mftEnumerator = new MasterFileTableEnumerable(_usnJournalRootHandle, usnState);

            foreach (var entry in mftEnumerator)
            {
                switch (filterOption)
                {
                    case FilterOption.OnlyFiles when entry.IsFolder:
                    case FilterOption.OnlyDirectories when !entry.IsFolder:
                        continue;
                }

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    yield return entry;
                }
                else
                {
                    var options = new GlobOptions { Evaluation = { CaseInsensitive = true } };
                    var glob = Glob.Parse(keyword, options);
                    if (glob.IsMatch(entry.Name.AsSpan()))
                    {
                        yield return entry;
                    }
                }
            }
        }

        public unsafe bool TryGetPathFromFileId(ulong frn, out string? path)
        {
            if (!_isUsnSupported)
                throw new Exception($"{_driveInfo.Name} is not an NTFS volume.");

            if (_usnJournalRootHandle.IsInvalid)
                throw new Win32Exception((int)Win32Error.ERROR_INVALID_HANDLE);

            path = null;
            if (frn == 0) return false;

            long allocSize = 0;
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
            objAttributes.rootDirectory = _usnJournalRootHandle.DangerousGetHandle();
            objAttributes.attributes = (int)ObjectAttribute.OBJ_CASE_INSENSITIVE;
            try
            {
                var bSuccess = NtCreateFile(
                    out var hFile,
                    FileAccess.GENERIC_READ | FileAccess.GENERIC_WRITE,
                    objAttributes,
                    out ioStatusBlock,
                    allocSize,
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

        public USN_JOURNAL_DATA_V0 GetUsnJournalState()
        {
            if (!_isUsnSupported)
                throw new Exception($"{_driveInfo.Name} is not an NTFS volume.");

            if (_usnJournalRootHandle.IsInvalid)
                throw new Win32Exception((int)Win32Error.ERROR_INVALID_HANDLE);

            try
            {
                return QueryUsnJournal();
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == (int)Win32Error.ERROR_JOURNAL_NOT_ACTIVE)
                {
                    var create = Prompt.GetYesNo($"The change journal of volume {VolumeName} is not active, active it now?",
                            defaultAnswer: true,
                            promptColor: ConsoleColor.White,
                            promptBgColor: ConsoleColor.Black);

                    if (create)
                    {
                        // Set default max size to 32MB, default allocation delta to 8MB.
                        CreateUsnJournal(0x2000000, 0x800000);
                        return QueryUsnJournal();
                    }
                }

                throw;
            }
        }

        public IEnumerable<UsnEntry> GetUsnJournalEntries(long startUsn, string keyword, FilterOption filterOption)
        {
            if (!_isUsnSupported)
                throw new Exception($"{_driveInfo.Name} is not an NTFS volume.");

            if (_usnJournalRootHandle.IsInvalid)
                throw new Win32Exception((int)Win32Error.ERROR_INVALID_HANDLE);

            var changeJournal = QueryUsnJournal();
            var options = new ChangeJournalEnumerationOptions
            {
                BytesToWaitFor = 1,
                Timeout = 0,
                ReturnOnlyOnClose = false,
                StartUsn = startUsn,
            };
            var changeJournalEnumerator = new ChangeJournalEnumerable(_usnJournalRootHandle, changeJournal, options);

            foreach (var usnEntry in changeJournalEnumerator)
            {
                switch (filterOption)
                {
                    case FilterOption.OnlyFiles when usnEntry.IsFolder:
                    case FilterOption.OnlyDirectories when !usnEntry.IsFolder:
                        continue;
                }

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    yield return usnEntry;
                }
                else
                {
                    var globOptions = new GlobOptions { Evaluation = { CaseInsensitive = true } };
                    var glob = Glob.Parse(keyword, globOptions);
                    if (glob.IsMatch(usnEntry.Name.AsSpan()))
                    {
                        yield return usnEntry;
                    }
                }
            }
        }

        public IEnumerable<UsnEntry> ReadUsnEntries(string keyword, FilterOption filterOption)
        {
            if (!_isUsnSupported)
                throw new Exception($"{_driveInfo.Name} is not an NTFS/ReFS volume, and does not support change journal.");

            if (_usnJournalRootHandle.IsInvalid)
                throw new Win32Exception((int)Win32Error.ERROR_INVALID_HANDLE);

            // Get current USN journal state.
            var changeJournal = QueryUsnJournal();

            var changeJournalEnumerator = new ChangeJournalEnumerable(_usnJournalRootHandle, changeJournal);

            foreach (var usnEntry in changeJournalEnumerator)
            {
                switch (filterOption)
                {
                    case FilterOption.OnlyFiles when usnEntry.IsFolder:
                    case FilterOption.OnlyDirectories when !usnEntry.IsFolder:
                        continue;
                }

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    yield return usnEntry;
                }
                else
                {
                    var options = new GlobOptions { Evaluation = { CaseInsensitive = true } };
                    var glob = Glob.Parse(keyword, options);
                    if (glob.IsMatch(usnEntry.Name.AsSpan()))
                    {
                        yield return usnEntry;
                    }
                }
            }
        }

        public bool IsUsnJournalActive()
        {
            if (_isUsnSupported)
            {
                if (!_usnJournalRootHandle.IsInvalid)
                {
                    try
                    {
                        var usnJournalCurrentState = QueryUsnJournal();
                    }
                    catch (Win32Exception)
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        public bool IsUsnJournalValid(USN_JOURNAL_DATA_V0 usnJournalPreviousState)
        {
            if (!_isUsnSupported) return false;
            if (_usnJournalRootHandle.IsInvalid) return false;

            try
            {
                var usnJournalState = QueryUsnJournal();
                if (usnJournalPreviousState.UsnJournalID != usnJournalState.UsnJournalID) return false;
                if (usnJournalPreviousState.NextUsn < usnJournalState.NextUsn) return false;

                return true;
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        private SafeFileHandle GetRootHandle()
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

        private unsafe USN_JOURNAL_DATA_V0 QueryUsnJournal()
        {
            var journalDataSize = sizeof(USN_JOURNAL_DATA_V0);
            IntPtr usnJournalStatePtr = Marshal.AllocHGlobal(journalDataSize);
            var bSuccess = DeviceIoControl(
                _usnJournalRootHandle,
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

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                _usnJournalRootHandle.Dispose();
        }

        #endregion
    }
}

