using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public class FeatureAnalysisRewriteRuleSpec : RewriteRuleSpec
    {
        private readonly Pattern<Word, ShapeNode> _analysisRhs;

        public FeatureAnalysisRewriteRuleSpec(
            MatcherSettings<ShapeNode> matcherSettings,
            Pattern<Word, ShapeNode> lhs,
            RewriteSubrule subrule
        ) : base(false)
        {
            var rhsAntiFSs = new List<FeatureStruct>();
            foreach (
                Constraint<Word, ShapeNode> constraint in subrule.Rhs.Children
                    .OfType<Constraint<Word, ShapeNode>>()
                    .Where(c => c.Type() == HCFeatureSystem.Segment)
            )
            {
                rhsAntiFSs.Add(constraint.FeatureStruct.AntiFeatureStruct());
            }

            Pattern.Acceptable = match => IsUnapplicationNonvacuous(match, rhsAntiFSs);

            _analysisRhs = new Pattern<Word, ShapeNode>();
            int i = 0;
            foreach (
                Tuple<PatternNode<Word, ShapeNode>, PatternNode<Word, ShapeNode>> tuple in lhs.Children.Zip(
                    subrule.Rhs.Children
                )
            )
            {
                var lhsConstraint = (Constraint<Word, ShapeNode>)tuple.Item1;
                var rhsConstraint = (Constraint<Word, ShapeNode>)tuple.Item2;

                if (lhsConstraint.Type() == HCFeatureSystem.Segment && rhsConstraint.Type() == HCFeatureSystem.Segment)
                {
                    Constraint<Word, ShapeNode> targetConstraint = lhsConstraint.Clone();
                    targetConstraint.FeatureStruct.PriorityUnion(rhsConstraint.FeatureStruct);
                    targetConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
                    Pattern.Children.Add(new Group<Word, ShapeNode>("target" + i) { Children = { targetConstraint } });

                    FeatureStruct fs = rhsConstraint.FeatureStruct.AntiFeatureStruct();
                    fs.Subtract(lhsConstraint.FeatureStruct.AntiFeatureStruct());
                    fs.AddValue(HCFeatureSystem.Type, HCFeatureSystem.Segment);
                    _analysisRhs.Children.Add(new Constraint<Word, ShapeNode>(fs));

                    i++;
                }
            }
            Pattern.Freeze();

            SubruleSpecs.Add(new AnalysisRewriteSubruleSpec(matcherSettings, subrule, Unapply));
        }

        private bool IsUnapplicationNonvacuous(Match<Word, ShapeNode> match, IEnumerable<FeatureStruct> rhsAntiFSs)
        {
            int i = 0;
            foreach (FeatureStruct fs in rhsAntiFSs)
            {
                ShapeNode node = match.GroupCaptures["target" + i].Range.GetStart(match.Matcher.Direction);
                foreach (SymbolicFeature sf in fs.Features.OfType<SymbolicFeature>())
                {
                    SymbolicFeatureValue sfv = fs.GetValue(sf);
                    SymbolicFeatureValue nodeSfv;
                    if (node.Annotation.FeatureStruct.TryGetValue(sf, out nodeSfv))
                    {
                        if (sfv.IsVariable)
                        {
                            SymbolicFeatureValue varSfv;
                            if (
                                !match.VariableBindings.TryGetValue(sfv.VariableName, out varSfv)
                                || !nodeSfv.IsSupersetOf(varSfv, !sfv.Agree)
                            )
                            {
                                return true;
                            }
                        }
                        else if (!nodeSfv.IsSupersetOf(sfv))
                        {
                            return true;
                        }
                    }
                }
                i++;
            }

            return false;
        }

        private void Unapply(Match<Word, ShapeNode> targetMatch, Range<ShapeNode> range, VariableBindings varBindings)
        {
            int i = 0;
            foreach (
                Constraint<Word, ShapeNode> constraint in _analysisRhs.Children.Cast<Constraint<Word, ShapeNode>>()
            )
            {
                ShapeNode node = targetMatch.GroupCaptures["target" + i].Range.GetStart(targetMatch.Matcher.Direction);
                FeatureStruct fs = node.Annotation.FeatureStruct.Clone();
                fs.PriorityUnion(constraint.FeatureStruct);
                node.Annotation.FeatureStruct.Union(fs, varBindings);
                node.SetDirty(true);
                i++;
            }
        }
    }
}
