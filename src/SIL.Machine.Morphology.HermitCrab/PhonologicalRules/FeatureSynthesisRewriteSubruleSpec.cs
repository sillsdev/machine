using System;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public class FeatureSynthesisRewriteSubruleSpec : SynthesisRewriteSubruleSpec
    {
        private readonly Pattern<Word, int> _rhs;

        public FeatureSynthesisRewriteSubruleSpec(
            MatcherSettings<int> matcherSettings,
            bool isIterative,
            RewriteSubrule subrule,
            int index
        )
            : base(matcherSettings, isIterative, subrule, index)
        {
            _rhs = subrule.Rhs;
        }

        public override void ApplyRhs(
            Match<Word, int> targetMatch,
            Range<ShapeNode> range,
            VariableBindings varBindings
        )
        {
            foreach (
                Tuple<ShapeNode, PatternNode<Word, int>> tuple in targetMatch
                    .Input.Shape.GetNodes(range)
                    .Zip(_rhs.Children)
            )
            {
                var constraints = (Constraint<Word, int>)tuple.Item2;
                tuple.Item1.Annotation.FeatureStruct.PriorityUnion(constraints.FeatureStruct, varBindings);
                if (IsIterative)
                    tuple.Item1.SetDirty(true);
            }

            MarkSuccessfulApply(targetMatch.Input);
        }
    }
}
