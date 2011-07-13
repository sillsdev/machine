using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.APRE.Fsa;

namespace SIL.APRE
{
	public class Alternation<TOffset> : PatternNode<TOffset>
	{
		private readonly List<PatternNode<TOffset>> _nodes;

		public Alternation(IEnumerable<PatternNode<TOffset>> nodes)
		{
			_nodes = new List<PatternNode<TOffset>>(nodes);
		}

		public Alternation(params PatternNode<TOffset>[] nodes)
			: this((IEnumerable<PatternNode<TOffset>>) nodes)
		{
		}

		public Alternation(Alternation<TOffset> alternation)
			: this(alternation._nodes.Select(node => node.Clone()))
		{
		}

		public override NodeType Type
		{
			get { return NodeType.Alternation; }
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
		public IEnumerable<PatternNode<TOffset>> Nodes
		{
			get
			{
				return _nodes;
			}
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

			State<TOffset, FeatureStructure> endState = fsa.CreateState();
			foreach (PatternNode<TOffset> node in _nodes)
			{
				State<TOffset, FeatureStructure> nodeStartState = fsa.CreateState();
				startState.AddTransition(new Transition<TOffset, FeatureStructure>(nodeStartState));
				State<TOffset, FeatureStructure> nodeEndState = node.GenerateNfa(fsa, startState);
				nodeEndState.AddTransition(new Transition<TOffset, FeatureStructure>(endState));
			}
			return base.GenerateNfa(fsa, endState);
		}

		public override PatternNode<TOffset> Clone()
		{
			return new Alternation<TOffset>(this);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append("(");
			bool first = true;
			foreach (PatternNode<TOffset> node in _nodes)
			{
				if (!first)
					sb.Append("|");
				sb.Append(node);
				first = false;
			}
			return sb.ToString();
		}

		public override int GetHashCode()
		{
			return _nodes.Aggregate(0, (current, node) => current ^ node.GetHashCode());
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Alternation<TOffset>);
		}

		public bool Equals(Alternation<TOffset> other)
		{
			if (other == null)
				return false;
			return _nodes.SequenceEqual(other._nodes);
		}
	}
}
