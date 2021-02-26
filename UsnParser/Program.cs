using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using McMaster.Extensions.CommandLineUtils;
using UsnParser.Extensions;
using UsnParser.Native;

namespace UsnParser
{
    [Command(
        Name = "NtfsJournal",
        FullName = "NTFS USN Journal parser",
        Description = "Parse NTFS USN Journal.")]
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

            if(!OperatingSystem.IsWindows())
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

#if DEBUG
                PrintUsnJournalState(console, usnState);
#endif

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
            var builder = new StringBuilder();
            builder.AppendLine($"Journal ID:        {usnData.UsnJournalID:X}");
            builder.AppendLine($"First USN:         {usnData.FirstUsn:X}");
            builder.AppendLine($"Next USN:          {usnData.NextUsn:X}");
            builder.AppendLine($"Lowest Valid USN:  {usnData.LowestValidUsn:X}");
            builder.AppendLine($"Max USN:           {usnData.MaxUsn:X}");
            builder.AppendLine($"Max Size:          {usnData.MaximumSize:X}");
            builder.AppendLine($"Allocation Delta:  {usnData.AllocationDelta:X}");
            console.WriteLine(builder);
        }

        public static void PrintEntryPath(IConsole console, UsnJournal usnJournal, UsnEntry usnEntry)
        {
            var builder = new StringBuilder();
            builder.AppendLine();
            builder.AppendLine($"Name:              {usnEntry.Name}");
            builder.AppendLine($"IsFolder:          {usnEntry.IsFolder}");
            if (usnJournal.TryGetPathFromFileId(usnEntry.ParentFileReferenceNumber, out var path))
            {
                path = $"{usnJournal.VolumeName.TrimEnd('\\')}{path}";
                builder.AppendLine($"Folder:            {path}");
            }
            console.Write(builder);
        }

        public static void PrintUsnEntry(IConsole console, UsnJournal usnJournal, UsnEntry usnEntry)
        {
            var builder = new StringBuilder();
            builder.AppendLine();
            builder.AppendLine($"USN:\t\t{usnEntry.USN:X}");
            builder.AppendLine(usnEntry.IsFolder
                ? $"Directory:\t\t{usnEntry.Name}"
                : $"File:\t\t{usnEntry.Name}");
            if (usnJournal.TryGetPathFromFileId(usnEntry.ParentFileReferenceNumber, out var path))
            {
                path = $"{usnJournal.VolumeName.TrimEnd('\\')}{path}";
                builder.AppendLine($"Parent:\t\t{path}");
            }

            if (usnEntry.TimeStamp > 0)
                builder.AppendLine($"Time Stamp:\t\t{DateTime.FromFileTimeUtc(usnEntry.TimeStamp).ToLocalTime()}");

            builder.AppendLine($"File Ref No:\t\t{usnEntry.FileReferenceNumber:X}");
            builder.AppendLine($"Parent FRN:\t\t{usnEntry.ParentFileReferenceNumber:X}");

            if (usnEntry.Reason > 0)
                builder.AppendLine($"Reason:\t\t{(UsnReason)usnEntry.Reason}");

            console.WriteLine(builder);
        }
    }
}
