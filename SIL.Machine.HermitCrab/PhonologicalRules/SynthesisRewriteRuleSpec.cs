using System;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.HermitCrab.PhonologicalRules
{
	public abstract class SynthesisRewriteRuleSpec : IPatternRuleSpec<Word, ShapeNode>
	{
		private readonly Pattern<Word, ShapeNode> _pattern;
		private readonly RewriteSubrule _subrule;
		private readonly int _index;

		protected SynthesisRewriteRuleSpec(Pattern<Word, ShapeNode> lhs, RewriteSubrule subrule, int index)
		{
			_subrule = subrule;
			_index = index;
			_pattern = new Pattern<Word, ShapeNode> {Acceptable = match => CheckTarget(match, lhs)};
			if (_subrule.LeftEnvironment.Children.Count > 0)
				_pattern.Children.Add(new Group<Word, ShapeNode>("leftEnv", _subrule.LeftEnvironment.Children.CloneItems()));

			var target = new Group<Word, ShapeNode>("target");
			foreach (Constraint<Word, ShapeNode> constraint in lhs.Children.Cast<Constraint<Word, ShapeNode>>())
			{
				Constraint<Word, ShapeNode> newConstraint = constraint.Clone();
				newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
				target.Children.Add(newConstraint);
			}
			_pattern.Children.Add(target);
			if (_subrule.RightEnvironment.Children.Count > 0)
				_pattern.Children.Add(new Group<Word, ShapeNode>("rightEnv", _subrule.RightEnvironment.Children.CloneItems()));
		}

		private static bool CheckTarget(Match<Word, ShapeNode> match, Pattern<Word, ShapeNode> lhs)
		{
			GroupCapture<ShapeNode> target = match.GroupCaptures["target"];
			if (target.Success)
			{
				foreach (Tuple<ShapeNode, PatternNode<Word, ShapeNode>> tuple in target.Span.Start.GetNodes(target.Span.End).Zip(lhs.Children))
				{
					var constraints = (Constraint<Word, ShapeNode>) tuple.Item2;
					if (tuple.Item1.Annotation.Type() != constraints.Type())
						return false;
				}
			}
			return true;
		}

		public Pattern<Word, ShapeNode> Pattern
		{
			get { return _pattern; }
		}

		public bool IsApplicable(Word input)
		{
			if (!_subrule.RequiredSyntacticFeatureStruct.IsUnifiable(input.SyntacticFeatureStruct))
			{
				if (input.CurrentRuleResults != null)
					input.CurrentRuleResults[_index] = new Tuple<FailureReason, object>(FailureReason.RequiredSyntacticFeatureStruct, _subrule.RequiredSyntacticFeatureStruct);
				return false;
			}

			MprFeatureGroup group;
			if (_subrule.RequiredMprFeatures.Count > 0 && !_subrule.RequiredMprFeatures.IsMatchRequired(input.MprFeatures, out group))
			{
				if (input.CurrentRuleResults != null)
					input.CurrentRuleResults[_index] = new Tuple<FailureReason, object>(FailureReason.RequiredMprFeatures, group);
				return false;
			}

			if (_subrule.ExcludedMprFeatures.Count > 0 && !_subrule.ExcludedMprFeatures.IsMatchExcluded(input.MprFeatures, out group))
			{
				if (input.CurrentRuleResults != null)
					input.CurrentRuleResults[_index] = new Tuple<FailureReason, object>(FailureReason.ExcludedMprFeatures, group);
				return false;
			}

			return true;
		}

		protected void MarkSuccessfulApply(Word word)
		{
			if (word.CurrentRuleResults != null)
				word.CurrentRuleResults[_index] = new Tuple<FailureReason, object>(FailureReason.None, null);
		}

		public abstract ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output);
	}
}
