namespace UsnParser
{
    public abstract class BaseEnumerationOptions
    {
        public int BufferSize { get; set; } = 256 * 1024;

        public bool FileOnly { get; set; }

        public bool DirectoryOnly { get; set; }

        public string Filter { get; set; } = string.Empty;
    }
}
