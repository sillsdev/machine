using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class SynthesisRewriteRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly RewriteRule _rule;
		private readonly PatternRule<Word, ShapeNode> _patternRule; 

		public SynthesisRewriteRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, RewriteRule rule)
		{
			_morpher = morpher;
			_rule = rule;

			var ruleSpec = new BatchPatternRuleSpec<Word, ShapeNode>();
			foreach (RewriteSubrule sr in rule.Subrules)
			{
				if (rule.Lhs.Children.Count == sr.Rhs.Children.Count)
					ruleSpec.RuleSpecs.Add(new FeatureSynthesisRewriteRuleSpec(rule.Lhs, sr));
				else if (rule.Lhs.Children.Count > sr.Rhs.Children.Count)
					ruleSpec.RuleSpecs.Add(new NarrowSynthesisRewriteRuleSpec(rule.Lhs, sr));
				else if (rule.Lhs.Children.Count == 0)
					ruleSpec.RuleSpecs.Add(new EpenthesisSynthesisRewriteRuleSpec(rule.Lhs, sr));
			}

			var settings = new MatcherSettings<ShapeNode>
			               	{
			               		Direction = rule.Direction,
			               		Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary, HCFeatureSystem.Anchor) && !ann.IsDeleted(),
			               		UseDefaults = true
			               	};

			_patternRule = null;
			switch (rule.ApplicationMode)
			{
				case RewriteApplicationMode.Iterative:
					_patternRule = new BacktrackingPatternRule(spanFactory, ruleSpec, settings);
					break;

				case RewriteApplicationMode.Simultaneous:
					_patternRule = new SimultaneousPatternRule<Word, ShapeNode>(spanFactory, ruleSpec, settings);
					break;
			}
		}

		public IEnumerable<Word> Apply(Word input)
		{
			Trace trace = null;
			if (_morpher.TraceRules.Contains(_rule))
			{
				trace = new Trace(TraceType.PhonologicalRuleSynthesis, _rule) { Input = input.DeepClone() };
				input.CurrentTrace.Children.Add(trace);
			}

			IEnumerable<Word> output = _patternRule.Apply(input);

			if (trace != null)
				trace.Output = input.DeepClone();

			return output;
		}
	}
}
