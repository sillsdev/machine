using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Fsa;

namespace SIL.Machine.Matching
{
    /// <summary>
    /// This class represents a nested phonetic pattern within another phonetic pattern.
    /// </summary>
	public class Quantifier<TData, TOffset> : PatternNode<TData, TOffset>, IDeepCloneable<Quantifier<TData, TOffset>> where TData : IData<TOffset>
    {
    	public const int Infinite = -1;

		public Quantifier()
			: this(0, Infinite)
		{
		}

		public Quantifier(PatternNode<TData, TOffset> node)
			: this(0, Infinite, node)
		{
		}

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
    		MinOccur = minOccur;
            MaxOccur = maxOccur;
        }

		protected Quantifier(Quantifier<TData, TOffset> quantifier)
			: base(quantifier)
        {
            MinOccur = quantifier.MinOccur;
            MaxOccur = quantifier.MaxOccur;
    		IsGreedy = quantifier.IsGreedy;
        }

    	/// <summary>
    	/// Gets the minimum number of occurrences of this pattern.
    	/// </summary>
    	/// <value>The minimum number of occurrences.</value>
		public int MinOccur { get; set; }

    	/// <summary>
    	/// Gets the maximum number of occurrences of this pattern.
    	/// </summary>
    	/// <value>The maximum number of occurrences.</value>
		public int MaxOccur { get; set; }

    	public bool IsGreedy { get; set; }

    	protected override bool CanAdd(PatternNode<TData, TOffset> child)
		{
			if (child is Pattern<TData, TOffset>)
				return false;
			return true;
		}

		internal override State<TData, TOffset> GenerateNfa(FiniteStateAutomaton<TData, TOffset> fsa, State<TData, TOffset> startState, out bool hasVariables)
		{
			hasVariables = false;
			ArcPriorityType priorityType = IsGreedy ? ArcPriorityType.High : ArcPriorityType.Low;
			State<TData, TOffset> endState;
			State<TData, TOffset> currentState = startState;
			var startStates = new List<State<TData, TOffset>>();
			if (MinOccur == 0)
			{
				endState = startState.AddArc(fsa.CreateState(), priorityType);
				endState = base.GenerateNfa(fsa, endState, out hasVariables);
				startStates.Add(currentState);
			}
			else
			{
				endState = startState;
				for (int i = 0; i < MinOccur; i++)
				{
					currentState = endState;
					endState = base.GenerateNfa(fsa, currentState, out hasVariables);
				}
			}

			if (MaxOccur == Infinite)
			{
				endState.AddArc(currentState, priorityType);
			}
			else
			{
				int numCopies = MaxOccur - MinOccur;
				if (MinOccur == 0)
					numCopies--;
				for (int i = 1; i <= numCopies; i++)
				{
					startStates.Add(endState);
					endState = endState.AddArc(fsa.CreateState(), priorityType);
					endState = base.GenerateNfa(fsa, endState, out hasVariables);
				}
			}
			foreach (State<TData, TOffset> state in startStates)
				state.AddArc(endState);

			return endState;
		}

    	public new Quantifier<TData, TOffset> DeepClone()
    	{
			return new Quantifier<TData, TOffset>(this);
    	}

		protected override PatternNode<TData, TOffset> DeepCloneImpl()
		{
			return DeepClone();
		}

    	public override string ToString()
        {
        	string quantifierStr;
			if (MinOccur == 0 && MaxOccur == Infinite)
				quantifierStr = "*";
			else if (MinOccur == 1 && MaxOccur == Infinite)
				quantifierStr = "+";
			else if (MinOccur == 0 && MaxOccur == 1)
				quantifierStr = "?";
			else if (MaxOccur == Infinite)
				quantifierStr = string.Format("[{0},]", MinOccur);
			else
				quantifierStr = string.Format("[{0},{1}]", MinOccur, MaxOccur);
        	return string.Concat(Children) + quantifierStr;
        }
    }
}
