using System;
using System.Runtime.InteropServices;

namespace UsnParser.Native
{
    /// <summary>
    /// <para>
    /// A driver sets an IRP's I/O status block to indicate the final status of an I/O request, before calling IoCompleteRequest for the IRP.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unless a driver's dispatch routine completes an IRP with an error status value, the lowest-level driver in the chain frequently
    /// sets the IRP's I/O status block to the values that will be returned to the original requester of the I/O operation.
    /// </para>
    /// <para>
    /// The IoCompletion routines of higher-level drivers usually check the I/O status block in IRPs completed by lower drivers. By
    /// design, the I/O status block in an IRP is the only information passed back from the underlying device driver to all higher-level
    /// drivers' IoCompletion routines.
    /// </para>
    /// <para>
    /// The operating system implements support routines that write <c>IO_STATUS_BLOCK</c> values to caller-supplied output buffers. For
    /// example, see ZwOpenFile or NtOpenFile. These routines return status codes that might not match the status codes in the
    /// <c>IO_STATUS_BLOCK</c> structures. If one of these routines returns STATUS_PENDING, the caller should wait for the I/O operation
    /// to complete, and then check the status code in the <c>IO_STATUS_BLOCK</c> structure to determine the final status of the
    /// operation. If the routine returns a status code other than STATUS_PENDING, the caller should rely on this status code instead of
    /// the status code in the <c>IO_STATUS_BLOCK</c> structure.
    /// </para>
    /// <para>For more information, see I/O Status Blocks.</para>
    /// </remarks>
    // https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/content/wdm/ns-wdm-_io_status_block typedef struct _IO_STATUS_BLOCK
    // { union { NTSTATUS Status; PVOID Pointer; } DUMMYUNIONNAME; ULONG_PTR Information; } IO_STATUS_BLOCK, *PIO_STATUS_BLOCK;
    [StructLayout(LayoutKind.Sequential)]
    public struct IO_STATUS_BLOCK
    {
        /// <summary>
        /// This is the completion status, either STATUS_SUCCESS if the requested operation was completed successfully or an
        /// informational, warning, or error STATUS_XXX value. For more information, see Using NTSTATUS values.
        /// </summary>
        public uint Status;

        /// <summary>
        /// This is set to a request-dependent value. For example, on successful completion of a transfer request, this is set to the
        /// number of bytes transferred. If a transfer request is completed with another STATUS_XXX, this member is set to zero.
        /// </summary>
        public IntPtr Information;
    }
}
