namespace UsnParser.Native
{
    /// <summary>Specifies the options to apply when the driver creates or opens the file.</summary>
    public enum NtFileCreateOptions
    {
        /// <summary>
        /// The file is a directory. Compatible CreateOptions flags are FILE_SYNCHRONOUS_IO_ALERT, FILE_SYNCHRONOUS_IO_NONALERT,
        /// FILE_WRITE_THROUGH, FILE_OPEN_FOR_BACKUP_INTENT, and FILE_OPEN_BY_FILE_ID. The CreateDisposition parameter must be set to
        /// FILE_CREATE, FILE_OPEN, or FILE_OPEN_IF.
        /// </summary>
        FILE_DIRECTORY_FILE = 0x00000001,

        /// <summary>
        /// System services, file-system drivers, and drivers that write data to the file must actually transfer the data to the file
        /// before any requested write operation is considered complete.
        /// </summary>
        FILE_WRITE_THROUGH = 0x00000002,

        /// <summary>
        /// <para>All access to the file will be sequential.</para>
        /// </summary>
        FILE_SEQUENTIAL_ONLY = 0x00000004,

        /// <summary>
        /// The file cannot be cached or buffered in a driver's internal buffers. This flag is incompatible with the DesiredAccess
        /// parameter's FILE_APPEND_DATA flag.
        /// </summary>
        FILE_NO_INTERMEDIATE_BUFFERING = 0x00000008,

        /// <summary>
        /// All operations on the file are performed synchronously. Any wait on behalf of the caller is subject to premature termination
        /// from alerts. This flag also causes the I/O system to maintain the file-position pointer. If this flag is set, the
        /// SYNCHRONIZE flag must be set in the DesiredAccess parameter.
        /// </summary>
        FILE_SYNCHRONOUS_IO_ALERT = 0x00000010,

        /// <summary>
        /// All operations on the file are performed synchronously. Waits in the system that synchronize I/O queuing and completion are
        /// not subject to alerts. This flag also causes the I/O system to maintain the file-position context. If this flag is set, the
        /// SYNCHRONIZE flag must be set in the DesiredAccess parameter.
        /// </summary>
        FILE_SYNCHRONOUS_IO_NONALERT = 0x00000020,

        /// <summary>
        /// The file is a directory. The file object to open can represent a data file; a logical, virtual, or physical device; or a volume.
        /// </summary>
        FILE_NON_DIRECTORY_FILE = 0x00000040,

        /// <summary>
        /// Create a tree connection for this file in order to open it over the network. This flag is not used by device and
        /// intermediate drivers.
        /// </summary>
        FILE_CREATE_TREE_CONNECTION = 0x00000080,

        /// <summary>
        /// Complete this operation immediately with an alternate success code of STATUS_OPLOCK_BREAK_IN_PROGRESS if the target file is
        /// oplocked, rather than blocking the caller's thread. If the file is oplocked, another caller already has access to the file.
        /// This flag is not used by device and intermediate drivers.
        /// </summary>
        FILE_COMPLETE_IF_OPLOCKED = 0x00000100,

        /// <summary>
        /// If the extended attributes (EAs) for an existing file being opened indicate that the caller must understand EAs to properly
        /// interpret the file, NtCreateFile should return an error. This flag is irrelevant for device and intermediate drivers.
        /// </summary>
        FILE_NO_EA_KNOWLEDGE = 0x00000200,

        /// <summary>formerly known as FILE_OPEN_FOR_RECOVERY</summary>
        FILE_OPEN_REMOTE_INSTANCE = 0x00000400,

        /// <summary>
        /// Access to the file can be random, so no sequential read-ahead operations should be performed by file-system drivers or by
        /// the system.
        /// </summary>
        FILE_RANDOM_ACCESS = 0x00000800,

        /// <summary>
        /// The system deletes the file when the last handle to the file is passed to NtClose. If this flag is set, the DELETE flag must
        /// be set in the DesiredAccess parameter.
        /// </summary>
        FILE_DELETE_ON_CLOSE = 0x00001000,

        /// <summary>
        /// The file name that is specified by the ObjectAttributes parameter includes a binary 8-byte or 16-byte file reference number
        /// or object ID for the file, depending on the file system as shown below. Optionally, a device name followed by a backslash
        /// character may proceed these binary values. For example, a device name will have the following format. This number is
        /// assigned by and specific to the particular file system.
        /// </summary>
        FILE_OPEN_BY_FILE_ID = 0x00002000,

        /// <summary>
        /// The file is being opened for backup intent. Therefore, the system should check for certain access rights and grant the
        /// caller the appropriate access to the file—before checking the DesiredAccess parameter against the file's security
        /// descriptor. This flag not used by device and intermediate drivers.
        /// </summary>
        FILE_OPEN_FOR_BACKUP_INTENT = 0x00004000,

        /// <summary>
        /// When a new file is created, the file MUST NOT be compressed, even if it is on a compressed volume. The flag MUST be ignored
        /// when opening an existing file.
        /// </summary>
        FILE_NO_COMPRESSION = 0x00008000,

        /// <summary>
        /// The file is being opened and an opportunistic lock (oplock) on the file is being requested as a single atomic operation. The
        /// file system checks for oplocks before it performs the create operation, and will fail the create with a return code of
        /// STATUS_CANNOT_BREAK_OPLOCK if the result would be to break an existing oplock.
        /// </summary>
        FILE_OPEN_REQUIRING_OPLOCK = 0x00010000,

        /// <summary>
        /// This flag allows an application to request a Filter opportunistic lock (oplock) to prevent other applications from getting
        /// share violations. If there are already open handles, the create request will fail with STATUS_OPLOCK_NOT_GRANTED. For more
        /// information, see the following Remarks section.
        /// </summary>
        FILE_RESERVE_OPFILTER = 0x00100000,

        /// <summary>
        /// Open a file with a reparse point and bypass normal reparse point processing for the file. For more information, see the
        /// following Remarks section.
        /// </summary>
        FILE_OPEN_REPARSE_POINT = 0x00200000,

        /// <summary>
        /// In a hierarchical storage management environment, this option requests that the file SHOULD NOT be recalled from tertiary
        /// storage such as tape. A file recall can take up to several minutes in a hierarchical storage management environment. The
        /// clients can specify this option to avoid such delays.
        /// </summary>
        FILE_OPEN_NO_RECALL = 0x00400000,

        /// <summary>Open file to query for free space. The client SHOULD set this to 0 and the server MUST ignore it.</summary>
        FILE_OPEN_FOR_FREE_SPACE_QUERY = 0x00800000,

        /// <summary>Undocumented.</summary>
        FILE_VALID_OPTION_FLAGS = 0x00ffffff,

        /// <summary>Undocumented.</summary>
        FILE_VALID_PIPE_OPTION_FLAGS = 0x00000032,

        /// <summary>Undocumented.</summary>
        FILE_VALID_MAILSLOT_OPTION_FLAGS = 0x00000032,

        /// <summary>Undocumented.</summary>
        FILE_VALID_SET_FLAGS = 0x00000036,
    }
}
