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
                           .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

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

        public CancellationToken Token { get; private set; }

        public FilterOption FilterOption
        {
            get
            {
                if (FileOnly) return FilterOption.OnlyFiles;
                if (DirectoryOnly) return FilterOption.OnlyDirectories;
                return FilterOption.All;
            }
        }

        public UsnJournal Journal { get; private set; }

        public USN_JOURNAL_DATA_V0 UsnState { get; private set; }

        protected abstract int OnExecute(CommandLineApplication app);

        protected int ExecuteCommand(Func<int> op)
        {
            var console = PhysicalConsole.Singleton;

            try
            {
                var cts = new CancellationTokenSource();
                Token = cts.Token;

                console.CancelKeyPress += (o, e) =>
                {
                    console.WriteLine("Keyboard interrupt, exiting...");
                    cts.Cancel();
                };

                if (!OperatingSystem.IsWindows())
                {
                    console.PrintError($"This tool only support Windows, since it used NTFS specific features.");
                    return -1;
                }

                var driveInfo = new DriveInfo(Volume);
                using (Journal = new UsnJournal(driveInfo))
                {
                    UsnState = Journal.GetUsnJournalState();
#if DEBUG
                    ConsoleUtils.PrintUsnJournalState(console, UsnState);
#endif

                    op();

                    return 0;
                }
            }
            catch (Exception ex)
            {
                console.PrintError(ex.Message);

                if (ex is Win32Exception win32Ex && win32Ex.NativeErrorCode == (int)Win32Errors.ERROR_ACCESS_DENIED && !HasAdministratorPrivilege())
                {
                    console.PrintError($"You need system administrator privileges to access the USN journal of {Volume.ToUpper()}.");
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
        public string Keyword { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            return ExecuteCommand(() =>
            {
                MonitorRealTimeUsnJournal(PhysicalConsole.Singleton, Journal, UsnState, Keyword, FilterOption, Token);
                return 0;
            });
        }

        private static void MonitorRealTimeUsnJournal(IConsole console, UsnJournal journal, USN_JOURNAL_DATA_V0 usnState, string keyword, FilterOption filterOption, CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested) return;

                var usnEntries = journal.GetUsnJournalEntries(usnState, Win32Api.USN_REASON_MASK, keyword, filterOption, out usnState);

                foreach (var entry in usnEntries)
                {
                    ConsoleUtils.PrintUsnEntry(console, journal, entry);
                }
            }
        }
    }

    [Command("search", Description = "Search the Master File Table")]
    internal class SearchCommand : SubCommandBase
    {
        [Argument(1, Description = "Search keyword, wildcards are permitted <Required>")]
        [Required]
        public string Keyword { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            return ExecuteCommand(() =>
            {
                SearchMasterFileTable(PhysicalConsole.Singleton, Journal, Keyword, FilterOption, Token);
                return 0;
            });
        }

        private static void SearchMasterFileTable(IConsole console, UsnJournal journal, string keyword, FilterOption filterOption, CancellationToken token)
        {
            var usnEntries = journal.EnumerateUsnEntries(keyword, filterOption);

            foreach (var entry in usnEntries)
            {
                if (token.IsCancellationRequested) break;

                ConsoleUtils.PrintEntryPath(console, journal, entry);
            }
        }
    }

    [Command("read", Description = "Read history USN journal entries")]
    internal class ReadCommand : SubCommandBase
    {
        [Option("-f|--filter", Description = "Filter the result with keyword, wildcards are permitted")]
        public string Keyword { get; set; }

        protected override int OnExecute(CommandLineApplication app)
        {
            return ExecuteCommand(() =>
            {
                ReadHistoryUsnJournals(PhysicalConsole.Singleton, Journal, UsnState.UsnJournalID, Keyword, FilterOption, Token);
                return 0;
            });
        }

        private static void ReadHistoryUsnJournals(IConsole console, UsnJournal journal, ulong usnJournalId, string keyword, FilterOption filterOption, CancellationToken token)
        {
            var usnReadState = new USN_JOURNAL_DATA_V0
            {
                NextUsn = 0,
                UsnJournalID = usnJournalId
            };

            var usnEntries = journal.ReadUsnEntries(usnReadState, Win32Api.USN_REASON_MASK, keyword, filterOption);

            foreach (var entry in usnEntries)
            {
                if (token.IsCancellationRequested) break;

                ConsoleUtils.PrintUsnEntry(console, journal, entry);
            }
        }
    }
}
