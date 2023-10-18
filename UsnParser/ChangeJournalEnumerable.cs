using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace UsnParser
{
    public class ChangeJournalEnumerable : IEnumerable<UsnEntry>
    {
        private ChangeJournalEnumerator? _enumerator;
        private readonly SafeFileHandle _volumeRootHandle;
        private readonly ulong _changeJournalId;
        private readonly ChangeJournalEnumerationOptions _options;

        public ChangeJournalEnumerable(SafeFileHandle volumeRootHandle, ulong usnJournalId, ChangeJournalEnumerationOptions? options = null)
        {
            _volumeRootHandle = volumeRootHandle;
            _changeJournalId = usnJournalId;
            _options = options ?? ChangeJournalEnumerationOptions.Default;
        }

        public IEnumerator<UsnEntry> GetEnumerator()
        {
            return Interlocked.Exchange(ref _enumerator, null) ?? new ChangeJournalEnumerator(_volumeRootHandle, _changeJournalId, _options);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
