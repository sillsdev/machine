using System;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public abstract class SynthesisRewriteRuleSpec : IPatternRuleSpec<Word, ShapeNode>
	{
		private readonly Pattern<Word, ShapeNode> _pattern;
		private readonly RewriteSubrule _subrule;

		protected SynthesisRewriteRuleSpec(Pattern<Word, ShapeNode> lhs, RewriteSubrule subrule)
		{
			_subrule = subrule;
			_pattern = new Pattern<Word, ShapeNode> {Acceptable = match => CheckTarget(match, lhs)};
			if (_subrule.LeftEnvironment.Children.Count > 0)
				_pattern.Children.Add(new Group<Word, ShapeNode>("leftEnv", _subrule.LeftEnvironment.Children.DeepClone()));

			var target = new Group<Word, ShapeNode>("target");
			foreach (Constraint<Word, ShapeNode> constraint in lhs.Children.Cast<Constraint<Word, ShapeNode>>())
			{
				var newConstraint = constraint.DeepClone();
				newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
				target.Children.Add(newConstraint);
			}
			_pattern.Children.Add(target);
			if (_subrule.RightEnvironment.Children.Count > 0)
				_pattern.Children.Add(new Group<Word, ShapeNode>("rightEnv", _subrule.RightEnvironment.Children.DeepClone()));
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
			return input.SyntacticFeatureStruct.IsUnifiable(_subrule.RequiredSyntacticFeatureStruct)
				&& (_subrule.RequiredMprFeatures.Count == 0 || _subrule.RequiredMprFeatures.IsMatch(input.MprFeatures))
				&& (_subrule.ExcludedMprFeatures.Count == 0 || !_subrule.ExcludedMprFeatures.IsMatch(input.MprFeatures));
		}

		public abstract ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output);
	}
}
