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
        private readonly bool[] _leftRejects;
        private readonly bool[] _rightRejects;

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

            IReadOnlyList<FeatureStruct> segments = morpher.EdgeIndex.Segments;
            _leftRejects = SegmentsRejectingEveryAllomorph(segments, _ruleSpecs.ConvertAll(s => s.LeftEdgeConstraint));
            _rightRejects = SegmentsRejectingEveryAllomorph(
                segments,
                _ruleSpecs.ConvertAll(s => s.RightEdgeConstraint)
            );
        }

        // Null when some allomorph has no edge constraint on that side or no segment fails them all.
        private static bool[] SegmentsRejectingEveryAllomorph(
            IReadOnlyList<FeatureStruct> segments,
            IReadOnlyList<FeatureStruct> constraints
        )
        {
            if (constraints.Count == 0 || constraints.Any(c => c == null))
                return null;
            bool[] rejects = null;
            for (int s = 0; s < segments.Count; s++)
            {
                if (constraints.All(c => !segments[s].IsUnifiable(c, false, null)))
                {
                    rejects = rejects ?? new bool[segments.Count];
                    rejects[s] = true;
                }
            }
            return rejects;
        }

        // The matcher can skip optional annotations, so only a mandatory edge can reject a match.
        private static bool EdgePasses(FeatureStruct constraint, ShapeNode node) =>
            constraint == null
            || node == null
            || node.Annotation.Optional
            || node.Annotation.FeatureStruct.IsUnifiable(constraint, false, null);

        private static bool Rejects(bool[] rejects, int segment) =>
            rejects != null && segment != AnalysisEdgeIndex.NoSegment && rejects[segment];

        private bool EdgeIndexRejects(Word input)
        {
            if (_leftRejects == null && _rightRejects == null)
                return false;
            (int left, int right) = _morpher.EdgeIndex.GetEdgeSegments(input);
            return Rejects(_leftRejects, left) || Rejects(_rightRejects, right);
        }

        public IEnumerable<Word> Apply(Word input)
        {
            if (!_morpher.RuleSelector(_rule))
                return Enumerable.Empty<Word>();

            // Tracing keeps the per-allomorph path so its failure events are unchanged.
            if (_morpher.EdgeIndexEnabled && !_morpher.TraceManager.IsTracing && EdgeIndexRejects(input))
            {
                _morpher.EdgeIndexRejected?.Invoke();
                return Enumerable.Empty<Word>();
            }

            if (
                input.GetUnapplicationCount(_rule) >= _rule.MaxApplicationCount
                || !_outputFS.IsUnifiable(input.SyntacticFeatureStruct)
            )
            {
                return Enumerable.Empty<Word>();
            }

            ShapeNode first = _morpher.EdgePrefilterEnabled ? AnalysisEdgeIndex.FindEdge(input.Shape, true) : null;
            ShapeNode last = _morpher.EdgePrefilterEnabled ? AnalysisEdgeIndex.FindEdge(input.Shape, false) : null;
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
                    if (!_morpher.HasPartialRules || _morpher.AlwaysEnforceFinalTemplates)
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
