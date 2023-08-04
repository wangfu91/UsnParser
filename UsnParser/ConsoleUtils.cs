using McMaster.Extensions.CommandLineUtils;
using System;
using UsnParser.Native;

namespace UsnParser
{
    public static class ConsoleUtils
    {
        public static void Write(this IConsole console, ConsoleColor color, string message)
        {
            console.ForegroundColor = color;
            console.Write(message);
            console.ResetColor();
        }

        public static void WriteLine(this IConsole console, ConsoleColor color, string message)
        {
            console.ForegroundColor = color;
            console.WriteLine(message);
            console.ResetColor();
        }

        public static void PrintError(this IConsole console, string message)
        {
            console.WriteLine(ConsoleColor.Red, message);
        }

        public static void PrintUsnJournalState(IConsole console, USN_JOURNAL_DATA_V0 usnData)
        {
            console.WriteLine($"{"Journal ID",-20}: 0x{usnData.UsnJournalID:x16}");
            console.WriteLine($"{"First USN",-20}: {usnData.FirstUsn}");
            console.WriteLine($"{"Next USN",-20}: {usnData.NextUsn}");
            console.WriteLine($"{"Lowest Valid USN",-20}: {usnData.LowestValidUsn}");
            console.WriteLine($"{"Max USN",-20}: {usnData.MaxUsn}");
            console.WriteLine($"{"Max Size",-20}: {usnData.MaximumSize}");
            console.WriteLine($"{"Allocation Delta",-20}: {usnData.AllocationDelta}");
        }

        public static void PrintEntryPath(IConsole console, UsnJournal usnJournal, UsnEntry usnEntry)
        {
            console.WriteLine();
            console.WriteLine($"{"Name",-20}: {usnEntry.Name}");
            console.WriteLine($"{"IsFolder",-20}: {usnEntry.IsFolder}");
            if (usnJournal.TryGetPathFromFileId(usnEntry.ParentFileReferenceNumber, out var path))
            {
                path = $"{usnJournal.VolumeName.TrimEnd('\\')}{path}";
                console.WriteLine($"{"Parent",-20}: {path}");
            }
        }

        public static void PrintUsnEntry(IConsole console, UsnJournal usnJournal, UsnEntry usnEntry)
        {
            console.WriteLine();
            console.WriteLine($"{"USN",-20}: {usnEntry.USN}");
            console.WriteLine(usnEntry.IsFolder
                ? $"{"Directory",-20}: {usnEntry.Name}"
                : $"{"File",-20}: {usnEntry.Name}");
            if (usnJournal.TryGetPathFromFileId(usnEntry.ParentFileReferenceNumber, out var path))
            {
                path = $"{usnJournal.VolumeName.TrimEnd('\\')}{path}";
                console.WriteLine($"{"Parent",-20}: {path}");
            }

            if (usnEntry.TimeStamp > 0)
                console.WriteLine($"{"Timestamp",-20}: {DateTime.FromFileTimeUtc(usnEntry.TimeStamp).ToLocalTime()}");

            console.WriteLine($"{"File ID",-20}: {usnEntry.FileReferenceNumber:x}");
            console.WriteLine($"{"Parent File ID",-20}: {usnEntry.ParentFileReferenceNumber:x}");

            var reason = ((UsnReason)usnEntry.Reason).ToString().Replace(',', '|');
            console.WriteLine($"{"Reason",-20}: {reason}");

            var sourceInfo = ((UsnSource)usnEntry.SourceInfo).ToString().Replace(',', '|');
            console.WriteLine($"{"Source Info",-20}: {sourceInfo}");
        }
    }
}
