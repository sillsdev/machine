using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace SIL.Collections
{
	public class ReadOnlyMirroredList<TSource, TTarget> : ReadOnlyObservableList<TTarget>, IReadOnlyKeyedCollection<TSource, TTarget>, IKeyedCollection<TSource, TTarget>
	{
		private readonly Func<TSource, TTarget> _sourceToTarget;
		private readonly KeyedBulkObservableList<TSource, TTarget> _items;

		public ReadOnlyMirroredList(IReadOnlyObservableList<TSource> source, Func<TSource, TTarget> sourceToTarget, Func<TTarget, TSource> targetToSource)
			: this((IEnumerable<TSource>) source, sourceToTarget, targetToSource)
		{
			source.CollectionChanged += OnSourceCollectionChanged;
		}

		public ReadOnlyMirroredList(IObservableList<TSource> source, Func<TSource, TTarget> sourceToTarget, Func<TTarget, TSource> targetToSource)
			: this((IEnumerable<TSource>) source, sourceToTarget, targetToSource)
		{
			source.CollectionChanged += OnSourceCollectionChanged;
		}

		protected ReadOnlyMirroredList(IEnumerable<TSource> source, Func<TSource, TTarget> sourceToTarget, Func<TTarget, TSource> targetToSource)
			: base(new KeyedBulkObservableList<TSource, TTarget>(source.Select(sourceToTarget), targetToSource))
		{
			_sourceToTarget = sourceToTarget;
			_items = (KeyedBulkObservableList<TSource, TTarget>) Items;
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
			_items.InsertRange(index, items.Select(item => _sourceToTarget(item)));
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
			_items.ReplaceRange(index, count, items.Select(item => _sourceToTarget(item)));
		}

		protected virtual void MirrorReset(IEnumerable<TSource> source)
		{
			using (_items.BulkUpdate())
			{
				_items.Clear();
				_items.AddRange(source.Select(item => _sourceToTarget(item)));
			}
		}

		public bool TryGetValue(TSource key, out TTarget item)
		{
			return _items.TryGetValue(key, out item);
		}

		public TTarget this[TSource key]
		{
			get { return _items[key]; }
		}

		public bool Contains(TSource key)
		{
			return _items.Contains(key);
		}

		bool IKeyedCollection<TSource, TTarget>.Remove(TSource key)
		{
			throw new NotSupportedException();
		}
	}
}
