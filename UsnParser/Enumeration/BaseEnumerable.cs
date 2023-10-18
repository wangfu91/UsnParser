using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Collections.Generic;

namespace UsnParser.Enumeration
{
    public abstract class BaseEnumerable : IEnumerable<UsnEntry>
    {
        protected readonly SafeFileHandle _volumeRootHandle;
        protected readonly FindPredicate? _shouldIncludePredicate;

        public BaseEnumerable(SafeFileHandle volumeRootHandle, FindPredicate? shouldIncludePredicate = null)
        {
            _volumeRootHandle = volumeRootHandle;
            _shouldIncludePredicate = shouldIncludePredicate;
        }

        public abstract IEnumerator<UsnEntry> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public delegate bool FindPredicate(UsnEntry entry);
    }
}
