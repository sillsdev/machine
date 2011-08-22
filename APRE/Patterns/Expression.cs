using System.Collections.Generic;
using SIL.APRE.Fsa;

namespace SIL.APRE.Patterns
{
	public class Expression<TOffset> : PatternNode<TOffset>
	{
		public const string EntireGroupName = "*entire*";

		internal Expression()
		{
		}

		internal Expression(IEnumerable<PatternNode<TOffset>> children)
			: base(children)
		{
		}

		public Expression(Expression<TOffset> expr)
			: base(expr)
		{
		}

		public override PatternNodeType Type
		{
			get { return PatternNodeType.Expression; }
		}

		internal override State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa, State<TOffset> startState)
		{
			startState = fsa.CreateGroupTag(startState, EntireGroupName, true);
			State<TOffset> endState = base.GenerateNfa(fsa, startState);
			endState = fsa.CreateGroupTag(endState, EntireGroupName, false);
			endState.AddArc(new Arc<TOffset>(fsa.CreateState(true)));
			return null;
		}

		public override PatternNode<TOffset> Clone()
		{
			return new Expression<TOffset>(this);
		}
	}
}
