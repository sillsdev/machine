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
                // The matcher skips optional nodes and boundaries, so the matched RHS nodes need not be
                // adjacent; each is recovered from its own group capture.
                int i = 0;
                foreach (PatternNode<Word, ShapeNode> node in subrule.Rhs.Children)
                {
                    Pattern.Children.Add(new Group<Word, ShapeNode>("target" + i) { Children = { node.Clone() } });
                    i++;
                }
            }
            Pattern.Freeze();

            SubruleSpecs.Add(new AnalysisRewriteSubruleSpec(matcherSettings, subrule, Unapply));
        }

        private void Unapply(Match<Word, ShapeNode> targetMatch, Range<ShapeNode> range, VariableBindings varBindings)
        {
            ShapeNode curNode = IsTargetEmpty ? range.Start : range.End;
            foreach (
                Constraint<Word, ShapeNode> constraint in _analysisRhs.Children.CastToConstraints(
                    "The target (left-hand side) of a rewrite rule"
                )
            )
            {
                FeatureStruct fs = constraint.FeatureStruct.Clone();
                if (varBindings != null)
                    fs.ReplaceVariables(varBindings);
                curNode = targetMatch.Input.Shape.AddAfter(curNode, fs, true);
            }

            for (int i = 0; i < _targetCount; i++)
            {
                ShapeNode node = targetMatch.GroupCaptures["target" + i].Range.GetStart(targetMatch.Matcher.Direction);
                node.Annotation.Optional = true;
            }
        }
    }
}
