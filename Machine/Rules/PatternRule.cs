using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Matching;

namespace SIL.Machine.Rules
{
	public enum ApplicationMode
	{
		Single,
		Multiple,
		Iterative,
		Simultaneous
	}

	public class PatternRule<TData, TOffset> : IRule<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly SpanFactory<TOffset> _spanFactory;
		private readonly IPatternRuleSpec<TData, TOffset> _ruleSpec;
		private readonly Matcher<TData, TOffset> _matcher; 
		private readonly ApplicationMode _appMode;

		public PatternRule(SpanFactory<TOffset> spanFactory, IPatternRuleSpec<TData, TOffset> ruleSpec)
			: this(spanFactory, ruleSpec, ApplicationMode.Single)
		{
		}

		public PatternRule(SpanFactory<TOffset> spanFactory, IPatternRuleSpec<TData, TOffset> ruleSpec,
			ApplicationMode appMode)
			: this(spanFactory, ruleSpec, appMode, new MatcherSettings<TOffset>())
		{
		}

		public PatternRule(SpanFactory<TOffset> spanFactory, IPatternRuleSpec<TData, TOffset> ruleSpec,
			MatcherSettings<TOffset> matcherSettings)
			: this(spanFactory, ruleSpec, ApplicationMode.Single, matcherSettings)
		{
		}

		public PatternRule(SpanFactory<TOffset> spanFactory, IPatternRuleSpec<TData, TOffset> ruleSpec,
			ApplicationMode appMode, MatcherSettings<TOffset> matcherSettings)
		{
			_spanFactory = spanFactory;
			_ruleSpec = ruleSpec;
			_appMode = appMode;
			_matcher = new Matcher<TData, TOffset>(spanFactory, _ruleSpec.Pattern, matcherSettings);
		}

		public SpanFactory<TOffset> SpanFactory
		{
			get { return _spanFactory; }
		}

		public IPatternRuleSpec<TData, TOffset> RuleSpec
		{
			get { return _ruleSpec; }
		}

		public ApplicationMode ApplicationMode
		{
			get { return _appMode; }
		}

		public MatcherSettings<TOffset> MatcherSettings
		{
			get { return _matcher.Settings; }
		}

		public bool IsApplicable(TData input)
		{
			return _ruleSpec.IsApplicable(input);
		}

		public IEnumerable<TData> Apply(TData input)
		{
			return Apply(input, input.Span.GetStart(_matcher.Direction));
		}

		public virtual IEnumerable<TData> Apply(TData input, TOffset start)
		{
			if (input.Annotations.Count == 0)
				return Enumerable.Empty<TData>();

			switch (ApplicationMode)
			{
				case ApplicationMode.Simultaneous:
					{
						TData data = input;
						foreach (Match<TData, TOffset> match in _matcher.AllMatches(input, start).ToArray())
							_ruleSpec.ApplyRhs(this, match, out data);
						return data.ToEnumerable();
					}

				case ApplicationMode.Iterative:
					{
						bool applied = false;
						TData data = input;
						Match<TData, TOffset> match = _matcher.Match(input, start);
						while (match.Success)
						{
							TOffset nextOffset = _ruleSpec.ApplyRhs(this, match, out data);
							applied = true;
							match = _matcher.Match(data, nextOffset);
						}

						if (applied)
							return data.ToEnumerable();
					}
					break;

				case ApplicationMode.Single:
					{
						Match<TData, TOffset> match = _matcher.Match(input, start);
						if (match.Success)
						{
							TData outputData;
							_ruleSpec.ApplyRhs(this, match, out outputData);
							return outputData.ToEnumerable();
						}
					}
					break;

				case ApplicationMode.Multiple:
					{
						var results = new List<TData>();
						foreach (Match<TData, TOffset> match in _matcher.AllMatches(input, start))
						{
							TData outputData;
							_ruleSpec.ApplyRhs(this, match, out outputData);
							results.Add(outputData);
						}
						return results;
					}
			}

			return Enumerable.Empty<TData>();
		}
	}
}
