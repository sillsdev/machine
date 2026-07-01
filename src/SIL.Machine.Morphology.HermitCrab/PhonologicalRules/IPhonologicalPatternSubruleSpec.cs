using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public interface IPhonologicalPatternSubruleSpec
    {
        Matcher<Word, int> LeftEnvironmentMatcher { get; }
        Matcher<Word, int> RightEnvironmentMatcher { get; }

        bool IsApplicable(Word input);
        void ApplyRhs(Match<Word, int> targetMatch, Range<ShapeNode> range, VariableBindings varBindings);
    }
}
