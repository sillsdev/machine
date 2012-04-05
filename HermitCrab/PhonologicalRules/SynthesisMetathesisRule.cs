using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class SynthesisMetathesisRule : BacktrackingPatternRule
	{
		private readonly Morpher _morpher;
		private readonly MetathesisRule _rule;

		public SynthesisMetathesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, MetathesisRule rule)
			: base(spanFactory, CreateRuleSpec(rule), ApplicationMode.Iterative,
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

		private static IPatternRuleSpec<Word, ShapeNode> CreateRuleSpec(MetathesisRule rule)
		{
			var pattern = new Pattern<Word, ShapeNode>();
			foreach (PatternNode<Word, ShapeNode> node in rule.Pattern.Children)
			{
				var group = node as Group<Word, ShapeNode>;
				if (group != null)
				{
					var newGroup = new Group<Word, ShapeNode>(group.Name);
					foreach (Constraint<Word, ShapeNode> constraint in group.Children)
					{
						Constraint<Word, ShapeNode> newConstraint = constraint.DeepClone();
						newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
						newGroup.Children.Add(newConstraint);
					}
					pattern.Children.Add(newGroup);
				}
				else
				{
					pattern.Children.Add(node.DeepClone());
				}
			}

			return new MetathesisRuleSpec(pattern, rule.GroupOrder);
		}

		public override IEnumerable<Word> Apply(Word input, ShapeNode start)
		{
			Trace trace = null;
			if (_morpher.TraceRules.Contains(_rule))
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
