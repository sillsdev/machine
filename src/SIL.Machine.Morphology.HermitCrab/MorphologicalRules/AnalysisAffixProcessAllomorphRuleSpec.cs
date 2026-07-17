using SIL.Machine.Annotations;
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
            Pattern.Freeze();
        }

        public override Word ApplyRhs(PatternRule<Word, int> rule, Match<Word, int> match)
        {
            Word output = match.Input.Clone();
            GenerateShape(_allomorph.Lhs, output.Shape, match);
            return output;
        }
    }
}
