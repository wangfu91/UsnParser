using System;
using System.IO;
using System.Runtime.InteropServices;
using static UsnParser.Native.Kernel32;
using static UsnParser.Native.Ntdll;
using FileAccess = UsnParser.Native.FileAccess;
using UsnParser.Native;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;

namespace UsnParser
{
    internal static class PathHelper
    {
        public static void Main(string[] args)
        {
            /*
            var fid = 0x0000000000000000004900000006c2f5;
            var path = PathFromFid("D:", fid);
            Console.WriteLine($"path = {0}", path ?? "");
            */

            //var path = @"D:\tmp\input.docx";
            var path = @"D:\tmp";
            var fid = GetFileIdFromPath(path);
            Console.WriteLine($"fid = {fid}");
        }

        private static SafeFileHandle GetVolumeRootHandle(DriveInfo driveInfo)
        {
            var vol = string.Concat(@"\\.\", driveInfo.Name.TrimEnd('\\'));

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

        public unsafe static long? GetFileIdFromPath(string filePath)
        {
            // Add the '\??\' prefix to convert the it to NT path   
            filePath = @"\??\" + filePath;

            fixed (char* c = &MemoryMarshal.GetReference(filePath.AsSpan()))
            {
                var unicodeString = new UNICODE_STRING
                {
                    // Note that the Lenght and MaximumLenght are in bytes.
                    Length = checked((ushort)(filePath.Length * sizeof(char))),
                    MaximumLength = checked((ushort)((filePath.Length + 1) * sizeof(char))),
                    Buffer = (IntPtr)c
                };

                long fileId = 0L;

                var objAttributes = new OBJECT_ATTRIBUTES
                {
                    length = (uint)sizeof(OBJECT_ATTRIBUTES),
                    objectName = &unicodeString,
                    rootDirectory = IntPtr.Zero,
                    attributes = (int)ObjectAttribute.OBJ_CASE_INSENSITIVE
                };

                var status = NtCreateFile(
                    handle: out var fileHandle,
                    access: FileAccess.FILE_READ_ATTRIBUTES,
                    objectAttributes: objAttributes,
                    ioStatusBlock: out var ioStatusBlock,
                    allocationSize: 0,
                    fileAttributes: 0,
                    shareAccess: FileShare.ReadWrite | FileShare.Delete,
                    createDisposition: NtFileMode.FILE_OPEN,
                    createOptions: 0, // Set to zero, so that it can work for both file and directory.
                    eaBuffer: IntPtr.Zero,
                    eaLength: 0);

                using (fileHandle)
                {
                    if (status == STATUS_SUCCESS)
                    {
                        FILE_INTERNAL_INFORMATION internalInfo;

                        status = NtQueryInformationFile(
                            fileHandle,
                            ioStatusBlock,
                            &internalInfo,
                            (uint)sizeof(FILE_INTERNAL_INFORMATION),
                            FILE_INFORMATION_CLASS.FileInternalInformation);

                        if (status == STATUS_SUCCESS)
                        {
                            fileId = internalInfo.IndexNumber;
                        }
                    }
                }

                return fileId;
            }
        }

        public unsafe static string? PathFromFid(string volumeName, long fid)
        {
            string? path = null;
            var driveInfo = new DriveInfo(volumeName);
            var volumeRootHandle = GetVolumeRootHandle(driveInfo);

            var unicodeString = new UNICODE_STRING
            {
                Length = sizeof(long),
                MaximumLength = sizeof(long),
                Buffer = new IntPtr(&fid)
            };
            var objAttributes = new OBJECT_ATTRIBUTES
            {
                length = (uint)sizeof(OBJECT_ATTRIBUTES),
                objectName = &unicodeString,
                rootDirectory = volumeRootHandle.DangerousGetHandle(),
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
                    const int MaxStackAllocSize = 1024; // Define a threshold for stack allocation
                    byte* pathBuffer = null;
                    IntPtr heapBuffer = IntPtr.Zero;
                    try
                    {
                        while (true)
                        {
                            if (pathBufferSize <= MaxStackAllocSize)
                            {
                                // Allocate the buffer on the stack
                                var stackBuffer = stackalloc byte[pathBufferSize];
                                pathBuffer = stackBuffer;
                            }
                            else
                            {
                                // Allocate the buffer on the heap
                                heapBuffer = Marshal.AllocHGlobal(pathBufferSize);
                                pathBuffer = (byte*)heapBuffer;
                            }

                            status = NtQueryInformationFile(
                                fileHandle,
                                ioStatusBlock,
                                pathBuffer,
                                (uint)pathBufferSize,
                                FILE_INFORMATION_CLASS.FileNameInformation);
                            if (status == STATUS_SUCCESS)
                            {
                                var nameInfo = (FILE_NAME_INFORMATION*)pathBuffer;

                                path = Path.Join(volumeName.TrimEnd(Path.DirectorySeparatorChar), nameInfo->FileName.ToString());
                            }
                            else if (status == STATUS_INFO_LENGTH_MISMATCH || status == STATUS_BUFFER_OVERFLOW)
                            {
                                // The buffer size is not large enough to contain the name information,
                                // increase the buffer size by a factor of 2 then try again.
                                pathBufferSize *= 2;
                                continue;
                            }

                            break;

                        }
                    }
                    finally
                    {
                        if (heapBuffer != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(heapBuffer);
                        }
                    }
                }

                return path;
            }
        }
    }
}
