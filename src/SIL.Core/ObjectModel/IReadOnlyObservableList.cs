#if NET_STD13
using System.Collections.Generic;
#endif

namespace SIL.ObjectModel
{
	public interface IReadOnlyObservableList<out T> : IReadOnlyList<T>, IReadOnlyObservableCollection<T>
	{
	}
}
