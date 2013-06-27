using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIL.Collections
{
	public class ObservableList<T> : ObservableCollection<T>, IObservableList<T>
	{
		public ObservableList()
		{
		}

		public ObservableList(IEnumerable<T> items)
			: base(items)
		{
		}
	}
}
