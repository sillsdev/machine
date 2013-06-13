using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace SIL.Collections
{
	public class ReadOnlyMirroredCollection<TSource, TTarget> : ReadOnlyObservableCollection<TTarget>, IReadOnlyList<TTarget>
	{
		private readonly Func<TSource, TTarget> _converter;
		private readonly BulkObservableCollection<TTarget> _items;

		public ReadOnlyMirroredCollection(INotifyCollectionChanged source, Func<TSource, TTarget> converter)
			: base(new BulkObservableCollection<TTarget>(((IEnumerable<TSource>) source).Select(converter)))
		{
			_converter = converter;
			_items = (BulkObservableCollection<TTarget>) Items;
			source.CollectionChanged += OnSourceCollectionChanged;
		}

		private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					MirrorInsert(e.NewStartingIndex, e.NewItems.Cast<TSource>());
					break;

				case NotifyCollectionChangedAction.Move:
					MirrorMove(e.OldStartingIndex, e.OldItems.Count, e.NewStartingIndex);
					break;

				case NotifyCollectionChangedAction.Remove:
					MirrorRemove(e.OldStartingIndex, e.OldItems.Count);
					break;

				case NotifyCollectionChangedAction.Replace:
					MirrorReplace(e.OldStartingIndex, e.OldItems.Count, e.NewItems.Cast<TSource>());
					break;

				case NotifyCollectionChangedAction.Reset:
					MirrorReset((IEnumerable<TSource>) sender);
					break;
			}
		}

		protected virtual void MirrorInsert(int index, IEnumerable<TSource> items)
		{
			_items.InsertRange(index, items.Select(item => _converter(item)));
		}

		protected virtual void MirrorMove(int oldIndex, int count, int newIndex)
		{
			_items.MoveRange(oldIndex, count, newIndex);
		}

		protected virtual void MirrorRemove(int index, int count)
		{
			_items.RemoveRangeAt(index, count);
		}

		protected virtual void MirrorReplace(int index, int count, IEnumerable<TSource> items)
		{
			_items.ReplaceRange(index, count, items.Select(item => _converter(item)));
		}

		protected virtual void MirrorReset(IEnumerable<TSource> source)
		{
			using (_items.BulkUpdate())
			{
				_items.Clear();
				_items.AddRange(source.Select(item => _converter(item)));
			}
		}
	}
}
