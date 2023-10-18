using System;
using UsnParser.Native;

namespace UsnParser
{
    /// <summary>The 32bit File Name Length, the 32bit File Name Offset and the File Name.</summary>
    public unsafe class UsnEntry : IComparable<UsnEntry>
    {
        /// <summary>The 32bit USN Record Length.</summary>
        public uint RecordLength { get; }

        /// <summary>The 64bit USN.</summary>
        public long USN { get; }

        /// <summary>The 64bit TimeStamp.</summary>
        public DateTimeOffset TimeStamp { get; set; }

        /// <summary>The 64bit File Reference Number.</summary>
        public ulong FileReferenceNumber { get; }

        /// <summary>The 64bit Parent File Reference Number.</summary>
        public ulong ParentFileReferenceNumber { get; }

        public UsnReason Reason { get; }

        public UsnSource SourceInfo { get; }

        public uint SecurityId { get; }

        public string Name { get; }

        private string? _oldName;
        public string? OldName
        {
            get => 0 != (_fileAttributes & (uint)UsnReason.RENAME_OLD_NAME) ? _oldName : null;
            set => _oldName = value;
        }

        /// <summary>The 32bit File Attributes.</summary>
        private readonly uint _fileAttributes;

        public bool IsFolder => (_fileAttributes & (uint)FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY) != 0;

        public UsnEntry(USN_RECORD_V2* record)
        {
            RecordLength = record->RecordLength;
            FileReferenceNumber = record->FileReferenceNumber;
            ParentFileReferenceNumber = record->ParentFileReferenceNumber;
            USN = record->Usn;
            TimeStamp = (record->TimeStamp).ToDateTimeOffset();
            Reason = record->Reason;
            SourceInfo = record->SourceInfo;
            SecurityId = record->SecurityId;
            _fileAttributes = record->FileAttributes;
            Name = record->FileName.ToString();
        }

        #region IComparable<UsnEntry> Members

        public int CompareTo(UsnEntry other)
        {
            return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
