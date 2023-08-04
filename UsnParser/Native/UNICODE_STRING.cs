using System;
using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    /// <summary>The <c>UNICODE_STRING</c> structure is used to define Unicode strings.</summary>
    /// <remarks>
    /// <para>
    /// The <c>UNICODE_STRING</c> structure is used to pass Unicode strings. Use RtlUnicodeStringInit or RtlUnicodeStringInitEx to
    /// initialize a <c>UNICODE_STRING</c> structure.
    /// </para>
    /// <para>If the string is null-terminated, <c>Length</c> does not include the trailing null character.</para>
    /// <para>
    /// The <c>MaximumLength</c> is used to indicate the length of <c>Buffer</c> so that if the string is passed to a conversion routine
    /// such as RtlAnsiStringToUnicodeString the returned string does not exceed the buffer size.
    /// </para>
    /// </remarks>
    // https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/content/wudfwdm/ns-wudfwdm-_unicode_string typedef struct
    // _UNICODE_STRING { USHORT Length; USHORT MaximumLength; PWCH Buffer; } UNICODE_STRING;
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct UNICODE_STRING
    {
        /// <summary>The length, in bytes, of the string stored in <c>Buffer</c>.</summary>
        public ushort Length;

        /// <summary>The length, in bytes, of <c>Buffer</c>.</summary>
        public ushort MaximumLength;

        /// <summary>Pointer to a wide-character string.</summary>
        public IntPtr Buffer;
    }
}
