﻿using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    /// <summary>Represents an update sequence number (USN) change journal, its records, and its capacity. This structure is the output buffer for the FSCTL_QUERY_USN_JOURNAL control code.</summary>
    /// <remarks>Prior to Windows 8 and Windows Server 2012 this structure was named USN_JOURNAL_DATA.</remarks>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct USN_JOURNAL_DATA_V0
    {
        /// <summary>The current journal identifier. A journal is assigned a new identifier on creation and can be stamped with a new identifier in the course of its existence.
        /// The NTFS file system uses this identifier for an integrity check.</summary>
        [MarshalAs(UnmanagedType.U8)] public ulong UsnJournalID;


        /// <summary>The number of first record that can be read from the journal.</summary>
        [MarshalAs(UnmanagedType.I8)] public long FirstUsn;


        /// <summary>The number of next record to be written to the journal.</summary>
        [MarshalAs(UnmanagedType.I8)] public long NextUsn;


        /// <summary>The first record that was written into the journal for this journal instance.</summary>
        /// <remarks>Enumerating the files or directories on a volume can return a USN lower than this value (in other words, a FirstUsn member value less than the LowestValidUsn member value).
        /// If it does, the journal has been stamped with a new identifier since the last USN was written. In this case, LowestValidUsn may indicate a discontinuity in the journal,
        /// in which changes to some or all files or directories on the volume may have occurred that are not recorded in the change journal.</remarks>
        [MarshalAs(UnmanagedType.I8)] public long LowestValidUsn;


        /// <summary>The largest USN that the change journal supports. An administrator must delete the change journal as the value of NextUsn approaches this value.</summary>
        [MarshalAs(UnmanagedType.I8)] public long MaxUsn;


        /// <summary>The target maximum size for the change journal, in bytes.
        /// The change journal can grow larger than this value, but it is then truncated at the next NTFS file system checkpoint to less than this value.</summary>
        [MarshalAs(UnmanagedType.U8)] public ulong MaximumSize;


        /// <summary>The number of bytes of disk memory added to the end and removed from the beginning of the change journal each time memory is allocated or deallocated.
        /// In other words, allocation and deallocation take place in units of this size. An integer multiple of a cluster size is a reasonable value for this member.</summary>
        [MarshalAs(UnmanagedType.U8)] public ulong AllocationDelta;
    }
}
