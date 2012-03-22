using System.Collections.Generic;
using System.Text;
using SIL.Collections;
using SIL.Machine.Fsa;

namespace SIL.Machine.Matching
{
	public class Alternation<TData, TOffset> : PatternNode<TData, TOffset>, IDeepCloneable<Alternation<TData, TOffset>> where TData : IData<TOffset>
	{
		public Alternation()
		{
		}

		public Alternation(IEnumerable<PatternNode<TData, TOffset>> nodes)
			: base(nodes)
		{
		}

		protected Alternation(Alternation<TData, TOffset> alternation)
			: base(alternation)
		{
		}

		internal override State<TData, TOffset> GenerateNfa(FiniteStateAutomaton<TData, TOffset> fsa, State<TData, TOffset> startState, out bool hasVariables)
		{
			hasVariables = false;
			if (IsLeaf)
				return startState;

			State<TData, TOffset> endState = fsa.CreateState();
			foreach (PatternNode<TData, TOffset> node in Children)
			{
				bool childHasVariables;
				State<TData, TOffset> nodeEndState = node.GenerateNfa(fsa, startState, out childHasVariables);
				if (childHasVariables)
					hasVariables = true;
				nodeEndState.AddArc(endState);
			}
			return endState;
		}

		protected override PatternNode<TData, TOffset> DeepCloneImpl()
		{
			return DeepClone();
		}

		public new Alternation<TData, TOffset> DeepClone()
		{
			return new Alternation<TData, TOffset>(this);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append("(");
			bool first = true;
			foreach (PatternNode<TData, TOffset> node in Children)
			{
				if (!first)
					sb.Append("|");
				sb.Append(node);
				first = false;
			}
			sb.Append(")");
			return sb.ToString();
		}
	}
}
