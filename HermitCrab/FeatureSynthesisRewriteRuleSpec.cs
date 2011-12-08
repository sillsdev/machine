using System;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class FeatureSynthesisRewriteRuleSpec : SynthesisRewriteRuleSpec
	{
		private readonly Pattern<Word, ShapeNode> _rhs;

		public FeatureSynthesisRewriteRuleSpec(Pattern<Word, ShapeNode> lhs,
			Pattern<Word, ShapeNode> rhs, Pattern<Word, ShapeNode> leftEnv, Pattern<Word, ShapeNode> rightEnv,
			FeatureStruct requiredSyntacticFS)
			: base(lhs, leftEnv, rightEnv, requiredSyntacticFS)
		{
			_rhs = rhs;
		}

		public override ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			GroupCapture<ShapeNode> target = match["target"];
			foreach (Tuple<ShapeNode, PatternNode<Word, ShapeNode>> tuple in match.Input.Shape.GetNodes(target.Span).Zip(_rhs.Children))
			{
				var constraints = (Constraint<Word, ShapeNode>) tuple.Item2;
				tuple.Item1.Annotation.FeatureStruct.PriorityUnion(constraints.FeatureStruct, match.VariableBindings);
				if (tuple.Item1.Annotation.FeatureStruct.HasVariables)
					throw new MorphException(MorphErrorCode.UninstantiatedFeature);
			}

			ShapeNode resumeNode = match.Span.GetStart(match.Matcher.Direction).GetNext(match.Matcher.Direction);
			if (rule.ApplicationMode == ApplicationMode.Iterative)
				MarkSearchedNodes(resumeNode, target.Span.GetEnd(match.Matcher.Direction), match.Matcher.Direction);

			output = match.Input;
			return resumeNode;
		}
	}
}
