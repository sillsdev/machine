using System.Collections.Generic;
using SIL.APRE.Fsa;

namespace SIL.APRE.Patterns
{
	public class Group<TOffset> : PatternNode<TOffset>
	{
		private readonly string _groupName;

		public Group()
		{
		}

		public Group(string groupName)
		{
			_groupName = groupName;
		}
		
		public Group(params PatternNode<TOffset>[] nodes)
			: this((IEnumerable<PatternNode<TOffset>>) nodes)
		{
		}

		public Group(IEnumerable<PatternNode<TOffset>> nodes)
			: this(null, nodes)
		{
		}

		public Group(string groupName, params PatternNode<TOffset>[] nodes)
			: this(groupName, (IEnumerable<PatternNode<TOffset>>)nodes)
		{
		}

		public Group(string groupName, IEnumerable<PatternNode<TOffset>> nodes)
			: base(nodes)
		{
			_groupName = groupName;
		}

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="group">The nested pattern.</param>
        public Group(Group<TOffset> group)
			: base(group)
        {
        	_groupName = group._groupName;
        }

        /// <summary>
        /// Gets the node type.
        /// </summary>
        /// <value>The node type.</value>
        public override PatternNodeType Type
        {
            get
            {
                return PatternNodeType.Group;
            }
        }

		public string GroupName
		{
			get { return _groupName; }
		}

		internal override State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa, State<TOffset> startState)
		{
			if (IsLeaf)
				return startState;

			if (_groupName != null)
				startState = fsa.CreateGroupTag(startState, _groupName, true);
			State<TOffset> endState = base.GenerateNfa(fsa, startState);
			if (_groupName != null)
				endState = fsa.CreateGroupTag(endState, _groupName, false);
			return endState;
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
