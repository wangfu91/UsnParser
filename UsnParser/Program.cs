using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

namespace UsnParser
{
    class Program
    {
        private static bool _cancelled;

        static void Main(string[] args)
        {
            RequireAdministrator();

            Console.CancelKeyPress += Console_CancelKeyPress;

            var driveLetter = Console.ReadLine();

            var driveInfo = new DriveInfo(driveLetter);

            var journal = new NtfsUsnJournal(driveInfo);

            var journalState = new USN_JOURNAL_DATA_V0();
            var error = journal.GetUsnJournalState(ref journalState);

            if (error == 0)
            {
                FormatUsnJournalState(journalState);
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine($"Error: {error}");
            }

            try
            {
                const uint reasonMask =
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


                //while (true)
                //{
                //    if (_cancelled) break;

                //    var rtnCode = journal.GetUsnJournalEntries(journalState, reasonMask, out var usnEntries, out journalState);

                //    if (rtnCode == (int)UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
                //    {
                //        foreach (var entry in usnEntries)
                //        {
                //            FormatUsnEntry(journal, entry);
                //        }
                //    }
                //    else
                //    {
                //        Console.ForegroundColor = ConsoleColor.Red;
                //        Console.WriteLine($"GetUsnJournalEntries error: {rtnCode}, drive: {driveLetter}");
                //        Console.ResetColor();
                //        break;
                //    }
                //}


                var usnReadState = new USN_JOURNAL_DATA_V0
                {
                    NextUsn = 0x0000000005272d88,
                    UsnJournalID = journalState.UsnJournalID
                };

                var usnEntries= new List<UsnEntry>();
                var retCode= journal.GetUsnJournalEntries(usnReadState, reasonMask, out usnEntries, out var newUsnState);
                if (retCode == 0)
                {
                    foreach (var entry in usnEntries)
                    {
                        FormatUsnEntry(journal, entry);
                    }
                }
                else
                {
                    Console.WriteLine($"GetUsnJournalEntries Error: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            Console.Read();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                Console.WriteLine("Cancelling");
                _cancelled = true;
                e.Cancel = true;
            }
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

        public static void FormatUsnEntry(NtfsUsnJournal usnJournal, UsnEntry usnEntry)
        {
            Console.WriteLine(usnEntry.IsFolder ? "  Directory: {0}" : "  File: {0}", usnEntry.Name);

            var lastError = usnJournal.GetPathFromFileReference(usnEntry.ParentFileReferenceNumber, out var path);
            if (lastError == (int)UsnJournalReturnCode.USN_JOURNAL_SUCCESS && null != path)
            {
                path = $"{usnJournal.VolumeName.TrimEnd('\\')}{path}";
                Console.WriteLine($"  Path: {path}");
            }

            Console.WriteLine("  File Ref No:       {0}", usnEntry.FileReferenceNumber);
            Console.WriteLine("  Parent FRN         {0}", usnEntry.ParentFileReferenceNumber);
            Console.WriteLine("  Length:            {0}", usnEntry.RecordLength);
            Console.WriteLine("  USN:               {0}", usnEntry.USN);
            PrintReasonData(usnEntry);
        }

        private static void PrintReasonData(UsnEntry usnEntry)
        {
            Console.Write("  Reason:            ");

            var value = usnEntry.Reason & UsnReasons.USN_REASON_OBJECT_ID_CHANGE;
            if (0 != value)
                Console.Write("| OBJECT ID CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_DATA_OVERWRITE;
            if (0 != value)
                Console.Write("| DATA OVERWRITE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_DATA_EXTEND;
            if (0 != value)
                Console.Write("| DATA EXTEND");

            value = usnEntry.Reason & UsnReasons.USN_REASON_DATA_TRUNCATION;
            if (0 != value)
                Console.Write("| DATA TRUNCATION");

            value = usnEntry.Reason & UsnReasons.USN_REASON_NAMED_DATA_OVERWRITE;
            if (0 != value)
                Console.Write("| NAMED DATA OVERWRITE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_NAMED_DATA_EXTEND;
            if (0 != value)
                Console.Write("| NAMED DATA EXTEND");

            value = usnEntry.Reason & UsnReasons.USN_REASON_NAMED_DATA_TRUNCATION;
            if (0 != value)
                Console.Write("| NAMED DATA TRUNCATION");

            value = usnEntry.Reason & UsnReasons.USN_REASON_FILE_CREATE;
            if (0 != value)
                Console.Write("| FILE CREATE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_FILE_DELETE;
            if (0 != value)
                Console.Write("| FILE DELETE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_EA_CHANGE;
            if (0 != value)
                Console.Write("| EA CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_SECURITY_CHANGE;
            if (0 != value)
                Console.Write("| SECURITY CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_RENAME_OLD_NAME;
            if (0 != value)
                Console.Write("| RENAME OLD NAME");

            value = usnEntry.Reason & UsnReasons.USN_REASON_RENAME_NEW_NAME;
            if (0 != value)
                Console.Write("| RENAME NEW NAME");

            value = usnEntry.Reason & UsnReasons.USN_REASON_INDEXABLE_CHANGE;
            if (0 != value)
                Console.Write("| INDEXABLE CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_BASIC_INFO_CHANGE;
            if (0 != value)
                Console.Write("| BASIC INFO CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_HARD_LINK_CHANGE;
            if (0 != value)
                Console.Write("| HARD LINK CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_COMPRESSION_CHANGE;
            if (0 != value)
                Console.Write("| COMPRESSION CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_ENCRYPTION_CHANGE;
            if (0 != value)
                Console.Write("| ENCRYPTION CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_REPARSE_POINT_CHANGE;
            if (0 != value)
                Console.Write("| REPARSE POINT CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_STREAM_CHANGE;
            if (0 != value)
                Console.Write("| STREAM CHANGE");

            value = usnEntry.Reason & UsnReasons.USN_REASON_CLOSE;
            if (0 != value)
                Console.Write("| CLOSE");

            Console.WriteLine("\n");
        }

    }
}
