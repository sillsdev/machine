using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public interface IPhonologicalPatternRuleSpec
    {
        Pattern<Word, int> Pattern { get; }
        bool MatchSubrule(
            PhonologicalPatternRule rule,
            Match<Word, int> match,
            out PhonologicalSubruleMatch subruleMatch
        );
    }
}
