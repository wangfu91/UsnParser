using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    /// <summary>A driver sets an IRP's I/O status block to indicate the final status of an I/O request, before calling IoCompleteRequest for the IRP.</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct IO_STATUS_BLOCK
    {
        /// <summary>This is the completion status, either STATUS_SUCCESS if the requested operation was completed successfully or an informational, warning, or error STATUS_XXX value.</summary>
        [MarshalAs(UnmanagedType.U4)] public uint Status;


        /// <summary>This is set to a request-dependent value. For example, on successful completion of a transfer request, this is set to the number of bytes transferred.
        /// If a transfer request is completed with another STATUS_XXX, this member is set to zero.</summary>
        [MarshalAs(UnmanagedType.U8)] public ulong Information;
    }
}
