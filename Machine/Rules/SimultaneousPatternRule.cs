using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Matching;

namespace SIL.Machine.Rules
{
	public class SimultaneousPatternRule<TData, TOffset> : PatternRule<TData, TOffset> where TData : IData<TOffset>
	{
		public SimultaneousPatternRule(SpanFactory<TOffset> spanFactory, IPatternRuleSpec<TData, TOffset> ruleSpec)
			: base(spanFactory, ruleSpec)
		{
		}

		public SimultaneousPatternRule(SpanFactory<TOffset> spanFactory, IPatternRuleSpec<TData, TOffset> ruleSpec, MatcherSettings<TOffset> matcherSettings)
			: base(spanFactory, ruleSpec, matcherSettings)
		{
		}

		protected override IEnumerable<TData> ApplyImpl(TData input, TOffset start)
		{
			TData data = input;
			foreach (Match<TData, TOffset> match in Matcher.AllMatches(input, start).ToArray())
				RuleSpec.ApplyRhs(this, match, out data);
			return data.ToEnumerable();
		}
	}
}
