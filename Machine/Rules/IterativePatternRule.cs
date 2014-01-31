using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Rules
{
	public class IterativePatternRule<TData, TOffset> : PatternRule<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		public IterativePatternRule(SpanFactory<TOffset> spanFactory, IPatternRuleSpec<TData, TOffset> ruleSpec)
			: base(spanFactory, ruleSpec)
		{
		}

		public IterativePatternRule(SpanFactory<TOffset> spanFactory, IPatternRuleSpec<TData, TOffset> ruleSpec, MatcherSettings<TOffset> matcherSettings)
			: base(spanFactory, ruleSpec, matcherSettings)
		{
		}

		protected override IEnumerable<TData> ApplyImpl(TData input, TOffset start)
		{
			bool applied = false;
			TData data = input;
			Match<TData, TOffset> match = Matcher.Match(input, start);
			while (match.Success)
			{
				TOffset nextOffset = RuleSpec.ApplyRhs(this, match, out data);
				applied = true;
				match = Matcher.Match(data, nextOffset);
			}

			if (applied)
				return data.ToEnumerable();
			return Enumerable.Empty<TData>();
		}
	}
}
