using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class AnalysisAffixProcessAllomorphRuleSpec : AnalysisMorphologicalTransformRuleSpec
    {
        private readonly AffixProcessAllomorph _allomorph;

        public AnalysisAffixProcessAllomorphRuleSpec(AffixProcessAllomorph allomorph)
            : base(allomorph.Lhs, allomorph.Rhs)
        {
            _allomorph = allomorph;
            Pattern.Acceptable = CopyAgreementCheck;
            Pattern.Freeze();
            LeftEdgeConstraint = (Pattern.Children.First as Constraint<Word, ShapeNode>)?.FeatureStruct;
            RightEdgeConstraint = (Pattern.Children.Last as Constraint<Word, ShapeNode>)?.FeatureStruct;
        }

        internal FeatureStruct LeftEdgeConstraint { get; }
        internal FeatureStruct RightEdgeConstraint { get; }

        public override Word ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match)
        {
            Word output = match.Input.Clone();
            GenerateShape(_allomorph.Lhs, output.Shape, match);
            return output;
        }
    }
}
