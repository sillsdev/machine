using System.Collections;
using System.Collections.Generic;

namespace SIL.APRE
{
	public class IDBearerDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : IIDBearer
	{
		private readonly Dictionary<TKey, TValue> _idBearerDictionary;
		private readonly Dictionary<string, TValue> _idDictionary; 

		public IDBearerDictionary()
		{
			_idBearerDictionary = new Dictionary<TKey, TValue>();
			_idDictionary = new Dictionary<string, TValue>();
		}

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return _idBearerDictionary.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<TKey, TValue>>) this).GetEnumerator();
		}

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
		{
			((ICollection<KeyValuePair<TKey, TValue>>) _idBearerDictionary).Add(item);
			_idDictionary.Add(item.Key.ID, item.Value);
		}

		public void Clear()
		{
			_idBearerDictionary.Clear();
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
		{
			return ((ICollection<KeyValuePair<TKey, TValue>>) _idBearerDictionary).Contains(item);
		}

		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<TKey, TValue>>) _idBearerDictionary).CopyTo(array, arrayIndex);
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
		{
			_idDictionary.Remove(item.Key.ID);
			return ((ICollection<KeyValuePair<TKey, TValue>>) _idBearerDictionary).Remove(item);
		}

		public int Count
		{
			get { return _idBearerDictionary.Count; }
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
		{
			get { return false; }
		}

		public bool ContainsKey(TKey key)
		{
			return _idBearerDictionary.ContainsKey(key);
		}

		public bool ContainsKey(string keyID)
		{
			return _idDictionary.ContainsKey(keyID);
		}

		public void Add(TKey key, TValue value)
		{
			_idBearerDictionary.Add(key, value);
			_idDictionary.Add(key.ID, value);
		}

		public bool Remove(TKey key)
		{
			_idDictionary.Remove(key.ID);
			return _idBearerDictionary.Remove(key);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			return _idBearerDictionary.TryGetValue(key, out value);
		}

		public bool TryGetValue(string keyID, out TValue value)
		{
			return _idDictionary.TryGetValue(keyID, out value);
		}

		public TValue this[TKey key]
		{
			get { return _idBearerDictionary[key]; }
			set
			{
				_idBearerDictionary[key] = value;
				_idDictionary[key.ID] = value;
			}
		}

		public TValue this[string keyID]
		{
			get { return _idDictionary[keyID]; }
		}

		public ICollection<TKey> Keys
		{
			get { return _idBearerDictionary.Keys; }
		}

		public ICollection<TValue> Values
		{
			get { return _idBearerDictionary.Values; }
		}
	}
}
