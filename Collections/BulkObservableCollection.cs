using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SIL.Collections
{
	public class BulkObservableCollection<T> : ObservableCollection<T>
	{
		private bool _updating;

		public BulkObservableCollection()
		{
		}

		public BulkObservableCollection(IEnumerable<T> collection)
			: base(collection)
		{
		}

		public BulkObservableCollection(List<T> list)
			: base(list)
		{
		}

		public void AddRange(IEnumerable<T> collection)
		{
			var added = new List<T>();
			int startIndex = Count;
			foreach (T item in collection)
			{
				Items.Add(item);
				added.Add(item);
			}

			if (added.Count > 0)
			{
				OnPropertyChanged(new PropertyChangedEventArgs("Count"));
				OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, added, startIndex));
			}
		}

		public IDisposable BulkUpdate()
		{
			_updating = true;
			return new BulkUpdater(this);
		}

		private void EndUpdate()
		{
			_updating = false;
			OnPropertyChanged(new PropertyChangedEventArgs("Count"));
			OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (!_updating)
				base.OnCollectionChanged(e);
		}

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (!_updating)
				base.OnPropertyChanged(e);
		}

		private class BulkUpdater : IDisposable
		{
			private readonly BulkObservableCollection<T> _coll; 

			public BulkUpdater(BulkObservableCollection<T> coll)
			{
				_coll = coll;
			}

			public void Dispose()
			{
				_coll.EndUpdate();
			}
		}
	}
}
