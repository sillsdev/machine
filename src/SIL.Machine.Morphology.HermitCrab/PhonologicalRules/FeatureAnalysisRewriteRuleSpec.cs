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
        private readonly Pattern<Word, int> _analysisRhs;

        public FeatureAnalysisRewriteRuleSpec(
            MatcherSettings<int> matcherSettings,
            Pattern<Word, int> lhs,
            RewriteSubrule subrule
        )
            : base(false)
        {
            var rhsAntiFSs = new List<FeatureStruct>();
            foreach (
                Constraint<Word, int> constraint in subrule
                    .Rhs.Children.OfType<Constraint<Word, int>>()
                    .Where(c => c.Type() == HCFeatureSystem.Segment)
            )
            {
                rhsAntiFSs.Add(constraint.FeatureStruct.AntiFeatureStruct());
            }

            Pattern.Acceptable = match => IsUnapplicationNonvacuous(match, rhsAntiFSs);

            _analysisRhs = new Pattern<Word, int>();
            int i = 0;
            foreach (
                Tuple<PatternNode<Word, int>, PatternNode<Word, int>> tuple in lhs.Children.Zip(subrule.Rhs.Children)
            )
            {
                var lhsConstraint = (Constraint<Word, int>)tuple.Item1;
                var rhsConstraint = (Constraint<Word, int>)tuple.Item2;

                if (lhsConstraint.Type() == HCFeatureSystem.Segment && rhsConstraint.Type() == HCFeatureSystem.Segment)
                {
                    Constraint<Word, int> targetConstraint = lhsConstraint.Clone();
                    targetConstraint.FeatureStruct.PriorityUnion(rhsConstraint.FeatureStruct);
                    targetConstraint.FeatureStruct.AddValue(HCFeatureSystem.Modified, HCFeatureSystem.Clean);
                    Pattern.Children.Add(new Group<Word, int>("target" + i) { Children = { targetConstraint } });

                    FeatureStruct fs = rhsConstraint.FeatureStruct.AntiFeatureStruct();
                    fs.Subtract(lhsConstraint.FeatureStruct.AntiFeatureStruct());
                    fs.AddValue(HCFeatureSystem.Type, HCFeatureSystem.Segment);
                    _analysisRhs.Children.Add(new Constraint<Word, int>(fs));

                    i++;
                }
            }
            Pattern.Freeze();

            SubruleSpecs.Add(new AnalysisRewriteSubruleSpec(matcherSettings, subrule, Unapply));
        }

        private bool IsUnapplicationNonvacuous(Match<Word, int> match, IEnumerable<FeatureStruct> rhsAntiFSs)
        {
            int i = 0;
            foreach (FeatureStruct fs in rhsAntiFSs)
            {
                ShapeNode node = match.Input.Shape.GetStartNode(
                    match.GroupCaptures["target" + i].Range,
                    match.Matcher.Direction
                );
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

        private void Unapply(Match<Word, int> targetMatch, Range<ShapeNode> range, VariableBindings varBindings)
        {
            int i = 0;
            foreach (Constraint<Word, int> constraint in _analysisRhs.Children.Cast<Constraint<Word, int>>())
            {
                ShapeNode node = targetMatch.Input.Shape.GetStartNode(
                    targetMatch.GroupCaptures["target" + i].Range,
                    targetMatch.Matcher.Direction
                );
                FeatureStruct fs = node.Annotation.FeatureStruct.Clone();
                fs.PriorityUnion(constraint.FeatureStruct);
                node.Annotation.FeatureStruct.Union(fs, varBindings);
                node.SetDirty(true);
                i++;
            }
        }
    }
}
