using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SIL.Collections
{
	public sealed class ObservableIDBearerSet<T> : IDBearerSet<T>, IObservableSet<T> where T : IIDBearer
	{
		private readonly SimpleMonitor _reentrancyMonitor = new SimpleMonitor();
		private readonly SimpleMonitor _multiChangeMonitor = new SimpleMonitor();
		private readonly List<T> _added = new List<T>();
		private readonly List<T> _removed = new List<T>(); 

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { PropertyChanged += value; }
			remove { PropertyChanged -= value; }
		}

		private event PropertyChangedEventHandler PropertyChanged;

		public override void IntersectWith(IEnumerable<T> items)
		{
			CheckReentrancy();
			using (_multiChangeMonitor.Enter())
				base.IntersectWith(items);
			MultiChangeOccurred();
		}

		public override void UnionWith(IEnumerable<T> items)
		{
			CheckReentrancy();
			using (_multiChangeMonitor.Enter())
				base.UnionWith(items);
			MultiChangeOccurred();
		}

		public override void ExceptWith(IEnumerable<T> items)
		{
			CheckReentrancy();
			using (_multiChangeMonitor.Enter())
				base.ExceptWith(items);
			MultiChangeOccurred();
		}

		public override void SymmetricExceptWith(IEnumerable<T> other)
		{
			CheckReentrancy();
			using (_multiChangeMonitor.Enter())
				base.SymmetricExceptWith(other);
			MultiChangeOccurred();
		}

		private void MultiChangeOccurred()
		{
			if (_removed.Count > 0 || _added.Count > 0)
				OnPropertyChanged(new PropertyChangedEventArgs("Count"));

			if (_removed.Count > 0)
			{
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, _removed));
				_removed.Clear();
			}
			if (_added.Count > 0)
			{
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _added));
				_added.Clear();
			}
		}

		public override bool Add(T item)
		{
			CheckReentrancy();
			if (base.Add(item))
			{
				if (_multiChangeMonitor.Busy)
				{
					_added.Add(item);
				}
				else
				{
					OnPropertyChanged(new PropertyChangedEventArgs("Count"));
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
				}
				return true;
			}
			return false;
		}

		public override bool Remove(string id)
		{
			CheckReentrancy();
			T item;
			if (TryGetValue(id, out item))
			{
				base.Remove(id);
				if (_multiChangeMonitor.Busy)
				{
					_removed.Add(item);
				}
				else
				{
					OnPropertyChanged(new PropertyChangedEventArgs("Count"));
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
				}
				return true;
			}

			return false;
		}

		public override void Clear()
		{
			CheckReentrancy();

			if (Count > 0)
			{
				base.Clear();
				OnPropertyChanged(new PropertyChangedEventArgs("Count"));
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}
		}

		private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (CollectionChanged != null)
			{
				using (_reentrancyMonitor.Enter())
					CollectionChanged(this, e);
			}
		}

		private void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (PropertyChanged != null)
			{
				using (_reentrancyMonitor.Enter())
					PropertyChanged(this, e);
			}
		}

		private void CheckReentrancy()
		{
			if (_reentrancyMonitor.Busy)
				throw new InvalidOperationException("This collection cannot be changed during a CollectionChanged event.");
		}
	}
}
