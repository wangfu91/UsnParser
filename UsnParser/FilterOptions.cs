using System;

namespace UsnParser
{
    public class FilterOptions
    {
        public string? Keyword { get; }

        public bool FileOnly { get; }

        public bool DirectoryOnly { get; }

        public bool CaseSensitive { get; }

        public static FilterOptions Default { get; } = new FilterOptions();

        private FilterOptions() { }

        public FilterOptions(string? keyword, bool fileOnly, bool directoryOnly, bool caseSensitive)
        {
            Keyword = keyword;
            FileOnly = fileOnly;
            DirectoryOnly = directoryOnly;
            CaseSensitive = caseSensitive;

            if (FileOnly && DirectoryOnly)
                throw new InvalidOperationException($"{nameof(FileOnly)} and {nameof(DirectoryOnly)} can't both be set to true!");
        }
    }
}
