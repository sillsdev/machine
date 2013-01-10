using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Matching;

namespace SIL.Machine.Rules
{
	public class PatternRule<TData, TOffset> : IRule<TData, TOffset> where TData : IData<TOffset>, IDeepCloneable<TData>
	{
		private readonly SpanFactory<TOffset> _spanFactory;
		private readonly IPatternRuleSpec<TData, TOffset> _ruleSpec;
		private readonly Matcher<TData, TOffset> _matcher; 

		public PatternRule(SpanFactory<TOffset> spanFactory, IPatternRuleSpec<TData, TOffset> ruleSpec)
			: this(spanFactory, ruleSpec, new MatcherSettings<TOffset>())
		{
		}

		public PatternRule(SpanFactory<TOffset> spanFactory, IPatternRuleSpec<TData, TOffset> ruleSpec,
			MatcherSettings<TOffset> matcherSettings)
		{
			_spanFactory = spanFactory;
			_ruleSpec = ruleSpec;
			_matcher = new Matcher<TData, TOffset>(spanFactory, _ruleSpec.Pattern, matcherSettings);
		}

		public SpanFactory<TOffset> SpanFactory
		{
			get { return _spanFactory; }
		}

		public Matcher<TData, TOffset> Matcher
		{
			get { return _matcher; }
		}

		public IPatternRuleSpec<TData, TOffset> RuleSpec
		{
			get { return _ruleSpec; }
		}

		public IEnumerable<TData> Apply(TData input)
		{
			return Apply(input, input.Span.GetStart(_matcher.Direction));
		}

		public IEnumerable<TData> Apply(TData input, TOffset start)
		{
			if (!_ruleSpec.IsApplicable(input) || input.Annotations.Count == 0)
				return Enumerable.Empty<TData>();

			return ApplyImpl(input, start);
		}

		protected virtual IEnumerable<TData> ApplyImpl(TData input, TOffset start)
		{
			Match<TData, TOffset> match = _matcher.Match(input, start);
			if (match.Success)
			{
				TData outputData;
				_ruleSpec.ApplyRhs(this, match, out outputData);
				return outputData.ToEnumerable();
			}
			return Enumerable.Empty<TData>();
		}
	}
}
