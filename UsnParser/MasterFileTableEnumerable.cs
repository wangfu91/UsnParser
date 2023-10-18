using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UsnParser.Native;

namespace UsnParser
{
    public class MasterFileTableEnumerable : IEnumerable<UsnEntry>
    {
        private MasterFileTableEnumerator? _enumerator;
        private readonly SafeFileHandle _volumeRootHandle;
        private readonly USN_JOURNAL_DATA_V0 _changeJournal;
        private readonly MasterFileTableEnumerationOptions _options;

        public MasterFileTableEnumerable(SafeFileHandle volumeRootHandle, USN_JOURNAL_DATA_V0 changeJournal, MasterFileTableEnumerationOptions? options = null)
        {
            _volumeRootHandle = volumeRootHandle;
            _changeJournal = changeJournal;
            _options = options ?? MasterFileTableEnumerationOptions.Default;
        }

        public IEnumerator<UsnEntry> GetEnumerator()
        {
            return Interlocked.Exchange(ref _enumerator, null) ?? new MasterFileTableEnumerator(_volumeRootHandle, _changeJournal, _options);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
