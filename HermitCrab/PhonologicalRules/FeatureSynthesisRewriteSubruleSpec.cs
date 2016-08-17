using System;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class FeatureSynthesisRewriteSubruleSpec : SynthesisRewriteSubruleSpec
	{
		private readonly Pattern<Word, ShapeNode> _rhs;

		public FeatureSynthesisRewriteSubruleSpec(SpanFactory<ShapeNode> spanFactory, MatcherSettings<ShapeNode> matcherSettings, bool isIterative,
			RewriteSubrule subrule, int index)
			: base(spanFactory, matcherSettings, isIterative, subrule, index)
		{
			_rhs = subrule.Rhs;
		}

		public override void ApplyRhs(Match<Word, ShapeNode> targetMatch, Span<ShapeNode> span, VariableBindings varBindings)
		{
			foreach (Tuple<ShapeNode, PatternNode<Word, ShapeNode>> tuple in targetMatch.Input.Shape.GetNodes(span).Zip(_rhs.Children))
			{
				var constraints = (Constraint<Word, ShapeNode>) tuple.Item2;
				tuple.Item1.Annotation.FeatureStruct.PriorityUnion(constraints.FeatureStruct, varBindings);
				if (IsIterative)
					tuple.Item1.SetDirty(true);
			}

			MarkSuccessfulApply(targetMatch.Input);
		}
	}
}
