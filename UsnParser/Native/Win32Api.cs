using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    internal static partial class Win32Api
    {
        #region constants

        internal const uint USN_REASON_MASK = 0xFFFFFFFF;

        internal const int INVALID_HANDLE_VALUE = -1;

        internal const uint GENERIC_READ = 0x80000000;
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const uint FILE_SHARE_READ = 0x00000001;
        internal const uint FILE_SHARE_WRITE = 0x00000002;
        internal const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        internal const uint CREATE_NEW = 1;
        internal const uint CREATE_ALWAYS = 2;
        internal const uint OPEN_EXISTING = 3;
        internal const uint OPEN_ALWAYS = 4;
        internal const uint TRUNCATE_EXISTING = 5;

        //internal const uint FileNameInformationClass = 9;
        //internal const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        internal const uint FILE_FLAG_BACKUP_SEMANTICS = 33554432;
        internal const uint FILE_OPEN_FOR_BACKUP_INTENT = 16384;
        internal const uint FILE_OPEN_BY_FILE_ID = 8192;
        internal const uint FILE_OPEN = 1;
        internal const uint FILE_OPEN_IF = 0x00000003;

        internal const uint OBJ_CASE_INSENSITIVE = 0x40;
        //internal const OBJ_KERNEL_HANDLE = 0x200;

        // CTL_CODE( DeviceType, Function, Method, Access ) (((DeviceType) << 16) | ((Access) << 14) | ((Function) << 2) | (Method))
        private const uint FILE_DEVICE_FILE_SYSTEM = 0x00000009;

        private const uint METHOD_NEITHER = 3;
        private const uint METHOD_BUFFERED = 0;
        private const uint FILE_ANY_ACCESS = 0;
        //private const uint FILE_SPECIAL_ACCESS = 0;
        //private const uint FILE_READ_ACCESS = 1;
        //private const uint FILE_WRITE_ACCESS = 2;

        //internal static int GWL_EXSTYLE = -20;
        //internal static int WS_EX_LAYERED = 0x00080000;
        //internal static int WS_EX_TRANSPARENT = 0x00000020;

        //internal const uint FSCTL_GET_OBJECT_ID = 0x9009c;

        // FSCTL_ENUM_USN_DATA = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 44,  METHOD_NEITHER, FILE_ANY_ACCESS)
        internal const uint FSCTL_ENUM_USN_DATA = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (44 << 2) | METHOD_NEITHER;

        // FSCTL_READ_USN_JOURNAL = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 46,  METHOD_NEITHER, FILE_ANY_ACCESS)
        internal const uint FSCTL_READ_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (46 << 2) | METHOD_NEITHER;

        //  FSCTL_CREATE_USN_JOURNAL        CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 57,  METHOD_NEITHER, FILE_ANY_ACCESS)
        internal const uint FSCTL_CREATE_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (57 << 2) | METHOD_NEITHER;

        //  FSCTL_QUERY_USN_JOURNAL         CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 61, METHOD_BUFFERED, FILE_ANY_ACCESS)
        internal const uint FSCTL_QUERY_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (61 << 2) | METHOD_BUFFERED;

        // FSCTL_DELETE_USN_JOURNAL        CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 62, METHOD_BUFFERED, FILE_ANY_ACCESS)
        internal const uint FSCTL_DELETE_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (62 << 2) | METHOD_BUFFERED;

        #endregion

        #region dll imports

        /// <summary>
        /// Creates the file specified by 'lpFileName' with desired access, share mode, security attributes,
        /// creation disposition, flags and attributes.
        /// </summary>
        /// <param name="lpFileName">Fully qualified path to a file</param>
        /// <param name="dwDesiredAccess">Requested access (write, read, read/write, none)</param>
        /// <param name="dwShareMode">Share mode (read, write, read/write, delete, all, none)</param>
        /// <param name="lpSecurityAttributes">IntPtr to a 'SECURITY_ATTRIBUTES' structure</param>
        /// <param name="dwCreationDisposition">Action to take on file or device specified by 'lpFileName' (CREATE_NEW,
        /// CREATE_ALWAYS, OPEN_ALWAYS, OPEN_EXISTING, TRUNCATE_EXISTING)</param>
        /// <param name="dwFlagsAndAttributes">File or device attributes and flags (typically FILE_ATTRIBUTE_NORMAL)</param>
        /// <param name="hTemplateFile">IntPtr to a valid handle to a template file with 'GENERIC_READ' access right</param>
        /// <returns>IntPtr handle to the 'lpFileName' file or device or 'INVALID_HANDLE_VALUE'</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFileHandle
           CreateFile(string lpFileName,
              uint dwDesiredAccess,
              uint dwShareMode,
              IntPtr lpSecurityAttributes,
              uint dwCreationDisposition,
              uint dwFlagsAndAttributes,
              IntPtr hTemplateFile);

        /// <summary>
        /// Closes the file specified by the IntPtr 'hObject'.
        /// </summary>
        /// <param name="hObject">IntPtr handle to a file</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool
           CloseHandle(
              IntPtr hObject);

        /// <summary>
        /// Fills the 'BY_HANDLE_FILE_INFORMATION' structure for the file specified by 'hFile'.
        /// </summary>
        /// <param name="hFile">Fully qualified name of a file</param>
        /// <param name="lpFileInformation">Out BY_HANDLE_FILE_INFORMATION argument</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool
           GetFileInformationByHandle(
              SafeFileHandle hFile,
              out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        /// <summary>
        /// Deletes the file specified by 'fileName'.
        /// </summary>
        /// <param name="fileName">Fully qualified path to the file to delete</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteFile(
           string fileName);

        /// <summary>
        /// Read data from the file specified by 'hFile'.
        /// </summary>
        /// <param name="hFile">IntPtr handle to the file to read</param>
        /// <param name="lpBuffer">IntPtr to a buffer of bytes to receive the bytes read from 'hFile'</param>
        /// <param name="nNumberOfBytesToRead">Number of bytes to read from 'hFile'</param>
        /// <param name="lpNumberOfBytesRead">Number of bytes read from 'hFile'</param>
        /// <param name="lpOverlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReadFile(
           IntPtr hFile,
           IntPtr lpBuffer,
           uint nNumberOfBytesToRead,
           out uint lpNumberOfBytesRead,
           IntPtr lpOverlapped);

        /// <summary>
        /// Writes the 
        /// </summary>
        /// <param name="hFile">IntPtr handle to the file to write</param>
        /// <param name="bytes">IntPtr to a buffer of bytes to write to 'hFile'</param>
        /// <param name="nNumberOfBytesToWrite">Number of bytes in 'lpBuffer' to write to 'hFile'</param>
        /// <param name="lpNumberOfBytesWritten">Number of bytes written to 'hFile'</param>
        /// <param name="overlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WriteFile(
           IntPtr hFile,
           IntPtr bytes,
           uint nNumberOfBytesToWrite,
           out uint lpNumberOfBytesWritten,
           int overlapped);

        /// <summary>
        /// Writes the data in 'lpBuffer' to the file specified by 'hFile'.
        /// </summary>
        /// <param name="hFile">IntPtr handle to file to write</param>
        /// <param name="lpBuffer">Buffer of bytes to write to file 'hFile'</param>
        /// <param name="nNumberOfBytesToWrite">Number of bytes in 'lpBuffer' to write to 'hFile'</param>
        /// <param name="lpNumberOfBytesWritten">Number of bytes written to 'hFile'</param>
        /// <param name="overlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WriteFile(
           IntPtr hFile,
           byte[] lpBuffer,
           uint nNumberOfBytesToWrite,
           out uint lpNumberOfBytesWritten,
           int overlapped);

        /// <summary>
        /// Sends the 'dwIoControlCode' to the device specified by 'hDevice'.
        /// </summary>
        /// <param name="hDevice">IntPtr handle to the device to receive 'dwIoControlCode'</param>
        /// <param name="dwIoControlCode">Device IO Control Code to send</param>
        /// <param name="lpInBuffer">Input buffer if required</param>
        /// <param name="nInBufferSize">Size of input buffer</param>
        /// <param name="lpOutBuffer">Output buffer if required</param>
        /// <param name="nOutBufferSize">Size of output buffer</param>
        /// <param name="lpBytesReturned">Number of bytes returned in output buffer</param>
        /// <param name="lpOverlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeviceIoControl(
           SafeFileHandle hDevice,
           uint dwIoControlCode,
           IntPtr lpInBuffer,
           int nInBufferSize,
           out USN_JOURNAL_DATA_V0 lpOutBuffer,
           int nOutBufferSize,
           out uint lpBytesReturned,
           IntPtr lpOverlapped);

        /// <summary>
        /// Sends the control code 'dwIoControlCode' to the device driver specified by 'hDevice'.
        /// </summary>
        /// <param name="hDevice">IntPtr handle to the device to receive 'dwIoControlCode</param>
        /// <param name="dwIoControlCode">Device IO Control Code to send</param>
        /// <param name="lpInBuffer">Input buffer if required</param>
        /// <param name="nInBufferSize">Size of input buffer </param>
        /// <param name="lpOutBuffer">Output buffer if required</param>
        /// <param name="nOutBufferSize">Size of output buffer</param>
        /// <param name="lpBytesReturned">Number of bytes returned</param>
        /// <param name="lpOverlapped">Pointer to an 'OVERLAPPED' struture</param>
        /// <returns></returns>
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeviceIoControl(
           SafeFileHandle hDevice,
           uint dwIoControlCode,
           IntPtr lpInBuffer,
           int nInBufferSize,
           IntPtr lpOutBuffer,
           int nOutBufferSize,
           out uint lpBytesReturned,
           IntPtr lpOverlapped);


        /// <summary>Sets the number of bytes specified by 'size' of the memory associated with the argument 'ptr' to zero.</summary>
        [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode, EntryPoint = "RtlZeroMemory")]
        internal static extern void ZeroMemory(IntPtr ptr, int size);


        /// <summary>
        /// Creates a new file or directory, or opens an existing file, device, directory, or volume
        /// </summary>
        /// <param name="handle">A pointer to a variable that receives the file handle if the call is successful (out)</param>
        /// <param name="access">ACCESS_MASK value that expresses the type of access that the caller requires to the file or directory (in)</param>
        /// <param name="objectAttributes">A pointer to a structure already initialized with InitializeObjectAttributes (in)</param>
        /// <param name="ioStatus">A pointer to a variable that receives the final completion status and information about the requested operation (out)</param>
        /// <param name="allocSize">The initial allocation size in bytes for the file (in)(optional)</param>
        /// <param name="fileAttributes">file attributes (in)</param>
        /// <param name="share">type of share access that the caller would like to use in the file (in)</param>
        /// <param name="createDisposition">what to do, depending on whether the file already exists (in)</param>
        /// <param name="createOptions">options to be applied when creating or opening the file (in)</param>
        /// <param name="eaBuffer">Pointer to an EA buffer used to pass extended attributes (in)</param>
        /// <param name="eaLength">Length of the EA buffer</param>
        /// <returns>either STATUS_SUCCESS or an appropriate error status. If it returns an error status, the caller can find more information about the cause of the failure by checking the IoStatusBlock</returns>
        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern int NtCreateFile(ref IntPtr handle, FileAccess access,
           ref OBJECT_ATTRIBUTES objectAttributes, ref IO_STATUS_BLOCK ioStatus, ref long allocSize, uint fileAttributes,
           FileShare share, uint createDisposition, uint createOptions, IntPtr eaBuffer, uint eaLength);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <param name="IoStatusBlock"></param>
        /// <param name="pInfoBlock"></param>
        /// <param name="length"></param>
        /// <param name="fileInformation"></param>
        /// <returns></returns>
        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern int NtQueryInformationFile(
           IntPtr fileHandle,
           ref IO_STATUS_BLOCK IoStatusBlock,
           IntPtr pInfoBlock,
           uint length,
           FILE_INFORMATION_CLASS fileInformation);

        #endregion

    }
}
