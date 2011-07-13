using System.Collections.Generic;
using System.Linq;
using SIL.APRE.Fsa;

namespace SIL.APRE
{
	public class Group<TOffset> : PatternNode<TOffset>
	{
		private readonly BidirList<PatternNode<TOffset>> _nodes;
		private readonly string _groupName;

		public Group(string groupName, IEnumerable<PatternNode<TOffset>> nodes)
		{
			_groupName = groupName;
			_nodes = new BidirList<PatternNode<TOffset>>();
			_nodes.AddMany(nodes);
        }

		public Group(string groupName, params PatternNode<TOffset>[] nodes)
			: this(groupName, (IEnumerable<PatternNode<TOffset>>) nodes)
		{
		}

		public Group(IEnumerable<PatternNode<TOffset>> nodes)
			: this(null, nodes)
		{
		}

		public Group(params PatternNode<TOffset>[] nodes)
			: this((IEnumerable<PatternNode<TOffset>>) nodes)
		{
		}

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="group">The nested pattern.</param>
        public Group(Group<TOffset> group)
			: this(group._groupName, group._nodes)
        {
        }

        /// <summary>
        /// Gets the node type.
        /// </summary>
        /// <value>The node type.</value>
        public override NodeType Type
        {
            get
            {
                return NodeType.Group;
            }
        }

        /// <summary>
        /// Gets the features.
        /// </summary>
        /// <value>The features.</value>
        public override IEnumerable<Feature> Features
        {
            get
            {
				var features = new HashSet<Feature>();
				foreach (PatternNode<TOffset> node in _nodes)
					features.UnionWith(node.Features);
				return features;
            }
        }

		public override Pattern<TOffset> Pattern
		{
			get
			{
				return base.Pattern;
			}

			internal set
			{
				base.Pattern = value;
				foreach (PatternNode<TOffset> node in _nodes)
					node.Pattern = value;
			}
		}

        /// <summary>
        /// Gets the phonetic pattern.
        /// </summary>
        /// <value>The phonetic pattern.</value>
        public IBidirList<PatternNode<TOffset>> Nodes
        {
            get
            {
                return _nodes;
            }
        }

		public string GroupName
		{
			get { return _groupName; }
		}

		/// <summary>
		/// Determines whether this node references the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>
		/// 	<c>true</c> if the specified feature is referenced, otherwise <c>false</c>.
		/// </returns>
		public override bool IsFeatureReferenced(Feature feature)
		{
			return _nodes.Any(node => node.IsFeatureReferenced(feature));
		}

		internal override State<TOffset, FeatureStructure> GenerateNfa(FiniteStateAutomaton<TOffset, FeatureStructure> fsa,
			State<TOffset, FeatureStructure> startState)
		{
			if (_nodes.Count == 0)
				return base.GenerateNfa(fsa, startState);

			if (_groupName != null)
				startState = fsa.CreateGroupTag(startState, _groupName, true);
			State<TOffset, FeatureStructure> endState = _nodes.GetFirst(fsa.Direction).GenerateNfa(fsa, startState);
			if (_groupName != null)
				endState = fsa.CreateGroupTag(endState, _groupName, false);
			return base.GenerateNfa(fsa, endState);
		}

		public override string ToString()
		{
			return "(" + _nodes + ")";
		}

		public override int GetHashCode()
		{
			return _nodes.GetHashCode();
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
			return _nodes.Equals(other._nodes);
		}

		public override PatternNode<TOffset> Clone()
		{
			return new Group<TOffset>(this);
		}
	}
}
