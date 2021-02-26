using System;
using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    /// <summary>The OBJECT_ATTRIBUTES structure specifies attributes that can be applied to objects or object handles by routines that create objects and/or return handles to objects.</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct OBJECT_ATTRIBUTES
    {
        /// <summary>The number of bytes of data contained in this structure. The InitializeObjectAttributes macro sets this member to sizeof(OBJECT_ATTRIBUTES).</summary>
        [MarshalAs(UnmanagedType.U8)] public ulong Length;


        /// <summary>Optional handle to the root object directory for the path name specified by the ObjectName member.</summary>
        public IntPtr RootDirectory;


        /// <summary>Pointer to a Unicode string that contains the name of the object for which a handle is to be opened.</summary>
        public IntPtr ObjectName;


        /// <summary>Bitmask of flags that specify object handle attributes.</summary>
        [MarshalAs(UnmanagedType.U8)] public ulong Attributes;


        /// <summary>Specifies a security descriptor (SECURITY_DESCRIPTOR) for the object when the object is created. If this member is NULL, the object will receive default security settings.</summary>
        public IntPtr SecurityDescriptor;


        /// <summary>Optional quality of service to be applied to the object when it is created. Used to indicate the security impersonation level and context tracking mode (dynamic or static).</summary>
        public IntPtr SecurityQualityOfService;
    }
}
