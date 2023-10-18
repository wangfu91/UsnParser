using System;
using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct FILE_NAME_INFORMATION
    {
        public uint FileNameLength;
        public char _fileName;
        public unsafe ReadOnlySpan<char> FileName => MemoryMarshal.CreateReadOnlySpan(ref _fileName, (int)FileNameLength / sizeof(char));
    }
}
