using System;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public class PatternRule<TData, TOffset> : PatternRuleBase<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly IPatternRuleAction<TData, TOffset> _rhs;

		public PatternRule(Pattern<TData, TOffset> lhs, ApplyDelegate<TData, TOffset> rhs)
			: this(lhs, rhs, input => true)
		{
		}

		public PatternRule(Pattern<TData, TOffset> lhs, ApplyDelegate<TData, TOffset> rhs,
			Func<TData, bool> applicable)
			: this(lhs, new DelegatePatternRuleAction<TData, TOffset>(rhs, applicable))
		{
		}

		public PatternRule(Pattern<TData, TOffset> lhs, IPatternRuleAction<TData, TOffset> rhs)
			: base(lhs)
		{
			_rhs = rhs;
		}

		public IPatternRuleAction<TData, TOffset> Rhs
		{
			get { return _rhs; }
		}

		public override bool IsApplicable(TData input)
		{
			return _rhs.IsApplicable(input);
		}

		public override Annotation<TOffset> ApplyRhs(TData input, PatternMatch<TOffset> match, out TData output)
		{
			return _rhs.Apply(input, match, out output);
		}
	}
}
