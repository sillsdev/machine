using System.Collections.Generic;
using SIL.APRE.Fsa;

namespace SIL.APRE
{
    /// <summary>
    /// This class represents a nested phonetic pattern within another phonetic pattern.
    /// </summary>
    public class Quantifier<TOffset> : PatternNode<TOffset>
    {
    	private readonly int _minOccur;
    	private readonly int _maxOccur;
    	private readonly PatternNode<TOffset> _node;

    	/// <summary>
		/// Initializes a new instance of the <see cref="Quantifier{TOffset}"/> class.
    	/// </summary>
    	/// <param name="minOccur">The minimum number of occurrences.</param>
    	/// <param name="maxOccur">The maximum number of occurrences.</param>
		/// <param name="node">The pattern node.</param>
		public Quantifier(int minOccur, int maxOccur, PatternNode<TOffset> node)
        {
            _node = node;
            _minOccur = minOccur;
            _maxOccur = maxOccur;
        }

    	/// <summary>
    	/// Copy constructor.
    	/// </summary>
    	/// <param name="range">The range quantifier.</param>
    	public Quantifier(Quantifier<TOffset> range)
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
                return NodeType.Quantifier;
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

		internal override State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa, State<TOffset> startState)
		{
			State<TOffset> endState = _node.GenerateNfa(fsa, startState);

			if (_minOccur == 0 && _maxOccur == 1)
			{
				// optional
				startState.AddArc(new Arc<TOffset>(endState));
			}
			else if (_minOccur == 0 && _maxOccur == -1)
			{
				// kleene star
				startState.AddArc(new Arc<TOffset>(endState));
				endState.AddArc(new Arc<TOffset>(startState));
			}
			else if (_minOccur == 1 && _maxOccur == -1)
			{
				// plus
				endState.AddArc(new Arc<TOffset>(startState));
			}
			else
			{
				// range
				State<TOffset> currentState = startState;
				var startStates = new List<State<TOffset>>();
				if (_minOccur == 0)
				{
					startStates.Add(currentState);
				}
				else
				{
					for (int i = 1; i < _minOccur; i++)
					{
						currentState = endState;
						endState = _node.GenerateNfa(fsa, currentState);
					}
				}

				if (_maxOccur == -1)
				{
					endState.AddArc(new Arc<TOffset>(currentState));
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
						endState = _node.GenerateNfa(fsa, currentState);
					}
				}
				foreach (State<TOffset> state in startStates)
					state.AddArc(new Arc<TOffset>(endState));
			}

			return base.GenerateNfa(fsa, endState);
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
            return Equals(obj as Quantifier<TOffset>);
        }

        public bool Equals(Quantifier<TOffset> other)
        {
            if (other == null)
                return false;
            return _node.Equals(other._node) && _minOccur == other._minOccur
                && _maxOccur == other._maxOccur;
        }

        public override PatternNode<TOffset> Clone()
        {
            return new Quantifier<TOffset>(this);
        }
    }
}
