using System;
using System.Runtime.InteropServices;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.NtDll;

namespace UsnParser.Native
{
    public class NativeMethods
    {
        internal const uint USN_REASON_MASK = 0xFFFFFFFF;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <param name="ioStatusBlock"></param>
        /// <param name="pInfoBlock"></param>
        /// <param name="length"></param>
        /// <param name="fileInformation"></param>
        /// <returns></returns>
        [DllImport("ntdll.dll", SetLastError = true)]
        internal static extern int NtQueryInformationFile(
           SafeHFILE fileHandle,
           ref IO_STATUS_BLOCK ioStatusBlock,
           IntPtr pInfoBlock,
           uint length,
           FILE_INFORMATION_CLASS fileInformation);       
    }
}
