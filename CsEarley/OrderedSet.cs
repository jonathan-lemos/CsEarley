using System.Collections.Generic;
using System.Linq;

namespace CsEarley
{
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
            var list = new List<T>(enumerable);
            ExceptWith(list);
            foreach (var item in this)
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
    }
}