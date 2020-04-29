using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using McMaster.Extensions.CommandLineUtils;

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

                var journal = new NtfsUsnJournal(driveInfo);
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
                    ReadAllUsnJournals(console, journal, usnState.UsnJournalID, Filter, onlyFiles, cts.Token);
                }
            }
            catch (Exception ex)
            {
                console.PrintError(ex.Message);
            }
        }

        private void MonitorRealTimeUsnJournal(IConsole console, NtfsUsnJournal journal, USN_JOURNAL_DATA_V0 usnState, string filter, bool? onlyFiles, CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested) return;

                var usnEntries = journal.GetUsnJournalEntries(usnState, AllReasonMasks, filter, onlyFiles, out usnState);

                foreach (var entry in usnEntries)
                {
                    PrintUsnEntry(console, journal, entry);
                }
            }
        }

        private static void ReadAllUsnJournals(IConsole console, NtfsUsnJournal journal, ulong usnJournalId, string filter, bool? onlyFiles, CancellationToken token)
        {
            var usnReadState = new USN_JOURNAL_DATA_V0
            {
                NextUsn = 0,
                UsnJournalID = usnJournalId
            };

            var usnEntries = journal.ReadUsnEntries(usnReadState, AllReasonMasks, filter, onlyFiles);

            foreach (var entry in usnEntries)
            {
                if (token.IsCancellationRequested) break;

                PrintUsnEntry(console, journal, entry);
            }
        }

        private static void SearchMasterFileTable(IConsole console, NtfsUsnJournal journal, string filter, bool? onlyFiles, CancellationToken token)
        {
            var usnEntries = journal.EnumerateUsnEntries(filter, onlyFiles);

            foreach (var entry in usnEntries)
            {
                if (token.IsCancellationRequested) break;

                PrintUsnEntry(console, journal, entry);
            }
        }

        private static bool HasAdministratorPrivilege()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void PrintUsnJournalState(IConsole console, USN_JOURNAL_DATA_V0 _usnCurrentJournalState)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Journal ID:        {_usnCurrentJournalState.UsnJournalID:X}");
            builder.AppendLine($"First USN:         {_usnCurrentJournalState.FirstUsn:X}");
            builder.AppendLine($"Next USN:          {_usnCurrentJournalState.NextUsn:X}");
            builder.AppendLine($"Lowest Valid USN:  {_usnCurrentJournalState.LowestValidUsn:X}");
            builder.AppendLine($"Max USN:           {_usnCurrentJournalState.MaxUsn:X}");
            builder.AppendLine($"Max Size:          {_usnCurrentJournalState.MaximumSize:X}");
            builder.AppendLine($"Allocation Delta:  {_usnCurrentJournalState.AllocationDelta:X}");
            console.WriteLine(builder);
        }

        public static void PrintUsnEntry(IConsole console, NtfsUsnJournal usnJournal, UsnEntry usnEntry)
        {
            var builder = new StringBuilder();
            builder.AppendLine();
            builder.AppendLine($"USN:               {usnEntry.USN:X}");
            builder.AppendLine(usnEntry.IsFolder
                ? $"Directory:         {usnEntry.Name}"
                : $"File:              {usnEntry.Name}");
            if (usnJournal.TryGetPathFromFileId(usnEntry.ParentFileReferenceNumber, out var path))
            {
                path = $"{usnJournal.VolumeName.TrimEnd('\\')}{path}";
                builder.AppendLine($"Parent:            {path}");
            }

            if (usnEntry.TimeStamp > 0)
                builder.AppendLine($"Time Stamp:        {DateTime.FromFileTimeUtc(usnEntry.TimeStamp).ToLocalTime()}");

            builder.AppendLine($"File Ref No:       {usnEntry.FileReferenceNumber:X}");
            builder.AppendLine($"Parent FRN:        {usnEntry.ParentFileReferenceNumber:X}");

            if (usnEntry.Reason > 0)
                PrintReasonMask(builder, usnEntry);

            console.WriteLine(builder);
        }

        private static void PrintReasonMask(StringBuilder builder, UsnEntry usnEntry)
        {
            builder.Append("Reason:            ");

            var start = builder.Length;

            var value = usnEntry.Reason & UsnReasons.USN_REASON_OBJECT_ID_CHANGE;
            if (0 != value)
                builder.Append(" | OBJECT ID CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_DATA_OVERWRITE;
            if (0 != value)
                builder.Append(" | DATA OVERWRITE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_DATA_EXTEND;
            if (0 != value)
                builder.Append(" | DATA EXTEND");

            value = usnEntry.Reason & UsnReasons.USN_REASON_DATA_TRUNCATION;
            if (0 != value)
                builder.Append(" | DATA TRUNCATION");

            value = usnEntry.Reason & UsnReasons.USN_REASON_NAMED_DATA_OVERWRITE;
            if (0 != value)
                builder.Append(" | NAMED DATA OVERWRITE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_NAMED_DATA_EXTEND;
            if (0 != value)
                builder.Append(" | NAMED DATA EXTEND");

            value = usnEntry.Reason & UsnReasons.USN_REASON_NAMED_DATA_TRUNCATION;
            if (0 != value)
                builder.Append(" | NAMED DATA TRUNCATION");

            value = usnEntry.Reason & UsnReasons.USN_REASON_FILE_CREATE;
            if (0 != value)
                builder.Append(" | FILE CREATE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_FILE_DELETE;
            if (0 != value)
                builder.Append(" | FILE DELETE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_EA_CHANGE;
            if (0 != value)
                builder.Append(" | EA CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_SECURITY_CHANGE;
            if (0 != value)
                builder.Append(" | SECURITY CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_RENAME_OLD_NAME;
            if (0 != value)
                builder.Append(" | RENAME OLD NAME");

            value = usnEntry.Reason & UsnReasons.USN_REASON_RENAME_NEW_NAME;
            if (0 != value)
                builder.Append(" | RENAME NEW NAME");

            value = usnEntry.Reason & UsnReasons.USN_REASON_INDEXABLE_CHANGE;
            if (0 != value)
                builder.Append(" | INDEXABLE CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_BASIC_INFO_CHANGE;
            if (0 != value)
                builder.Append(" | BASIC INFO CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_HARD_LINK_CHANGE;
            if (0 != value)
                builder.Append(" | HARD LINK CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_COMPRESSION_CHANGE;
            if (0 != value)
                builder.Append(" | COMPRESSION CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_ENCRYPTION_CHANGE;
            if (0 != value)
                builder.Append(" | ENCRYPTION CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_REPARSE_POINT_CHANGE;
            if (0 != value)
                builder.Append(" | REPARSE POINT CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_STREAM_CHANGE;
            if (0 != value)
                builder.Append(" | STREAM CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_CLOSE;
            if (0 != value)
                builder.Append(" | CLOSE");

            if (builder.Length > start + 3)
                builder.Remove(start, 3);
        }

        private const uint AllReasonMasks =
            UsnReasons.USN_REASON_DATA_OVERWRITE |
            UsnReasons.USN_REASON_DATA_EXTEND |
            UsnReasons.USN_REASON_DATA_TRUNCATION |
            UsnReasons.USN_REASON_NAMED_DATA_OVERWRITE |
            UsnReasons.USN_REASON_NAMED_DATA_EXTEND |
            UsnReasons.USN_REASON_NAMED_DATA_TRUNCATION |
            UsnReasons.USN_REASON_FILE_CREATE |
            UsnReasons.USN_REASON_FILE_DELETE |
            UsnReasons.USN_REASON_EA_CHANGE |
            UsnReasons.USN_REASON_SECURITY_CHANGE |
            UsnReasons.USN_REASON_RENAME_OLD_NAME |
            UsnReasons.USN_REASON_RENAME_NEW_NAME |
            UsnReasons.USN_REASON_INDEXABLE_CHANGE |
            UsnReasons.USN_REASON_BASIC_INFO_CHANGE |
            UsnReasons.USN_REASON_HARD_LINK_CHANGE |
            UsnReasons.USN_REASON_COMPRESSION_CHANGE |
            UsnReasons.USN_REASON_ENCRYPTION_CHANGE |
            UsnReasons.USN_REASON_OBJECT_ID_CHANGE |
            UsnReasons.USN_REASON_REPARSE_POINT_CHANGE |
            UsnReasons.USN_REASON_STREAM_CHANGE |
            UsnReasons.USN_REASON_CLOSE;
    }
}
