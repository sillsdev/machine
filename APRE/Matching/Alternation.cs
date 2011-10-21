using System.Collections.Generic;
using System.Text;
using SIL.APRE.Fsa;

namespace SIL.APRE.Matching
{
	public class Alternation<TData, TOffset> : PatternNode<TData, TOffset> where TData : IData<TOffset>
	{
		public Alternation()
		{
		}

		public Alternation(IEnumerable<PatternNode<TData, TOffset>> nodes)
			: base(nodes)
		{
		}

		public Alternation(Alternation<TData, TOffset> alternation)
			: base(alternation)
		{
		}

		internal override State<TData, TOffset> GenerateNfa(FiniteStateAutomaton<TData, TOffset> fsa, State<TData, TOffset> startState)
		{
			if (IsLeaf)
				return startState;

			State<TData, TOffset> endState = fsa.CreateState();
			foreach (PatternNode<TData, TOffset> node in Children)
			{
				State<TData, TOffset> nodeEndState = node.GenerateNfa(fsa, startState);
				nodeEndState.AddArc(endState);
			}
			return endState;
		}

		public override PatternNode<TData, TOffset> Clone()
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
			return sb.ToString();
		}

		public override int GetHashCode()
		{
			return Children.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Alternation<TData, TOffset>);
		}

		public bool Equals(Alternation<TData, TOffset> other)
		{
			if (other == null)
				return false;
			return Children.Equals(other.Children);
		}
	}
}
