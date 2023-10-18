using System;
using UsnParser.Native;

namespace UsnParser.Enumeration
{
    public unsafe class UsnEntry
    {
        public uint RecordLength { get; }

        public long USN { get; }

        public DateTimeOffset TimeStamp { get; set; }

        public ulong FileReferenceNumber { get; }

        public ulong ParentFileReferenceNumber { get; }

        public UsnReason Reason { get; }

        public UsnSource SourceInfo { get; }

        public uint SecurityId { get; }

        public string FileName { get; }

        private FileFlagsAndAttributes Attributes { get; }

        public bool IsFolder => (Attributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY) != 0;

        public bool IsHidden => (Attributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_HIDDEN) != 0;

        public UsnEntry(USN_RECORD_V2* record)
        {
            RecordLength = record->RecordLength;
            FileReferenceNumber = record->FileReferenceNumber;
            ParentFileReferenceNumber = record->ParentFileReferenceNumber;
            USN = record->Usn;
            TimeStamp = record->TimeStamp.ToDateTimeOffset();
            Reason = record->Reason;
            SourceInfo = record->SourceInfo;
            SecurityId = record->SecurityId;
            Attributes = record->FileAttributes;
            FileName = record->FileName.ToString();
        }
    }
}
