using System.Collections.Generic;
using System.Linq;
using SIL.APRE.Fsa;

namespace SIL.APRE.Matching
{
	public class Group<TData, TOffset> : PatternNode<TData, TOffset> where TData : IData<TOffset>
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

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="group">The nested pattern.</param>
		public Group(Group<TData, TOffset> group)
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
			if (child is Expression<TData, TOffset>)
				return false;
			return true;
		}

		internal override State<TData, TOffset> GenerateNfa(FiniteStateAutomaton<TData, TOffset> fsa, State<TData, TOffset> startState)
		{
			if (IsLeaf)
				return startState;

			if (_name != null)
				startState = fsa.CreateTag(startState, fsa.CreateState(), _name, true);
			startState = base.GenerateNfa(fsa, startState);
			if (_name != null)
				startState = fsa.CreateTag(startState, fsa.CreateState(), _name, false);
			return startState;
		}

		public override string ToString()
		{
			return "(" + Children + ")";
		}

		public override int GetHashCode()
		{
			return Children.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Group<TData, TOffset>);
		}

		public bool Equals(Group<TData, TOffset> other)
		{
			if (other == null)
				return false;
			return Children.Equals(other.Children);
		}

		public override PatternNode<TData, TOffset> Clone()
		{
			return new Group<TData, TOffset>(this);
		}
	}
}
