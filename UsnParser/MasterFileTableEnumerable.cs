using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace UsnParser
{
    public class MasterFileTableEnumerable : IEnumerable<UsnEntry>
    {
        private MasterFileTableEnumerator? _enumerator;
        private readonly SafeFileHandle _volumeRootHandle;
        private readonly MasterFileTableEnumerationOptions _options;
        private readonly long _highUsn;

        public MasterFileTableEnumerable(SafeFileHandle volumeRootHandle, long highUsn, MasterFileTableEnumerationOptions? options = null)
        {
            _volumeRootHandle = volumeRootHandle;
            _highUsn = highUsn;
            _options = options ?? MasterFileTableEnumerationOptions.Default;
        }

        public IEnumerator<UsnEntry> GetEnumerator()
        {
            return Interlocked.Exchange(ref _enumerator, null) ?? new MasterFileTableEnumerator(_volumeRootHandle, _highUsn, _options);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
