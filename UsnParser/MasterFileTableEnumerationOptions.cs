namespace UsnParser
{
    public class MasterFileTableEnumerationOptions: BaseEnumerationOptions
    {
        public MasterFileTableEnumerationOptions()
        {
        }

        /// <summary>
        /// Singleton instance of <see cref="MasterFileTableEnumerationOptions"/> with default values.
        /// </summary>
        public static MasterFileTableEnumerationOptions Default { get; } = new MasterFileTableEnumerationOptions();
    }
}
