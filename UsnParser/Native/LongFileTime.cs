﻿using System;

namespace UsnParser.Native
{
    /// <summary>
    /// 100-nanosecond intervals (ticks) since January 1, 1601 (UTC).
    /// </summary>
    /// <remarks>
    /// For NT times that are defined as longs (LARGE_INTEGER, etc.).
    /// Do NOT use for FILETIME unless you are POSITIVE it will fall on an
    /// 8 byte boundary.
    /// </remarks>
    public struct LongFileTime
    {
        /// <summary>
        /// 100-nanosecond intervals (ticks) since January 1, 1601 (UTC).
        /// </summary>
        public long TicksSince1601;

        public DateTimeOffset ToDateTimeOffset() => new DateTimeOffset(DateTime.FromFileTimeUtc(TicksSince1601));
    }
}
