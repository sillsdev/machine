using System.Collections.Generic;
using SIL.APRE.Fsa;

namespace SIL.APRE.Matching
{
    /// <summary>
    /// This class represents a nested phonetic pattern within another phonetic pattern.
    /// </summary>
    public class Quantifier<TOffset> : PatternNode<TOffset>
    {
    	public const int Infinite = -1;

    	private readonly int _minOccur;
    	private readonly int _maxOccur;

		public Quantifier(int minOccur, int maxOccur)
		{
			_minOccur = minOccur;
			_maxOccur = maxOccur;
		}

    	/// <summary>
		/// Initializes a new instance of the <see cref="Quantifier{TOffset}"/> class.
    	/// </summary>
    	/// <param name="minOccur">The minimum number of occurrences.</param>
    	/// <param name="maxOccur">The maximum number of occurrences.</param>
		/// <param name="node">The pattern node.</param>
		public Quantifier(int minOccur, int maxOccur, PatternNode<TOffset> node)
			: base(node.ToEnumerable())
        {
            _minOccur = minOccur;
            _maxOccur = maxOccur;
        }

    	public Quantifier(Quantifier<TOffset> quantifier)
			: base(quantifier)
        {
            _minOccur = quantifier._minOccur;
            _maxOccur = quantifier._maxOccur;
        }

        /// <summary>
        /// Gets the minimum number of occurrences of this pattern.
        /// </summary>
        /// <value>The minimum number of occurrences.</value>
        public int MinOccur
        {
			get { return _minOccur; }
        }

    	/// <summary>
    	/// Gets the maximum number of occurrences of this pattern.
    	/// </summary>
    	/// <value>The maximum number of occurrences.</value>
    	public int MaxOccur
    	{
    		get { return _maxOccur; }
    	}

		protected override bool CanAdd(PatternNode<TOffset> child)
		{
			if (child is Expression<TOffset>)
				return false;
			return true;
		}

		internal override State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa, State<TOffset> startState)
		{
			State<TOffset> endState = base.GenerateNfa(fsa, startState);

			if (_minOccur == 0 && _maxOccur == 1)
			{
				// optional
				startState.AddArc(endState);
			}
			else if (_minOccur == 0 && _maxOccur == Infinite)
			{
				// kleene star
				startState.AddArc(endState);
				endState.AddArc(startState);
			}
			else if (_minOccur == 1 && _maxOccur == Infinite)
			{
				// plus
				endState.AddArc(startState);
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
						endState = base.GenerateNfa(fsa, currentState);
					}
				}

				if (_maxOccur == Infinite)
				{
					endState.AddArc(currentState);
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
						endState = base.GenerateNfa(fsa, currentState);
					}
				}
				foreach (State<TOffset> state in startStates)
					state.AddArc(endState);
			}

			return endState;
		}

        public override string ToString()
        {
        	string quantifierStr;
			if (_minOccur == 0 && _maxOccur == Infinite)
				quantifierStr = "*";
			else if (_minOccur == 1 && _maxOccur == Infinite)
				quantifierStr = "+";
			else if (_minOccur == 0 && _maxOccur == 1)
				quantifierStr = "?";
			else if (_maxOccur == Infinite)
				quantifierStr = string.Format("[{0},]", _minOccur);
			else
				quantifierStr = string.Format("[{0},{1}]", _minOccur, _maxOccur);
        	return Children + quantifierStr;
        }

        public override int GetHashCode()
        {
			return Children.GetHashCode() ^ _minOccur ^ _maxOccur;
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
			return Children.Equals(other.Children) && _minOccur == other._minOccur
                && _maxOccur == other._maxOccur;
        }

        public override PatternNode<TOffset> Clone()
        {
            return new Quantifier<TOffset>(this);
        }
    }
}
