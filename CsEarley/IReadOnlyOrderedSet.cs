using System.Collections;
using System.Collections.Generic;

namespace CsEarley
{
    public interface IReadOnlyOrderedSet<T> : IReadOnlyCollection<T>
    {
        IEnumerable<T> MutableIterator();
        T Get(T elem);
        T this[T index] { get; }
    }
}