using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    /// <summary>Contains information that describes an update sequence number (USN) change journal.</summary>
    /// <remarks>For more information, see Creating, Modifying, and Deleting a Change Journal.</remarks>
    // https://docs.microsoft.com/en-us/windows/win32/api/winioctl/ns-winioctl-create_usn_journal_data typedef struct { DWORDLONG
    // MaximumSize; DWORDLONG AllocationDelta; } CREATE_USN_JOURNAL_DATA, *PCREATE_USN_JOURNAL_DATA;
    [StructLayout(LayoutKind.Sequential)]
    public struct CREATE_USN_JOURNAL_DATA
    {
        /// <summary>
        /// <para>The target maximum size that the NTFS file system allocates for the change journal, in bytes.</para>
        /// <para>
        /// The change journal can grow larger than this value, but it is then truncated at the next NTFS file system checkpoint to less
        /// than this value.
        /// </para>
        /// </summary>
        public ulong MaximumSize;

        /// <summary>
        /// <para>The size of memory allocation that is added to the end and removed from the beginning of the change journal, in bytes.</para>
        /// <para>
        /// The change journal can grow to more than the sum of the values of <c>MaximumSize</c> and <c>AllocationDelta</c> before being trimmed.
        /// </para>
        /// </summary>
        public ulong AllocationDelta;
    }
}
