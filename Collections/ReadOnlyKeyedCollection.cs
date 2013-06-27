using System;

namespace SIL.Collections
{
	public class ReadOnlyKeyedCollection<TKey, TItem> : ReadOnlyCollection<TItem>, IReadOnlyKeyedCollection<TKey, TItem>, IKeyedCollection<TKey, TItem>
	{
		private readonly IKeyedCollection<TKey, TItem> _collection; 

		public ReadOnlyKeyedCollection(IKeyedCollection<TKey, TItem> collection)
			: base(collection)
		{
			_collection = collection;
		}

		public bool TryGetValue(TKey key, out TItem item)
		{
			return _collection.TryGetValue(key, out item);
		}

		public TItem this[TKey key]
		{
			get { return _collection[key]; }
		}

		public bool Contains(TKey key)
		{
			return _collection.Contains(key);
		}

		public bool Remove(TKey key)
		{
			throw new NotSupportedException();
		}
	}
}
