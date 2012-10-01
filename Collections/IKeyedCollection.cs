using System.Collections.Generic;

namespace SIL.Collections
{
	public interface IKeyedCollection<in TKey, TItem> : ICollection<TItem>
	{
		TItem this[TKey key] { get; }
		bool TryGetValue(TKey key, out TItem value);
		bool Contains(TKey key);
		bool Remove(TKey key);
	}
}
