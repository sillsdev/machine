using System;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;

namespace SIL.HermitCrab
{
	public class FeatureSynthesisRewriteRule : SynthesisRewriteRule
	{
		private readonly Expression<Word, ShapeNode> _rhs;

		public FeatureSynthesisRewriteRule(SpanFactory<ShapeNode> spanFactory, Expression<Word, ShapeNode> lhs,
			Expression<Word, ShapeNode> rhs, Expression<Word, ShapeNode> leftEnv, Expression<Word, ShapeNode> rightEnv,
			FeatureStruct requiredSyntacticFS)
			: base(spanFactory, lhs, leftEnv, rightEnv, requiredSyntacticFS)
		{
			_rhs = rhs;
		}

		public override Annotation<ShapeNode> ApplyRhs(Word input, PatternMatch<ShapeNode> match, out Word output)
		{
			Span<ShapeNode> target = match["target"];
			foreach (Tuple<ShapeNode, PatternNode<Word, ShapeNode>> tuple in input.Shape.GetNodes(target).Zip(_rhs.Children))
			{
				var constraints = (Constraint<Word, ShapeNode>) tuple.Item2;
				tuple.Item1.Annotation.FeatureStruct.PriorityUnion(constraints.FeatureStruct, match.VariableBindings);
				if (tuple.Item1.Annotation.FeatureStruct.HasVariables)
					throw new MorphException(MorphErrorCode.UninstantiatedFeature);
			}

			ShapeNode resumeNode = match.GetStart(Lhs.Direction).GetNext(Lhs.Direction);
			MarkSearchedNodes(resumeNode, target.GetEnd(Lhs.Direction));

			output = input;
			return resumeNode.Annotation;
		}
	}
}
