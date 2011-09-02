using System.Collections.Generic;
using System.Text;
using SIL.APRE.Fsa;

namespace SIL.APRE.Matching
{
	public class Alternation<TOffset> : PatternNode<TOffset>
	{
		public Alternation()
		{
		}

		public Alternation(IEnumerable<PatternNode<TOffset>> nodes)
			: base(nodes)
		{
		}

		public Alternation(params PatternNode<TOffset>[] nodes)
			: base(nodes)
		{
		}

		public Alternation(Alternation<TOffset> alternation)
			: base(alternation)
		{
		}

		internal override State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa, State<TOffset> startState)
		{
			if (IsLeaf)
				return startState;

			State<TOffset> endState = fsa.CreateState();
			foreach (PatternNode<TOffset> node in Children)
			{
				State<TOffset> nodeEndState = node.GenerateNfa(fsa, startState);
				nodeEndState.AddArc(new Arc<TOffset>(endState));
			}
			return endState;
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
			foreach (PatternNode<TOffset> node in Children)
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
			return Equals(obj as Alternation<TOffset>);
		}

		public bool Equals(Alternation<TOffset> other)
		{
			if (other == null)
				return false;
			return Children.Equals(other.Children);
		}
	}
}
