using System;
using System.CommandLine;
using System.ComponentModel;
using System.IO;
using System.Security.Principal;
using System.Threading;
using UsnParser.Extensions;
using UsnParser.Native;

namespace UsnParser;

internal static class Program
{
    public static int Main(string[] args)
    {
        var rootCmd = new RootCommand("A command utility for NTFS to search the MFT & monitoring the changes of USN Journal.");
        rootCmd.Subcommands.Add(BuildCommand("monitor", "Monitor real-time USN journal changes",
            static (journal, filterOptions, ct) =>
            {
                foreach (var entry in journal.MonitorLiveUsn(journal.JournalInfo.UsnJournalID, journal.JournalInfo.NextUsn, filterOptions))
                {
                    if (ct.IsCancellationRequested) return -1;
                    ConsoleExtension.PrintUsnEntryFull(journal, entry);
                }
                return 0;
            }));
        rootCmd.Subcommands.Add(BuildCommand("search", "Search the Master File Table",
            static (journal, filterOptions, ct) =>
            {
                foreach (var entry in journal.EnumerateMasterFileTable(journal.JournalInfo.NextUsn, filterOptions))
                {
                    if (ct.IsCancellationRequested) return -1;
                    ConsoleExtension.PrintUsnEntryBasic(journal, entry);
                }
                return 0;
            }));
        rootCmd.Subcommands.Add(BuildCommand("read", "Read history USN journal entries",
            static (journal, filterOptions, ct) =>
            {
                foreach (var entry in journal.EnumerateUsnEntries(journal.JournalInfo.UsnJournalID, filterOptions))
                {
                    if (ct.IsCancellationRequested) return -1;
                    ConsoleExtension.PrintUsnEntryFull(journal, entry);
                }
                return 0;
            }));
        return rootCmd.Parse(args).Invoke();
    }

    private static Command BuildCommand(string name, string description, Func<UsnJournal, FilterOptions, CancellationToken, int> run)
    {
        var volumeArg = new Argument<string>("volume") { Description = "Volume name, e.g. C: <Required>" };

        var filterOpt = new Option<string?>("--filter") { Description = "Filter the result with keyword, wildcards are permitted" };
        filterOpt.Aliases.Add("-f");

        var fileOnlyOpt = new Option<bool>("--FileOnly") { Description = "Only show the file entries" };
        fileOnlyOpt.Aliases.Add("-fo");

        var dirOnlyOpt = new Option<bool>("--DirectoryOnly") { Description = "Only show the directory entries" };
        dirOnlyOpt.Aliases.Add("-do");

        var caseSensitiveOpt = new Option<bool>("--caseSensitive") { Description = "Use case-sensitive matching, default is false" };

        var cmd = new Command(name, description) { volumeArg, filterOpt, fileOnlyOpt, dirOnlyOpt, caseSensitiveOpt };
        cmd.SetAction(r => ExecuteCore(
            r.GetValue(volumeArg)!,
            new FilterOptions(r.GetValue(filterOpt), r.GetValue(fileOnlyOpt), r.GetValue(dirOnlyOpt), r.GetValue(caseSensitiveOpt)),
            run));
        return cmd;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Top-level CLI handler — all unexpected exceptions are surfaced to the user as error messages.")]
    private static int ExecuteCore(string volume, FilterOptions filterOptions, Func<UsnJournal, FilterOptions, CancellationToken, int> run)
    {
        try
        {
            using var cts = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (_, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("Ctrl+C is pressed, exiting...");
                cts.Cancel();
            };
            Console.CancelKeyPress += cancelHandler;
            try
            {
                var driveInfo = new DriveInfo(volume);
                using var usnJournal = new UsnJournal(driveInfo);
#if DEBUG
                ConsoleExtension.PrintUsnJournalData(usnJournal.JournalInfo);
#endif
                return run(usnJournal, filterOptions, cts.Token);
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }
        catch (Exception ex)
        {
            ConsoleExtension.PrintError(ex.Message);
            if (ex is Win32Exception win32Ex
                && win32Ex.NativeErrorCode == (int)Win32Error.ERROR_ACCESS_DENIED
                && !HasAdministratorPrivilege())
            {
                ConsoleExtension.PrintError($"You need system administrator privileges to access the USN journal of drive {volume.ToUpper()}.");
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

