namespace SIL.Collections
{
	public interface IReadOnlyKeyedCollection<in TKey, TItem> : IReadOnlyCollection<TItem>
	{
		bool TryGetValue(TKey key, out TItem item);
		TItem this[TKey key] { get; }
		bool Contains(TKey key);
	}
}
