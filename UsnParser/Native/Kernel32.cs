using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    public static partial class Kernel32
    {
        internal const uint USN_REASON_MASK_ALL = 0xFFFFFFFF;

        internal const int MAX_PATH = 260; 

        // CTL_CODE( DeviceType, Function, Method, Access ) (((DeviceType) << 16) | ((Access) << 14) | ((Function) << 2) | (Method))
        private const uint FILE_DEVICE_FILE_SYSTEM = 0x00000009;

        private const uint METHOD_NEITHER = 3;
        private const uint METHOD_BUFFERED = 0;
        private const uint FILE_ANY_ACCESS = 0;

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


        /// <summary>
        /// The RtlZeroMemory routine fills a block of memory with zeros, given a pointer to the block and the length, in bytes, to be filled.
        /// </summary>
        /// <param name="Destination">A pointer to the memory block to be filled with zeros.</param>
        /// <param name="Length">The number of bytes to fill with zeros.</param>
        // https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/content/wdm/nf-wdm-rtlzeromemory
        [LibraryImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
        internal static partial void ZeroMemory(IntPtr Destination, int Length);

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
        /// <param name="lpOverlapped">Pointer to an 'OVERLAPPED' structure</param>
        /// <returns></returns>
        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DeviceIoControl(
           SafeFileHandle hDevice,
           uint dwIoControlCode,
           IntPtr lpInBuffer,
           int nInBufferSize,
           IntPtr lpOutBuffer,
           int nOutBufferSize,
           out uint lpBytesReturned,
           IntPtr lpOverlapped);

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
        [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial SafeFileHandle
           CreateFile(
            string lpFileName,
            FileAccess dwDesiredAccess,
            FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            FileMode dwCreationDisposition,
            FileFlagsAndAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        /// <summary>
        /// Fills the 'BY_HANDLE_FILE_INFORMATION' structure for the file specified by 'hFile'.
        /// </summary>
        /// <param name="hFile">Fully qualified name of a file</param>
        /// <param name="lpFileInformation">Out BY_HANDLE_FILE_INFORMATION argument</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = "Workaround SYSLIB1051 error")]
        internal static extern bool GetFileInformationByHandle(
            SafeFileHandle hFile,
            out BY_HANDLE_FILE_INFORMATION lpFileInformation);
    }
}
