using System;
using System.ComponentModel;
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
        Description = "A command utility for NTFS to search the MFT & monitoring the changes of USN Journal.")]
    [VersionOptionFromMember(MemberName = nameof(GetVersion))]
    [Subcommand(typeof(MonitorCommand), typeof(SearchCommand), typeof(ReadCommand))]
    [HelpOption("-h|--help")]
    internal class UsnParser
    {
        public static int Main(string[] args)
            => CommandLineApplication.Execute<UsnParser>(args);

        private static string GetVersion() =>
            typeof(UsnParser).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This method is required by the CommandLineUtils framework as an instance method for command execution, even though it does not access instance data. Marking it as static would break the expected signature.")]
        private int OnExecute(CommandLineApplication app)
        {
            // This shows help even if the --help option isn't specified
            app.ShowHelp();
            return 1;
        }

        [HelpOption("-h|--help", Inherited = true)]
        private abstract class SubCommandBase
        {
            [Argument(0, Description = "Volume name, e.g. C: <Required>")]
            [Required]
            public required string Volume { get; set; }

            [Option("-f|--filter", Description = "Filter the result with keyword, wildcards are permitted")]
            public string? Keyword { get; set; }

            [Option("-fo|--FileOnly", Description = "Only show the file entries")]
            public bool FileOnly { get; set; }

            [Option("-do|--DirectoryOnly", Description = "Only show the directory entries")]
            public bool DirectoryOnly { get; set; }

            [Option("--caseSensitive", Description = "Use case-sensitive matching, default is false")]
            public bool CaseSensitive { get; set; }

            protected CancellationToken _cancellationToken;

            protected readonly IConsole _console = PhysicalConsole.Singleton;

            protected FilterOptions _filterOptions = FilterOptions.Default;

            protected int OnExecute(CommandLineApplication app)
            {
                try
                {
                    var cts = new CancellationTokenSource();
                    _cancellationToken = cts.Token;

                    _console.CancelKeyPress += (o, e) =>
                    {
                        _console.WriteLine("Ctrl+C is pressed, exiting...");
                        cts.Cancel();
                    };

                    if (!OperatingSystem.IsWindows())
                    {
                        _console.PrintError($"This tool only support Windows, since it used NTFS specific features.");
                        return -1;
                    }

                    var driveInfo = new DriveInfo(Volume);
                    using var usnJournal = new UsnJournal(driveInfo);
#if DEBUG
                    _console.PrintUsnJournalData(usnJournal.JournalInfo);
#endif

                    _filterOptions = new FilterOptions(Keyword, FileOnly, DirectoryOnly, CaseSensitive);
                    return Run(usnJournal);
                }
                catch (Exception ex)
                {
                    _console.PrintError(ex.Message);

                    if (ex is Win32Exception win32Ex && win32Ex.NativeErrorCode == (int)Win32Error.ERROR_ACCESS_DENIED && !HasAdministratorPrivilege())
                    {
                        _console.PrintError($"You need system administrator privileges to access the USN journal of drive {Volume.ToUpper()}.");
                    }

                    return -1;
                }
            }

            protected abstract int Run(UsnJournal usnJournal);

            private static bool HasAdministratorPrivilege()
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        [Command("monitor", Description = "Monitor real-time USN journal changes")]
        private class MonitorCommand : SubCommandBase
        {
            protected override int Run(UsnJournal usnJournal)
            {
                var usnEntries = usnJournal.MonitorLiveUsn(usnJournal.JournalInfo.UsnJournalID, usnJournal.JournalInfo.NextUsn, _filterOptions);
                foreach (var entry in usnEntries)
                {
                    if (_cancellationToken.IsCancellationRequested) return -1;

                    _console.PrintUsnEntryFull(usnJournal, entry);
                }
                return 0;
            }
        }

        [Command("search", Description = "Search the Master File Table")]
        private class SearchCommand : SubCommandBase
        {
            protected override int Run(UsnJournal usnJournal)
            {
                var usnEntries = usnJournal.EnumerateMasterFileTable(usnJournal.JournalInfo.NextUsn, _filterOptions);
                foreach (var entry in usnEntries)
                {
                    if (_cancellationToken.IsCancellationRequested) return -1;

                    _console.PrintUsnEntryBasic(usnJournal, entry);
                }
                return 0;
            }
        }

        [Command("read", Description = "Read history USN journal entries")]
        private class ReadCommand : SubCommandBase
        {
            protected override int Run(UsnJournal usnJournal)
            {
                var usnEntries = usnJournal.EnumerateUsnEntries(usnJournal.JournalInfo.UsnJournalID, _filterOptions);
                foreach (var entry in usnEntries)
                {
                    if (_cancellationToken.IsCancellationRequested) return -1;

                    _console.PrintUsnEntryFull(usnJournal, entry);
                }
                return 0;
            }
        }
    }
}
