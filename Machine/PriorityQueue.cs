using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine
{
	/// <summary>
	/// This priority queue implementation works well when there are a small set of possible
	/// priority values, such as an enumeration. The queue is indexed by the values so that
	/// <c>Remove</c> and <c>Contains</c> are close to O(1).
	/// </summary>
	public class PriorityQueue<TPriority, TItem> : ICollection<TItem>
	{
		private struct IndexEntry
		{
			public TPriority Priority
			{
				get; set;
			}

			public LinkedListNode<TItem> Node
			{
				get; set;
			}
		}

		private readonly SortedDictionary<TPriority, LinkedList<TItem>> _queues;
		private readonly Dictionary<TItem, List<IndexEntry>> _index;

		/// <summary>
		/// Initializes a new instance of the <see cref="PriorityQueue&lt;P, T&gt;"/> class.
		/// </summary>
		public PriorityQueue()
			: this (Comparer<TPriority>.Default, EqualityComparer<TItem>.Default)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PriorityQueue&lt;P, T&gt;"/> class.
		/// </summary>
		/// <param name="priorityComparer">The priority comparer.</param>
		/// <param name="itemComparer">The item comparer.</param>
		public PriorityQueue(IComparer<TPriority> priorityComparer, IEqualityComparer<TItem> itemComparer)
		{
			_queues = new SortedDictionary<TPriority, LinkedList<TItem>>(priorityComparer);
			_index = new Dictionary<TItem, List<IndexEntry>>(itemComparer);
		}

		/// <summary>
		/// Gets the priority comparer.
		/// </summary>
		/// <value>The priority comparer.</value>
		public IComparer<TPriority> PriorityComparer
		{
			get
			{
				return _queues.Comparer;
			}
		}

		/// <summary>
		/// Gets the item comparer.
		/// </summary>
		/// <value>The item comparer.</value>
		public IEqualityComparer<TItem> ItemComparer
		{
			get
			{
				return _index.Comparer;
			}
		}

		/// <summary>
		/// Enqueues the specified item with the specified priority.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="item">The item.</param>
		public void Enqueue(TPriority priority, TItem item)
		{
			Enqueue(priority, item, true);
		}

		/// <summary>
		/// Enqueues the specified item with the specified priority.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="item">The item.</param>
		/// <param name="update">if set to <c>true</c> and the item is already in the queue, it will be updated.</param>
		public void Enqueue(TPriority priority, TItem item, bool update)
		{
			bool enqueue = true;
			List<IndexEntry> entries;
			if (_index.TryGetValue(item, out entries))
			{
				// the item is already in the queue
				if (update)
				{
					// try to update it
					for (int i = entries.Count - 1; i >= 0; i--)
					{
						if (_queues.Comparer.Compare(priority, entries[i].Priority) >= 0)
						{
							// the existing item is already at or higher than the specified priority, so just replace it
							var queue = entries[i].Node.List;
							var node = queue.AddAfter(entries[i].Node, item);
							queue.Remove(entries[i].Node);
							entries[i] = new IndexEntry {Priority = entries[i].Priority, Node = node};
							// do not add it to the queue, since it is already at the correct priority
							enqueue = false;
						}
						else
						{
							// the existing item is at a lower priority than the specified priority, so remove it
							RemoveItem(entries, i);
						}
					}
				}
			}
			else
			{
				entries = new List<IndexEntry>();
				_index[item] = entries;
			}

			if (enqueue)
			{
				LinkedList<TItem> queue;
				if (!_queues.TryGetValue(priority, out queue))
				{
					// create the queue for the specified priority if it doesn't exist
					queue = new LinkedList<TItem>();
					_queues.Add(priority, queue);
				}
				// add to the queue
				queue.AddLast(item);

				// add the item to the index
				entries.Add(new IndexEntry {Priority = priority, Node = queue.Last});
			}
		}

		/// <summary>
		/// Dequeues the next item in the queue.
		/// </summary>
		/// <returns></returns>
		public TItem Dequeue()
		{
			TPriority priority;
			return Dequeue(out priority);
		}

		/// <summary>
		/// Dequeues the next item in the queue.
		/// </summary>
		/// <returns></returns>
		public TItem Dequeue(out TPriority priority)
		{
			// get the first item in the first queue
			var priorityQueuePair = _queues.First();
			var item = priorityQueuePair.Value.First.Value;
			priority = priorityQueuePair.Key;

			List<IndexEntry> entries = _index[item];
			int entryIndex = 0;
			for (int i = 0; i < entries.Count; i++)
			{
				if (entries[i].Node == priorityQueuePair.Value.First)
				{
					entryIndex = i;
					break;
				}
			}

			RemoveItem(entries, entryIndex);
			if (entries.Count == 0)
				// clean up index
				_index.Remove(item);

			return item;
		}

		/// <summary>
		/// Returns the next item in the queue, but does not remove it.
		/// </summary>
		/// <returns></returns>
		public TItem Peek()
		{
			TPriority priority;
			return Peek(out priority);
		}

		/// <summary>
		/// Returns the next item in the queue, but does not remove it.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <returns></returns>
		public TItem Peek(out TPriority priority)
		{
			var priorityQueuePair = _queues.First();
			priority = priorityQueuePair.Key;
			return priorityQueuePair.Value.First.Value;
		}

		/// <summary>
		/// Gets a value indicating whether the queue is empty
		/// </summary>
		/// <value><c>true</c> if the queue is empty; otherwise, <c>false</c>.</value>
		public bool IsEmpty
		{
			get
			{
				return _queues.Count == 0;
			}
		}

		/// <summary>
		/// Gets the number of elements with the specified priority.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <returns></returns>
		public int GetPriorityCount(TPriority priority)
		{
			LinkedList<TItem> queue;
			if (_queues.TryGetValue(priority, out queue))
				return queue.Count;
			return 0;
		}

		private void RemoveItem(List<IndexEntry> entries, int entryIndex)
		{
			var queue = entries[entryIndex].Node.List;
			// remove the item from the queue
			queue.Remove(entries[entryIndex].Node);
			if (queue.Count == 0)
				// clean up queues
				_queues.Remove(entries[entryIndex].Priority);

			// remove the item from the index
			entries.RemoveAt(entryIndex);
		}

		#region Implementation of IEnumerable

		/// <summary>
		/// Returns an enumerator that iterates through the queue.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the queue.
		/// </returns>
		/// <filterpriority>1</filterpriority>
		IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
		{
			foreach (var queue in _queues.Values)
			{
				foreach (var item in queue)
					yield return item;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<TItem>) this).GetEnumerator();
		}

		#endregion

		#region Implementation of ICollection<T>

		/// <summary>
		/// Adds an item to the queue with the default priority.
		/// </summary>
		/// <param name="item">The object to add.</param>
		void ICollection<TItem>.Add(TItem item)
		{
			Enqueue(default(TPriority), item);
		}

		/// <summary>
		/// Removes all items from the queue.
		/// </summary>
		public void Clear()
		{
			_queues.Clear();
			_index.Clear();
		}

		/// <summary>
		/// Determines whether the queue contains a specific value.
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> is found in the queue; otherwise, false.
		/// </returns>
		/// <param name="item">The object to locate in the queue.</param>
		bool ICollection<TItem>.Contains(TItem item)
		{
			return _index.ContainsKey(item);
		}

		/// <summary>
		/// Copies the elements of the queue to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.
		///                     -or-
		///						<paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.
		///                     -or-
		///                     The number of elements in the source queue is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
		/// </exception>
		void ICollection<TItem>.CopyTo(TItem[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException("array");
			if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException("arrayIndex");		
			if (arrayIndex >= array.Length || ((ICollection<TItem>) this).Count > array.Length - arrayIndex)
				throw new ArgumentException("arrayIndex");
			if (array.Rank > 1)
				throw new ArgumentException("array");

			foreach (var item in this)
			{			
				if (arrayIndex >= array.Length)
					break;
				array[arrayIndex++] = item;
			}
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the queue.
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> was successfully removed from the queue; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		/// <param name="item">The object to remove.</param>
		bool ICollection<TItem>.Remove(TItem item)
		{
			List<IndexEntry> entries;
			// if the queue doesn't contain this item, just return false
			if (!_index.TryGetValue(item, out entries))
				return false;

			int bestIndex = -1;
			for (int i = 0; i < entries.Count; i++)
			{
				if (bestIndex == -1 || _queues.Comparer.Compare(entries[i].Priority, entries[bestIndex].Priority) < 0)
					bestIndex = i;
			}

			RemoveItem(entries, bestIndex);
			if (entries.Count == 0)
				// clean up index
				_index.Remove(item);

			return true;
		}

		/// <summary>
		/// Gets the number of elements contained in the queue.
		/// </summary>
		/// <returns>
		/// The number of elements contained in the queue.
		/// </returns>
		int ICollection<TItem>.Count
		{
			get
			{
				int count = 0;
				foreach (var queue in _queues.Values)
					count += queue.Count;
				return count;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the queue is read-only.
		/// </summary>
		/// <returns>
		/// true if the queue is read-only; otherwise, false.
		/// </returns>
		bool ICollection<TItem>.IsReadOnly
		{
			get { return false; }
		}

		#endregion
	}
}
