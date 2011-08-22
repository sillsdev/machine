using System;
using SIL.APRE.Patterns;

namespace SIL.APRE.Rules
{
	public class DelegateRuleAction<TOffset> : IRuleAction<TOffset>
	{
		private readonly Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> _action;

		public DelegateRuleAction(Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> action)
		{
			_action = action;
		}

		public Annotation<TOffset> Run(Pattern<TOffset> lhs, IBidirList<Annotation<TOffset>> input, PatternMatch<TOffset> match)
		{
			return _action(lhs, input, match);
		}
	}
}
