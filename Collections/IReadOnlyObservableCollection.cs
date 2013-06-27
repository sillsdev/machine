using System.Collections.Specialized;
using System.ComponentModel;

namespace SIL.Collections
{
	public interface IReadOnlyObservableCollection<out T> : INotifyCollectionChanged, INotifyPropertyChanged, IReadOnlyCollection<T>
	{
	}
}
