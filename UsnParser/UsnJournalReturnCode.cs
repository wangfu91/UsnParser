﻿namespace UsnParser
{
    public enum UsnJournalReturnCode
    {
        INVALID_HANDLE_VALUE = -1,
        USN_JOURNAL_SUCCESS = 0,
        ERROR_INVALID_FUNCTION = 1,
        ERROR_FILE_NOT_FOUND = 2,
        ERROR_PATH_NOT_FOUND = 3,
        ERROR_TOO_MANY_OPEN_FILES = 4,
        ERROR_ACCESS_DENIED = 5,
        ERROR_INVALID_HANDLE = 6,
        ERROR_INVALID_DATA = 13,
        ERROR_HANDLE_EOF = 38,
        ERROR_NOT_SUPPORTED = 50,
        ERROR_INVALID_PARAMETER = 87,
        ERROR_JOURNAL_DELETE_IN_PROGRESS = 1178,
        USN_JOURNAL_NOT_ACTIVE = 1179,
        ERROR_JOURNAL_ENTRY_DELETED = 1181,
        ERROR_INVALID_USER_BUFFER = 1784,
        USN_JOURNAL_INVALID = 17001,
        VOLUME_NOT_NTFS = 17003,
        INVALID_FILE_REFERENCE_NUMBER = 17004,
        USN_JOURNAL_ERROR = 17005
    }

}
