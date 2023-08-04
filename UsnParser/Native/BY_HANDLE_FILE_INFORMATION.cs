using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace UsnParser.Native
{
    /// <summary>Contains information that the GetFileInformationByHandle function retrieves.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BY_HANDLE_FILE_INFORMATION
    {
        /// <summary>The file attributes. For possible values and their descriptions, see File Attribute Constants.</summary>
        public FileFlagsAndAttributes dwFileAttributes;

        /// <summary>
        /// A FILETIME structure that specifies when a file or directory is created. If the underlying file system does not support
        /// creation time, this member is zero (0).
        /// </summary>
        public FILETIME ftCreationTime;

        /// <summary>
        /// A FILETIME structure. For a file, the structure specifies the last time that a file is read from or written to. For a
        /// directory, the structure specifies when the directory is created. For both files and directories, the specified date is
        /// correct, but the time of day is always set to midnight. If the underlying file system does not support the last access time,
        /// this member is zero (0).
        /// </summary>
        public FILETIME ftLastAccessTime;

        /// <summary>
        /// A FILETIME structure. For a file, the structure specifies the last time that a file is written to. For a directory, the
        /// structure specifies when the directory is created. If the underlying file system does not support the last write time, this
        /// member is zero (0).
        /// </summary>
        public FILETIME ftLastWriteTime;

        /// <summary>The serial number of the volume that contains a file.</summary>
        public uint dwVolumeSerialNumber;

        /// <summary>The high-order part of the file size.</summary>
        public uint nFileSizeHigh;

        /// <summary>The low-order part of the file size.</summary>
        public uint nFileSizeLow;

        /// <summary>
        /// The number of links to this file. For the FAT file system this member is always 1. For the NTFS file system, it can be more
        /// than 1.
        /// </summary>
        public uint nNumberOfLinks;

        /// <summary>The high-order part of a unique identifier that is associated with a file. For more information, see nFileIndexLow.</summary>
        public uint nFileIndexHigh;

        /// <summary>
        /// The low-order part of a unique identifier that is associated with a file.
        /// <para>
        /// The identifier (low and high parts) and the volume serial number uniquely identify a file on a single computer. To determine
        /// whether two open handles represent the same file, combine the identifier and the volume serial number for each file and
        /// compare them.
        /// </para>
        /// <para>
        /// The ReFS file system, introduced with Windows Server 2012, includes 128-bit file identifiers. To retrieve the 128-bit file
        /// identifier use the GetFileInformationByHandleEx function with FileIdInfo to retrieve the FILE_ID_INFO structure. The 64-bit
        /// identifier in this structure is not guaranteed to be unique on ReFS.
        /// </para>
        /// </summary>
        public uint nFileIndexLow;
    }
}
