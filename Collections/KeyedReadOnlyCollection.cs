using System;
using System.Collections.Generic;

namespace SIL.Collections
{
	public abstract class KeyedReadOnlyCollection<TKey, TItem> : ReadOnlyCollection<TItem>, IKeyedReadOnlyCollection<TKey, TItem>
	{
		private readonly IEqualityComparer<TKey> _comparer;
		private readonly Dictionary<TKey, TItem> _dict;

		protected KeyedReadOnlyCollection(ICollection<TItem> collection)
			: this(collection, null)
		{
		}

		protected KeyedReadOnlyCollection(ICollection<TItem> collection, IEqualityComparer<TKey> comparer)
			: this(collection, comparer, 0)
		{
		}

		protected KeyedReadOnlyCollection(ICollection<TItem> collection, IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold)
			: base(collection)
		{
			if (comparer == null)
				comparer = EqualityComparer<TKey>.Default;

			if (dictionaryCreationThreshold == -1)
				dictionaryCreationThreshold = 2147483647;

			if (dictionaryCreationThreshold < -1)
				throw new ArgumentOutOfRangeException("dictionaryCreationThreshold");

			_comparer = comparer;

			if (collection.Count >= dictionaryCreationThreshold)
			{
				_dict = new Dictionary<TKey, TItem>(_comparer);
				PopulateDictionary();
			}
		}

		private void PopulateDictionary()
		{
			foreach (TItem item in this)
			{
				TKey key = GetKeyForItem(item);
				if (_dict.ContainsKey(key))
					throw new ArgumentException("The collection cannot contain duplicate keys.");
				if (key != null)
					_dict.Add(key, item);
			}
		}

		public bool TryGetValue(TKey key, out TItem item)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			if (_dict != null)
				return _dict.TryGetValue(key, out item);

			foreach (TItem current in this)
			{
				if (_comparer.Equals(GetKeyForItem(current), key))
				{
					item = current;
					return true;
				}
			}

			item = default(TItem);
			return false;
		}

		public TItem this[TKey key]
		{
			get
			{
				if (key == null)
					throw new ArgumentNullException("key");

				if (_dict != null)
					return _dict[key];

				foreach (TItem current in this)
				{
					if (_comparer.Equals(GetKeyForItem(current), key))
						return current;
				}
				throw new KeyNotFoundException();
			}
		}

		public bool Contains(TKey key)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			if (_dict != null)
				return _dict.ContainsKey(key);

			foreach (TItem current in this)
			{
				if (_comparer.Equals(GetKeyForItem(current), key))
					return true;
			}
			return false;
		}

		protected abstract TKey GetKeyForItem(TItem item);
	}
}
