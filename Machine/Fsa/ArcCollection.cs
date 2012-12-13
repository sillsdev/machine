using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Fsa
{
	public class ArcCollection<TData, TOffset, TResult> : ICollection<Arc<TData, TOffset, TResult>> where TData : IData<TOffset>
	{
		private readonly State<TData, TOffset, TResult> _state;
		private readonly List<Arc<TData, TOffset, TResult>> _arcs;
		private readonly IComparer<Arc<TData, TOffset, TResult>> _arcComparer;

		public ArcCollection(State<TData, TOffset, TResult> state)
		{
			_state = state;
			_arcs = new List<Arc<TData, TOffset, TResult>>();
			_arcComparer = ProjectionComparer<Arc<TData, TOffset, TResult>>.Create(arc => arc.PriorityType).Reverse();
		}

		IEnumerator<Arc<TData, TOffset, TResult>> IEnumerable<Arc<TData, TOffset, TResult>>.GetEnumerator()
		{
			return _arcs.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _arcs.GetEnumerator();
		}

		public State<TData, TOffset, TResult> Add(State<TData, TOffset, TResult> target)
		{
			return Add(target, ArcPriorityType.Medium);
		}

		public State<TData, TOffset, TResult> Add(State<TData, TOffset, TResult> target, ArcPriorityType priorityType)
		{
			return AddInternal(new Arc<TData, TOffset, TResult>(_state, target, priorityType));
		}

		public State<TData, TOffset, TResult> Add(FeatureStruct input, State<TData, TOffset, TResult> target)
		{
			if (!input.IsFrozen)
				throw new ArgumentException("The input must be immutable.", "input");
			return AddInternal(new Arc<TData, TOffset, TResult>(_state, new Predicate(input), target));
		}

		public State<TData, TOffset, TResult> Add(FeatureStruct input, FeatureStruct output, State<TData, TOffset, TResult> target)
		{
			return Add(input, output, false, target);
		}

		public State<TData, TOffset, TResult> Add(FeatureStruct input, FeatureStruct output, bool identity, State<TData, TOffset, TResult> target)
		{
			if (!input.IsFrozen)
				throw new ArgumentException("The input must be immutable.", "input");
			if (output != null && !output.IsFrozen)
				throw new ArgumentException("The output must be immutable.", "output");
			return AddInternal(new Arc<TData, TOffset, TResult>(_state, new Predicate(input, identity), output == null ? Enumerable.Empty<Predicate>() : new Predicate(output, identity).ToEnumerable(), target));
		}

		internal State<TData, TOffset, TResult> Add(Predicate input, IEnumerable<Predicate> output, State<TData, TOffset, TResult> target)
		{
			return AddInternal(new Arc<TData, TOffset, TResult>(_state, input, output, target));
		}

		internal State<TData, TOffset, TResult> Add(State<TData, TOffset, TResult> target, int tag)
		{
			return AddInternal(new Arc<TData, TOffset, TResult>(_state, target, tag));
		}

		internal State<TData, TOffset, TResult> Add(Predicate input, State<TData, TOffset, TResult> target, IEnumerable<TagMapCommand> cmds)
		{
			return AddInternal(new Arc<TData, TOffset, TResult>(_state, input, target, cmds));
		}

		void ICollection<Arc<TData, TOffset, TResult>>.Add(Arc<TData, TOffset, TResult> arc)
		{
			AddInternal(arc);
		}

		private State<TData, TOffset, TResult> AddInternal(Arc<TData, TOffset, TResult> arc)
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

		public bool Contains(Arc<TData, TOffset, TResult> item)
		{
			return _arcs.Contains(item);
		}

		public void CopyTo(Arc<TData, TOffset, TResult>[] array, int arrayIndex)
		{
			_arcs.CopyTo(array, arrayIndex);
		}

		public bool Remove(Arc<TData, TOffset, TResult> item)
		{
			return _arcs.Remove(item);
		}

		public int Count
		{
			get { return _arcs.Count; }
		}

		bool ICollection<Arc<TData, TOffset, TResult>>.IsReadOnly
		{
			get { return false; }
		}
	}
}
