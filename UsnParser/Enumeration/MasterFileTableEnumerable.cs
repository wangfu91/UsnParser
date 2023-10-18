using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Threading;

namespace UsnParser.Enumeration
{
    public class MasterFileTableEnumerable : BaseEnumerable
    {
        private MasterFileTableEnumerator? _enumerator;
        private readonly MasterFileTableEnumerationOptions _options;
        private readonly long _highUsn;

        public MasterFileTableEnumerable(SafeFileHandle volumeRootHandle, long highUsn, MasterFileTableEnumerationOptions? options = null, FindPredicate? shouldIncludePredicate = null)
            : base(volumeRootHandle, shouldIncludePredicate)
        {
            _highUsn = highUsn;
            _options = options ?? MasterFileTableEnumerationOptions.Default;
        }

        public override IEnumerator<UsnEntry> GetEnumerator()
        {
            return Interlocked.Exchange(ref _enumerator, null) ?? new MasterFileTableEnumerator(_volumeRootHandle, _highUsn, _options, _shouldIncludePredicate);
        }
    }
}
