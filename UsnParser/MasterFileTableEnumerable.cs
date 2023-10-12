using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UsnParser.Native;

namespace UsnParser
{
    public class MasterFileTableEnumerable : IEnumerable<UsnEntry>
    {
        private MasterFileTableEnumerator _enumerator;
        private readonly SafeFileHandle _volumeRootHandle;
        private readonly USN_JOURNAL_DATA_V0 _changeJournal;

        public MasterFileTableEnumerable(SafeFileHandle volumeRootHandle, USN_JOURNAL_DATA_V0 changeJournal)
        {
            _volumeRootHandle = volumeRootHandle;
            _changeJournal = changeJournal;
        }

        public IEnumerator<UsnEntry> GetEnumerator()
        {
            return Interlocked.Exchange(ref _enumerator, null) ?? new MasterFileTableEnumerator(_volumeRootHandle, _changeJournal);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
