using System.Runtime.InteropServices;

namespace UsnParser
{
    /// <summary>Contains information defining a set of update sequence number (USN) change journal records to return to the calling process.
    /// It is used by the FSCTL_QUERY_USN_JOURNAL and FSCTL_READ_USN_JOURNAL control codes.</summary>
    /// <remarks>Prior to Windows 8 and Windows Server 2012 this structure was named READ_USN_JOURNAL_DATA.</remarks>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct READ_USN_JOURNAL_DATA_V0
    {
        /// <summary>The USN at which to begin reading the change journal.</summary>
        [MarshalAs(UnmanagedType.U8)] public ulong StartUsn;


        /// <summary>A mask of flags, each flag noting a change for which the file or directory has a record in the change journal.</summary>
        [MarshalAs(UnmanagedType.U4)] public uint ReasonMask;


        /// <summary>A value that specifies when to return change journal records.</summary>
        [MarshalAs(UnmanagedType.U4)] public uint ReturnOnlyOnClose;


        /// <summary>The time-out value, in seconds, used with the <see cref="BytesToWaitFor"/> member to tell the operating system what to do
        /// if the FSCTL_READ_USN_JOURNAL operation requests more data than exists in the change journal.</summary>
        [MarshalAs(UnmanagedType.U8)] public ulong Timeout;


        /// <summary>The number of bytes of unfiltered data added to the change journal. Use this value with <see cref="Timeout"/> to tell the operating system what to do
        /// if the FSCTL_READ_USN_JOURNAL operation requests more data than exists in the change journal.</summary>
        [MarshalAs(UnmanagedType.U8)] public ulong BytesToWaitFor;


        /// <summary>The identifier for the instance of the journal that is current for the volume.</summary>
        [MarshalAs(UnmanagedType.U8)] public ulong UsnJournalId;
    }
}
