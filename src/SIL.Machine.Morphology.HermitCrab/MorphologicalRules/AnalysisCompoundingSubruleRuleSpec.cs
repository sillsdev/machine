using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class AnalysisCompoundingSubruleRuleSpec : AnalysisMorphologicalTransformRuleSpec
    {
        private readonly CompoundingSubrule _subrule;

        public AnalysisCompoundingSubruleRuleSpec(CompoundingSubrule subrule)
            : base(subrule.HeadLhs.Concat(subrule.NonHeadLhs), subrule.Rhs)
        {
            _subrule = subrule;
            Pattern.Acceptable = match => _subrule.HeadLhs.Any(part => IsPartCaptured(match, part.Name));
            Pattern.Freeze();
        }

        public override Word ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match)
        {
            Word output = match.Input.Clone();
            GenerateShape(_subrule.HeadLhs, output.Shape, match);
            var nonHeadShape = new Shape(begin => new ShapeNode(
                begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor
            ));
            GenerateShape(_subrule.NonHeadLhs, nonHeadShape, match);
            output.NonHeadUnapplied(new Word(output.Stratum, nonHeadShape));
            return output;
        }
    }
}
