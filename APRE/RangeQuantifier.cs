using System.Collections.Generic;
using SIL.APRE.Fsa;

namespace SIL.APRE
{
    /// <summary>
    /// This class represents a nested phonetic pattern within another phonetic pattern.
    /// </summary>
    public class RangeQuantifier<TOffset> : PatternNode<TOffset>
    {
    	private readonly int _minOccur;
    	private readonly int _maxOccur;
    	private readonly PatternNode<TOffset> _node;

    	/// <summary>
		/// Initializes a new instance of the <see cref="RangeQuantifier&lt;TOffset&gt;"/> class.
    	/// </summary>
    	/// <param name="minOccur">The minimum number of occurrences.</param>
    	/// <param name="maxOccur">The maximum number of occurrences.</param>
		/// <param name="node">The pattern node.</param>
		public RangeQuantifier(int minOccur, int maxOccur, PatternNode<TOffset> node)
        {
            _node = node;
            _minOccur = minOccur;
            _maxOccur = maxOccur;
        }

    	/// <summary>
    	/// Copy constructor.
    	/// </summary>
    	/// <param name="range">The range quantifier.</param>
    	public RangeQuantifier(RangeQuantifier<TOffset> range)
        {
			_node = range._node.Clone();
            _minOccur = range._minOccur;
            _maxOccur = range._maxOccur;
        }

        /// <summary>
        /// Gets the node type.
        /// </summary>
        /// <value>The node type.</value>
        public override NodeType Type
        {
            get
            {
                return NodeType.Range;
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
                return _node.Features;
            }
        }

        /// <summary>
        /// Gets the phonetic pattern.
        /// </summary>
        /// <value>The phonetic pattern.</value>
        public PatternNode<TOffset> Node
        {
            get
            {
                return _node;
            }
        }

        /// <summary>
        /// Gets the minimum number of occurrences of this pattern.
        /// </summary>
        /// <value>The minimum number of occurrences.</value>
        public int MinOccur
        {
            get
            {
                return _minOccur;
            }
        }

        /// <summary>
        /// Gets the maximum number of occurrences of this pattern.
        /// </summary>
        /// <value>The maximum number of occurrences.</value>
        public int MaxOccur
        {
            get
            {
                return _maxOccur;
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
				_node.Pattern = value;
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
            return _node.IsFeatureReferenced(feature);
        }

		internal override State<TOffset, FeatureStructure> GenerateNfa(FiniteStateAutomaton<TOffset, FeatureStructure> fsa,
			State<TOffset, FeatureStructure> startState, Direction dir)
		{
			State<TOffset, FeatureStructure> endState = _node.GenerateNfa(fsa, startState, dir);

			if (_minOccur == 0 && _maxOccur == 1)
			{
				// optional
				startState.AddTransition(new Transition<TOffset, FeatureStructure>(endState));
			}
			else if (_minOccur == 0 && _maxOccur == -1)
			{
				// kleene star
				startState.AddTransition(new Transition<TOffset, FeatureStructure>(endState));
				endState.AddTransition(new Transition<TOffset, FeatureStructure>(startState));
			}
			else if (_minOccur == 1 && _maxOccur == -1)
			{
				// plus
				endState.AddTransition(new Transition<TOffset, FeatureStructure>(startState));
			}
			else
			{
				// range
				State<TOffset, FeatureStructure> currentState = startState;
				var startStates = new List<State<TOffset, FeatureStructure>>();
				if (_minOccur == 0)
				{
					startStates.Add(currentState);
				}
				else
				{
					for (int i = 1; i < _minOccur; i++)
					{
						currentState = endState;
						endState = _node.GenerateNfa(fsa, currentState, dir);
					}
				}

				if (_maxOccur == -1)
				{
					endState.AddTransition(new Transition<TOffset, FeatureStructure>(currentState));
				}
				else
				{
					int numCopies = _maxOccur - _minOccur;
					if (_minOccur == 0)
						numCopies--;
					for (int i = 1; i <= numCopies; i++)
					{
						currentState = endState;
						startStates.Add(currentState);
						endState = _node.GenerateNfa(fsa, currentState, dir);
					}
				}
				foreach (State<TOffset, FeatureStructure> state in startStates)
					state.AddTransition(new Transition<TOffset, FeatureStructure>(endState));
			}

			return base.GenerateNfa(fsa, endState, dir);
		}

        public override string ToString()
        {
            return "(" + _node + ")";
        }

        public override int GetHashCode()
        {
            return _node.GetHashCode() ^ _minOccur ^ _maxOccur;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return Equals(obj as RangeQuantifier<TOffset>);
        }

        public bool Equals(RangeQuantifier<TOffset> other)
        {
            if (other == null)
                return false;
            return _node.Equals(other._node) && _minOccur == other._minOccur
                && _maxOccur == other._maxOccur;
        }

        public override PatternNode<TOffset> Clone()
        {
            return new RangeQuantifier<TOffset>(this);
        }
    }
}
