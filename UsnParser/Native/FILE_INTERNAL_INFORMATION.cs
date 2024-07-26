using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsnParser.Native
{
    /// <summary>
    ///  Used to get the reference number for a file. [FILE_INTERNAL_INFORMATION]
    /// </summary>
    /// <remarks>
    /// <see cref="https://msdn.microsoft.com/en-us/library/windows/hardware/ff540318.aspx"/>
    /// </remarks>
    public readonly struct FILE_INTERNAL_INFORMATION
    {
        /// <summary>
        ///  Reference number for the file.
        /// </summary>
        public readonly long IndexNumber;
    }
}
