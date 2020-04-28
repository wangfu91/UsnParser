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

        [Option("-f|--filter", Description = "Filter USN journal")]
        public string Filter { get; }

        [Option("-r|--read", Description = "Read real-time USN journal")]
        public bool Read { get; }

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

                PrintUsnJournalState(console, usnState);

                if (Read)
                {
                    ReadRealTimeUsnJournal(console, journal, usnState, Filter, cts.Token);
                }
                else
                {
                    ReadAllUsnJournals(console, journal, usnState.UsnJournalID, Filter, cts.Token);
                }
            }
            catch (Exception ex)
            {
                console.PrintError(ex.Message);
            }
        }

        private void ReadRealTimeUsnJournal(IConsole console, NtfsUsnJournal journal, USN_JOURNAL_DATA_V0 usnState, string filter, CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested) return;

                var rtnCode = journal.GetUsnJournalEntries(usnState, AllReasonMasks, out var usnEntries, out usnState);

                if (rtnCode == (int)UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
                {
                    foreach (var entry in usnEntries)
                    {
                        PrintUsnEntry(console, journal, entry);
                    }
                }
                else
                {
                    console.PrintError($"FSCTL_READ_USN_JOURNAL failed with error: {rtnCode}");
                    break;
                }
            }

        }

        private static void ReadAllUsnJournals(IConsole console, NtfsUsnJournal journal, ulong usnJournalId, string filter, CancellationToken token)
        {
            var usnReadState = new USN_JOURNAL_DATA_V0
            {
                NextUsn = 0,
                UsnJournalID = usnJournalId
            };

            var usnEntries = journal.EnumerateUsnJournalEntries(usnReadState, AllReasonMasks, filter);

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
            console.WriteLine($"Journal ID: {_usnCurrentJournalState.UsnJournalID:X}");
            console.WriteLine($"First USN: {_usnCurrentJournalState.FirstUsn:X}");
            console.WriteLine($"Next USN: {_usnCurrentJournalState.NextUsn:X}");
            console.WriteLine($"Lowest Valid USN: {_usnCurrentJournalState.LowestValidUsn:X}");
            console.WriteLine($"Max USN: {_usnCurrentJournalState.MaxUsn:X}");
            console.WriteLine($"Max Size: {_usnCurrentJournalState.MaximumSize:X}");
            console.WriteLine($"Allocation Delta: {_usnCurrentJournalState.AllocationDelta:X}");
        }

        public static void PrintUsnEntry(IConsole console, NtfsUsnJournal usnJournal, UsnEntry usnEntry)
        {
            console.WriteLine();
            console.WriteLine($"USN:               {usnEntry.USN:X}");
            console.WriteLine(usnEntry.IsFolder
                ? $"Directory:         {usnEntry.Name}"
                : $"File:              {usnEntry.Name}");
            if (usnJournal.TryGetPathFromFileId(usnEntry.ParentFileReferenceNumber, out var path))
            {
                path = $"{usnJournal.VolumeName.TrimEnd('\\')}{path}";
                console.WriteLine($"Parent:            {path}");
            }
            console.WriteLine($"Time Stamp:        {DateTime.FromFileTimeUtc(usnEntry.TimeStamp).ToLocalTime()}");
            console.WriteLine($"File Ref No:       {usnEntry.FileReferenceNumber:X}");
            console.WriteLine($"Parent FRN:        {usnEntry.ParentFileReferenceNumber:X}");
            PrintReasonMask(console, usnEntry);
        }

        private static void PrintReasonMask(IConsole console, UsnEntry usnEntry)
        {
            console.Write("Reason:            ");

            var builder = new StringBuilder();

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

            builder.Remove(0, 3);
            console.WriteLine(builder.ToString());
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
