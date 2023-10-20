using System;
using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    //
    // Summary:
    //     The OBJECT_ATTRIBUTES structure specifies attributes that can be applied to objects
    //     or object handles by routines that create objects and/or return handles to objects.
    //
    // Remarks:
    //     Use the InitializeObjectAttributes macro to initialize the members of the OBJECT_ATTRIBUTES
    //     structure. Note that InitializeObjectAttributes initializes the SecurityQualityOfService
    //     member to NULL. If you must specify a non- NULL value, set the SecurityQualityOfService
    //     member after initialization.
    //     To apply the attributes contained in this structure to an object or object handle,
    //     pass a pointer to this structure to a routine that accesses objects or returns
    //     object handles, such as ZwCreateFile or ZwCreateDirectoryObject.
    //     All members of this structure are read-only. If a member of this structure is
    //     a pointer, the object that this member points to is read-only as well. Read-only
    //     members and objects can be used to acquire relevant information but must not
    //     be modified. To set the members of this structure, use the InitializeObjectAttributes
    //     macro.
    //     Driver routines that run in a process context other than that of the system process
    //     must set the OBJ_KERNEL_HANDLE flag for the Attributes member (by using the InitializeObjectAttributes
    //     macro). This restricts the use of a handle opened for that object to processes
    //     running only in kernel mode. Otherwise, the handle can be accessed by the process
    //     in whose context the driver is running.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public unsafe struct OBJECT_ATTRIBUTES
    {
        //
        // Summary:
        //     The number of bytes of data contained in this structure. The InitializeObjectAttributes
        //     macro sets this member to sizeof( OBJECT_ATTRIBUTES).
        public uint length;

        //
        // Summary:
        //     Optional handle to the root object directory for the path name specified by the
        //     ObjectName member. If RootDirectory is NULL, ObjectName must point to a fully
        //     qualified object name that includes the full path to the target object. If RootDirectory
        //     is non- NULL, ObjectName specifies an object name relative to the RootDirectory
        //     directory. The RootDirectory handle can refer to a file system directory or an
        //     object directory in the object manager namespace.
        public IntPtr rootDirectory;

        //
        // Summary:
        //     Pointer to a Unicode string that contains the name of the object for which a
        //     handle is to be opened. This must either be a fully qualified object name, or
        //     a relative path name to the directory specified by the RootDirectory member.
        public UNICODE_STRING* objectName;

        //
        // Summary:
        //     Bitmask of flags that specify object handle attributes. This member can contain
        //     one or more of the flags in the following table.
        //     Flag – Meaning –
        //     OBJ_INHERIT – This handle can be inherited by child processes of the current
        //     process. –
        //     OBJ_PERMANENT – This flag only applies to objects that are named within the object
        //     manager. By default, such objects are deleted when all open handles to them are
        //     closed. If this flag is specified, the object is not deleted when all open handles
        //     are closed. Drivers can use the ZwMakeTemporaryObject routine to make a permanent
        //     object non-permanent. –
        //     OBJ_EXCLUSIVE – If this flag is set and the OBJECT_ATTRIBUTES structure is passed
        //     to a routine that creates an object, the object can be accessed exclusively.
        //     That is, once a process opens such a handle to the object, no other processes
        //     can open handles to this object. If this flag is set and the OBJECT_ATTRIBUTES
        //     structure is passed to a routine that creates an object handle, the caller is
        //     requesting exclusive access to the object for the process context that the handle
        //     was created in. This request can be granted only if the OBJ_EXCLUSIVE flag was
        //     set when the object was created. –
        //     OBJ_CASE_INSENSITIVE – If this flag is specified, a case-insensitive comparison
        //     is used when matching the name pointed to by the ObjectName member against the
        //     names of existing objects. Otherwise, object names are compared using the default
        //     system settings. –
        //     OBJ_OPENIF – If this flag is specified, by using the object handle, to a routine
        //     that creates objects and if that object already exists, the routine should open
        //     that object. Otherwise, the routine creating the object returns an NTSTATUS code
        //     of STATUS_OBJECT_NAME_COLLISION. –
        //     OBJ_OPENLINK – If an object handle, with this flag set, is passed to a routine
        //     that opens objects and if the object is a symbolic link object, the routine should
        //     open the symbolic link object itself, rather than the object that the symbolic
        //     link refers to (which is the default behavior). –
        //     OBJ_KERNEL_HANDLE – The handle is created in system process context and can only
        //     be accessed from kernel mode. –
        //     OBJ_FORCE_ACCESS_CHECK – The routine that opens the handle should enforce all
        //     access checks for the object, even if the handle is being opened in kernel mode.
        //     –
        //     OBJ_VALID_ATTRIBUTES – Reserved. –
        public uint attributes;

        //
        // Summary:
        //     Specifies a security descriptor (SECURITY_DESCRIPTOR) for the object when the
        //     object is created. If this member is NULL, the object will receive default security
        //     settings.
        public IntPtr securityDescriptor;

        //
        // Summary:
        //     Optional quality of service to be applied to the object when it is created. Used
        //     to indicate the security impersonation level and context tracking mode (dynamic
        //     or static). Currently, the InitializeObjectAttributes macro sets this member
        //     to NULL.
        public IntPtr securityQualityOfService;
    }
}
