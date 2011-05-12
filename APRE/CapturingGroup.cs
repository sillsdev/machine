using System.Collections.Generic;
using System.Linq;
using SIL.APRE.Fsa;

namespace SIL.APRE
{
	public class CapturingGroup<TOffset> : PatternNode<TOffset>
	{
		private readonly BidirList<PatternNode<TOffset>> _nodes;
		private readonly int _groupNum;

		public CapturingGroup(int groupNum, IEnumerable<PatternNode<TOffset>> nodes)
		{
			_groupNum = groupNum;
			_nodes = new BidirList<PatternNode<TOffset>>();
			_nodes.AddMany(nodes);
        }

		public CapturingGroup(int groupNum, params PatternNode<TOffset>[] nodes)
			: this(groupNum, (IEnumerable<PatternNode<TOffset>>) nodes)
		{
		}

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="capturingGroup">The nested pattern.</param>
        public CapturingGroup(CapturingGroup<TOffset> capturingGroup)
			: this(capturingGroup._groupNum, capturingGroup._nodes)
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

		public int GroupNum
		{
			get { return _groupNum; }
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
			State<TOffset, FeatureStructure> startState, Direction dir)
		{
			if (_nodes.Count == 0)
				return base.GenerateNfa(fsa, startState, dir);

			startState = fsa.CreateTag(startState, GroupNum, true);
			State<TOffset, FeatureStructure> endState = _nodes.GetFirst(dir).GenerateNfa(fsa, startState, dir);
			endState = fsa.CreateTag(endState, GroupNum, false);
			return base.GenerateNfa(fsa, endState, dir);
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
			return Equals(obj as CapturingGroup<TOffset>);
		}

		public bool Equals(CapturingGroup<TOffset> other)
		{
			if (other == null)
				return false;
			return _nodes.Equals(other._nodes);
		}

		public override PatternNode<TOffset> Clone()
		{
			return new CapturingGroup<TOffset>(this);
		}
	}
}
