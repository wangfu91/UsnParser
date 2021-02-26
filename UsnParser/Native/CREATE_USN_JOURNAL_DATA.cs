using System.Runtime.InteropServices;

namespace UsnParser
{
    /// <summary>Contains information that describes an update sequence number (USN) change journal.</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct CREATE_USN_JOURNAL_DATA
    {
        /// <summary>The target maximum size that the NTFS file system allocates for the change journal, in bytes.</summary>
        /// <remarks>The change journal can grow larger than this value, but it is then truncated at the next NTFS file system checkpoint to less than this value.</remarks>
        [MarshalAs(UnmanagedType.U8)] public ulong MaximumSize;


        /// <summary>The size of memory allocation that is added to the end and removed from the beginning of the change journal, in bytes.</summary>
        /// <remarks>The change journal can grow to more than the sum of the values of <see cref="MaximumSize"/> and <see cref="AllocationDelta"/> before being trimmed.</remarks>
        [MarshalAs(UnmanagedType.U8)] public ulong AllocationDelta;
    }
}
