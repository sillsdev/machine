using System;
using System.Collections;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class ArcCollection<TData, TOffset> : ICollection<Arc<TData, TOffset>>, IFreezable where TData : IAnnotatedData<TOffset>
	{
		private readonly State<TData, TOffset> _state;
		private readonly List<Arc<TData, TOffset>> _arcs;
		private readonly IComparer<Arc<TData, TOffset>> _arcComparer;
		private readonly bool _isFsa;

		public ArcCollection(bool isFsa, State<TData, TOffset> state)
		{
			_state = state;
			_isFsa = isFsa;
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
			CheckFrozen();

			return Add(target, ArcPriorityType.Medium);
		}

		public State<TData, TOffset> Add(State<TData, TOffset> target, ArcPriorityType priorityType)
		{
			CheckFrozen();

			return AddInternal(new Arc<TData, TOffset>(_state, target, priorityType));
		}

		public State<TData, TOffset> Add(FeatureStruct input, State<TData, TOffset> target)
		{
			CheckFrozen();

			if (!input.IsFrozen)
				throw new ArgumentException("The input must be immutable.", "input");
			return AddInternal(new Arc<TData, TOffset>(_state, new Input(input, 1), new PriorityUnionOutput<TData, TOffset>(FeatureStruct.New().Value).ToEnumerable(), target));
		}

		public State<TData, TOffset> Add(FeatureStruct input, FeatureStruct output, State<TData, TOffset> target)
		{
			CheckFrozen();

			return Add(input, output, false, target);
		}

		public State<TData, TOffset> Add(FeatureStruct input, FeatureStruct output, bool replace, State<TData, TOffset> target)
		{
			CheckFrozen();

			if (_isFsa)
				throw new InvalidOperationException("Outputs are not valid on acceptors.");

			if (input != null && !input.IsFrozen)
				throw new ArgumentException("The input must be immutable.", "input");
			if (output != null && !output.IsFrozen)
				throw new ArgumentException("The output must be immutable.", "output");

			Output<TData, TOffset> outputAction;
			if (input == null)
				outputAction = new InsertOutput<TData, TOffset>(output);
			else if (output == null)
				outputAction = new RemoveOutput<TData, TOffset>();
			else if (replace)
				outputAction = new ReplaceOutput<TData, TOffset>(output);
			else
				outputAction = new PriorityUnionOutput<TData, TOffset>(output);

			return AddInternal(new Arc<TData, TOffset>(_state, new Input(input, 1), outputAction.ToEnumerable(), target));
		}

		internal State<TData, TOffset> Add(Input input, IEnumerable<Output<TData, TOffset>> output, State<TData, TOffset> target)
		{
			return AddInternal(new Arc<TData, TOffset>(_state, input, output, target));
		}

		internal State<TData, TOffset> Add(State<TData, TOffset> target, int tag)
		{
			return AddInternal(new Arc<TData, TOffset>(_state, target, tag));
		}

		internal State<TData, TOffset> Add(Input input, IEnumerable<Output<TData, TOffset>> outputs, State<TData, TOffset> target, IEnumerable<TagMapCommand> cmds, int priority)
		{
			return AddInternal(new Arc<TData, TOffset>(_state, input, outputs, target, cmds) {Priority = priority});
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
			CheckFrozen();

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
			CheckFrozen();

			return _arcs.Remove(item);
		}

		public int Count
		{
			get { return _arcs.Count; }
		}

		bool ICollection<Arc<TData, TOffset>>.IsReadOnly
		{
			get { return IsFrozen; }
		}

		public int IndexOf(Arc<TData, TOffset> item)
		{
			return _arcs.IndexOf(item);
		}

		public Arc<TData, TOffset> this[int index]
		{
			get { return _arcs[index]; }
		}

		private void CheckFrozen()
		{
			if (IsFrozen)
				throw new InvalidOperationException("The FST is immutable.");
		}

		public bool IsFrozen { get; private set; }

		public void Freeze()
		{
			if (IsFrozen)
				return;

			IsFrozen = true;
		}

		public int GetFrozenHashCode()
		{
			return GetHashCode();
		}
	}
}
