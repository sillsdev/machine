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

    	private readonly bool _greedy;

		public Quantifier(int minOccur, int maxOccur)
			: this(minOccur, maxOccur, true)
		{
		}

		public Quantifier(int minOccur, int maxOccur, bool greedy)
		{
			_minOccur = minOccur;
			_maxOccur = maxOccur;
			_greedy = greedy;
		}

		public Quantifier(int minOccur, int maxOccur, PatternNode<TOffset> node)
			: this(minOccur, maxOccur, true, node)
		{
		}

    	/// <summary>
    	/// Initializes a new instance of the <see cref="Quantifier{TOffset}"/> class.
    	/// </summary>
    	/// <param name="minOccur">The minimum number of occurrences.</param>
    	/// <param name="maxOccur">The maximum number of occurrences.</param>
    	/// <param name="greedy"></param>
    	/// <param name="node">The pattern node.</param>
    	public Quantifier(int minOccur, int maxOccur, bool greedy, PatternNode<TOffset> node)
			: base(node.ToEnumerable())
        {
            _minOccur = minOccur;
            _maxOccur = maxOccur;
    		_greedy = greedy;
        }

    	public Quantifier(Quantifier<TOffset> quantifier)
			: base(quantifier)
        {
            _minOccur = quantifier._minOccur;
            _maxOccur = quantifier._maxOccur;
    		_greedy = quantifier._greedy;
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

    	public bool IsGreedy
    	{
    		get { return _greedy; }
    	}

		protected override bool CanAdd(PatternNode<TOffset> child)
		{
			if (child is Expression<TOffset>)
				return false;
			return true;
		}

		internal override State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa, State<TOffset> startState)
		{
			PriorityType priorityType = _greedy ? PriorityType.High : PriorityType.Low;
			State<TOffset> endState;
			if (_minOccur == 0 && _maxOccur == 1)
			{
				// optional
				startState = startState.AddArc(fsa.CreateState(), priorityType);
				endState = base.GenerateNfa(fsa, startState);
				startState.AddArc(endState);
			}
			else if (_minOccur == 0 && _maxOccur == Infinite)
			{
				// kleene star
				startState = startState.AddArc(fsa.CreateState(), priorityType);
				endState = base.GenerateNfa(fsa, startState);
				startState.AddArc(endState);
				endState.AddArc(startState, priorityType);
			}
			else if (_minOccur == 1 && _maxOccur == Infinite)
			{
				// plus
				endState = base.GenerateNfa(fsa, startState);
				endState.AddArc(startState, priorityType);
			}
			else
			{
				// range
				State<TOffset> currentState = startState;
				var startStates = new List<State<TOffset>>();
				if (_minOccur == 0)
				{
					endState = startState.AddArc(fsa.CreateState(), priorityType);
					endState = base.GenerateNfa(fsa, endState);
					startStates.Add(currentState);
				}
				else
				{
					endState = startState;
					for (int i = 0; i < _minOccur; i++)
					{
						currentState = endState;
						endState = base.GenerateNfa(fsa, currentState);
					}
				}

				if (_maxOccur == Infinite)
				{
					endState.AddArc(currentState, priorityType);
				}
				else
				{
					int numCopies = _maxOccur - _minOccur;
					if (_minOccur == 0)
						numCopies--;
					for (int i = 1; i <= numCopies; i++)
					{
						startStates.Add(endState);
						endState = endState.AddArc(fsa.CreateState(), priorityType);
						endState = base.GenerateNfa(fsa, endState);
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
