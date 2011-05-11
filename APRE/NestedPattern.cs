using System.Collections.Generic;
using SIL.APRE.Fsa;

namespace SIL.APRE
{
	public class NestedPattern<TOffset> : PatternNode<TOffset>
	{
		private readonly Pattern<TOffset> _pattern;

		public NestedPattern(int groupNum, Pattern<TOffset> pattern)
			: base(groupNum)
        {
            _pattern = pattern;
        }

		public NestedPattern(Pattern<TOffset> pattern)
			: this(-1, pattern)
		{
		}

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="nestedPattern">The nested pattern.</param>
        public NestedPattern(NestedPattern<TOffset> nestedPattern)
            : base(nestedPattern)
        {
            _pattern = new Pattern<TOffset>(nestedPattern._pattern);
        }

        /// <summary>
        /// Gets the node type.
        /// </summary>
        /// <value>The node type.</value>
        public override NodeType Type
        {
            get
            {
                return NodeType.Pattern;
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
                return _pattern.Features;
            }
        }

        /// <summary>
        /// Gets the phonetic pattern.
        /// </summary>
        /// <value>The phonetic pattern.</value>
        public Pattern<TOffset> Pattern
        {
            get
            {
                return _pattern;
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
			return _pattern.IsFeatureReferenced(feature);
		}

		internal override State<TOffset, FeatureStructure> GenerateNfa(FiniteStateAutomaton<TOffset, FeatureStructure> fsa,
			State<TOffset, FeatureStructure> startState, Direction dir)
		{
			if (_pattern.Count == 0)
				return base.GenerateNfa(fsa, startState, dir);

			if (GroupNum > 0)
				startState = fsa.CreateTag(startState, GroupNum, true);
			State<TOffset, FeatureStructure> endState = _pattern.GetFirst(dir).GenerateNfa(fsa, startState, dir);
			if (GroupNum > 0)
				endState = fsa.CreateTag(endState, GroupNum, false);
			return base.GenerateNfa(fsa, endState, dir);
		}

		public override string ToString()
		{
			return "(" + _pattern + ")";
		}

		public override int GetHashCode()
		{
			return _pattern.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as NestedPattern<TOffset>);
		}

		public bool Equals(NestedPattern<TOffset> other)
		{
			if (other == null)
				return false;
			return _pattern.Equals(other._pattern);
		}

		public override PatternNode<TOffset> Clone()
		{
			return new NestedPattern<TOffset>(this);
		}
	}
}
