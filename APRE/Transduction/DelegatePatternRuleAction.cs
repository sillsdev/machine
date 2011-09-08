using System;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public class DelegatePatternRuleAction<TOffset> : IPatternRuleAction<TOffset>
	{
		private readonly Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> _action;
		private readonly Func<IBidirList<Annotation<TOffset>>, bool> _applicable;

		public DelegatePatternRuleAction(Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> action)
			: this(action, ann => true)
		{
		}

		public DelegatePatternRuleAction(Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> action,
			Func<IBidirList<Annotation<TOffset>>, bool> applicable)
		{
			_action = action;
			_applicable = applicable;
		}

		public bool IsApplicable(IBidirList<Annotation<TOffset>> input)
		{
			return _applicable(input);
		}

		public Annotation<TOffset> Apply(Pattern<TOffset> lhs, IBidirList<Annotation<TOffset>> input, PatternMatch<TOffset> match)
		{
			return _action(lhs, input, match);
		}
	}
}
