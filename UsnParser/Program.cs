using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using McMaster.Extensions.CommandLineUtils;
using UsnParser.Extensions;
using UsnParser.Native;

namespace UsnParser
{
    [Command(
        Name = "UsnParser",
        FullName = "NTFS USN Journal parser",
        Description = "A command utility to monitoring and filtering NTFS USN Journal.")]
    [HelpOption("-?|-h|--help")]
    [VersionOptionFromMember(MemberName = nameof(GetVersion))]
    internal class Program
    {
        private static void Main(string[] args)
        {
            CommandLineApplication.Execute<Program>(args);
        }

        [Argument(0, Description = "Volume pathname. <Required>")]
        [Required]
        [MinLength(1)]
        public string Volume { get; set; }

        [Option("-m|--monitor", Description = "Monitor real-time USN journal")]
        public bool Read { get; set; }

        [Option("-s|--search", Description = "Search NTFS Master File Table")]
        public bool Search { get; }

        [Option("-f|--filter", Description = "Filter USN journal by entry name")]
        public string Filter { get; set; }

        [Option("-fo|--FileOnly", Description = "Get only the file entries")]
        public bool FileOnly { get; set; }

        [Option("-do|--DirOnly", Description = "Get only the directory entries")]
        public bool DirectoryOnly { get; set; }

        private static string GetVersion()
            => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        private void OnExecute()
        {
            var console = PhysicalConsole.Singleton;
            var cts = new CancellationTokenSource();

            console.CancelKeyPress += (o, e) =>
            {
                console.WriteLine("Keyboard interrupt, exiting...");
                cts.Cancel();
            };

            if (!OperatingSystem.IsWindows())
            {
                console.PrintError($"This tool only support Windows OS.");
                return;
            }

            if (!HasAdministratorPrivilege())
            {
                console.PrintError($"You must have system administrator privileges to access the change journal of \"{Volume}\".");
                return;
            }

            bool? onlyFiles;
            if (FileOnly)
            {
                onlyFiles = true;
            }
            else if (DirectoryOnly)
            {
                onlyFiles = false;
            }
            else
            {
                onlyFiles = null;
            }

            try
            {
                var driveInfo = new DriveInfo(Volume);

                var journal = new UsnJournal(driveInfo);
                var usnState = new USN_JOURNAL_DATA_V0();
                var retCode = journal.GetUsnJournalState(ref usnState);
                if (retCode != 0)
                {
                    console.PrintError($"FSCTL_QUERY_USN_JOURNAL failed with {retCode}");
                    return;
                }

                PrintUsnJournalState(console, usnState);

                if (Read)
                {
                    MonitorRealTimeUsnJournal(console, journal, usnState, Filter, onlyFiles, cts.Token);
                }
                else if (Search)
                {
                    SearchMasterFileTable(console, journal, Filter, onlyFiles, cts.Token);
                }
                else
                {
                    ReadHistoryUsnJournals(console, journal, usnState.UsnJournalID, Filter, onlyFiles, cts.Token);
                }
            }
            catch (Exception ex)
            {
                console.PrintError(ex.Message);
            }
        }

        private static void MonitorRealTimeUsnJournal(IConsole console, UsnJournal journal, USN_JOURNAL_DATA_V0 usnState, string filter, bool? onlyFiles, CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested) return;

                var usnEntries = journal.GetUsnJournalEntries(usnState, 0xFFFFFFFF, filter, onlyFiles, out usnState);

                foreach (var entry in usnEntries)
                {
                    PrintUsnEntry(console, journal, entry);
                }
            }
        }

        private static void ReadHistoryUsnJournals(IConsole console, UsnJournal journal, ulong usnJournalId, string filter, bool? onlyFiles, CancellationToken token)
        {
            var usnReadState = new USN_JOURNAL_DATA_V0
            {
                NextUsn = 0,
                UsnJournalID = usnJournalId
            };

            var usnEntries = journal.ReadUsnEntries(usnReadState, 0xFFFFFFFF, filter, onlyFiles);

            foreach (var entry in usnEntries)
            {
                if (token.IsCancellationRequested) break;

                PrintUsnEntry(console, journal, entry);
            }
        }

        private static void SearchMasterFileTable(IConsole console, UsnJournal journal, string filter, bool? onlyFiles, CancellationToken token)
        {
            var usnEntries = journal.EnumerateUsnEntries(filter, onlyFiles);

            foreach (var entry in usnEntries)
            {
                if (token.IsCancellationRequested) break;

                PrintEntryPath(console, journal, entry);
            }
        }

        private static bool HasAdministratorPrivilege()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void PrintUsnJournalState(IConsole console, USN_JOURNAL_DATA_V0 usnData)
        {
            console.WriteLine($"{"Journal ID:",-20}{usnData.UsnJournalID:X}");
            console.WriteLine($"{"First USN:",-20}{usnData.FirstUsn:X}");
            console.WriteLine($"{"Next USN:",-20}{usnData.NextUsn:X}");
            console.WriteLine($"{"Lowest Valid USN:",-20}{usnData.LowestValidUsn:X}");
            console.WriteLine($"{"Max USN:",-20}{usnData.MaxUsn:X}");
            console.WriteLine($"{"Max Size:",-20}{usnData.MaximumSize:X}");
            console.WriteLine($"{"Allocation Delta:",-20}{usnData.AllocationDelta:X}");
        }

        public static void PrintEntryPath(IConsole console, UsnJournal usnJournal, UsnEntry usnEntry)
        {
            console.WriteLine();
            console.WriteLine($"{"Name:",-20}{usnEntry.Name}");
            console.WriteLine($"{"IsFolder:",-20}{usnEntry.IsFolder}");
            if (usnJournal.TryGetPathFromFileId(usnEntry.ParentFileReferenceNumber, out var path))
            {
                path = $"{usnJournal.VolumeName.TrimEnd('\\')}{path}";
                console.WriteLine($"{"Parent:",-20}{path}");
            }
        }

        public static void PrintUsnEntry(IConsole console, UsnJournal usnJournal, UsnEntry usnEntry)
        {
            console.WriteLine();
            console.WriteLine($"{"USN:",-20}{usnEntry.USN:X}");
            console.WriteLine(usnEntry.IsFolder
                ? $"{"Directory:",-20}{usnEntry.Name}"
                : $"{"File:",-20}{usnEntry.Name}");
            if (usnJournal.TryGetPathFromFileId(usnEntry.ParentFileReferenceNumber, out var path))
            {
                path = $"{usnJournal.VolumeName.TrimEnd('\\')}{path}";
                console.WriteLine($"{"Parent:",-20}{path}");
            }

            if (usnEntry.TimeStamp > 0)
                console.WriteLine($"{"Timestamp:",-20}{DateTime.FromFileTimeUtc(usnEntry.TimeStamp).ToLocalTime()}");

            console.WriteLine($"{"File ID:",-20}{usnEntry.FileReferenceNumber:X}");
            console.WriteLine($"{"Parent File ID:",-20}{usnEntry.ParentFileReferenceNumber:X}");

            var reason = ((UsnReason)usnEntry.Reason).ToString().Replace(',', '|');
            console.WriteLine($"{"Reason:",-20}{reason}");

            var sourceInfo = ((UsnSource)usnEntry.SourceInfo).ToString().Replace(',', '|');
            console.WriteLine($"{"Source Info:",-20}{sourceInfo}");
        }
    }
}
