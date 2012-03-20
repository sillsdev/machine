using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class AnalysisCompoundingRuleSpec : AnalysisMorphologicalRuleSpec
	{
		public AnalysisCompoundingRuleSpec(CompoundingSubrule subrule)
			: base(subrule.Headedness == Headedness.LeftHeaded ? subrule.LeftLhs : subrule.RightLhs, subrule.Headedness == Headedness.LeftHeaded ? subrule.LeftRhs : subrule.RightRhs)
		{
			Pattern.Acceptable = match => match.Span.Start != match.Input.Shape.First || match.Span.End != match.Input.Shape.Last;
		}

		public override ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			base.ApplyRhs(rule, match, out output);

			SpanFactory<ShapeNode> spanFactory = rule.SpanFactory;
			Shape inputShape = match.Input.Shape;
			Direction dir = match.Matcher.Direction;
			var nonHeadShape = new Shape(spanFactory, begin => begin ? new ShapeNode(spanFactory, FeatureStruct.New().Symbol(HCFeatureSystem.Anchor).Symbol(HCFeatureSystem.LeftSide).Value)
				: new ShapeNode(spanFactory, FeatureStruct.New().Symbol(HCFeatureSystem.Anchor).Symbol(HCFeatureSystem.RightSide).Value));
			inputShape.CopyTo(spanFactory.Create(match.Span.GetEnd(dir).GetNext(dir), inputShape.GetLast(dir), dir), nonHeadShape);
			output.NonHeadUnapplied(new Word(output.Stratum, nonHeadShape));

			return null;
		}
	}
}
