namespace UsnParser
{
    public class ChangeJournalEnumerationOptions : BaseEnumerationOptions
    {
        public ChangeJournalEnumerationOptions()
        {
        }

        /// <summary>
        /// Singleton instance of <see cref="ChangeJournalEnumerationOptions"/> with default values.
        /// </summary>
        public static ChangeJournalEnumerationOptions Default { get; } = new ChangeJournalEnumerationOptions();

        public bool ReturnOnlyOnClose { get; set; }

        public ulong BytesToWaitFor { get; set; }

        public ulong Timeout { get; set; }

        public long StartUsn { get; set; }
    }
}
