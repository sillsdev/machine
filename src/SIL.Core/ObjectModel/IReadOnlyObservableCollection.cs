#if NET_STD13
using System.Collections.Generic;
#endif
using System.Collections.Specialized;
using System.ComponentModel;

namespace SIL.ObjectModel
{
	public interface IReadOnlyObservableCollection<out T> : INotifyCollectionChanged, INotifyPropertyChanged, IReadOnlyCollection<T>
	{
	}
}
