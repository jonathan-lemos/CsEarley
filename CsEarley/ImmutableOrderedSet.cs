using System.Collections;
using System.Collections.Generic;

namespace CsEarley
{
    public class ImmutableOrderedSet<T> : IReadOnlyCollection<T>
    {
        private OrderedSet<T> _set;
        
        public ImmutableOrderedSet(OrderedSet<T> set)
        {
            _set = set;
        }

        public bool Contains(T item) => _set.Contains(item);

        public int Count => _set.Count;

        public IEnumerator<T> GetEnumerator() => _set.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static implicit operator ImmutableOrderedSet<T>(OrderedSet<T> set) => new ImmutableOrderedSet<T>(set);
    }
}