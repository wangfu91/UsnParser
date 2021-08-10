using System;
using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    /// <summary>The 32bit File Name Length, the 32bit File Name Offset and the File Name.</summary>
    public class UsnEntry : IComparable<UsnEntry>
    {
        private const int FR_OFFSET = 8;
        private const int PFR_OFFSET = 16;
        private const int USN_OFFSET = 24;
        private const int TIMESTAMP_OFFSET = 32;
        private const int REASON_OFFSET = 40;
        private const int SOURCE_INFO_OFFSET = 44;
        private const int SECURITY_ID_OFFSET = 48;
        public const int FA_OFFSET = 52;
        private const int FNL_OFFSET = 56;
        private const int FN_OFFSET = 58;


        /// <summary>The 32bit USN Record Length.</summary>
        public uint RecordLength { get; }


        /// <summary>The 64bit USN.</summary>
        public long USN { get; }

        /// <summary>The 64bit TimeStamp.</summary>
        public long TimeStamp { get; set; }

        /// <summary>The 64bit File Reference Number.</summary>
        public ulong FileReferenceNumber { get; }


        /// <summary>The 64bit Parent File Reference Number.</summary>
        public ulong ParentFileReferenceNumber { get; }


        /// <summary>The 32bit Reason Code.</summary>
        public uint Reason { get; }

        public uint SourceInfo { get; }

        public uint SecurityId { get; }

        /// <summary>The 32bit Reason Code.</summary>
        public string Name { get; }


        private string _oldName;
        public string OldName
        {
            get => 0 != (_fileAttributes & (uint)UsnReason.RENAME_OLD_NAME) ? _oldName : null;
            set => _oldName = value;
        }


        /// <summary>The 32bit File Attributes.</summary>
        private readonly uint _fileAttributes;


        public bool IsFolder => (_fileAttributes & Win32Api.FILE_ATTRIBUTE_DIRECTORY) != 0;


        /// <summary>USN Record Constructor.</summary>
        /// <param name="ptrToUsnRecord">Buffer pointer to first byte of the USN Record</param>
        public UsnEntry(IntPtr ptrToUsnRecord)
        {
            RecordLength = (uint)Marshal.ReadInt32(ptrToUsnRecord);

            FileReferenceNumber = (ulong)Marshal.ReadInt64(ptrToUsnRecord, FR_OFFSET);
            ParentFileReferenceNumber = (ulong)Marshal.ReadInt64(ptrToUsnRecord, PFR_OFFSET);
            USN = Marshal.ReadInt64(ptrToUsnRecord, USN_OFFSET);
            TimeStamp = Marshal.ReadInt64(ptrToUsnRecord, TIMESTAMP_OFFSET);
            Reason = (uint)Marshal.ReadInt32(ptrToUsnRecord, REASON_OFFSET);
            SourceInfo = (uint)Marshal.ReadInt32(ptrToUsnRecord, SOURCE_INFO_OFFSET);
            SecurityId = (uint)Marshal.ReadInt32(ptrToUsnRecord, SECURITY_ID_OFFSET);

            _fileAttributes = (uint)Marshal.ReadInt32(ptrToUsnRecord, FA_OFFSET);

            var fileNameLength = Marshal.ReadInt16(ptrToUsnRecord, FNL_OFFSET);
            var fileNameOffset = Marshal.ReadInt16(ptrToUsnRecord, FN_OFFSET);

            Name = Marshal.PtrToStringUni(new IntPtr(ptrToUsnRecord.ToInt64() + fileNameOffset), fileNameLength / sizeof(char));
        }


        #region IComparable<UsnEntry> Members

        public int CompareTo(UsnEntry other)
        {
            return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
