using System;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public class FeatureSynthesisRewriteSubruleSpec : SynthesisRewriteSubruleSpec
    {
        private readonly Pattern<Word, ShapeNode> _rhs;

        public FeatureSynthesisRewriteSubruleSpec(
            MatcherSettings<ShapeNode> matcherSettings,
            bool isIterative,
            RewriteSubrule subrule,
            int index
        )
            : base(matcherSettings, isIterative, subrule, index)
        {
            _rhs = subrule.Rhs;
        }

        public override void ApplyRhs(
            Match<Word, ShapeNode> targetMatch,
            Range<ShapeNode> range,
            VariableBindings varBindings
        )
        {
            foreach (
                Tuple<ShapeNode, PatternNode<Word, ShapeNode>> tuple in targetMatch
                    .Input.Shape.GetNodes(range)
                    .Zip(_rhs.Children)
            )
            {
                var constraints = (Constraint<Word, ShapeNode>)tuple.Item2;
                tuple.Item1.Annotation.FeatureStruct.PriorityUnion(constraints.FeatureStruct, varBindings);
                if (IsIterative)
                    tuple.Item1.SetDirty(true);
            }

            MarkSuccessfulApply(targetMatch.Input);
        }
    }
}
