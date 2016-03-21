using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class AnalysisCompoundingSubruleRuleSpec : AnalysisMorphologicalTransformRuleSpec
	{
		private readonly CompoundingSubrule _subrule;

		public AnalysisCompoundingSubruleRuleSpec(CompoundingSubrule subrule)
			: base(subrule.HeadLhs.Concat(subrule.NonHeadLhs), subrule.Rhs)
		{
			_subrule = subrule;
			Pattern.Acceptable = match => _subrule.HeadLhs.Any(part => match.GroupCaptures.Captured(part.Name));
		}

		public override ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			output = match.Input.DeepClone();
			GenerateShape(_subrule.HeadLhs, output.Shape, match);
			var nonHeadShape = new Shape(rule.SpanFactory, begin => new ShapeNode(rule.SpanFactory, begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor));
			GenerateShape(_subrule.NonHeadLhs, nonHeadShape, match);
			output.NonHeadUnapplied(new Word(output.Stratum, nonHeadShape));
			return null;
		}
	}
}
