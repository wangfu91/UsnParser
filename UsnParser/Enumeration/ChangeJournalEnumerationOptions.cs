namespace UsnParser.Enumeration
{
    public class ChangeJournalEnumerationOptions : BaseEnumerationOptions
    {
        public static ChangeJournalEnumerationOptions Default { get; } = new();

        public bool ReturnOnlyOnClose { get; set; }

        public ulong BytesToWaitFor { get; set; }

        public ulong Timeout { get; set; }

        public long StartUsn { get; set; }
    }
}
