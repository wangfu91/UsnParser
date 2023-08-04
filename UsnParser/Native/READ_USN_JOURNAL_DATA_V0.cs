using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    /// <summary>
    /// Contains information defining a set of update sequence number (USN) change journal records to return to the calling process. It
    /// is used by the FSCTL_QUERY_USN_JOURNAL and FSCTL_READ_USN_JOURNAL control codes. Prior to Windows 8 and Windows Server 2012 this
    /// structure was named <c>READ_USN_JOURNAL_DATA</c>. Use that name to compile with older SDKs and compilers. Windows Server 2012
    /// introduced READ_USN_JOURNAL_DATA_V1 to support 128-bit file identifiers used by ReFS.
    /// </summary>
    // https://docs.microsoft.com/en-us/windows/win32/api/winioctl/ns-winioctl-read_usn_journal_data_v0 typedef struct { USN StartUsn;
    // DWORD ReasonMask; DWORD ReturnOnlyOnClose; DWORDLONG Timeout; DWORDLONG BytesToWaitFor; DWORDLONG UsnJournalID; }
    // READ_USN_JOURNAL_DATA_V0, *PREAD_USN_JOURNAL_DATA_V0;
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct READ_USN_JOURNAL_DATA_V0
    {
        /// <summary>
        /// <para>The USN at which to begin reading the change journal.</para>
        /// <para>
        /// To start the read operation at the first record in the journal, set the <c>StartUsn</c> member to zero. Because a USN is
        /// contained in every journal record, the output buffer tells at which record the read operation actually started.
        /// </para>
        /// <para>To start the read operation at a specific record, set <c>StartUsn</c> to that record USN.</para>
        /// <para>
        /// If a nonzero USN is specified that is less than the first USN in the change journal, then an error occurs and the
        /// <c>ERROR_JOURNAL_ENTRY_DELETED</c> error code is returned. This code may indicate a case in which the specified USN is valid
        /// at one time but has since been deleted.
        /// </para>
        /// <para>
        /// For more information on navigating the change journal buffer returned in <c>READ_USN_JOURNAL_DATA_V0</c>, see Walking a
        /// Buffer of Change Journal Records.
        /// </para>
        /// </summary>
        public long StartUsn;

        /// <summary>
        /// <para>
        /// A mask of flags, each flag noting a change for which the file or directory has a record in the change journal. To be returned
        /// in a FSCTL_READ_USN_JOURNAL operation, a change journal record must have at least one of these flags set.
        /// </para>
        /// <para>The list of valid flags is as follows. Unused bits are reserved.</para>
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>USN_REASON_BASIC_INFO_CHANGE 0x00008000</term>
        /// <term>
        /// A user has either changed one or more file or directory attributes (such as the read-only, hidden, system, archive, or sparse
        /// attribute), or one or more time stamps.
        /// </term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_CLOSE 0x80000000</term>
        /// <term>The file or directory is closed.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_COMPRESSION_CHANGE 0x00020000</term>
        /// <term>The compression state of the file or directory is changed from or to compressed.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_DATA_EXTEND 0x00000002</term>
        /// <term>The file or directory is added to.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_DATA_OVERWRITE 0x00000001</term>
        /// <term>Data in the file or directory is overwritten.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_DATA_TRUNCATION 0x00000004</term>
        /// <term>The file or directory is truncated.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_EA_CHANGE 0x00000400</term>
        /// <term>
        /// The user makes a change to the file or directory extended attributes. These NTFS file system attributes are not accessible to
        /// Windows-based applications.
        /// </term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_ENCRYPTION_CHANGE 0x00040000</term>
        /// <term>The file or directory is encrypted or decrypted.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_FILE_CREATE 0x00000100</term>
        /// <term>The file or directory is created for the first time.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_FILE_DELETE 0x00000200</term>
        /// <term>The file or directory is deleted.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_HARD_LINK_CHANGE 0x00010000</term>
        /// <term>
        /// An NTFS file system hard link is added to or removed from the file or directory. An NTFS file system hard link, similar to a
        /// POSIX hard link, is one of several directory entries that see the same file or directory.
        /// </term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_INDEXABLE_CHANGE 0x00004000</term>
        /// <term>
        /// A user changed the FILE_ATTRIBUTE_NOT_CONTENT_INDEXED attribute. That is, the user changed the file or directory from one
        /// that can be content indexed to one that cannot, or vice versa. (Content indexing permits rapid searching of data by building
        /// a database of selected content.)
        /// </term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_NAMED_DATA_EXTEND 0x00000020</term>
        /// <term>One or more named data streams for the file were added to.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_NAMED_DATA_OVERWRITE 0x00000010</term>
        /// <term>Data in one or more named data streams for the file is overwritten.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_NAMED_DATA_TRUNCATION 0x00000040</term>
        /// <term>One or more named data streams for the file is truncated.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_OBJECT_ID_CHANGE 0x00080000</term>
        /// <term>The object identifier of the file or directory is changed.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_RENAME_NEW_NAME 0x00002000</term>
        /// <term>
        /// The file or directory is renamed, and the file name in the USN_RECORD_V2 or USN_RECORD_V3 structure holding this journal
        /// record is the new name.
        /// </term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_RENAME_OLD_NAME 0x00001000</term>
        /// <term>
        /// The file or directory is renamed, and the file name in the USN_RECORD_V2 or USN_RECORD_V3 structure holding this journal
        /// record is the previous name.
        /// </term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_REPARSE_POINT_CHANGE 0x00100000</term>
        /// <term>
        /// The reparse point contained in the file or directory is changed, or a reparse point is added to or deleted from the file or directory.
        /// </term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_SECURITY_CHANGE 0x00000800</term>
        /// <term>A change is made in the access permissions to the file or directory.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_STREAM_CHANGE 0x00200000</term>
        /// <term>A named stream is added to or removed from the file or directory, or a named stream is renamed.</term>
        /// </item>
        /// </list>
        /// </summary>
        public uint ReasonMask;

        /// <summary>
        /// <para>A value that specifies when to return change journal records.</para>
        /// <para>
        /// To receive notification when the final handle for the changed file or directory is closed, rather than at the time a change
        /// occurs, set <c>ReturnOnlyOnClose</c> to any nonzero value and specify the <c>USN_REASON_CLOSE</c> flag in the
        /// <c>ReasonMask</c> member.
        /// </para>
        /// <para>
        /// All changes indicated by <c>ReasonMask</c> flags eventually generate a call to the change journal software when the file is
        /// closed. If your DeviceIoControl call is waiting for the file to be closed, that call in turn will allow your
        /// <c>DeviceIoControl</c> call to return. In the event that a file or directory is not closed prior to a volume failure,
        /// operating system failure, or shutdown, a cleanup call to the change journal software occurs the next time the volume is
        /// mounted. The call occurs even if there is an intervening system restart.
        /// </para>
        /// <para>
        /// To receive notification the first time each change is logged, as well as at cleanup, set <c>ReturnOnlyOnClose</c> to zero.
        /// </para>
        /// <para>
        /// Whether <c>ReturnOnlyOnClose</c> is zero or nonzero, the records generated at cleanup log within the change journal all
        /// reasons for USN changes that occurred to the file or directory. Each time a final close operation occurs for an item, a USN
        /// close record is written to the change journal, and the <c>ReasonMask</c> flags for the item are all reset.
        /// </para>
        /// <para>
        /// For a file or directory for which no user data exists (for example, a mounted folder), the final close operation occurs when
        /// the CloseHandle function is called on the last user handle to the item.
        /// </para>
        /// </summary>
        public uint ReturnOnlyOnClose;

        /// <summary>
        /// <para>
        /// The time-out value, in seconds, used with the <c>BytesToWaitFor</c> member to tell the operating system what to do if the
        /// FSCTL_READ_USN_JOURNAL operation requests more data than exists in the change journal.
        /// </para>
        /// <para>
        /// If <c>Timeout</c> is zero and <c>BytesToWaitFor</c> is nonzero, and the FSCTL_READ_USN_JOURNAL operation call reaches the end
        /// of the change journal without finding data to return, <c>FSCTL_READ_USN_JOURNAL</c> waits until <c>BytesToWaitFor</c> bytes
        /// of unfiltered data have been added to the change journal and then retrieves the specified records.
        /// </para>
        /// <para>
        /// If <c>Timeout</c> is nonzero and <c>BytesToWaitFor</c> is nonzero, and the FSCTL_READ_USN_JOURNAL operation call reaches the
        /// end of the change journal without finding data to return, <c>FSCTL_READ_USN_JOURNAL</c> waits <c>Timeout</c> seconds and then
        /// attempts to return the specified records. After <c>Timeout</c> seconds, <c>FSCTL_READ_USN_JOURNAL</c> retrieves any records
        /// available within the specified range.
        /// </para>
        /// <para>
        /// In either case, after the time-out period any new data appended to the change journal is processed. If there are still no
        /// records to return from the specified set, the time-out period is repeated. In this mode, FSCTL_READ_USN_JOURNAL remains
        /// outstanding until at least one record is returned or I/O is canceled.
        /// </para>
        /// <para>
        /// If <c>BytesToWaitFor</c> is zero, then <c>Timeout</c> is ignored. <c>Timeout</c> is also ignored for asynchronously opened handles.
        /// </para>
        /// </summary>
        public ulong Timeout;

        /// <summary>
        /// <para>
        /// The number of bytes of unfiltered data added to the change journal. Use this value with <c>Timeout</c> to tell the operating
        /// system what to do if the FSCTL_READ_USN_JOURNAL operation requests more data than exists in the change journal.
        /// </para>
        /// <para>
        /// If <c>BytesToWaitFor</c> is zero, then <c>Timeout</c> is ignored. In this case, the FSCTL_READ_USN_JOURNAL operation always
        /// returns successfully when the end of the change journal file is encountered. It also retrieves the USN that should be used
        /// for the next <c>FSCTL_READ_USN_JOURNAL</c> operation. When the returned next USN is the same as the <c>StartUsn</c> supplied,
        /// there are no records available. The calling process should not use <c>FSCTL_READ_USN_JOURNAL</c> again immediately.
        /// </para>
        /// <para>
        /// Because the amount of data returned cannot be predicted when <c>BytesToWaitFor</c> is zero, you run a risk of overflowing the
        /// output buffer. To reduce this risk, specify a nonzero <c>BytesToWaitFor</c> value in repeated FSCTL_READ_USN_JOURNAL
        /// operations until all records in the change journal are exhausted. Then specify zero to await new records.
        /// </para>
        /// <para>
        /// Alternatively, use the lpBytesReturned parameter of DeviceIoControl in the FSCTL_READ_USN_JOURNAL operation call to determine
        /// the amount of data available, reallocate the output buffer (with room to spare for new records), and call
        /// <c>DeviceIoControl</c> again.
        /// </para>
        /// </summary>
        public ulong BytesToWaitFor;

        /// <summary>
        /// <para>The identifier for the instance of the journal that is current for the volume.</para>
        /// <para>
        /// The NTFS file system can miss putting events in the change journal if the change journal is stopped and restarted or deleted
        /// and re-created. If either of these events occurs, the NTFS file system gives the journal a new identifier. If the journal
        /// identifier does not agree with the current journal identifier, the call to DeviceIoControl fails and returns an appropriate
        /// error code. To retrieve the new journal identifier, call <c>DeviceIoControl</c> with the FSCTL_QUERY_USN_JOURNAL operation.
        /// </para>
        /// </summary>
        public ulong UsnJournalID;
    }
}
