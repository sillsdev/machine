using System;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;

namespace SIL.HermitCrab
{
	public class FeatureSynthesisRewriteRule : SynthesisRewriteRule
	{
		private readonly Expression<PhoneticShapeNode> _rhs; 

		public FeatureSynthesisRewriteRule(SpanFactory<PhoneticShapeNode> spanFactory, Direction dir, bool simult, Expression<PhoneticShapeNode> lhs,
			Expression<PhoneticShapeNode> rhs, Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv,
			FeatureStruct applicableFS)
			: base(spanFactory, dir, simult, lhs, leftEnv, rightEnv, applicableFS)
		{
			_rhs = rhs;
		}

		public override Annotation<PhoneticShapeNode> ApplyRhs(IBidirList<Annotation<PhoneticShapeNode>> input, PatternMatch<PhoneticShapeNode> match)
		{
			Span<PhoneticShapeNode> target = match["target"];
			foreach (Tuple<PhoneticShapeNode, PatternNode<PhoneticShapeNode>> tuple in target.Start.GetNodes(target.End).Zip(_rhs.Children))
			{
				var constraints = (Constraint<PhoneticShapeNode>)tuple.Item2;
				tuple.Item1.Annotation.FeatureStruct.Replace(constraints.FeatureStruct);
				tuple.Item1.Annotation.FeatureStruct.ReplaceVariables(match.VariableBindings);
				if (HasVariable(tuple.Item1.Annotation.FeatureStruct))
					throw new MorphException(MorphException.MorphErrorType.UninstantiatedFeature);
			}

			MarkSearchedNodes(match.GetStart(Lhs.Direction), target.GetEnd(Lhs.Direction));

			return match.GetStart(Lhs.Direction).Annotation;
		}
	}
}
