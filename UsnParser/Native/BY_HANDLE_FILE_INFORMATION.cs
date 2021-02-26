﻿using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace UsnParser.Native
{
    /// <summary>
    /// By Handle File Information structure, contains File Attributes (32bits), Creation Time(FILETIME),
    /// Last Access Time(FILETIME), Last Write Time(FILETIME), Volume Serial Number (32bits),
    /// File Size High (32bits), File Size Low (32bits), Number of Links (32bits), File Index High (32bits),
    /// File Index Low (32bits).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct BY_HANDLE_FILE_INFORMATION
    {
        public uint FileAttributes;
        public FILETIME CreationTime;
        public FILETIME LastAccessTime;
        public FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }
}
