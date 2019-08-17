using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CsEarley
{
    /// <summary>
    /// A hashset that preserves insertion order.
    /// </summary>
    /// It features amortized O(1) insertions, deletions, and lookups but uses twice the memory as a normal hashset.
    /// <typeparam name="T">The type of the items within this collection.</typeparam>
    public class OrderedSet<T> : ISet<T>
    {
        private readonly Dictionary<T, LinkedListNode<T>> _dict;
        private readonly LinkedList<T> _list;

        public OrderedSet() : this(new List<T>())
        {
        }

        public OrderedSet(IEqualityComparer<T> comparer) : this(new List<T>(), comparer)
        {
        }

        public OrderedSet(IEnumerable<T> elements, IEqualityComparer<T> comparer = null)
        {
            _dict = new Dictionary<T, LinkedListNode<T>>(comparer ?? EqualityComparer<T>.Default);
            _list = new LinkedList<T>();
            foreach (var elem in elements)
            {
                Add(elem);
            }
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        /// <summary>
        /// Adds an item to the <see cref="OrderedSet"/>.
        /// </summary>
        /// This operation is amortized O(1), worst-case O(n).
        /// <param name="item">The item to add.</param>
        public bool Add(T item)
        {
            if (Contains(item))
            {
                return false;
            }

            _list.AddLast(item);
            _dict.Add(item, _list.Last);
            return true;
        }

        public void Clear()
        {
            _dict.Clear();
            _list.Clear();
        }

        /// <summary>
        /// Checks if an item is present within the set.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            return _dict.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _dict.Keys.CopyTo(array, arrayIndex);
        }

        public int Count => _dict.Count;

        public void ExceptWith(IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                if (Contains(item))
                {
                    Remove(item);
                }
            }
        }

        public T First => _list.First.Value;

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void IntersectWith(IEnumerable<T> enumerable)
        {
            var list = new HashSet<T>(enumerable);
            foreach (var item in list)
            {
                if (!Contains(item))
                {
                    Remove(item);
                }
            }
            // New collection allows iteration to continue despite removing elements.
            foreach (var item in new List<T>(this))
            {
                if (!list.Contains(item))
                {
                    Remove(item);
                }
            }
        }

        public bool IsProperSubsetOf(IEnumerable<T> enumerable)
        {
            var list = new List<T>(enumerable);
            return IsSubsetOf(list) && Count < list.Count;
        }

        public bool IsProperSupersetOf(IEnumerable<T> enumerable)
        {
            var list = new List<T>(enumerable);
            return IsSupersetOf(list) && Count > list.Count;
        }

        public bool IsReadOnly => false;

        public bool IsSubsetOf(IEnumerable<T> enumerable)
        {
            var set = new HashSet<T>(enumerable);
            return this.All(x => set.Contains(x));
        }

        public bool IsSupersetOf(IEnumerable<T> enumerable)
        {
            return enumerable.All(Contains);
        }

        public T Last => _list.Last.Value;

        public IEnumerable<T> MutableIterator()
        {
            for (var ptr = _list.First; ptr != null; ptr = ptr.Next)
            {
                yield return ptr.Value;
            }
        }

        public bool Overlaps(IEnumerable<T> enumerable)
        {
            return enumerable.Any(Contains);
        }

        public bool Remove(T item)
        {
            if (!Contains(item))
            {
                return false;
            }

            _list.Remove(_dict[item]);
            _dict.Remove(item);
            return true;
        }

        public bool SetEquals(IEnumerable<T> enumerable)
        {
            return new HashSet<T>(enumerable) == new HashSet<T>(this);
        }

        public void SymmetricExceptWith(IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                if (Contains(item))
                {
                    Remove(item);
                }
                else
                {
                    Add(item);
                }
            }
        }

        public T this[T index] => _dict[index].Value;

        public void UnionWith(IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                if (!Contains(item))
                {
                    Add(item);
                }
            }
        }

        public override string ToString()
        {
            return "{" + string.Join(", ", this) + "}";
        }
    }
}