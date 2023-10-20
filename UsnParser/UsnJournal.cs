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
using UsnParser.Cache;

namespace UsnParser
{
    public class UsnJournal : IDisposable
    {
        private readonly DriveInfo _driveInfo;
        private readonly bool _isChangeJournalSupported;
        private readonly SafeFileHandle _volumeRootHandle;
        private readonly LRUCache<ulong, string> _lruCache;

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
            _lruCache = new LRUCache<ulong, string>(4096);
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

                var globOptions = new GlobOptions { Evaluation = { CaseInsensitive = !filterOptions.CaseSensitive } };
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

        public unsafe bool TryGetPath(UsnEntry entry, out string? path)
        {
            path = null;
            if (entry.ParentFileReferenceNumber == 0) return false;
            if (_lruCache.TryGet(entry.FileReferenceNumber, out path))
            {
                return true;
            }

            if (_lruCache.TryGet(entry.ParentFileReferenceNumber, out var parentPath))
            {
                path = Path.Join(parentPath, entry.FileName);
                _lruCache.Set(entry.FileReferenceNumber, path);
                return true;
            }

            var parentFileId = entry.ParentFileReferenceNumber;
            var unicodeString = new UNICODE_STRING
            {
                Length = sizeof(long),
                MaximumLength = sizeof(long),
                Buffer = new IntPtr(&parentFileId)
            };
            var objAttributes = new OBJECT_ATTRIBUTES
            {
                length = (uint)sizeof(OBJECT_ATTRIBUTES),
                objectName = &unicodeString,
                rootDirectory = _volumeRootHandle.DangerousGetHandle(),
                attributes = (int)ObjectAttribute.OBJ_CASE_INSENSITIVE
            };

            var status = NtCreateFile(
                out var fileHandle,
                FileAccess.GENERIC_READ | FileAccess.GENERIC_WRITE,
                objAttributes,
                out var ioStatusBlock,
                0,
                0,
                FileShare.ReadWrite | FileShare.Delete,
                NtFileMode.FILE_OPEN,
                NtFileCreateOptions.FILE_OPEN_BY_FILE_ID | NtFileCreateOptions.FILE_OPEN_FOR_BACKUP_INTENT,
                IntPtr.Zero,
                0);

            using (fileHandle)
            {
                if (status == STATUS_SUCCESS)
                {
                    var pathBufferSize = MAX_PATH;
                    while (true)
                    {
                        var pathBuffer = Marshal.AllocHGlobal(pathBufferSize);
                        try
                        {
                            status = NtQueryInformationFile(
                                fileHandle,
                                ioStatusBlock,
                                pathBuffer,
                                (uint)pathBufferSize,
                                FILE_INFORMATION_CLASS.FileNameInformation);
                            if (status == STATUS_SUCCESS)
                            {
                                var nameInfo = (FILE_NAME_INFORMATION*)pathBuffer;

                                parentPath = Path.Join(VolumeName.TrimEnd(Path.DirectorySeparatorChar), nameInfo->FileName.ToString());
                                _lruCache.Set(parentFileId, parentPath);

                                path = Path.Join(parentPath, entry.FileName);
                                if (entry.IsFolder)
                                {
                                    _lruCache.Set(entry.FileReferenceNumber, path);
                                }

                                return true;
                            }
                            else if (status == STATUS_INFO_LENGTH_MISMATCH || status == STATUS_BUFFER_OVERFLOW)
                            {
                                // The buffer size is not large enough to contain the name information,
                                // increase the buffer size by a factor of 2 then try again.
                                pathBufferSize *= 2;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(pathBuffer);
                        }
                    }
                }
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

