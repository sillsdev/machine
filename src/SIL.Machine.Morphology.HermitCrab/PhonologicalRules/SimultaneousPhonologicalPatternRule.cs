using System;
using System.Collections.Generic;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
	public class SimultaneousPhonologicalPatternRule : PhonologicalPatternRule
	{
		private readonly IPhonologicalPatternRuleSpec _ruleSpec;

		public SimultaneousPhonologicalPatternRule(SpanFactory<ShapeNode> spanFactory, IPhonologicalPatternRuleSpec ruleSpec, MatcherSettings<ShapeNode> matcherSettings)
			: base(spanFactory, ruleSpec, matcherSettings)
		{
			_ruleSpec = ruleSpec;
		}

		public override IEnumerable<Word> Apply(Word input)
		{
			var matches = new List<Tuple<Match<Word, ShapeNode>, PhonologicalSubruleMatch>>();
			foreach (Match<Word, ShapeNode> targetMatch in Matcher.AllMatches(input))
			{
				PhonologicalSubruleMatch srMatch;
				if (_ruleSpec.MatchSubrule(this, targetMatch, out srMatch))
					matches.Add(Tuple.Create(targetMatch, srMatch));
			}

			foreach (Tuple<Match<Word, ShapeNode>, PhonologicalSubruleMatch> match in matches)
				match.Item2.SubruleSpec.ApplyRhs(match.Item1, match.Item2.Span, match.Item2.VariableBindings);

			return input.ToEnumerable();
		}
	}
}
