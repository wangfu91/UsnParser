namespace UsnParser
{
    public static class UsnReasons
    {
        public const uint USN_REASON_DATA_OVERWRITE = 0x00000001;
        public const uint USN_REASON_DATA_EXTEND = 0x00000002;
        public const uint USN_REASON_DATA_TRUNCATION = 0x00000004;
        public const uint USN_REASON_NAMED_DATA_OVERWRITE = 0x00000010;
        public const uint USN_REASON_NAMED_DATA_EXTEND = 0x00000020;
        public const uint USN_REASON_NAMED_DATA_TRUNCATION = 0x00000040;
        public const uint USN_REASON_FILE_CREATE = 0x00000100;
        public const uint USN_REASON_FILE_DELETE = 0x00000200;
        public const uint USN_REASON_EA_CHANGE = 0x00000400;
        public const uint USN_REASON_SECURITY_CHANGE = 0x00000800;
        public const uint USN_REASON_RENAME_OLD_NAME = 0x00001000;
        public const uint USN_REASON_RENAME_NEW_NAME = 0x00002000;
        public const uint USN_REASON_INDEXABLE_CHANGE = 0x00004000;
        public const uint USN_REASON_BASIC_INFO_CHANGE = 0x00008000;
        public const uint USN_REASON_HARD_LINK_CHANGE = 0x00010000;
        public const uint USN_REASON_COMPRESSION_CHANGE = 0x00020000;
        public const uint USN_REASON_ENCRYPTION_CHANGE = 0x00040000;
        public const uint USN_REASON_OBJECT_ID_CHANGE = 0x00080000;
        public const uint USN_REASON_REPARSE_POINT_CHANGE = 0x00100000;
        public const uint USN_REASON_STREAM_CHANGE = 0x00200000;
        public const uint USN_REASON_CLOSE = 0x80000000;
    }
}
