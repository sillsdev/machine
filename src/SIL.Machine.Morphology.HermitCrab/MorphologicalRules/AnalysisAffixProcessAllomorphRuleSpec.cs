using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class AnalysisAffixProcessAllomorphRuleSpec : AnalysisMorphologicalTransformRuleSpec
    {
        private readonly AffixProcessAllomorph _allomorph;

        public AnalysisAffixProcessAllomorphRuleSpec(AffixProcessAllomorph allomorph, Morpher morpher = null)
            : base(allomorph.Lhs, allomorph.Rhs, morpher)
        {
            _allomorph = allomorph;
            // the matcher builds a Match for every candidate it tests, so rules that copy nothing get no check
            if (HasRepeatedParts)
                Pattern.Acceptable = CopiesMayAgree;
            Pattern.Freeze();
        }

        public override Word ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match)
        {
            Word output = match.Input.Clone();
            GenerateShape(_allomorph.Lhs, output.Shape, match);
            return output;
        }
    }
}
