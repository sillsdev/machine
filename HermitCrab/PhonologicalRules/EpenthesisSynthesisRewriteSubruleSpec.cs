using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class EpenthesisSynthesisRewriteSubruleSpec : SynthesisRewriteSubruleSpec
	{
		private readonly Pattern<Word, ShapeNode> _rhs;

		public EpenthesisSynthesisRewriteSubruleSpec(SpanFactory<ShapeNode> spanFactory, MatcherSettings<ShapeNode> matcherSettings, bool isIterative,
			RewriteSubrule subrule, int index)
			: base(spanFactory, matcherSettings, isIterative, subrule, index)
		{
			_rhs = subrule.Rhs;
		}

		public override void ApplyRhs(Match<Word, ShapeNode> targetMatch, Span<ShapeNode> span, VariableBindings varBindings)
		{
			ShapeNode curNode = span.Start;
			foreach (PatternNode<Word, ShapeNode> node in _rhs.Children.GetNodes(targetMatch.Matcher.Direction))
			{
				if (targetMatch.Input.Shape.Count == 256)
					throw new InfiniteLoopException("An epenthesis rewrite rule is stuck in an infinite loop.");
				var constraint = (Constraint<Word, ShapeNode>) node;
				FeatureStruct fs = constraint.FeatureStruct.DeepClone();
				if (varBindings != null)
					fs.ReplaceVariables(varBindings);
				curNode = targetMatch.Input.Shape.AddAfter(curNode, fs);
				if (IsIterative)
					curNode.SetDirty(true);
			}
			MarkSuccessfulApply(targetMatch.Input);
		}
	}
}
