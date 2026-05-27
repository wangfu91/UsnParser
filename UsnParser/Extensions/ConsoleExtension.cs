using System;
using UsnParser.Enumeration;
using UsnParser.Native;

namespace UsnParser.Extensions
{
    public static class ConsoleExtension
    {
        public static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ResetColor();
        }

        public static void PrintUsnJournalData(USN_JOURNAL_DATA_V0 usnData)
        {
            Console.WriteLine($"{"Journal ID",-20}: 0x{usnData.UsnJournalID:x16}");
            Console.WriteLine($"{"First USN",-20}: {usnData.FirstUsn}");
            Console.WriteLine($"{"Next USN",-20}: {usnData.NextUsn}");
            Console.WriteLine($"{"Lowest Valid USN",-20}: {usnData.LowestValidUsn}");
            Console.WriteLine($"{"Max USN",-20}: {usnData.MaxUsn}");
            Console.WriteLine($"{"Max Size",-20}: {usnData.MaximumSize}");
            Console.WriteLine($"{"Allocation Delta",-20}: {usnData.AllocationDelta}");
        }

        public static void PrintUsnEntryBasic(UsnJournal usnJournal, UsnEntry usnEntry)
        {
            Console.WriteLine();
            Console.WriteLine($"{"Type",-20}: {(usnEntry.IsFolder ? "Directory" : "File")}");
            if (usnJournal.TryGetPath(usnEntry, out var path))
            {
                Console.WriteLine($"{"Path",-20}: {path}");
            }
            Console.WriteLine($"{"File ID",-20}: 0x{usnEntry.FileReferenceNumber:x}");
            Console.WriteLine($"{"Parent File ID",-20}: 0x{usnEntry.ParentFileReferenceNumber:x}");
        }

        public static void PrintUsnEntryFull(UsnJournal usnJournal, UsnEntry usnEntry)
        {
            Console.WriteLine();
            Console.WriteLine($"{"USN",-20}: {usnEntry.USN}");
            Console.WriteLine($"{"Type",-20}: {(usnEntry.IsFolder ? "Directory" : "File")}");
            if (usnJournal.TryGetPath(usnEntry, out var path))
            {
                Console.WriteLine($"{"Path",-20}: {path}");
            }

            Console.WriteLine($"{"Timestamp",-20}: {usnEntry.TimeStamp.ToLocalTime()}");

            Console.WriteLine($"{"File ID",-20}: 0x{usnEntry.FileReferenceNumber:x}");
            Console.WriteLine($"{"Parent File ID",-20}: 0x{usnEntry.ParentFileReferenceNumber:x}");

            var reason = usnEntry.Reason.ToString().Replace(',', '|');
            Console.WriteLine($"{"Reason",-20}: {reason}");

            var sourceInfo = usnEntry.SourceInfo.ToString().Replace(',', '|');
            Console.WriteLine($"{"Source Info",-20}: {sourceInfo}");
        }
    }
}
