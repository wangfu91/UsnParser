using System;
using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    /// <summary>The UNICODE_STRING structure is used to define Unicode strings.</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct UNICODE_STRING
    {
        /// <summary>The length, in bytes, of the string stored in <see cref="Buffer"/>.</summary>
        public ushort Length;


        /// <summary>The length, in bytes, of <see cref="Buffer"/>.</summary>
        public ushort MaximumLength;


        /// <summary>Pointer to a buffer used to contain a string of wide characters.</summary>
        public IntPtr Buffer;
    }
}
