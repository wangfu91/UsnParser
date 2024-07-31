using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    public static partial class Ntdll
    {
        /// <summary>The operation completed successfully.</summary>
		public const int STATUS_SUCCESS = 0x00000000;

        /// <summary>{Access Denied} A process has requested access to an object but has not been granted those access rights.</summary>
        public const int STATUS_ACCESS_DENIED = unchecked((int)0xC0000022);

        /// <summary>The specified information record length does not match the length that is required for the specified information class.</summary>
        internal const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);

        /// <summary>{Buffer Overflow} The data was too large to fit into the specified buffer.</summary>
        public const int STATUS_BUFFER_OVERFLOW = unchecked((int)0x80000005);

        /// <summary>
        /// The NtQueryInformationFile routine returns various kinds of information about a file object. 
        /// </summary>
        /// <param name="fileHandle">Handle to a file object. The handle is created by a successful call to NtCreateFile or NtOpenFile, or to an equivalent file create or open routine.</param>
        /// <param name="ioStatusBlock">Pointer to an IO_STATUS_BLOCK structure that receives the final completion status and information about the operation. The Information member receives the number of bytes that this routine actually writes to the FileInformation buffer.</param>
        /// <param name="fileInformation">Pointer to a caller-allocated buffer into which the routine writes the requested information about the file object. The FileInformationClass parameter specifies the type of information that the caller requests.</param>
        /// <param name="length">The size, in bytes, of the buffer pointed to by FileInformation.</param>
        /// <param name="fileInformationClass">Specifies the type of information to be returned about the file, in the buffer that FileInformation points to. </param>
        /// <returns></returns>
        [DllImport("ntdll.dll", SetLastError = true)]
        internal static unsafe extern int NtQueryInformationFile(
            SafeFileHandle fileHandle,
            in IO_STATUS_BLOCK ioStatusBlock,
            void* fileInformation,
            uint length,
            FILE_INFORMATION_CLASS fileInformationClass);

        /// <summary>
        /// Creates a new file or directory, or opens an existing file, device, directory, or volume
        /// </summary>
        /// <param name="handle">A pointer to a variable that receives the file handle if the call is successful (out)</param>
        /// <param name="access">ACCESS_MASK value that expresses the type of access that the caller requires to the file or directory (in)</param>
        /// <param name="objectAttributes">A pointer to a structure already initialized with InitializeObjectAttributes (in)</param>
        /// <param name="ioStatus">A pointer to a variable that receives the final completion status and information about the requested operation (out)</param>
        /// <param name="ioStatusBlock">The initial allocation size in bytes for the file (in)(optional)</param>
        /// <param name="fileAttributes">file attributes (in)</param>
        /// <param name="shareAccess">type of share access that the caller would like to use in the file (in)</param>
        /// <param name="createDisposition">what to do, depending on whether the file already exists (in)</param>
        /// <param name="createOptions">options to be applied when creating or opening the file (in)</param>
        /// <param name="eaBuffer">Pointer to an EA buffer used to pass extended attributes (in)</param>
        /// <param name="eaLength">Length of the EA buffer</param>
        /// <returns>either STATUS_SUCCESS or an appropriate error status. If it returns an error status, the caller can find more information about the cause of the failure by checking the IoStatusBlock</returns>
        [LibraryImport("ntdll.dll", SetLastError = true)]
        internal static partial int NtCreateFile(
            out SafeFileHandle handle,
            FileAccess access,
            in OBJECT_ATTRIBUTES objectAttributes,
            out IO_STATUS_BLOCK ioStatusBlock,
            in long allocationSize,
            FileFlagsAndAttributes fileAttributes,
            FileShare shareAccess,
            NtFileMode createDisposition,
            NtFileCreateOptions createOptions,
            IntPtr eaBuffer,
            uint eaLength);
    }
}
