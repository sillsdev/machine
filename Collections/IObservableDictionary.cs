using System.Collections.Generic;

namespace SIL.Collections
{
	public interface IObservableDictionary<TKey, TValue> : IObservableCollection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>
	{
	}
}
