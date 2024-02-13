using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.DataStructures
{
    public class IDBearerSet<T> : ISet<T>, IKeyedCollection<string, T>
        where T : IIDBearer
    {
        private readonly Dictionary<string, T> _idBearers;

        public IDBearerSet()
        {
            _idBearers = new Dictionary<string, T>();
        }

        public IDBearerSet(IEnumerable<T> items)
            : this()
        {
            foreach (T item in items)
                _idBearers[item.ID] = item;
        }

        public bool TryGetValue(string id, out T value)
        {
            return _idBearers.TryGetValue(id, out value);
        }

        public virtual void IntersectWith(IEnumerable<T> items)
        {
            foreach (T item in this.Where(i => !items.Contains(i)).ToArray())
                Remove(item);
        }

        public virtual void UnionWith(IEnumerable<T> items)
        {
            foreach (T item in items)
                Add(item);
        }

        public virtual void ExceptWith(IEnumerable<T> items)
        {
            foreach (T item in this.Where(items.Contains).ToArray())
                Remove(item);
        }

        public virtual void SymmetricExceptWith(IEnumerable<T> other)
        {
            foreach (T item in other.Distinct())
                if (!Remove(item))
                    Add(item);
        }

        public bool Overlaps(IEnumerable<T> items)
        {
            return items.Any(Contains);
        }

        public bool Overlaps(IEnumerable<string> ids)
        {
            return ids.Any(Contains);
        }

        public bool IsProperSubsetOf(IEnumerable<T> items)
        {
            int unfoundCount;
            return CheckSubset(items, out unfoundCount) && unfoundCount > 0;
        }

        public bool IsSubsetOf(IEnumerable<T> items)
        {
            int unfoundCount;
            return CheckSubset(items, out unfoundCount) && unfoundCount >= 0;
        }

        private bool CheckSubset(IEnumerable<T> items, out int unfoundCount)
        {
            int foundCount = 0;
            unfoundCount = 0;
            foreach (T item in items.Distinct())
            {
                if (Contains(item))
                    foundCount++;
                else
                    unfoundCount++;
            }
            return foundCount == _idBearers.Count;
        }

        public bool IsProperSupersetOf(IEnumerable<T> items)
        {
            int foundCount;
            return CheckSuperset(items, out foundCount) && foundCount < _idBearers.Count;
        }

        public bool IsSupersetOf(IEnumerable<T> items)
        {
            int foundCount;
            return CheckSuperset(items, out foundCount) && foundCount <= _idBearers.Count;
        }

        private bool CheckSuperset(IEnumerable<T> items, out int foundCount)
        {
            foundCount = 0;
            foreach (T item in items.Distinct())
            {
                if (!Contains(item))
                    return false;
                foundCount++;
            }
            return true;
        }

        public bool SetEquals(IEnumerable<T> items)
        {
            int foundCount;
            return CheckSuperset(items, out foundCount) && foundCount == _idBearers.Count;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _idBearers.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public virtual bool Add(T item)
        {
            if (_idBearers.ContainsKey(item.ID))
                return false;
            _idBearers[item.ID] = item;
            return true;
        }

        public virtual void Clear()
        {
            _idBearers.Clear();
        }

        public bool Contains(T item)
        {
            return _idBearers.ContainsKey(item.ID);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _idBearers.Values.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return Remove(item.ID);
        }

        public virtual bool Remove(string id)
        {
            return _idBearers.Remove(id);
        }

        public int Count
        {
            get { return _idBearers.Count; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        public T this[string id]
        {
            get { return _idBearers[id]; }
        }

        public bool TryGet(string key, out T item)
        {
            return _idBearers.TryGetValue(key, out item);
        }

        public bool Contains(string id)
        {
            return _idBearers.ContainsKey(id);
        }
    }
}
