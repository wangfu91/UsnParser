using McMaster.Extensions.CommandLineUtils;
using System;
using UsnParser.Enumeration;
using UsnParser.Extensions;
using UsnParser.Native;

namespace UsnParser.Extensions
{
    public static class ConsoleExtension
    {
        private static void WriteLine(this IConsole console, ConsoleColor color, string message)
        {
            console.ForegroundColor = color;
            console.WriteLine(message);
            console.ResetColor();
        }

        public static void PrintError(this IConsole console, string message)
        {
            console.WriteLine(ConsoleColor.Red, message);
        }

        public static void PrintUsnJournalState(this IConsole console, USN_JOURNAL_DATA_V0 usnData)
        {
            console.WriteLine($"{"Journal ID",-20}: 0x{usnData.UsnJournalID:x16}");
            console.WriteLine($"{"First USN",-20}: {usnData.FirstUsn}");
            console.WriteLine($"{"Next USN",-20}: {usnData.NextUsn}");
            console.WriteLine($"{"Lowest Valid USN",-20}: {usnData.LowestValidUsn}");
            console.WriteLine($"{"Max USN",-20}: {usnData.MaxUsn}");
            console.WriteLine($"{"Max Size",-20}: {usnData.MaximumSize}");
            console.WriteLine($"{"Allocation Delta",-20}: {usnData.AllocationDelta}");
        }

        public static void PrintEntryPath(this IConsole console, UsnJournal usnJournal, UsnEntry usnEntry)
        {
            console.WriteLine();
            console.WriteLine($"{"Name",-20}: {usnEntry.FileName}");
            console.WriteLine($"{"IsFolder",-20}: {usnEntry.IsFolder}");
            if (usnJournal.TryGetPathFromFileId(usnEntry.ParentFileReferenceNumber, out var path))
            {
                path = $"{usnJournal.VolumeName.TrimEnd('\\')}{path}";
                console.WriteLine($"{"Parent",-20}: {path}");
            }
        }

        public static void PrintUsnEntry(this IConsole console, UsnJournal usnJournal, UsnEntry usnEntry)
        {
            console.WriteLine();
            console.WriteLine($"{"USN",-20}: {usnEntry.USN}");
            console.WriteLine(usnEntry.IsFolder
                ? $"{"Directory",-20}: {usnEntry.FileName}"
                : $"{"File",-20}: {usnEntry.FileName}");
            if (usnJournal.TryGetPathFromFileId(usnEntry.ParentFileReferenceNumber, out var path))
            {
                path = $"{usnJournal.VolumeName.TrimEnd('\\')}{path}";
                console.WriteLine($"{"Parent",-20}: {path}");
            }

            console.WriteLine($"{"Timestamp",-20}: {usnEntry.TimeStamp.ToLocalTime()}");

            console.WriteLine($"{"File ID",-20}: {usnEntry.FileReferenceNumber:x}");
            console.WriteLine($"{"Parent File ID",-20}: {usnEntry.ParentFileReferenceNumber:x}");

            var reason = usnEntry.Reason.ToString().Replace(',', '|');
            console.WriteLine($"{"Reason",-20}: {reason}");

            var sourceInfo = usnEntry.SourceInfo.ToString().Replace(',', '|');
            console.WriteLine($"{"Source Info",-20}: {sourceInfo}");
        }
    }
}
