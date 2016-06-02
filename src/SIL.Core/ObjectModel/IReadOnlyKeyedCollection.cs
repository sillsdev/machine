#if NET_STD13
using System.Collections.Generic;
#endif

namespace SIL.ObjectModel
{
	public interface IReadOnlyKeyedCollection<in TKey, TItem> : IReadOnlyCollection<TItem>
	{
		bool TryGet(TKey key, out TItem item);
		TItem this[TKey key] { get; }
		bool Contains(TKey key);
	}
}
