using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class AnalysisAffixProcessRule : IRule<Word, ShapeNode>
    {
        private readonly Morpher _morpher;
        private readonly AffixProcessRule _rule;
        private readonly List<PatternRule<Word, ShapeNode>> _rules;
        private readonly List<AnalysisAffixProcessAllomorphRuleSpec> _ruleSpecs;
        private readonly FeatureStruct _outputFS;

        public AnalysisAffixProcessRule(Morpher morpher, AffixProcessRule rule)
        {
            _morpher = morpher;
            _rule = rule;

            // synthesis computes PriorityUnion(Unify(stem, Required), Out), so every word this rule produces is
            // subsumed by PriorityUnion(Required, Out): a tighter gate than Out alone that still admits them all
            _outputFS = rule.RequiredSyntacticFeatureStruct.Clone();
            _outputFS.PriorityUnion(rule.OutSyntacticFeatureStruct);
            _outputFS.Freeze();

            _rules = new List<PatternRule<Word, ShapeNode>>();
            _ruleSpecs = new List<AnalysisAffixProcessAllomorphRuleSpec>();
            foreach (AffixProcessAllomorph allo in rule.Allomorphs)
            {
                var spec = new AnalysisAffixProcessAllomorphRuleSpec(allo) { PruningMorpher = morpher };
                _ruleSpecs.Add(spec);
                _rules.Add(
                    new MultiplePatternRule<Word, ShapeNode>(
                        spec,
                        new MatcherSettings<ShapeNode>
                        {
                            Filter = ann => ann.Type() == HCFeatureSystem.Segment,
                            MatchingMethod = MatchingMethod.Unification,
                            AnchoredToStart = true,
                            AnchoredToEnd = true,
                            AllSubmatches = true,
                        }
                    )
                );
            }
        }

        private static ShapeNode FindEdge(Shape shape, bool first)
        {
            ShapeNode node = first ? shape.First : shape.Last;
            ShapeNode end = first ? shape.End : shape.Begin;
            while (node != null && node != end && node.Type() != HCFeatureSystem.Segment)
                node = first ? node.Next : node.Prev;
            return node == end ? null : node;
        }

        // The matcher can skip optional annotations, so only a mandatory edge can reject a match.
        private static bool EdgePasses(FeatureStruct constraint, ShapeNode node) =>
            constraint == null
            || node == null
            || node.Annotation.Optional
            || node.Annotation.FeatureStruct.IsUnifiable(constraint, false, null);

        public IEnumerable<Word> Apply(Word input)
        {
            if (!_morpher.RuleSelector(_rule))
                return Enumerable.Empty<Word>();

            if (
                input.GetUnapplicationCount(_rule) >= _rule.MaxApplicationCount
                || !_outputFS.IsUnifiable(input.SyntacticFeatureStruct)
            )
            {
                return Enumerable.Empty<Word>();
            }

            ShapeNode first = _morpher.EdgePrefilterEnabled ? FindEdge(input.Shape, true) : null;
            ShapeNode last = _morpher.EdgePrefilterEnabled ? FindEdge(input.Shape, false) : null;
            var output = new List<Word>();
            for (int i = 0; i < _rules.Count; i++)
            {
                if (
                    _morpher.EdgePrefilterEnabled
                    && (
                        !EdgePasses(_ruleSpecs[i].LeftEdgeConstraint, first)
                        || !EdgePasses(_ruleSpecs[i].RightEdgeConstraint, last)
                    )
                )
                {
                    _morpher.EdgePrefilterRejected?.Invoke();
                    if (_morpher.TraceManager.IsTracing)
                        _morpher.TraceManager.MorphologicalRuleNotUnapplied(_rule, i, input);
                    continue;
                }
                bool unapplied = false;
                foreach (Word outWord in _rules[i].Apply(input).RemoveDuplicates())
                {
                    // the paths Out writes describe this rule's output, not the stem it applied to, so they are
                    // stripped rather than carried backwards; what is left must unify with Required
                    outWord.SyntacticFeatureStruct.Restrict(_rule.OutSyntacticFeatureStruct);
                    if (!_rule.RequiredSyntacticFeatureStruct.IsEmpty)
                    {
                        if (
                            !outWord.SyntacticFeatureStruct.Unify(
                                _rule.RequiredSyntacticFeatureStruct,
                                out FeatureStruct syntacticFS
                            )
                        )
                        {
                            continue;
                        }

                        outWord.SyntacticFeatureStruct = syntacticFS;
                    }
                    if (!_morpher.IsPartial || _morpher.AlwaysEnforceFinalTemplates)
                    {
                        if (!_rule.GetIsCliticRule())
                        {
                            outWord.FinalTemplateState = !_rule.IsTemplateRule
                                ? FinalTemplateState.NonTemplate
                                : FinalTemplateState.None;
                        }
                    }
                    outWord.MorphologicalRuleUnapplied(_rule, _rule.Allomorphs[i]);
                    outWord.Freeze();
                    if (_morpher.TraceManager.IsTracing)
                        _morpher.TraceManager.MorphologicalRuleUnapplied(_rule, i, input, outWord);
                    output.Add(outWord);
                    unapplied = true;
                }

                if (_morpher.TraceManager.IsTracing && !unapplied)
                    _morpher.TraceManager.MorphologicalRuleNotUnapplied(_rule, i, input);
            }
            return output;
        }
    }
}
