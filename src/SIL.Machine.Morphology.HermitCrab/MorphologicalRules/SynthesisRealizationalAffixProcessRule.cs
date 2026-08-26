using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class SynthesisRealizationalAffixProcessRule : IRule<Word, ShapeNode>
    {
        private readonly Morpher _morpher;
        private readonly RealizationalAffixProcessRule _rule;
        private readonly List<PatternRule<Word, ShapeNode>> _rules;

        public SynthesisRealizationalAffixProcessRule(Morpher morpher, RealizationalAffixProcessRule rule)
        {
            _morpher = morpher;
            _rule = rule;
            _rules = new List<PatternRule<Word, ShapeNode>>();
            foreach (AffixProcessAllomorph allo in rule.Allomorphs)
            {
                _rules.Add(
                    new PatternRule<Word, ShapeNode>(
                        new SynthesisAffixProcessAllomorphRuleSpec(allo),
                        new MatcherSettings<ShapeNode>
                        {
                            Filter = ann =>
                                ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary)
                                && !ann.IsDeleted(),
                            AnchoredToStart = true,
                            AnchoredToEnd = true,
                        }
                    )
                );
            }
        }

        public IEnumerable<Word> Apply(Word input)
        {
            if (!_morpher.RuleSelector(_rule))
                return Enumerable.Empty<Word>();

            // RealizationalRule has no multipleApplication attribute, so it applies at most once per
            // word; otherwise a rule cascade that retries a matching rule against its own output would
            // never terminate.
            if (input.GetApplicationCount(_rule) >= 1)
            {
                if (_morpher.TraceManager.IsTracing)
                {
                    _morpher.TraceManager.MorphologicalRuleNotApplied(
                        _rule,
                        -1,
                        input,
                        FailureReason.MaxApplicationCount,
                        1
                    );
                }
                SynthesisProbe.RecordDie(SynthesisDiePoint.ApplicationCount);
                return Enumerable.Empty<Word>();
            }

            if (!_rule.RealizationalFeatureStruct.Subsumes(input.RealizationalFeatureStruct))
            {
                SynthesisProbe.RecordDie(SynthesisDiePoint.RealizationalSubsumptionOrBlocking);
                return Enumerable.Empty<Word>();
            }

            if (
                !_rule.RealizationalFeatureStruct.IsEmpty
                && IsBlocked(
                    _rule.RealizationalFeatureStruct,
                    input.SyntacticFeatureStruct,
                    new HashSet<Tuple<FeatureStruct, FeatureStruct>>()
                )
            )
            {
                SynthesisProbe.RecordDie(SynthesisDiePoint.RealizationalSubsumptionOrBlocking);
                return Enumerable.Empty<Word>();
            }

            FeatureStruct syntacticFS;
            if (!_rule.RequiredSyntacticFeatureStruct.Unify(input.SyntacticFeatureStruct, true, out syntacticFS))
            {
                if (_morpher.TraceManager.IsTracing)
                {
                    _morpher.TraceManager.MorphologicalRuleNotApplied(
                        _rule,
                        -1,
                        input,
                        FailureReason.RequiredSyntacticFeatureStruct,
                        _rule.RequiredSyntacticFeatureStruct
                    );
                }
                SynthesisProbe.RecordDie(SynthesisDiePoint.FeatureUnification);
                return Enumerable.Empty<Word>();
            }

            var appliedAllomorphIndices = new HashSet<int>();
            var output = new List<Word>();
            for (int i = 0; i < _rules.Count; i++)
            {
                AffixProcessAllomorph allo = _rule.Allomorphs[i];
                MprFeatureGroup group;
                if (
                    allo.RequiredMprFeatures.Count > 0
                    && !allo.RequiredMprFeatures.IsMatchRequired(input.MprFeatures, out group)
                )
                {
                    if (_morpher.TraceManager.IsTracing)
                    {
                        _morpher.TraceManager.MorphologicalRuleNotApplied(
                            _rule,
                            i,
                            input,
                            FailureReason.RequiredMprFeatures,
                            group
                        );
                    }
                    SynthesisProbe.RecordDie(SynthesisDiePoint.MprFeatures);
                    continue;
                }
                if (
                    allo.ExcludedMprFeatures.Count > 0
                    && !allo.ExcludedMprFeatures.IsMatchExcluded(input.MprFeatures, out group)
                )
                {
                    if (_morpher.TraceManager.IsTracing)
                    {
                        _morpher.TraceManager.MorphologicalRuleNotApplied(
                            _rule,
                            i,
                            input,
                            FailureReason.ExcludedMprFeatures,
                            group
                        );
                    }
                    SynthesisProbe.RecordDie(SynthesisDiePoint.MprFeatures);
                    continue;
                }

                Word outWord = _rules[i].Apply(input).SingleOrDefault();
                if (outWord != null)
                {
                    outWord.SyntacticFeatureStruct = syntacticFS;
                    outWord.SyntacticFeatureStruct.PriorityUnion(_rule.RealizationalFeatureStruct);

                    outWord.MorphologicalRuleApplied(_rule, appliedAllomorphIndices);
                    appliedAllomorphIndices.Add(i);

                    if (_rule.Blockable && outWord.CheckBlocking(out Word newWord))
                    {
                        if (_morpher.TraceManager.IsTracing)
                            _morpher.TraceManager.Blocked(_rule, newWord);
                        outWord = newWord;
                    }
                    else
                    {
                        outWord.Freeze();
                    }

                    if (_morpher.TraceManager.IsTracing)
                        _morpher.TraceManager.MorphologicalRuleApplied(_rule, i, input, outWord);

                    output.Add(outWord);

                    // return all word syntheses that match subrules that are constrained by environments,
                    // HC violates the disjunctive property of allomorphs here because it cannot check the
                    // environmental constraints until it has a surface form, we will enforce the disjunctive
                    // property of allomorphs at that time

                    // HC also checks for free fluctuation, if the next subrule has the same constraints, we
                    // do not treat them as disjunctive
                    if (
                        (i != _rule.Allomorphs.Count - 1 && !allo.FreeFluctuatesWith(_rule.Allomorphs[i + 1]))
                        && allo.Environments.Count == 0
                        && allo.RequiredSyntacticFeatureStruct.IsEmpty
                    )
                    {
                        break;
                    }
                }
                else
                {
                    if (_morpher.TraceManager.IsTracing)
                    {
                        _morpher.TraceManager.MorphologicalRuleNotApplied(
                            _rule,
                            i,
                            input,
                            FailureReason.Pattern,
                            null
                        );
                    }
                    SynthesisProbe.RecordDie(SynthesisDiePoint.RuleNotApplicableOrPatternMismatch);
                }
            }

            // Recorded once for the whole call, not per allomorph -- see the matching comment in
            // SynthesisAffixProcessRule.Apply: several allomorphs can legitimately all pattern-match one
            // input, so (fingerprint, rule) is a set-valued fold step here.
            if (output.Count > 0)
                SynthesisProbe.RecordApplications(input, _rule, output);

            return output;
        }

        private bool IsBlocked(
            FeatureStruct realizationalFS,
            FeatureStruct syntacticFS,
            HashSet<Tuple<FeatureStruct, FeatureStruct>> visited
        )
        {
            Tuple<FeatureStruct, FeatureStruct> pair = Tuple.Create(realizationalFS, syntacticFS);
            if (visited.Contains(pair))
                return true;

            visited.Add(pair);

            foreach (Feature f in realizationalFS.Features)
            {
                if (!syntacticFS.ContainsFeature(f))
                    return false;

                if (f is ComplexFeature cf)
                {
                    FeatureStruct realFS = realizationalFS.GetValue(cf);
                    FeatureStruct synFS = syntacticFS.GetValue(cf);
                    if (!IsBlocked(realFS, synFS, visited))
                        return false;
                }
            }

            return true;
        }
    }
}
