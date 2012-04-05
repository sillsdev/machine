using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class AnalysisMetathesisRule : BacktrackingPatternRule
	{
		private readonly Morpher _morpher;
		private readonly MetathesisRule _rule;

		public AnalysisMetathesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher, MetathesisRule rule)
			: base(spanFactory, CreateRuleSpec(rule), ApplicationMode.Iterative,
				new MatcherSettings<ShapeNode>
					{
						Direction = rule.Direction == Direction.LeftToRight ? Direction.RightToLeft : Direction.LeftToRight,
						Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Anchor)
					})
		{
			_morpher = morpher;
			_rule = rule;
		}

		private static IPatternRuleSpec<Word, ShapeNode> CreateRuleSpec(MetathesisRule rule)
		{
			Group<Word, ShapeNode>[] groupOrder = rule.Pattern.Children.OfType<Group<Word, ShapeNode>>().ToArray();
			Dictionary<string, Group<Word, ShapeNode>> groups = groupOrder.ToDictionary(g => g.Name);
			var pattern = new Pattern<Word, ShapeNode>();
			foreach (PatternNode<Word, ShapeNode> node in rule.Pattern.Children.TakeWhile(n => !(n is Group<Word, ShapeNode>)))
				pattern.Children.Add(node.DeepClone());
			foreach (string name in rule.GroupOrder)
			{
				var newGroup = new Group<Word, ShapeNode>(name);
				foreach (Constraint<Word, ShapeNode> constraint in groups[name].Children)
				{
					Constraint<Word, ShapeNode> newConstraint = constraint.DeepClone();
					newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
					newGroup.Children.Add(newConstraint);
				}
				pattern.Children.Add(newGroup);
			}
			foreach (PatternNode<Word, ShapeNode> node in rule.Pattern.Children.GetNodes(Direction.RightToLeft).TakeWhile(n => !(n is Group<Word, ShapeNode>)).Reverse())
				pattern.Children.Add(node.DeepClone());

			return new MetathesisRuleSpec(pattern, groupOrder.Select(g => g.Name));
		}

		public override IEnumerable<Word> Apply(Word input, ShapeNode start)
		{
			Trace trace = null;
			if (_morpher.TraceRules.Contains(_rule))
			{
				trace = new Trace(TraceType.PhonologicalRuleAnalysis, _rule) { Input = input.DeepClone() };
				input.CurrentTrace.Children.Add(trace);
			}

			IEnumerable<Word> output = base.Apply(input, start);

			if (trace != null)
				trace.Output = input.DeepClone();

			return output;
		}
	}
}
