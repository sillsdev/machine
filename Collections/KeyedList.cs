using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIL.Collections
{
	public class KeyedList<TKey, TItem> : KeyedCollection<TKey, TItem>, IKeyedCollection<TKey, TItem>
	{
		private readonly Func<TItem, TKey> _getKeyForItem; 

		public KeyedList(Func<TItem, TKey> getKeyForItem)
		{
			_getKeyForItem = getKeyForItem;
		}

		public KeyedList(Func<TItem, TKey> getKeyForItem, IEqualityComparer<TKey> comparer)
			: base(comparer)
		{
			_getKeyForItem = getKeyForItem;
		}

		public KeyedList(Func<TItem, TKey> getKeyForItem, IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold)
			: base(comparer, dictionaryCreationThreshold)
		{
			_getKeyForItem = getKeyForItem;
		}

		protected KeyedList()
		{
		}

		protected KeyedList(IEqualityComparer<TKey> comparer)
			: base(comparer)
		{
		}

		protected KeyedList(IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold)
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
