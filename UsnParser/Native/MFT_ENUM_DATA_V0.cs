using System.Runtime.InteropServices;

namespace UsnParser
{
    /// <summary>Contains information defining the boundaries for and starting place of an enumeration of update sequence number (USN) change journal records.
    /// It is used as the input buffer for the FSCTL_ENUM_USN_DATA control code.</summary>
    /// <remarks>Prior to Windows Server 2012 this structure was named MFT_ENUM_DATA.</remarks>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MFT_ENUM_DATA_V0
    {
        /// <summary>The ordinal position within the files on the current volume at which the enumeration is to begin.</summary>
        /// <remarks>The first call to FSCTL_ENUM_USN_DATA during an enumeration must have the StartFileReferenceNumber member set to (DWORDLONG)0.
        /// Each call to FSCTL_ENUM_USN_DATA retrieves the starting point for the subsequent call as the first entry in the output buffer.
        /// Subsequent calls must be made with StartFileReferenceNumber set to this value. For more information, see FSCTL_ENUM_USN_DATA.</remarks>
        [MarshalAs(UnmanagedType.U8)] public ulong StartFileReferenceNumber;


        /// <summary>The lower boundary of the range of USN values used to filter which records are returned.
        /// Only records whose last change journal USN is between or equal to the LowUsn and HighUsn member values are returned.</summary>
        [MarshalAs(UnmanagedType.I8)] public long LowUsn;


        /// <summary>The upper boundary of the range of USN values used to filter which files are returned.</summary>
        [MarshalAs(UnmanagedType.I8)] public long HighUsn;
    }
}
