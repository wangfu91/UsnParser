﻿using System;

namespace UsnParser.Native
{
    [Flags]
    public enum UsnSource : uint
    {
        NONE = 0x00000000,

        /// <summary>
        /// The operation adds a private data stream to a file or directory.
        /// <para>
        /// An example might be a virus detector adding checksum information. As the virus detector modifies the item, the system
        /// generates USN records. USN_SOURCE_AUXILIARY_DATA indicates that the modifications did not change the application data.
        /// </para>
        /// </summary>
        DATA_MANAGEMENT = 0x00000001,

        /// <summary>
        /// The operation provides information about a change to the file or directory made by the operating system.
        /// <para>
        /// A typical use is when the Remote Storage system moves data from external to local storage. Remote Storage is the hierarchical
        /// storage management software. Such a move usually at a minimum adds the USN_REASON_DATA_OVERWRITE flag to a USN record.
        /// However, the data has not changed from the user's point of view. By noting USN_SOURCE_DATA_MANAGEMENT in the SourceInfo
        /// member, you can determine that although a write operation is performed on the item, data has not changed.
        /// </para>
        /// </summary>
        AUXILIARY_DATA = 0x00000002,


        /// <summary>
        /// The operation is modifying a file to match the contents of the same file which exists in another member of the replica set.
        /// </summary>
        REPLICATION_MANAGEMENT = 0x00000004,

        /// <summary>
        /// The operation is modifying a file on client systems to match the contents of the same file that exists in the cloud.
        /// </summary>
        CLIENT_REPLICATION_MANAGEMENT = 0x00000008,
    }
}
