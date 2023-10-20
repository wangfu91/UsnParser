using System;
using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    /// <summary>
    /// Contains the information for an update sequence number (USN) change journal version 2.0 record. Applications should not attempt
    /// to work with change journal versions earlier than 2.0. Prior to Windows 8 and Windows Server 2012 this structure was named
    /// <c>USN_RECORD</c>. Use that name to compile with older SDKs and compilers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In output buffers returned from DeviceIoControl operations that work with <c>USN_RECORD_V2</c>, all records are aligned on 64-bit
    /// boundaries from the start of the buffer.
    /// </para>
    /// <para>
    /// To provide a path for upward compatibility in change journal clients, Microsoft provides a major and minor version number of the
    /// change journal software in the <c>USN_RECORD_V2</c> structure. Your code should examine these values, detect its own
    /// compatibility with the change journal software, and if necessary gracefully handle any incompatibility.
    /// </para>
    /// <para>
    /// A change in the minor version number indicates that the existing <c>USN_RECORD_V2</c> structure members are still valid, but that
    /// new members may have been added between the penultimate member and the last, which is a variable-length string.
    /// </para>
    /// <para>
    /// To handle such a change gracefully, your code should not do any compile-time pointer arithmetic that relies on the location of
    /// the last member. For example, this makes the C code unreliable. Instead, rely on run-time calculations by using the
    /// <c>RecordLength</c> member.
    /// </para>
    /// <para>
    /// An increase in the major version number of the change journal software indicates that the <c>USN_RECORD_V2</c> structure may have
    /// undergone major changes, and that the current definition may not be reliable. If your code detects a change in the major version
    /// number of the change journal software, it should not work with the change journal.
    /// </para>
    /// <para>For more information, see Creating, Modifying, and Deleting a Change Journal.</para>
    /// </remarks>
    // https://docs.microsoft.com/en-us/windows/win32/api/winioctl/ns-winioctl-usn_record_v2 typedef struct { DWORD RecordLength; WORD
    // MajorVersion; WORD MinorVersion; DWORDLONG FileReferenceNumber; DWORDLONG ParentFileReferenceNumber; USN Usn; LARGE_INTEGER
    // TimeStamp; DWORD Reason; DWORD SourceInfo; DWORD SecurityId; DWORD FileAttributes; WORD FileNameLength; WORD FileNameOffset; WCHAR
    // FileName[1]; } USN_RECORD_V2, *PUSN_RECORD_V2;
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct USN_RECORD_V2
    {
        /// <summary>
        /// <para>The total length of a record, in bytes.</para>
        /// <para>
        /// Because <c>USN_RECORD_V2</c> is a variable size, the <c>RecordLength</c> member should be used when calculating the address
        /// of the next record in an output buffer, for example, a buffer that is returned from operations for the DeviceIoControl
        /// function that work with <c>USN_RECORD_V2</c>.
        /// </para>
        /// <para>
        /// The size in bytes of any change journal record is at most the size of the <c>USN_RECORD_V2</c> structure, plus
        /// MaximumComponentLength characters minus 1 (for the character declared in the structure) times the size of a wide character.
        /// The value of MaximumComponentLength may be determined by calling the GetVolumeInformation function. In C, you can determine a
        /// record size by using the following code example.
        /// </para>
        /// <para>
        /// To maintain compatibility across version changes of the change journal software, use a run-time calculation to determine the
        /// size of the
        /// </para>
        /// <para>USN_RECORD_V2</para>
        /// <para>structure. For more information about compatibility across version changes, see the Remarks section in this topic.</para>
        /// </summary>
        public uint RecordLength;

        /// <summary>
        /// <para>The major version number of the change journal software for this record.</para>
        /// <para>For example, if the change journal software is version 2.0, the major version number is 2.</para>
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>2</term>
        /// <term>The structure is a USN_RECORD_V2 structure and the remainder of the structure should be parsed using that layout.</term>
        /// </item>
        /// <item>
        /// <term>3</term>
        /// <term>The structure is a USN_RECORD_V3 structure and the remainder of the structure should be parsed using that layout.</term>
        /// </item>
        /// <item>
        /// <term>4</term>
        /// <term>The structure is a USN_RECORD_V4 structure and the remainder of the structure should be parsed using that layout.</term>
        /// </item>
        /// </list>
        /// </summary>
        public ushort MajorVersion;

        /// <summary>
        /// The minor version number of the change journal software for this record. For example, if the change journal software is
        /// version 2.0, the minor version number is zero.
        /// </summary>
        public ushort MinorVersion;

        /// <summary>
        /// <para>The ordinal number of the file or directory for which this record notes changes.</para>
        /// <para>This is an arbitrarily assigned value that associates a journal record with a file.</para>
        /// </summary>
        public ulong FileReferenceNumber;

        /// <summary>
        /// <para>The ordinal number of the directory where the file or directory that is associated with this record is located.</para>
        /// <para>This is an arbitrarily assigned value that associates a journal record with a parent directory.</para>
        /// </summary>
        public ulong ParentFileReferenceNumber;

        /// <summary>The USN of this record.</summary>
        public long Usn;

        /// <summary>The standard UTC time stamp (FILETIME) of this record, in 64-bit format.</summary>
        public LongFileTime TimeStamp;

        /// <summary>
        /// <para>
        /// The flags that identify reasons for changes that have accumulated in this file or directory journal record since the file or
        /// directory opened.
        /// </para>
        /// <para>
        /// When a file or directory closes, then a final USN record is generated with the <c>USN_REASON_CLOSE</c> flag set. The next
        /// change (for example, after the next open operation or deletion) starts a new record with a new set of reason flags.
        /// </para>
        /// <para>
        /// A rename or move operation generates two USN records, one that records the old parent directory for the item, and one that
        /// records a new parent.
        /// </para>
        /// <para>The following table identifies the possible flags.</para>
        /// <para><c>Note</c> Unused bits are reserved.</para>
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>USN_REASON_BASIC_INFO_CHANGE 0x00008000</term>
        /// <term>
        /// A user has either changed one or more file or directory attributes (for example, the read-only, hidden, system, archive, or
        /// sparse attribute), or one or more time stamps.
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
        /// <term>The file or directory is extended (added to).</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_DATA_OVERWRITE 0x00000001</term>
        /// <term>The data in the file or directory is overwritten.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_DATA_TRUNCATION 0x00000004</term>
        /// <term>The file or directory is truncated.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_EA_CHANGE 0x00000400</term>
        /// <term>
        /// The user made a change to the extended attributes of a file or directory. These NTFS file system attributes are not
        /// accessible to Windows-based applications.
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
        /// A user changes the FILE_ATTRIBUTE_NOT_CONTENT_INDEXED attribute. That is, the user changes the file or directory from one
        /// where content can be indexed to one where content cannot be indexed, or vice versa. Content indexing permits rapid searching
        /// of data by building a database of selected content.
        /// </term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_INTEGRITY_CHANGE 0x00800000</term>
        /// <term>
        /// A user changed the state of the FILE_ATTRIBUTE_INTEGRITY_STREAM attribute for the given stream. On the ReFS file system,
        /// integrity streams maintain a checksum of all data for that stream, so that the contents of the file can be validated during
        /// read or write operations.
        /// </term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_NAMED_DATA_EXTEND 0x00000020</term>
        /// <term>The one or more named data streams for a file are extended (added to).</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_NAMED_DATA_OVERWRITE 0x00000010</term>
        /// <term>The data in one or more named data streams for a file is overwritten.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_NAMED_DATA_TRUNCATION 0x00000040</term>
        /// <term>The one or more named data streams for a file is truncated.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_OBJECT_ID_CHANGE 0x00080000</term>
        /// <term>The object identifier of a file or directory is changed.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_RENAME_NEW_NAME 0x00002000</term>
        /// <term>A file or directory is renamed, and the file name in the USN_RECORD_V2 structure is the new name.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_RENAME_OLD_NAME 0x00001000</term>
        /// <term>The file or directory is renamed, and the file name in the USN_RECORD_V2 structure is the previous name.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_REPARSE_POINT_CHANGE 0x00100000</term>
        /// <term>
        /// The reparse point that is contained in a file or directory is changed, or a reparse point is added to or deleted from a file
        /// or directory.
        /// </term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_SECURITY_CHANGE 0x00000800</term>
        /// <term>A change is made in the access rights to a file or directory.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_STREAM_CHANGE 0x00200000</term>
        /// <term>A named stream is added to or removed from a file, or a named stream is renamed.</term>
        /// </item>
        /// <item>
        /// <term>USN_REASON_TRANSACTED_CHANGE 0x00400000</term>
        /// <term>The given stream is modified through a TxF transaction.</term>
        /// </item>
        /// </list>
        /// </summary>
        public UsnReason Reason;

        /// <summary>
        /// <para>Additional information about the source of the change, set by the FSCTL_MARK_HANDLE of the DeviceIoControl operation.</para>
        /// <para>
        /// When a thread writes a new USN record, the source information flags in the prior record continues to be present only if the
        /// thread also sets those flags. Therefore, the source information structure allows applications to filter out USN records that
        /// are set only by a known source, for example, an antivirus filter.
        /// </para>
        /// <para>One of the two following values can be set.</para>
        /// <list type="table">
        /// <listheader>
        /// <term>Value</term>
        /// <term>Meaning</term>
        /// </listheader>
        /// <item>
        /// <term>USN_SOURCE_AUXILIARY_DATA 0x00000002</term>
        /// <term>
        /// The operation adds a private data stream to a file or directory. An example might be a virus detector adding checksum
        /// information. As the virus detector modifies the item, the system generates USN records. USN_SOURCE_AUXILIARY_DATA indicates
        /// that the modifications did not change the application data.
        /// </term>
        /// </item>
        /// <item>
        /// <term>USN_SOURCE_DATA_MANAGEMENT 0x00000001</term>
        /// <term>
        /// The operation provides information about a change to the file or directory made by the operating system. A typical use is
        /// when the Remote Storage system moves data from external to local storage. Remote Storage is the hierarchical storage
        /// management software. Such a move usually at a minimum adds the USN_REASON_DATA_OVERWRITE flag to a USN record. However, the
        /// data has not changed from the user's point of view. By noting USN_SOURCE_DATA_MANAGEMENT in the SourceInfo member, you can
        /// determine that although a write operation is performed on the item, data has not changed.
        /// </term>
        /// </item>
        /// <item>
        /// <term>USN_SOURCE_REPLICATION_MANAGEMENT 0x00000004</term>
        /// <term>
        /// The operation is modifying a file to match the contents of the same file which exists in another member of the replica set.
        /// </term>
        /// </item>
        /// <item>
        /// <term>USN_SOURCE_CLIENT_REPLICATION_MANAGEMENT 0x00000008</term>
        /// <term>The operation is modifying a file on client systems to match the contents of the same file that exists in the cloud.</term>
        /// </item>
        /// </list>
        /// </summary>
        public UsnSource SourceInfo;

        /// <summary>The unique security identifier assigned to the file or directory associated with this record.</summary>
        public uint SecurityId;

        /// <summary>
        /// The attributes for the file or directory associated with this record, as returned by the GetFileAttributes function.
        /// Attributes of streams associated with the file or directory are excluded.
        /// </summary>
        public FileFlagsAndAttributes FileAttributes;

        /// <summary>
        /// The length of the name of the file or directory associated with this record, in bytes. The <c>FileName</c> member contains
        /// this name. Use this member to determine file name length, rather than depending on a trailing '\0' to delimit the file name
        /// in <c>FileName</c>.
        /// </summary>
        public ushort FileNameLength;

        /// <summary>The offset of the <c>FileName</c> member from the beginning of the structure.</summary>
        public ushort FileNameOffset;

        /// <summary>
        /// <para>
        /// The name of the file or directory associated with this record in Unicode format. This file or directory name is of variable length.
        /// </para>
        /// <para>
        /// When working with <c>FileName</c>, do not count on the file name that contains a trailing '\0' delimiter, but instead
        /// determine the length of the file name by using <c>FileNameLength</c>.
        /// </para>
        /// <para>
        /// Do not perform any compile-time pointer arithmetic using <c>FileName</c>. Instead, make necessary calculations at run time by
        /// using the value of the <c>FileNameOffset</c> member. Doing so helps make your code compatible with any future versions of <c>USN_RECORD_V2</c>.
        /// </para>
        /// </summary>       
        private char _fileName;

        public unsafe ReadOnlySpan<char> FileName => MemoryMarshal.CreateReadOnlySpan(ref _fileName, FileNameLength / sizeof(char));
    }
}
