using System;
using System.Collections.Generic;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public class PatternRuleBatch<TOffset> : PatternRuleBatchBase<TOffset>
	{
		private readonly IPatternRuleAction<TOffset> _rhs;

		public PatternRuleBatch(Pattern<TOffset> pattern)
			: this(pattern, false)
		{
		}

		public PatternRuleBatch(Pattern<TOffset> pattern, bool simult)
			: this(pattern, new NullPatternRuleAction<TOffset>(), simult)
		{
		}

		public PatternRuleBatch(Pattern<TOffset> pattern, Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> rhs)
			: this(pattern, rhs, ann => true)
		{
		}

		public PatternRuleBatch(Pattern<TOffset> pattern, Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> rhs,
			Func<IBidirList<Annotation<TOffset>>, bool> applicable)
			: this(pattern, rhs, applicable, false)
		{
		}

		public PatternRuleBatch(Pattern<TOffset> pattern, Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> rhs,
			Func<IBidirList<Annotation<TOffset>>, bool> applicable, bool simult)
			: this(pattern, new DelegatePatternRuleAction<TOffset>(rhs, applicable), simult)
		{
		}

		public PatternRuleBatch(Pattern<TOffset> pattern, IPatternRuleAction<TOffset> rhs)
			: this(pattern, rhs, false)
		{
		}

		public PatternRuleBatch(Pattern<TOffset> pattern, IPatternRuleAction<TOffset> rhs, bool simult)
			: base(pattern, simult)
        {
        	_rhs = rhs;
        }

		public PatternRuleBatch(IEnumerable<IPatternRule<TOffset>> rules)
			: this(rules, new NullPatternRuleAction<TOffset>())
		{
		}

		public PatternRuleBatch(IEnumerable<IPatternRule<TOffset>> rules, Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> rhs)
			: this(rules, rhs, ann => true)
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

		public void AddRule(IPatternRule<TOffset> rule)
		{
			AddRuleInternal(rule);
		}

		public override bool IsApplicable(IBidirList<Annotation<TOffset>> input)
		{
			return _rhs.IsApplicable(input);
		}

		public override Annotation<TOffset> ApplyRhs(IBidirList<Annotation<TOffset>> input, PatternMatch<TOffset> match)
		{
			Annotation<TOffset> last = base.ApplyRhs(input, match);
			return Rhs.Apply(Lhs, input, match) ?? last;
		}
	}
}
