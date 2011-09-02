using System;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public class PatternRule<TOffset> : PatternRuleBase<TOffset>
	{
		private readonly IPatternRuleAction<TOffset> _rhs;

		public PatternRule(Pattern<TOffset> lhs, Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> rhs)
			: this(lhs, rhs, null)
		{
		}

		public PatternRule(Pattern<TOffset> lhs, Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> rhs,
			Func<IBidirList<Annotation<TOffset>>, bool> applicable)
			: this(lhs, rhs, applicable, false)
		{
		}

		public PatternRule(Pattern<TOffset> lhs, Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> rhs,
			Func<IBidirList<Annotation<TOffset>>, bool> applicable, bool simultaneous)
			: this(lhs, new DelegatePatternRuleAction<TOffset>(rhs, applicable), simultaneous)
		{
		}

		public PatternRule(Pattern<TOffset> lhs, IPatternRuleAction<TOffset> rhs)
			: this(lhs, rhs, false)
		{
		}

		public PatternRule(Pattern<TOffset> lhs, IPatternRuleAction<TOffset> rhs, bool simult)
			: base(lhs, simult)
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
			if (_rhs == null)
				return input.GetView(match).GetLast(Lhs.Direction);
			return _rhs.Apply(Lhs, input, match);
		}
	}
}
