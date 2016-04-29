using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.HermitCrab.PhonologicalRules
{
	public class SynthesisMetathesisRule : IRule<Word, ShapeNode>
	{
		private readonly Morpher _morpher;
		private readonly MetathesisRule _rule;
		private readonly PatternRule<Word, ShapeNode> _patternRule; 

		public SynthesisMetathesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, MetathesisRule rule)
		{
			_morpher = morpher;
			_rule = rule;

			var pattern = new Pattern<Word, ShapeNode>();
			foreach (PatternNode<Word, ShapeNode> node in rule.Pattern.Children)
			{
				var group = node as Group<Word, ShapeNode>;
				if (group != null)
				{
					var newGroup = new Group<Word, ShapeNode>(group.Name);
					foreach (Constraint<Word, ShapeNode> constraint in group.Children.Cast<Constraint<Word, ShapeNode>>())
					{
						Constraint<Word, ShapeNode> newConstraint = constraint.Clone();
						newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
						newGroup.Children.Add(newConstraint);
					}
					pattern.Children.Add(newGroup);
				}
				else
				{
					pattern.Children.Add(node.Clone());
				}
			}

			var ruleSpec = new SynthesisMetathesisRuleSpec(pattern, rule.LeftGroupName, rule.RightGroupName);

			var settings = new MatcherSettings<ShapeNode>
			               	{
			               		Direction = rule.Direction,
			               		Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary, HCFeatureSystem.Anchor) && !ann.IsDeleted(),
								UseDefaults = true
			               	};

			_patternRule = new BacktrackingPatternRule(spanFactory, ruleSpec, settings);
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
					_morpher.TraceManager.PhonologicalRuleApplied(_rule, -1, origInput, input);
				return input.ToEnumerable();
			}

			if (_morpher.TraceManager.IsTracing)
				_morpher.TraceManager.PhonologicalRuleNotApplied(_rule, -1, input, FailureReason.Pattern, null);
			return Enumerable.Empty<Word>();
		}
	}
}
