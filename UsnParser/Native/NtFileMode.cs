namespace UsnParser.Native
{
    public enum NtFileMode
    {
        /// <summary>Replaces the file if it exists. Creates the file if it doesn't exist.</summary>
        FILE_SUPERSEDE = 0x00000000,

        /// <summary>Opens the file if it exists. Returns an error if it doesn't exist.</summary>
        FILE_OPEN = 0x00000001,

        /// <summary>Returns an error if the file exists. Creates the file if it doesn't exist.</summary>
        FILE_CREATE = 0x00000002,

        /// <summary>Opens the file if it exists. Creates the file if it doesn't exist.</summary>
        FILE_OPEN_IF = 0x00000003,

        /// <summary>Open the file, and overwrite it if it exists. Returns an error if it doesn't exist.</summary>
        FILE_OVERWRITE = 0x00000004,

        /// <summary>Open the file, and overwrite it if it exists. Creates the file if it doesn't exist.</summary>
        FILE_OVERWRITE_IF = 0x00000005,

        /// <summary>Undocumented.</summary>
        FILE_MAXIMUM_DISPOSITION = 0x00000005
    }
}
