using System.Collections.Generic;
using SIL.APRE.Fsa;

namespace SIL.APRE.Matching
{
	public class Group<TOffset> : PatternNode<TOffset>
	{
		private readonly string _name;

		public Group()
		{
		}

		public Group(string name)
		{
			_name = name;
		}
		
		public Group(params PatternNode<TOffset>[] nodes)
			: this((IEnumerable<PatternNode<TOffset>>) nodes)
		{
		}

		public Group(IEnumerable<PatternNode<TOffset>> nodes)
			: this(null, nodes)
		{
		}

		public Group(string name, params PatternNode<TOffset>[] nodes)
			: this(name, (IEnumerable<PatternNode<TOffset>>)nodes)
		{
		}

		public Group(string name, IEnumerable<PatternNode<TOffset>> nodes)
			: base(nodes)
		{
			_name = name;
		}

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="group">The nested pattern.</param>
        public Group(Group<TOffset> group)
			: base(group)
        {
        	_name = group._name;
        }

		public string Name
		{
			get { return _name; }
		}

		protected override bool CanAdd(PatternNode<TOffset> child)
		{
			if (child is Expression<TOffset>)
				return false;
			return true;
		}

		internal override State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa, State<TOffset> startState)
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
			return Equals(obj as Group<TOffset>);
		}

		public bool Equals(Group<TOffset> other)
		{
			if (other == null)
				return false;
			return Children.Equals(other.Children);
		}

		public override PatternNode<TOffset> Clone()
		{
			return new Group<TOffset>(this);
		}
	}
}
