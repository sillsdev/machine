using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class SynthesisAffixProcessRule : IRule<Word, ShapeNode>
    {
        private readonly Morpher _morpher;
        private readonly AffixProcessRule _rule;
        private readonly List<PatternRule<Word, ShapeNode>> _rules;

        public SynthesisAffixProcessRule(Morpher morpher, AffixProcessRule rule)
        {
            _morpher = morpher;
            _rule = rule;
            _rules = new List<PatternRule<Word, ShapeNode>>();
            foreach (AffixProcessAllomorph allo in rule.Allomorphs)
            {
                var ruleSpec = new SynthesisAffixProcessAllomorphRuleSpec(allo);
                _rules.Add(
                    new PatternRule<Word, ShapeNode>(
                        ruleSpec,
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
            // The trail-position gate stays outside the memo unconditionally, on both the memoized and
            // unmemoized paths: it is an O(1) index-plus-reference check (docs/hermitcrab-synthesis-fold-probes.md
            // section 3's "~40x free" observation), so a SynthesisStateKey is never worth constructing for
            // the ~72-73% of rejections that die here (section 6.1's P1b histogram).
            if (!input.IsMorphologicalRuleApplicable(_rule))
            {
                SynthesisProbe.RecordDie(SynthesisDiePoint.RuleNotApplicableOrPatternMismatch);
                return Enumerable.Empty<Word>();
            }

            SynthesisFoldScope foldScope = _morpher.UseSynthesisFoldMemo ? input.SynthesisFoldScope : null;
            if (foldScope == null)
                return ApplyMatchingAllomorphs(input);

            SynthesisStateKey key = SynthesisStateKey.PinAndKey(input);
            if (foldScope.TryGet(key, _rule, out IReadOnlyList<Word> stored))
            {
                foldScope.Hits++;
                var replayed = new List<Word>(stored.Count);
                foreach (Word storedOutput in stored)
                    replayed.Add(storedOutput.ReanchorSynthesisStep(input, trailConsuming: true));
                return replayed;
            }

            var computed = ApplyMatchingAllomorphs(input).ToList();
            foldScope.Store(key, _rule, computed);
            return computed;
        }

        // Everything past the trail-position gate: this is the expensive part of the step (per-allomorph
        // MPR checks, pattern matching, unification), and everything it reads is covered by
        // SynthesisStateKey -- see that class's doc comment for the field-by-field audit.
        private IEnumerable<Word> ApplyMatchingAllomorphs(Word input)
        {
            if (input.GetApplicationCount(_rule) >= _rule.MaxApplicationCount)
            {
                if (_morpher.TraceManager.IsTracing)
                {
                    _morpher.TraceManager.MorphologicalRuleNotApplied(
                        _rule,
                        -1,
                        input,
                        FailureReason.MaxApplicationCount,
                        _rule.MaxApplicationCount
                    );
                }
                SynthesisProbe.RecordDie(SynthesisDiePoint.ApplicationCount);
                return Enumerable.Empty<Word>();
            }

            // if a final template was last applied,
            // do not allow a non-partial rule to apply unless the input is partial
            if (
                !_rule.IsTemplateRule
                && (input.IsLastAppliedRuleFinal ?? false)
                && !input.IsPartial
                && !_rule.IsPartial
            )
            {
                if (_morpher.TraceManager.IsTracing)
                {
                    _morpher.TraceManager.MorphologicalRuleNotApplied(
                        _rule,
                        -1,
                        input,
                        FailureReason.NonPartialRuleProhibitedAfterFinalTemplate,
                        null
                    );
                }
                SynthesisProbe.RecordDie(SynthesisDiePoint.RuleNotApplicableOrPatternMismatch);
                return Enumerable.Empty<Word>();
            }

            // if a non-final template was last applied,
            // only allow a non-partial rule to apply unless the input is partial
            if (
                !_rule.IsTemplateRule
                && input.IsLastAppliedRuleFinal.HasValue
                && !input.IsLastAppliedRuleFinal.Value
                && !input.IsPartial
                && _rule.IsPartial
            )
            {
                if (_morpher.TraceManager.IsTracing)
                {
                    _morpher.TraceManager.MorphologicalRuleNotApplied(
                        _rule,
                        -1,
                        input,
                        FailureReason.NonPartialRuleRequiredAfterNonFinalTemplate,
                        null
                    );
                }
                SynthesisProbe.RecordDie(SynthesisDiePoint.RuleNotApplicableOrPatternMismatch);
                return Enumerable.Empty<Word>();
            }

            if (_rule.RequiredStemName != null && _rule.RequiredStemName != input.RootAllomorph.StemName)
            {
                if (_morpher.TraceManager.IsTracing)
                {
                    _morpher.TraceManager.MorphologicalRuleNotApplied(
                        _rule,
                        -1,
                        input,
                        FailureReason.RequiredStemName,
                        _rule.RequiredStemName
                    );
                }
                SynthesisProbe.RecordDie(SynthesisDiePoint.RuleNotApplicableOrPatternMismatch);
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
                    outWord.SyntacticFeatureStruct.PriorityUnion(_rule.OutSyntacticFeatureStruct);

                    foreach (Feature obligFeature in _rule.ObligatorySyntacticFeatures)
                        outWord.ObligatorySyntacticFeatures.Add(obligFeature);

                    if (!_rule.IsTemplateRule)
                    {
                        if (_rule.IsPartial)
                            outWord.IsPartial = true;
                        else
                            outWord.IsLastAppliedRuleFinal = null;
                    }

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

            // Recorded once for the whole call, not per allomorph: several allomorphs of the same _rule
            // can legitimately all pattern-match the same input before the disjunctive-environment break
            // above, so (fingerprint, rule) is properly a set-valued fold step here -- exactly the shape
            // the plan doc's second trap already calls out for realizational rules ("any stored partial
            // must be a set, like MemoEntry.Results, not a value"). Recording per allomorph instead would
            // make ordinary disjunctive fan-out look like a determinism violation.
            if (output.Count > 0)
                SynthesisProbe.RecordApplications(input, _rule, output);

            return output;
        }
    }
}
