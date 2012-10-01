using System;
using System.Collections.Generic;

namespace SIL.Collections
{
	public class KeyedCollection<TKey, TItem> : System.Collections.ObjectModel.KeyedCollection<TKey, TItem>, IKeyedCollection<TKey, TItem>
	{
		private readonly Func<TItem, TKey> _getKeyForItem; 

		public KeyedCollection(Func<TItem, TKey> getKeyForItem)
		{
			_getKeyForItem = getKeyForItem;
		}

		public KeyedCollection(Func<TItem, TKey> getKeyForItem, IEqualityComparer<TKey> comparer)
			: base(comparer)
		{
			_getKeyForItem = getKeyForItem;
		}

		public KeyedCollection(Func<TItem, TKey> getKeyForItem, IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold)
			: base(comparer, dictionaryCreationThreshold)
		{
			_getKeyForItem = getKeyForItem;
		}

		protected KeyedCollection()
		{
		}

		protected KeyedCollection(IEqualityComparer<TKey> comparer)
			: base(comparer)
		{
		}

		protected KeyedCollection(IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold)
			: base(comparer, dictionaryCreationThreshold)
		{
		}

		protected override TKey GetKeyForItem(TItem item)
		{
			return _getKeyForItem(item);
		}

		public bool TryGetValue(TKey key, out TItem value)
		{
			if (Contains(key))
			{
				value = this[key];
				return true;
			}

			value = default(TItem);
			return false;
		}
	}
}
