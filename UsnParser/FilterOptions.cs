using System;

namespace UsnParser
{
    public class FilterOptions
    {
        public string? Keyword { get; }

        public bool FileOnly { get; }

        public bool DirectoryOnly { get; }

        public bool IgnoreCase { get; }

        public static FilterOptions Default { get; } = new FilterOptions();

        private FilterOptions() { }

        public FilterOptions(string? keyword, bool fileOnly, bool directoryOnly, bool ignoreCase)
        {
            Keyword = keyword;
            FileOnly = fileOnly;
            DirectoryOnly = directoryOnly;
            IgnoreCase = ignoreCase;

            if (FileOnly && DirectoryOnly)
                throw new InvalidOperationException($"{nameof(FileOnly)} and {nameof(DirectoryOnly)} can't both be set to true!");
        }
    }
}
