using DotNet.Globbing;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using UsnParser.Native;
using static UsnParser.Native.Win32Api;

namespace UsnParser
{
    public class UsnJournal : IDisposable
    {
        private readonly DriveInfo _driveInfo;
        private readonly bool _isNtfsVolume;
        private readonly SafeFileHandle _usnJournalRootHandle;

        public string VolumeName { get; }

        public UsnJournal(string drive)
            : this(new DriveInfo(drive))
        {
        }

        public UsnJournal(DriveInfo driveInfo)
        {
            _driveInfo = driveInfo;
            VolumeName = driveInfo.Name;

            _isNtfsVolume = _driveInfo.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase);

            if (!_isNtfsVolume)
            {
                throw new Exception($"{_driveInfo.Name} is not an NTFS volume.");
            }

            _usnJournalRootHandle = GetRootHandle();
        }

        public void CreateUsnJournal(ulong maxSize, ulong allocationDelta)
        {
            if (!_isNtfsVolume)
                throw new Exception($"{_driveInfo.Name} is not an NTFS volume.");

            if (_usnJournalRootHandle.IsInvalid)
                throw new Win32Exception((int)Win32Errors.ERROR_INVALID_HANDLE);

            var cujd = new CREATE_USN_JOURNAL_DATA
            {
                MaximumSize = maxSize,
                AllocationDelta = allocationDelta
            };

            var sizeCujd = Marshal.SizeOf(cujd);
            var cujdBuffer = Marshal.AllocHGlobal(sizeCujd);
            try
            {
                ZeroMemory(cujdBuffer, sizeCujd);
                Marshal.StructureToPtr(cujd, cujdBuffer, true);
                var fOk = DeviceIoControl(
                   _usnJournalRootHandle,
                   FSCTL_CREATE_USN_JOURNAL,
                   cujdBuffer,
                   sizeCujd,
                   IntPtr.Zero,
                   0,
                   out _,
                   IntPtr.Zero);
                if (!fOk)
                {
                    var lastError = Marshal.GetLastWin32Error();
                    throw new Win32Exception(lastError);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(cujdBuffer);
            }
        }

        public void DeleteUsnJournal(USN_JOURNAL_DATA_V0 journalState)
        {
            if (!_isNtfsVolume)
                throw new Exception($"{_driveInfo.Name} is not an NTFS volume.");

            if (_usnJournalRootHandle.IsInvalid)
                throw new Win32Exception((int)Win32Errors.ERROR_INVALID_HANDLE);

            var dujd = new DELETE_USN_JOURNAL_DATA
            {
                UsnJournalID = journalState.UsnJournalID,
                DeleteFlags = (uint)UsnJournalDeleteFlags.USN_DELETE_FLAG_DELETE
            };

            var sizeDujd = Marshal.SizeOf(dujd);
            var dujdBuffer = Marshal.AllocHGlobal(sizeDujd);

            try
            {
                ZeroMemory(dujdBuffer, sizeDujd);
                Marshal.StructureToPtr(dujd, dujdBuffer, true);
                var bSuccess = DeviceIoControl(_usnJournalRootHandle, FSCTL_DELETE_USN_JOURNAL, dujdBuffer, sizeDujd, IntPtr.Zero, 0, out _, IntPtr.Zero);

                if (!bSuccess)
                {
                    var lastError = Marshal.GetLastWin32Error();
                    throw new Win32Exception(lastError);
                }
            }
            finally
            {

                Marshal.FreeHGlobal(dujdBuffer);
            }
        }

        public IEnumerable<UsnEntry> EnumerateUsnEntries(string keyword, FilterOption filterOption)
        {
            var usnState = QueryUsnJournal();

            // Set up MFT_ENUM_DATA_V0 structure.
            var mftData = new MFT_ENUM_DATA_V0
            {
                StartFileReferenceNumber = 0,
                LowUsn = 0,
                HighUsn = usnState.NextUsn
            };

            var mftDataSize = Marshal.SizeOf(mftData);
            var mftDataBuffer = Marshal.AllocHGlobal(mftDataSize);

            ZeroMemory(mftDataBuffer, mftDataSize);
            Marshal.StructureToPtr(mftData, mftDataBuffer, true);


            // Set up the data buffer which receives the USN_RECORD data.
            const int pDataSize = sizeof(ulong) + 10000;
            var pData = Marshal.AllocHGlobal(pDataSize);
            ZeroMemory(pData, pDataSize);


            // Gather up volume's directories.
            while (DeviceIoControl(
                _usnJournalRootHandle,
                FSCTL_ENUM_USN_DATA,
                mftDataBuffer,
                mftDataSize,
                pData,
                pDataSize,
                out var outBytesReturned,
                IntPtr.Zero))
            {
                var pUsnRecord = new IntPtr(pData.ToInt64() + sizeof(long));

                // While there is at least one entry in the USN journal.
                while (outBytesReturned > 60)
                {
                    var usnEntry = new UsnEntry(pUsnRecord);

                    switch (filterOption)
                    {
                        case FilterOption.OnlyFiles when usnEntry.IsFolder:
                        case FilterOption.OnlyDirectories when !usnEntry.IsFolder:
                            {
                                pUsnRecord = new IntPtr(pUsnRecord.ToInt64() + usnEntry.RecordLength);
                                outBytesReturned -= usnEntry.RecordLength;
                                continue;
                            }
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

                    pUsnRecord = new IntPtr(pUsnRecord.ToInt64() + usnEntry.RecordLength);
                    outBytesReturned -= usnEntry.RecordLength;
                }

                Marshal.WriteInt64(mftDataBuffer, Marshal.ReadInt64(pData, 0));
            }

            Marshal.FreeHGlobal(pData);
        }

        public bool TryGetPathFromFileId(ulong frn, out string path)
        {
            path = null;
            if (_isNtfsVolume)
            {
                if (!_usnJournalRootHandle.IsInvalid)
                {
                    if (frn != 0)
                    {
                        long allocSize = 0;
                        UNICODE_STRING unicodeString;
                        var objAttributes = new OBJECT_ATTRIBUTES();
                        var ioStatusBlock = new IO_STATUS_BLOCK();
                        var hFile = IntPtr.Zero;

                        var buffer = Marshal.AllocHGlobal(4096);
                        var refPtr = Marshal.AllocHGlobal(8);
                        var objAttIntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(objAttributes));

                        // Pointer >> fileid.
                        Marshal.WriteInt64(refPtr, (long)frn);

                        unicodeString.Length = 8;
                        unicodeString.MaximumLength = 8;
                        unicodeString.Buffer = refPtr;

                        // Copy unicode structure to pointer.
                        Marshal.StructureToPtr(unicodeString, objAttIntPtr, true);


                        //  InitializeObjectAttributes.
                        objAttributes.Length = (ulong)Marshal.SizeOf(objAttributes);
                        objAttributes.ObjectName = objAttIntPtr;
                        objAttributes.RootDirectory = _usnJournalRootHandle.DangerousGetHandle();
                        objAttributes.Attributes = (int)OBJ_CASE_INSENSITIVE;


                        var fOk = NtCreateFile(ref hFile, FileAccess.Read, ref objAttributes, ref ioStatusBlock, ref allocSize, 0,
                           FileShare.ReadWrite, FILE_OPEN_IF, FILE_OPEN_BY_FILE_ID/* | Win32Api.FILE_OPEN_FOR_BACKUP_INTENT*/, IntPtr.Zero, 0);


                        if (fOk == 0)
                        {
                            fOk = NtQueryInformationFile(hFile, ref ioStatusBlock, buffer, 4096, FILE_INFORMATION_CLASS.FileNameInformation);

                            if (fOk == 0)
                            {
                                // The first 4 bytes are the name length.
                                var nameLength = Marshal.ReadInt32(buffer, 0);

                                // The next bytes are the name.
                                path = Marshal.PtrToStringUni(new IntPtr(buffer.ToInt64() + 4), nameLength / 2);

                                return true;
                            }
                            else
                            {
                                // throw new Exception($"NtQueryInformationFile failed with error: 0x{fOk:X8}");
                            }
                        }
                        else
                        {
                            // throw new Exception($"NtCreateFile failed with error: {fOk:X8}");
                        }


                        CloseHandle(hFile);
                        Marshal.FreeHGlobal(buffer);
                        Marshal.FreeHGlobal(objAttIntPtr);
                        Marshal.FreeHGlobal(refPtr);
                    }
                }
            }

            return false;
        }

        public USN_JOURNAL_DATA_V0 GetUsnJournalState()
        {
            if (!_isNtfsVolume)
                throw new Exception($"{_driveInfo.Name} is not an NTFS volume.");

            if (_usnJournalRootHandle.IsInvalid)
                throw new Win32Exception((int)Win32Errors.ERROR_INVALID_HANDLE);

            return QueryUsnJournal();
        }

        public IEnumerable<UsnEntry> GetUsnJournalEntries(USN_JOURNAL_DATA_V0 previousUsnState, uint reasonMask, string keyword, FilterOption filterOption, out USN_JOURNAL_DATA_V0 newUsnState)
        {
            var usnEntries = new List<UsnEntry>();
            newUsnState = new USN_JOURNAL_DATA_V0();
            var lastError = (int)UsnJournalReturnCode.VOLUME_NOT_NTFS;
            if (_isNtfsVolume)
            {
                if (!_usnJournalRootHandle.IsInvalid)
                {
                    // Get current USN journal state.
                    lastError = QueryUsnJournal(ref newUsnState);
                    if (lastError == (int)UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
                    {
                        var bReadMore = true;

                        // Sequentially process the USN journal looking for image file entries.
                        const int pbDataSize = sizeof(ulong) * 16384;
                        var pbData = Marshal.AllocHGlobal(pbDataSize);
                        ZeroMemory(pbData, pbDataSize);

                        var rujd = new READ_USN_JOURNAL_DATA_V0
                        {
                            StartUsn = (ulong)previousUsnState.NextUsn,
                            ReasonMask = reasonMask,
                            ReturnOnlyOnClose = 0,
                            Timeout = 0,
                            BytesToWaitFor = 1,
                            UsnJournalId = previousUsnState.UsnJournalID
                        };

                        var sizeRujd = Marshal.SizeOf(rujd);
                        var rujdBuffer = Marshal.AllocHGlobal(sizeRujd);
                        ZeroMemory(rujdBuffer, sizeRujd);
                        Marshal.StructureToPtr(rujd, rujdBuffer, true);

                        // Read USN journal entries.
                        while (bReadMore)
                        {
                            var bRtn = DeviceIoControl(_usnJournalRootHandle, FSCTL_READ_USN_JOURNAL, rujdBuffer, sizeRujd, pbData, pbDataSize, out var bytesRemaining, IntPtr.Zero);
                            if (bRtn)
                            {
                                var pUsnRecord = new IntPtr(pbData.ToInt64() + sizeof(ulong));

                                // While there is at least one entry in the USN journal.
                                while (bytesRemaining > 60)
                                {
                                    var usnEntry = new UsnEntry(pUsnRecord);

                                    // Only read until the current usn points beyond the current state's USN.
                                    if (usnEntry.USN >= newUsnState.NextUsn)
                                    {
                                        bReadMore = false;
                                        break;
                                    }

                                    switch (filterOption)
                                    {
                                        case FilterOption.OnlyFiles when usnEntry.IsFolder:
                                        case FilterOption.OnlyDirectories when !usnEntry.IsFolder:
                                            {
                                                pUsnRecord = new IntPtr(pUsnRecord.ToInt64() + usnEntry.RecordLength);
                                                bytesRemaining -= usnEntry.RecordLength;
                                                continue;
                                            }
                                    }

                                    if (string.IsNullOrWhiteSpace(keyword))
                                    {
                                        usnEntries.Add(usnEntry);
                                    }
                                    else
                                    {
                                        var options = new GlobOptions { Evaluation = { CaseInsensitive = true } };
                                        var glob = Glob.Parse(keyword, options);
                                        if (glob.IsMatch(usnEntry.Name.AsSpan()))
                                        {
                                            usnEntries.Add(usnEntry);
                                        }
                                    }

                                    pUsnRecord = new IntPtr(pUsnRecord.ToInt64() + usnEntry.RecordLength);
                                    bytesRemaining -= usnEntry.RecordLength;
                                }
                            }

                            else
                            {
                                var lastWin32Error = Marshal.GetLastWin32Error();
                                if (lastWin32Error == (int)Win32Errors.ERROR_HANDLE_EOF)
                                    lastError = (int)UsnJournalReturnCode.USN_JOURNAL_SUCCESS;

                                break;
                            }

                            var nextUsn = Marshal.ReadInt64(pbData, 0);
                            if (nextUsn >= newUsnState.NextUsn)
                                break;

                            Marshal.WriteInt64(rujdBuffer, nextUsn);
                        }

                        Marshal.FreeHGlobal(rujdBuffer);
                        Marshal.FreeHGlobal(pbData);
                    }
                }

                else
                    lastError = (int)UsnJournalReturnCode.INVALID_HANDLE_VALUE;
            }

            if (lastError != (int)UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
            {
                throw new Win32Exception(lastError, "FSCTL_READ_USN_JOURNAL failed.");
            }

            return usnEntries;
        }

        public IEnumerable<UsnEntry> ReadUsnEntries(USN_JOURNAL_DATA_V0 previousUsnState, uint reasonMask, string keyword, FilterOption filterOption)
        {
            if (_isNtfsVolume)
            {
                if (!_usnJournalRootHandle.IsInvalid)
                {
                    // Get current USN journal state.
                    var newUsnState = QueryUsnJournal();

                    var bReadMore = true;

                    // Sequentially process the USN journal looking for image file entries.
                    const int pbDataSize = sizeof(ulong) * 16384;
                    var pbData = Marshal.AllocHGlobal(pbDataSize);
                    ZeroMemory(pbData, pbDataSize);

                    var rujd = new READ_USN_JOURNAL_DATA_V0
                    {
                        StartUsn = (ulong)previousUsnState.NextUsn,
                        ReasonMask = reasonMask,
                        ReturnOnlyOnClose = 0,
                        Timeout = 0,
                        BytesToWaitFor = 0,
                        UsnJournalId = previousUsnState.UsnJournalID
                    };

                    var sizeRujd = Marshal.SizeOf(rujd);
                    var rujdBuffer = Marshal.AllocHGlobal(sizeRujd);
                    ZeroMemory(rujdBuffer, sizeRujd);
                    Marshal.StructureToPtr(rujd, rujdBuffer, true);

                    // Read USN journal entries.
                    while (bReadMore)
                    {
                        var bRtn = DeviceIoControl(_usnJournalRootHandle, FSCTL_READ_USN_JOURNAL, rujdBuffer, sizeRujd, pbData, pbDataSize, out var outBytesReturned, IntPtr.Zero);
                        if (bRtn)
                        {
                            var pUsnRecord = new IntPtr(pbData.ToInt64() + sizeof(ulong));

                            // While there is at least one entry in the USN journal.
                            while (outBytesReturned > 60)
                            {
                                var usnEntry = new UsnEntry(pUsnRecord);

                                // Only read until the current usn points beyond the current state's USN.
                                if (usnEntry.USN >= newUsnState.NextUsn)
                                {
                                    bReadMore = false;
                                    break;
                                }

                                switch (filterOption)
                                {
                                    case FilterOption.OnlyFiles when usnEntry.IsFolder:
                                    case FilterOption.OnlyDirectories when !usnEntry.IsFolder:
                                        {
                                            pUsnRecord = new IntPtr(pUsnRecord.ToInt64() + usnEntry.RecordLength);
                                            outBytesReturned -= usnEntry.RecordLength;
                                            continue;
                                        }
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

                                pUsnRecord = new IntPtr(pUsnRecord.ToInt64() + usnEntry.RecordLength);
                                outBytesReturned -= usnEntry.RecordLength;
                            }
                        }
                        else
                        {
                            var lastWin32Error = Marshal.GetLastWin32Error();
                            if (lastWin32Error != (int)Win32Errors.ERROR_HANDLE_EOF)
                            {
                                throw new Win32Exception(lastWin32Error);
                            }

                            break;
                        }

                        var nextUsn = Marshal.ReadInt64(pbData, 0);
                        if (nextUsn >= newUsnState.NextUsn)
                            break;

                        Marshal.WriteInt64(rujdBuffer, nextUsn);
                    }

                    Marshal.FreeHGlobal(rujdBuffer);
                    Marshal.FreeHGlobal(pbData);
                }
            }
        }

        public bool IsUsnJournalActive()
        {
            var success = false;

            if (_isNtfsVolume)
            {
                if (!_usnJournalRootHandle.IsInvalid)
                {
                    var usnJournalCurrentState = new USN_JOURNAL_DATA_V0();
                    var lastError = QueryUsnJournal(ref usnJournalCurrentState);

                    if (lastError == (int)UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
                        success = true;
                }
            }

            return success;
        }


        public bool IsUsnJournalValid(USN_JOURNAL_DATA_V0 usnJournalPreviousState)
        {
            var usnJournalState = new USN_JOURNAL_DATA_V0();

            var success = _isNtfsVolume &&
                !_usnJournalRootHandle.IsInvalid &&
                   QueryUsnJournal(ref usnJournalState) == (int)UsnJournalReturnCode.USN_JOURNAL_SUCCESS &&
                   usnJournalPreviousState.UsnJournalID == usnJournalState.UsnJournalID &&
                   usnJournalPreviousState.NextUsn >= usnJournalState.NextUsn;

            return success;
        }

        private static uint GetVolumeSerialNumber(DriveInfo driveInfo, out uint volumeSerialNumber)
        {
            volumeSerialNumber = 0;
            var pathRoot = string.Concat(@"\\.\", driveInfo.Name);

            using var hRoot = CreateFile(pathRoot,
               0,
               FILE_SHARE_READ | FILE_SHARE_WRITE,
               IntPtr.Zero,
               OPEN_EXISTING,
               FILE_FLAG_BACKUP_SEMANTICS,
               IntPtr.Zero);

            if (hRoot.IsInvalid)
            {
                var lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
            }


            if (!hRoot.IsInvalid)
            {
                var bRtn = GetFileInformationByHandle(hRoot, out var fi);
                if (bRtn)
                {
                    volumeSerialNumber = fi.VolumeSerialNumber;
                }
                else
                {
                    var lastError = Marshal.GetLastWin32Error();
                    throw new Win32Exception(lastError);
                }
            }

            return volumeSerialNumber;
        }


        private SafeFileHandle GetRootHandle()
        {
            var vol = string.Concat(@"\\.\", _driveInfo.Name.TrimEnd('\\'));

            var rootHandle = CreateFile(
                vol,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                default,
                OPEN_EXISTING,
                0,
                default);

            if (rootHandle.IsInvalid)
            {
                var lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
            }

            return rootHandle;
        }

        private USN_JOURNAL_DATA_V0 QueryUsnJournal()
        {
            var result = DeviceIoControl(
                _usnJournalRootHandle,
                FSCTL_QUERY_USN_JOURNAL,
                IntPtr.Zero,
                0,
                out var usnJournalState,
                Marshal.SizeOf(typeof(USN_JOURNAL_DATA_V0)),
                out _,
                IntPtr.Zero);

            if (!result)
            {
                var lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
            }

            return usnJournalState;
        }

        private int QueryUsnJournal(ref USN_JOURNAL_DATA_V0 usnJournalState)
        {
            DeviceIoControl(_usnJournalRootHandle, FSCTL_QUERY_USN_JOURNAL, IntPtr.Zero, 0, out usnJournalState, Marshal.SizeOf(usnJournalState), out _, IntPtr.Zero);
            return Marshal.GetLastWin32Error();
        }


        #region IDisposable

        ~UsnJournal()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _usnJournalRootHandle.Dispose();
            }
        }

        #endregion // Disposable Members
    }
}

