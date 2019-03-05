using System;
using System.IO;
using System.Security.Principal;

namespace UsnParser
{
    class Program
    {
        static void Main(string[] args)
        {
            RequireAdministrator();

            var driveLetter = Console.ReadLine();

            var driveInfo = new DriveInfo(driveLetter);

            var journal = new NtfsUsnJournal(driveInfo);

            var journalState = new USN_JOURNAL_DATA_V0();
            var error = journal.GetUsnJournalState(ref journalState);

            if (error == 0)
            {
                FormatUsnJournalState(journalState);
            }
            else
            {
                Console.WriteLine($"Error: {error}");
            }

            Console.Read();
        }

        private static void RequireAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    throw new InvalidOperationException("Application must be run as administrator");
                }
            }
        }

        private static void FormatUsnJournalState(USN_JOURNAL_DATA_V0 _usnCurrentJournalState)
        {
            Console.WriteLine($" Journal ID: {_usnCurrentJournalState.UsnJournalID:X}");
            Console.WriteLine($" First USN: {_usnCurrentJournalState.FirstUsn:X}");
            Console.WriteLine($" Next USN: {_usnCurrentJournalState.NextUsn:X}");
            Console.WriteLine($" Lowest Valid USN: {_usnCurrentJournalState.LowestValidUsn:X}");
            Console.WriteLine($" Max USN: {_usnCurrentJournalState.MaxUsn:X}");
            Console.WriteLine($" Max Size: {_usnCurrentJournalState.MaximumSize:X}");
            Console.WriteLine($" Allocation Delta: {_usnCurrentJournalState.AllocationDelta:X}");
        }
    }
}
