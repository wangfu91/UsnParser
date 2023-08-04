using System;

namespace UsnParser.Native
{
    [Flags]
    public enum UsnReason : uint
    {
        NONE = 0x00000000,

        /// <summary>
        /// A user has either changed one or more file or directory attributes (for example, the read-only, hidden, system, archive, or
        /// sparse attribute), or one or more time stamps.
        /// </summary>
        BASIC_INFO_CHANGE = 0x00008000,

        /// <summary>The file or directory is closed.</summary>
        CLOSE = 0x80000000,

        /// <summary>The compression state of the file or directory is changed from or to compressed.</summary>
        COMPRESSION_CHANGE = 0x00020000,

        /// <summary>The file or directory is extended (added to).</summary>
        DATA_EXTEND = 0x00000002,

        /// <summary>The data in the file or directory is overwritten.</summary>
        DATA_OVERWRITE = 0x00000001,

        /// <summary>The file or directory is truncated.</summary>
        DATA_TRUNCATION = 0x00000004,

        /// <summary>
        /// The user made a change to the extended attributes of a file or directory.
        /// <para>These NTFS file system attributes are not accessible to Windows-based applications.</para>
        /// </summary>
        EA_CHANGE = 0x00000400,

        /// <summary>The file or directory is encrypted or decrypted.</summary>
        ENCRYPTION_CHANGE = 0x00040000,

        /// <summary>The file or directory is created for the first time.</summary>
        FILE_CREATE = 0x00000100,

        /// <summary>The file or directory is deleted.</summary>
        FILE_DELETE = 0x00000200,

        /// <summary>
        /// An NTFS file system hard link is added to or removed from the file or directory.
        /// <para>
        /// An NTFS file system hard link, similar to a POSIX hard link, is one of several directory entries that see the same file or directory.
        /// </para>
        /// </summary>
        HARD_LINK_CHANGE = 0x00010000,

        /// <summary>
        /// A user changes the FILE_ATTRIBUTE_NOT_CONTENT_INDEXED attribute.
        /// <para>
        /// That is, the user changes the file or directory from one where content can be indexed to one where content cannot be indexed,
        /// or vice versa. Content indexing permits rapid searching of data by building a database of selected content.
        /// </para>
        /// </summary>
        INDEXABLE_CHANGE = 0x00004000,

        /// <summary>
        /// A user changed the state of the FILE_ATTRIBUTE_INTEGRITY_STREAM attribute for the given stream.
        /// <para>
        /// On the ReFS file system, integrity streams maintain a checksum of all data for that stream, so that the contents of the file
        /// can be validated during read or write operations.
        /// </para>
        /// </summary>
        INTEGRITY_CHANGE = 0x00800000,

        /// <summary>The one or more named data streams for a file are extended (added to).</summary>
        NAMED_DATA_EXTEND = 0x00000020,

        /// <summary>The data in one or more named data streams for a file is overwritten.</summary>
        NAMED_DATA_OVERWRITE = 0x00000010,

        /// <summary>The one or more named data streams for a file is truncated.</summary>
        NAMED_DATA_TRUNCATION = 0x00000040,

        /// <summary>The object identifier of a file or directory is changed.</summary>
        OBJECT_ID_CHANGE = 0x00080000,

        /// <summary>A file or directory is renamed, and the file name in the USN_RECORD_V2 structure is the new name.</summary>
        RENAME_NEW_NAME = 0x00002000,

        /// <summary>The file or directory is renamed, and the file name in the USN_RECORD_V2 structure is the previous name.</summary>
        RENAME_OLD_NAME = 0x00001000,

        /// <summary>
        /// The reparse point that is contained in a file or directory is changed, or a reparse point is added to or deleted from a file
        /// or directory.
        /// </summary>
        REPARSE_POINT_CHANGE = 0x00100000,

        /// <summary>A change is made in the access rights to a file or directory.</summary>
        SECURITY_CHANGE = 0x00000800,

        /// <summary>A named stream is added to or removed from a file, or a named stream is renamed.</summary>
        STREAM_CHANGE = 0x00200000,

        /// <summary>The given stream is modified through a TxF transaction.</summary>
        TRANSACTED_CHANGE = 0x00400000,
    }
}
