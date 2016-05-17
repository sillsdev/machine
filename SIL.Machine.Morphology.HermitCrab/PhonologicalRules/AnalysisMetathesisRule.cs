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
		private readonly PatternRule<Word, ShapeNode> _patternRule; 

		public AnalysisMetathesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, MetathesisRule rule)
		{
			_morpher = morpher;
			_rule = rule;

			Group<Word, ShapeNode>[] groupOrder = rule.Pattern.Children.OfType<Group<Word, ShapeNode>>().ToArray();
			Dictionary<string, Group<Word, ShapeNode>> groups = groupOrder.ToDictionary(g => g.Name);
			var pattern = new Pattern<Word, ShapeNode>();
			foreach (PatternNode<Word, ShapeNode> node in rule.Pattern.Children.TakeWhile(n => !(n is Group<Word, ShapeNode>)))
				pattern.Children.Add(node.Clone());

			AddGroup(groups, pattern, rule.LeftGroupName);
			AddGroup(groups, pattern, rule.RightGroupName);

			foreach (PatternNode<Word, ShapeNode> node in rule.Pattern.Children.GetNodes(Direction.RightToLeft).TakeWhile(n => !(n is Group<Word, ShapeNode>)).Reverse())
				pattern.Children.Add(node.Clone());

			var ruleSpec = new AnalysisMetathesisRuleSpec(pattern, rule.LeftGroupName, rule.RightGroupName);

			var settings = new MatcherSettings<ShapeNode>
			               	{
			               		Direction = rule.Direction == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight,
			               		Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Anchor),
								MatchingMethod = MatchingMethod.Unification,
								UseDefaults = true
			               	};

			_patternRule = new BacktrackingPatternRule(spanFactory, ruleSpec, settings);
		}

		private static void AddGroup(Dictionary<string, Group<Word, ShapeNode>> groups, Pattern<Word, ShapeNode> pattern, string name)
		{
			var newGroup = new Group<Word, ShapeNode>(name);
			foreach (Constraint<Word, ShapeNode> constraint in groups[name].Children.Cast<Constraint<Word, ShapeNode>>())
			{
				Constraint<Word, ShapeNode> newConstraint = constraint.Clone();
				newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
				newGroup.Children.Add(newConstraint);
			}
			pattern.Children.Add(newGroup);
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
