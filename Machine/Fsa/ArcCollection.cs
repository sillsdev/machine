using System;
using System.Collections;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Fsa
{
	public class ArcCollection<TData, TOffset> : ICollection<Arc<TData, TOffset>> where TData : IData<TOffset>
	{
		private readonly State<TData, TOffset> _state;
		private readonly List<Arc<TData, TOffset>> _arcs;
		private readonly IComparer<Arc<TData, TOffset>> _arcComparer;

		public ArcCollection(State<TData, TOffset> state)
		{
			_state = state;
			_arcs = new List<Arc<TData, TOffset>>();
			_arcComparer = ProjectionComparer<Arc<TData, TOffset>>.Create(arc => arc.PriorityType).Reverse();
		}

		IEnumerator<Arc<TData, TOffset>> IEnumerable<Arc<TData, TOffset>>.GetEnumerator()
		{
			return _arcs.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _arcs.GetEnumerator();
		}

		public State<TData, TOffset> Add(State<TData, TOffset> target)
		{
			return Add(target, ArcPriorityType.Medium);
		}

		public State<TData, TOffset> Add(State<TData, TOffset> target, ArcPriorityType priorityType)
		{
			return AddInternal(new Arc<TData, TOffset>(_state, target, priorityType));
		}

		public State<TData, TOffset> Add(FeatureStruct condition, State<TData, TOffset> target)
		{
			if (!condition.IsFrozen)
				throw new ArgumentException("The condition must be immutable.", "condition");
			return AddInternal(new Arc<TData, TOffset>(_state, condition, target));
		}

		internal State<TData, TOffset> Add(State<TData, TOffset> target, int tag)
		{
			return AddInternal(new Arc<TData, TOffset>(_state, target, tag));
		}

		internal State<TData, TOffset> Add(FeatureStruct condition, State<TData, TOffset> target, IEnumerable<TagMapCommand> cmds)
		{
			return AddInternal(new Arc<TData, TOffset>(_state, condition, target, cmds));
		}

		void ICollection<Arc<TData, TOffset>>.Add(Arc<TData, TOffset> arc)
		{
			AddInternal(arc);
		}

		private State<TData, TOffset> AddInternal(Arc<TData, TOffset> arc)
		{
			int index = _arcs.BinarySearch(arc, _arcComparer);
			if (index < 0)
				index = ~index;
			_arcs.Insert(index, arc);
			return arc.Target;
		}

		public void Clear()
		{
			_arcs.Clear();
		}

		public bool Contains(Arc<TData, TOffset> item)
		{
			return _arcs.Contains(item);
		}

		public void CopyTo(Arc<TData, TOffset>[] array, int arrayIndex)
		{
			_arcs.CopyTo(array, arrayIndex);
		}

		public bool Remove(Arc<TData, TOffset> item)
		{
			return _arcs.Remove(item);
		}

		public int Count
		{
			get { return _arcs.Count; }
		}

		bool ICollection<Arc<TData, TOffset>>.IsReadOnly
		{
			get { return false; }
		}
	}
}
