using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Threading;

namespace UsnParser.Enumeration
{
    public class ChangeJournalEnumerable : BaseEnumerable
    {
        private ChangeJournalEnumerator? _enumerator;
        private readonly ulong _changeJournalId;
        private readonly ChangeJournalEnumerationOptions _options;

        public ChangeJournalEnumerable(SafeFileHandle volumeRootHandle, ulong usnJournalId, ChangeJournalEnumerationOptions? options = null, FindPredicate? shouldIncludePredicate = null)
            : base(volumeRootHandle, shouldIncludePredicate)
        {
            _changeJournalId = usnJournalId;
            _options = options ?? ChangeJournalEnumerationOptions.Default;
        }

        public override IEnumerator<UsnEntry> GetEnumerator()
        {
            return Interlocked.Exchange(ref _enumerator, null) ?? new ChangeJournalEnumerator(_volumeRootHandle, _changeJournalId, _options, _shouldIncludePredicate);
        }
    }
}
