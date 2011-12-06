using System;
using SIL.Machine.Matching;

namespace SIL.Machine.Transduction
{
	public class ActionPatternRuleBatch<TData, TOffset> : PatternRuleBatch<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly IPatternRuleAction<TData, TOffset> _rhs;

		public ActionPatternRuleBatch(SpanFactory<TOffset> spanFactory)
			: this(spanFactory, new NullPatternRuleAction<TData, TOffset>())
		{
			
		}

		public ActionPatternRuleBatch(SpanFactory<TOffset> spanFactory, ApplyDelegate<TData, TOffset> rhs)
			: this(spanFactory, rhs, ann => true)
		{
		}

		public ActionPatternRuleBatch(SpanFactory<TOffset> spanFactory, ApplyDelegate<TData, TOffset> rhs,
			Func<TData, bool> applicable)
			: this(spanFactory, new DelegatePatternRuleAction<TData, TOffset>(rhs, applicable))
		{
		}

		public ActionPatternRuleBatch(SpanFactory<TOffset> spanFactory, IPatternRuleAction<TData, TOffset> rhs)
			: base(spanFactory)
		{
			_rhs = rhs;
		}

		public IPatternRuleAction<TData, TOffset> Rhs
		{
			get { return _rhs; }
		}

		public void AddRule(PatternRule<TData, TOffset> rule)
		{
			InsertRuleInternal(Rules.Count, rule);
		}

		public void InsertRule(int index, PatternRule<TData, TOffset> rule)
		{
			InsertRuleInternal(index, rule);
		}

		public override bool IsApplicable(TData input)
		{
			return _rhs.IsApplicable(input);
		}

		public override Annotation<TOffset> ApplyRhs(TData input, PatternMatch<TOffset> match, out TData output)
		{
			Annotation<TOffset> last = base.ApplyRhs(input, match, out output);
			return Rhs.Apply(this, output, match, out output) ?? last;
		}
	}
}
