using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using McMaster.Extensions.CommandLineUtils;
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

        private static string GetVersion()
                => Assembly.GetExecutingAssembly()
                           .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";

#pragma warning disable CA1822 // Mark members as static
        private int OnExecute(CommandLineApplication app)
#pragma warning restore CA1822 // Mark members as static
        {
            // This shows help even if the --help option isn't specified
            app.ShowHelp();
            return 1;
        }
    }

    internal abstract class SubCommandBase
    {
        [Argument(0, Description = "Volume pathname, e.g. C: <Required>")]
        [Required]
        public string Volume { get; set; }

        [Option("-fo|--FileOnly", Description = "Only show the file entries")]
        public bool FileOnly { get; set; }

        [Option("-do|--DirOnly", Description = "Only show the directory entries")]
        public bool DirectoryOnly { get; set; }

        protected CancellationToken _cancellationToken;

        protected readonly IConsole _console = PhysicalConsole.Singleton;

        public FilterOption FilterOption
        {
            get
            {
                if (FileOnly) return FilterOption.OnlyFiles;
                if (DirectoryOnly) return FilterOption.OnlyDirectories;
                return FilterOption.All;
            }
        }

        protected UsnJournal _usnJournal;
        protected USN_JOURNAL_DATA_V0 _usnJournalData;

        protected abstract int OnExecute(CommandLineApplication app);

        protected int ExecuteCommand(Func<int> op)
        {
            try
            {
                var cts = new CancellationTokenSource();
                _cancellationToken = cts.Token;

                _console.CancelKeyPress += (o, e) =>
                {
                    _console.WriteLine("Keyboard interrupt, exiting...");
                    cts.Cancel();
                };

                if (!OperatingSystem.IsWindows())
                {
                    _console.PrintError($"This tool only support Windows, since it used NTFS specific features.");
                    return -1;
                }

                var driveInfo = new DriveInfo(Volume);
                using (_usnJournal = new UsnJournal(driveInfo))
                {
                    _usnJournalData = _usnJournal.GetUsnJournalState();
#if DEBUG
                    _console.PrintUsnJournalState(_usnJournalData);
#endif

                    return op();
                }
            }
            catch (Exception ex)
            {
                _console.PrintError(ex.Message);

                if (ex is Win32Exception win32Ex && win32Ex.NativeErrorCode == (int)Win32Error.ERROR_ACCESS_DENIED && !HasAdministratorPrivilege())
                {
                    _console.PrintError($"You need system administrator privileges to access the USN journal of {Volume.ToUpper()}.");
                }

                return -1;
            }
        }

        private static bool HasAdministratorPrivilege()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    [Command("monitor", Description = "Monitor real-time USN journal changes")]
    internal class MonitorCommand : SubCommandBase
    {
        [Option("-f|--filter", Description = "Filter the result with keyword, wildcards are permitted")]
        public string? Keyword { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            return ExecuteCommand(() =>
            {
                var usnEntries = _usnJournal.GetUsnJournalEntries(_usnJournalData.UsnJournalID, _usnJournalData.NextUsn, Keyword, FilterOption);

                foreach (var entry in usnEntries)
                {
                    if (_cancellationToken.IsCancellationRequested) return -1;
                    _console.PrintUsnEntry(_usnJournal, entry);
                }
                return 0;
            });
        }
    }

    [Command("search", Description = "Search the Master File Table")]
    internal class SearchCommand : SubCommandBase
    {
        [Argument(1, Description = "Search keyword, wildcards are permitted")]
        public string? Keyword { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            return ExecuteCommand(() =>
            {
                // Search through NTFS Master File Table
                var usnEntries = _usnJournal.EnumerateUsnEntries(Keyword, FilterOption, _usnJournalData.NextUsn);

                foreach (var entry in usnEntries)
                {
                    if (_cancellationToken.IsCancellationRequested) return -1;

                    _console.PrintEntryPath(_usnJournal, entry);
                }
                return 0;
            });
        }
    }

    [Command("read", Description = "Read history USN journal entries")]
    internal class ReadCommand : SubCommandBase
    {
        [Option("-f|--filter", Description = "Filter the result with keyword, wildcards are permitted")]
        public string? Keyword { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            return ExecuteCommand(() =>
            {
                var usnEntries = _usnJournal.ReadUsnEntries(_usnJournalData.UsnJournalID, Keyword, FilterOption);

                foreach (var entry in usnEntries)
                {
                    if (_cancellationToken.IsCancellationRequested) return -1;

                    _console.PrintUsnEntry(_usnJournal, entry);
                }
                return 0;
            });
        }
    }
}
