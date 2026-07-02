using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public abstract class RewriteSubruleSpec : IPhonologicalPatternSubruleSpec
    {
        private readonly Matcher<Word, int> _leftEnvMatcher;
        private readonly Matcher<Word, int> _rightEnvMatcher;

        protected RewriteSubruleSpec(
            MatcherSettings<int> matcherSettings,
            Pattern<Word, int> leftEnv,
            Pattern<Word, int> rightEnv
        )
        {
            if (leftEnv != null && !leftEnv.IsEmpty)
            {
                MatcherSettings<int> leftEnvMatcherSettings = matcherSettings.Clone();
                leftEnvMatcherSettings.Direction = Direction.RightToLeft;
                leftEnvMatcherSettings.AnchoredToStart = true;
                _leftEnvMatcher = new Matcher<Word, int>(leftEnv, leftEnvMatcherSettings);
            }

            if (rightEnv != null && !rightEnv.IsEmpty)
            {
                MatcherSettings<int> rightEnvMatcherSettings = matcherSettings.Clone();
                rightEnvMatcherSettings.Direction = Direction.LeftToRight;
                rightEnvMatcherSettings.AnchoredToStart = true;
                _rightEnvMatcher = new Matcher<Word, int>(rightEnv, rightEnvMatcherSettings);
            }
        }

        public Matcher<Word, int> LeftEnvironmentMatcher
        {
            get { return _leftEnvMatcher; }
        }

        public Matcher<Word, int> RightEnvironmentMatcher
        {
            get { return _rightEnvMatcher; }
        }

        public virtual bool IsApplicable(Word input)
        {
            return true;
        }

        public abstract void ApplyRhs(
            Match<Word, int> targetMatch,
            Range<ShapeNode> range,
            VariableBindings varBindings
        );
    }
}
