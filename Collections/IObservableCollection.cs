using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SIL.Collections
{
	public interface IObservableCollection<T> : INotifyCollectionChanged, INotifyPropertyChanged, ICollection<T>
	{
	}
}
