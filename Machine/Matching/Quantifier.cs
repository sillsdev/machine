using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Fsa;

namespace SIL.Machine.Matching
{
    /// <summary>
    /// This class represents a nested phonetic pattern within another phonetic pattern.
    /// </summary>
	public class Quantifier<TData, TOffset> : PatternNode<TData, TOffset> where TData : IData<TOffset>
    {
    	public const int Infinite = -1;

    	private readonly int _minOccur;
    	private readonly int _maxOccur;

    	public Quantifier(int minOccur, int maxOccur)
			: this(minOccur, maxOccur, null)
		{
		}

    	/// <summary>
    	/// Initializes a new instance of the <see cref="Quantifier{TData, TOffset}"/> class.
    	/// </summary>
    	/// <param name="minOccur">The minimum number of occurrences.</param>
    	/// <param name="maxOccur">The maximum number of occurrences.</param>

    	/// <param name="node">The pattern node.</param>
		public Quantifier(int minOccur, int maxOccur, PatternNode<TData, TOffset> node)
			: base(node == null ? Enumerable.Empty<PatternNode<TData, TOffset>>() : node.ToEnumerable())
        {
    		IsGreedy = true;
    		_minOccur = minOccur;
            _maxOccur = maxOccur;
        }

		public Quantifier(Quantifier<TData, TOffset> quantifier)
			: base(quantifier)
        {
            _minOccur = quantifier._minOccur;
            _maxOccur = quantifier._maxOccur;
    		IsGreedy = quantifier.IsGreedy;
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

    	public bool IsGreedy { get; set; }

    	protected override bool CanAdd(PatternNode<TData, TOffset> child)
		{
			if (child is Expression<TData, TOffset>)
				return false;
			return true;
		}

		internal override State<TData, TOffset> GenerateNfa(FiniteStateAutomaton<TData, TOffset> fsa, State<TData, TOffset> startState)
		{
			ArcPriorityType priorityType = IsGreedy ? ArcPriorityType.High : ArcPriorityType.Low;
			State<TData, TOffset> endState;
			if (_minOccur == 0 && _maxOccur == 1)
			{
				// optional
				State<TData, TOffset> nextState = startState.AddArc(fsa.CreateState(), priorityType);
				endState = base.GenerateNfa(fsa, nextState);
				startState.AddArc(endState);
			}
			else if (_minOccur == 0 && _maxOccur == Infinite)
			{
				// kleene star
				State<TData, TOffset> nextState = startState.AddArc(fsa.CreateState(), priorityType);
				endState = base.GenerateNfa(fsa, nextState);
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
				State<TData, TOffset> currentState = startState;
				var startStates = new List<State<TData, TOffset>>();
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
				foreach (State<TData, TOffset> state in startStates)
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

		public override PatternNode<TData, TOffset> Clone()
        {
			return new Quantifier<TData, TOffset>(this);
        }
    }
}
