using System;
using System.Collections.Generic;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public class PatternRuleBatch<TOffset> : PatternRuleBatchBase<TOffset>
	{
		private readonly IPatternRuleAction<TOffset> _rhs;

		public PatternRuleBatch(IEnumerable<IPatternRule<TOffset>> rules)
			: this(rules, (IPatternRuleAction<TOffset>) null)
		{
		}

		public PatternRuleBatch(IEnumerable<IPatternRule<TOffset>> rules, Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> rhs)
			: this(rules, rhs, null)
		{
		}

		public PatternRuleBatch(IEnumerable<IPatternRule<TOffset>> rules, Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> rhs,
			Func<IBidirList<Annotation<TOffset>>, bool> applicable)
			: this(rules, new DelegatePatternRuleAction<TOffset>(rhs, applicable))
		{
		}

		public PatternRuleBatch(IEnumerable<IPatternRule<TOffset>> rules, IPatternRuleAction<TOffset> rhs)
			: base(rules)
		{
			_rhs = rhs;
		}

		public IPatternRuleAction<TOffset> Rhs
		{
			get { return _rhs; }
		}

		public override bool IsApplicable(IBidirList<Annotation<TOffset>> input)
		{
			return _rhs == null || _rhs.IsApplicable(input);
		}

		public override Annotation<TOffset> ApplyRhs(IBidirList<Annotation<TOffset>> input, PatternMatch<TOffset> match)
		{
			Annotation<TOffset> last = base.ApplyRhs(input, match);
			if (Rhs == null)
				return last;
			return Rhs.Apply(Lhs, input, match);
		}
	}
}
