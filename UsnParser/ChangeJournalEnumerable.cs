using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UsnParser.Native;

namespace UsnParser
{
    public class ChangeJournalEnumerable : IEnumerable<UsnEntry>
    {
        private ChangeJournalEnumerator? _enumerator;
        private readonly SafeFileHandle _volumeRootHandle;
        private readonly USN_JOURNAL_DATA_V0 _changeJournal;
        private readonly ChangeJournalEnumerationOptions _options;

        public ChangeJournalEnumerable(SafeFileHandle volumeRootHandle, USN_JOURNAL_DATA_V0 changeJournal, ChangeJournalEnumerationOptions? options = null)
        {
            _volumeRootHandle = volumeRootHandle;
            _changeJournal = changeJournal;
            _options = options ?? ChangeJournalEnumerationOptions.Default;
        }

        public IEnumerator<UsnEntry> GetEnumerator()
        {
            return Interlocked.Exchange(ref _enumerator, null) ?? new ChangeJournalEnumerator(_volumeRootHandle, _changeJournal, _options);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
