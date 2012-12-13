using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Fsa;

namespace SIL.Machine.Matching
{
	public class Group<TData, TOffset> : PatternNode<TData, TOffset>, IDeepCloneable<Group<TData, TOffset>> where TData : IData<TOffset>
	{
		private readonly string _name;

		public Group()
		{
		}

		public Group(string name)
			: this(name, Enumerable.Empty<PatternNode<TData, TOffset>>())
		{
		}

		public Group(IEnumerable<PatternNode<TData, TOffset>> nodes)
			: this(null, nodes)
		{
		}

		public Group(string name, IEnumerable<PatternNode<TData, TOffset>> nodes)
			: base(nodes)
		{
			_name = name;
		}

		public Group(PatternNode<TData, TOffset> node)
			: this(null, node)
		{
		}

		public Group(string name, PatternNode<TData, TOffset> node)
			: base(node)
		{
			_name = name;
		}

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="group">The nested pattern.</param>
		protected Group(Group<TData, TOffset> group)
			: base(group)
        {
        	_name = group._name;
        }

		public string Name
		{
			get { return _name; }
		}

    	protected override bool CanAdd(PatternNode<TData, TOffset> child)
		{
			if (!base.CanAdd(child) || child is Pattern<TData, TOffset>)
				return false;
			return true;
		}

		internal override State<TData, TOffset, FsaMatch<TOffset>> GenerateNfa(FiniteStateAcceptor<TData, TOffset> fsa, State<TData, TOffset, FsaMatch<TOffset>> startState, out bool hasVariables)
		{
			if (IsLeaf)
			{
				hasVariables = false;
				return startState;
			}

			if (_name != null)
				startState = fsa.CreateTag(startState, fsa.CreateState(), _name, true);
			startState = base.GenerateNfa(fsa, startState, out hasVariables);
			startState = _name != null ? fsa.CreateTag(startState, fsa.CreateState(), _name, false) : startState.Arcs.Add(fsa.CreateState());
			return startState;
		}

		public new Group<TData, TOffset> DeepClone()
		{
			return new Group<TData, TOffset>(this);
		}

		protected override PatternNode<TData, TOffset> DeepCloneImpl()
		{
			return DeepClone();
		}

		public override bool ValueEquals(PatternNode<TData, TOffset> other)
		{
			var otherGroup = other as Group<TData, TOffset>;
			return otherGroup != null && ValueEquals(otherGroup);
		}

		public bool ValueEquals(Group<TData, TOffset> other)
		{
			return base.ValueEquals(other);
		}

		public override string ToString()
		{
			return "(" + string.Concat(Children) + ")";
		}
	}
}
