using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Matching;

namespace SIL.Machine.Transduction
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
			_ruleSpec = ruleSpec;
			_appMode = appMode;
			_matcher = new Matcher<TData, TOffset>(spanFactory, _ruleSpec.Pattern, matcherSettings);
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

		public virtual bool Apply(TData input, out IEnumerable<TData> output)
		{
			if (input.Annotations.Count == 0 || !IsApplicable(input))
			{
				output = null;
				return false;
			}

			List<TData> outputList = null;
			switch (ApplicationMode)
			{
				case ApplicationMode.Simultaneous:
					{
						TData data = input;
						Match<TData, TOffset>[] matches = _matcher.Matches(input).ToArray();
						if (matches.Length > 0)
						{
							foreach (Match<TData, TOffset> match in matches)
								_ruleSpec.ApplyRhs(this, match, out data);
							outputList = new List<TData> {data};
						}
					}
					break;

				case ApplicationMode.Iterative:
					{
						bool applied = false;
						TData data = input;
						Match<TData, TOffset> match = _matcher.Match(input);
						while (match.Success)
						{
							TOffset nextOffset = _ruleSpec.ApplyRhs(this, match, out data);
							applied = true;
							match = _matcher.Match(data, nextOffset);
						}

						if (applied)
							outputList = new List<TData> {data};
					}
					break;

				case ApplicationMode.Single:
					{
						Match<TData, TOffset> match = _matcher.Match(input);
						if (match.Success)
						{
							TData outputData;
							_ruleSpec.ApplyRhs(this, match, out outputData);
							outputList = new List<TData> {outputData};
						}
					}
					break;

				case ApplicationMode.Multiple:
					{
						foreach (Match<TData, TOffset> match in _matcher.Matches(input))
						{
							TData outputData;
							_ruleSpec.ApplyRhs(this, match, out outputData);
							if (outputList == null)
								outputList = new List<TData>();
							outputList.Add(outputData);
						}
					}
					break;
			}

			output = outputList;
			return output != null;
		}
	}
}
