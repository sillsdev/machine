using System;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public class AnalysisRewriteSubruleSpec : RewriteSubruleSpec
    {
        private readonly Action<Match<Word, int>, Range<ShapeNode>, VariableBindings> _applyAction;

        public AnalysisRewriteSubruleSpec(
            MatcherSettings<int> matcherSettings,
            RewriteSubrule subrule,
            Action<Match<Word, int>, Range<ShapeNode>, VariableBindings> applyAction
        )
            : base(
                matcherSettings,
                CreateEnvironmentPattern(subrule.LeftEnvironment),
                CreateEnvironmentPattern(subrule.RightEnvironment)
            )
        {
            _applyAction = applyAction;
        }

        private static Pattern<Word, int> CreateEnvironmentPattern(Pattern<Word, int> env)
        {
            Pattern<Word, int> pattern = null;
            if (!env.IsEmpty)
                pattern = new Pattern<Word, int>(env.Children.DeepCloneExceptBoundaries());
            return pattern;
        }

        public override void ApplyRhs(
            Match<Word, int> targetMatch,
            Range<ShapeNode> range,
            VariableBindings varBindings
        )
        {
            _applyAction(targetMatch, range, varBindings);
        }
    }
}
