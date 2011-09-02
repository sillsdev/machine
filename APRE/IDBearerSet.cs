using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.APRE
{
	public class IDBearerSet<T> : ICollection<T> where T : IIDBearer
	{
		private readonly Dictionary<string, T> _idBearers; 

		public IDBearerSet()
		{
			_idBearers = new Dictionary<string, T>();
		}

		public IDBearerSet(IEnumerable<T> items)
			: this()
		{
			UnionWith(items);
		}

		public bool TryGetValue(string id, out T value)
		{
			return _idBearers.TryGetValue(id, out value);
		}

		public void IntersectWith(IEnumerable<T> items)
		{
			foreach (T item in this.Where(i => !items.Contains(i)))
				Remove(item);
		}

		public void UnionWith(IEnumerable<T> items)
		{
			foreach (T item in items)
				Add(item);
		}

		public void ExceptWith(IEnumerable<T> items)
		{
			foreach (T item in this.Where(items.Contains))
				Remove(item);
		}

		public bool Overlaps(IEnumerable<T> items)
		{
			return items.Any(Contains);
		}

		public bool Overlaps(IEnumerable<string> ids)
		{
			return ids.Any(Contains);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return _idBearers.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<T>) this).GetEnumerator();
		}

		void ICollection<T>.Add(T item)
		{
			Add(item);
		}

		public bool Add(T item)
		{
			if (_idBearers.ContainsKey(item.ID))
				return false;
			_idBearers[item.ID] = item;
			return true;
		}

		public void Clear()
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
			return _idBearers.Remove(item.ID);
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

		public bool Contains(string id)
		{
			return _idBearers.ContainsKey(id);
		}
	}
}
