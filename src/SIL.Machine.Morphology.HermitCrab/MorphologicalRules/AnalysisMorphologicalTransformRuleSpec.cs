using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public abstract class AnalysisMorphologicalTransformRuleSpec
        : AnalysisMorphologicalTransform,
            IPatternRuleSpec<Word, int>
    {
        protected AnalysisMorphologicalTransformRuleSpec(
            IEnumerable<Pattern<Word, int>> lhs,
            IList<MorphologicalOutputAction> rhs
        )
            : base(lhs, rhs) { }

        public bool IsApplicable(Word input)
        {
            return true;
        }

        protected bool IsPartCaptured(Match<Word, int> match, string partName)
        {
            int count;
            if (CapturedParts.TryGetValue(partName, out count))
            {
                for (int i = 0; i < count; i++)
                {
                    if (match.GroupCaptures.Captured(GetGroupName(partName, i)))
                        return true;
                }
            }
            return false;
        }

        public abstract Word ApplyRhs(PatternRule<Word, int> rule, Match<Word, int> match);
    }
}
