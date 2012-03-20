using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class SynthesisRewriteRule : BacktrackingPatternRule
	{
		private readonly Morpher _morpher;
		private readonly RewriteRule _rule;

		public SynthesisRewriteRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, RewriteRule rule)
			: base(spanFactory, CreateRuleSpec(rule), rule.ApplicationMode,
				new MatcherSettings<ShapeNode>
					{
						Direction = rule.Direction,
						Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary, HCFeatureSystem.Anchor),
						UseDefaults = true
					})
		{
			_morpher = morpher;
			_rule = rule;
		}

		private static IPatternRuleSpec<Word, ShapeNode> CreateRuleSpec(RewriteRule rule)
		{
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
			return ruleSpec;
		}

		public override IEnumerable<Word> Apply(Word input, ShapeNode start)
		{
			Trace trace = null;
			if (_morpher.GetTraceRule(_rule))
			{
				trace = new Trace(TraceType.PhonologicalRuleSynthesis, _rule) { Input = input.DeepClone() };
				input.CurrentTrace.Children.Add(trace);
			}

			IEnumerable<Word> output = base.Apply(input, start);

			if (trace != null)
				trace.Output = input.DeepClone();

			return output;
		}
	}
}
