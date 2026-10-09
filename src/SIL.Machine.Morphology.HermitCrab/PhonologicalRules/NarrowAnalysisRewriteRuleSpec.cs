using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public class NarrowAnalysisRewriteRuleSpec : RewriteRuleSpec
    {
        private readonly Pattern<Word, ShapeNode> _analysisRhs;
        private readonly int _targetCount;

        public NarrowAnalysisRewriteRuleSpec(
            MatcherSettings<ShapeNode> matcherSettings,
            Pattern<Word, ShapeNode> lhs,
            RewriteSubrule subrule
        )
            : base(subrule.Rhs.IsEmpty)
        {
            _analysisRhs = lhs;
            _targetCount = subrule.Rhs.Children.Count;

            if (subrule.Rhs.IsEmpty)
            {
                Pattern.Children.Add(
                    new Constraint<Word, ShapeNode>(
                        FeatureStruct.New().Symbol(HCFeatureSystem.Segment, HCFeatureSystem.Anchor).Value
                    )
                );
            }
            else
            {
                Pattern.Children.AddRange(subrule.Rhs.Children.CloneItems());
            }
            Pattern.Freeze();

            SubruleSpecs.Add(new AnalysisRewriteSubruleSpec(matcherSettings, subrule, Unapply));
        }

        private void Unapply(Match<Word, ShapeNode> targetMatch, Range<ShapeNode> range, VariableBindings varBindings)
        {
            ShapeNode curNode = IsTargetEmpty ? range.Start : range.End;
            ShapeNode targetNode = range.Start;
            int targetPos = 0;
            bool expanding = _analysisRhs.Children.Count > _targetCount;
            foreach (
                Constraint<Word, ShapeNode> constraint in _analysisRhs.Children.CastToConstraints(
                    "The target (left-hand side) of a rewrite rule"
                )
            )
            {
                FeatureStruct fs = constraint.FeatureStruct.Clone();
                if (varBindings != null)
                    fs.ReplaceVariables(varBindings);
                if (targetPos < _targetCount)
                {
                    // Union fs into the existing node's feature structure.
                    targetNode.Annotation.FeatureStruct.Union(fs);
                    if (expanding)
                        targetNode.SetDirty(true);
                    targetNode = targetNode.Next;
                    targetPos++;
                }
                else
                {
                    // Add an optional node for fs.
                    curNode = targetMatch.Input.Shape.AddAfter(curNode, fs, true);
                    if (expanding)
                        curNode.SetDirty(true);
                }
            }

            // Make the rest of the existing nodes optional.
            for (int i = targetPos; i < _targetCount; i++)
            {
                targetNode.Annotation.Optional = true;
                if (expanding)
                    targetNode.SetDirty(true);
                targetNode = targetNode.Next;
            }
        }
    }
}
