using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Rules
{
	public class MultiplePatternRule<TData, TOffset> : PatternRule<TData, TOffset> where TData : IAnnotatedData<TOffset>
	{
		public MultiplePatternRule(IPatternRuleSpec<TData, TOffset> ruleSpec)
			: base(ruleSpec)
		{
		}

		public MultiplePatternRule(IPatternRuleSpec<TData, TOffset> ruleSpec, MatcherSettings<TOffset> matcherSettings)
			: base(ruleSpec, matcherSettings)
		{
		}

		protected override IEnumerable<TData> ApplyImpl(TData input, TOffset start)
		{
			var results = new List<TData>();
			foreach (Match<TData, TOffset> match in Matcher.AllMatches(input, start))
				results.Add(RuleSpec.ApplyRhs(this, match));
			return results;
		}
	}
}
