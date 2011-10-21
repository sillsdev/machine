using System;
using System.Collections.Generic;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public class PatternRuleBatch<TData, TOffset> : PatternRuleBatchBase<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly IPatternRuleAction<TData, TOffset> _rhs;

		public PatternRuleBatch(Pattern<TData, TOffset> pattern)
			: this(pattern, new NullPatternRuleAction<TData, TOffset>())
		{
		}

		public PatternRuleBatch(Pattern<TData, TOffset> pattern, ApplyDelegate<TData, TOffset> rhs)
			: this(pattern, rhs, ann => true)
		{
		}

		public PatternRuleBatch(Pattern<TData, TOffset> pattern, ApplyDelegate<TData, TOffset> rhs,
			Func<TData, bool> applicable)
			: this(pattern, new DelegatePatternRuleAction<TData, TOffset>(rhs, applicable))
		{
		}

		public PatternRuleBatch(Pattern<TData, TOffset> pattern, IPatternRuleAction<TData, TOffset> rhs)
			: base(pattern)
		{
			_rhs = rhs;
		}

		public PatternRuleBatch(IEnumerable<IPatternRule<TData, TOffset>> rules)
			: this(rules, new NullPatternRuleAction<TData, TOffset>())
		{
		}

		public PatternRuleBatch(IEnumerable<IPatternRule<TData, TOffset>> rules, ApplyDelegate<TData, TOffset> rhs)
			: this(rules, rhs, ann => true)
		{
		}

		public PatternRuleBatch(IEnumerable<IPatternRule<TData, TOffset>> rules, ApplyDelegate<TData, TOffset> rhs,
			Func<TData, bool> applicable)
			: this(rules, new DelegatePatternRuleAction<TData, TOffset>(rhs, applicable))
		{
		}

		public PatternRuleBatch(IEnumerable<IPatternRule<TData, TOffset>> rules, IPatternRuleAction<TData, TOffset> rhs)
			: base(rules)
		{
			_rhs = rhs;
		}

		public IPatternRuleAction<TData, TOffset> Rhs
		{
			get { return _rhs; }
		}

		public void AddRule(IPatternRule<TData, TOffset> rule)
		{
			AddRuleInternal(rule, true);
		}

		public override bool IsApplicable(TData input)
		{
			return _rhs.IsApplicable(input);
		}

		public override Annotation<TOffset> ApplyRhs(TData input, PatternMatch<TOffset> match, out TData output)
		{
			Annotation<TOffset> last = base.ApplyRhs(input, match, out output);
			return Rhs.Apply(output, match, out output) ?? last;
		}
	}
}
