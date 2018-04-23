using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
	public class AnalysisMetathesisRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly MetathesisRule _rule;
		private readonly PhonologicalPatternRule _patternRule; 

		public AnalysisMetathesisRule(Morpher morpher, MetathesisRule rule)
		{
			_morpher = morpher;
			_rule = rule;

			var ruleSpec = new AnalysisMetathesisRuleSpec(rule.Pattern, rule.LeftSwitchName, rule.RightSwitchName);

			var settings = new MatcherSettings<ShapeNode>
			{
			    Direction = rule.Direction == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight,
			    Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Anchor),
				MatchingMethod = MatchingMethod.Unification,
				UseDefaults = true,
				// during analysis shape nodes can have features that are underspecified,
				// so this must be non-deterministic
				Nondeterministic = true
			};

			_patternRule = new IterativePhonologicalPatternRule(ruleSpec, settings);
		}

		public IEnumerable<Word> Apply(Word input)
		{
			if (!_morpher.RuleSelector(_rule))
				return Enumerable.Empty<Word>();

			Word origInput = null;
			if (_morpher.TraceManager.IsTracing)
				origInput = input.Clone();

			if (_patternRule.Apply(input).Any())
			{
				if (_morpher.TraceManager.IsTracing)
					_morpher.TraceManager.PhonologicalRuleUnapplied(_rule, -1, origInput, input);
				return input.ToEnumerable();
			}

			if (_morpher.TraceManager.IsTracing)
				_morpher.TraceManager.PhonologicalRuleNotUnapplied(_rule, -1, input);
			return Enumerable.Empty<Word>();
		}
	}
}
