using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    /// <summary>Contains information on the deletion of an update sequence number (USN) change journal using the FSCTL_DELETE_USN_JOURNAL control code.</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DELETE_USN_JOURNAL_DATA
    {
        /// <summary>The identifier of the change journal to be deleted.</summary>
        /// <remarks> If the journal is active and deletion is requested by setting the USN_DELETE_FLAG_DELETE flag in the DeleteFlags member, then this identifier must specify the change journal for the current volume.
        /// Use FSCTL_QUERY_USN_JOURNAL to retrieve the identifier of this change journal. If in this case the identifier is not for the current volume's change journal, FSCTL_DELETE_USN_JOURNAL fails.
        /// If notification instead of deletion is requested by setting only the USN_DELETE_FLAG_NOTIFY flag in DeleteFlags, UsnJournalID is ignored.</remarks>
        [MarshalAs(UnmanagedType.U8)] public ulong UsnJournalID;


        /// <summary>Indicates whether deletion or notification regarding deletion is performed, or both.</summary>
        [MarshalAs(UnmanagedType.U4)] public uint DeleteFlags;
    }
}
