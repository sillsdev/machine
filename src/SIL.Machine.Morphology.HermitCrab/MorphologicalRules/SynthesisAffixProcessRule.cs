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
                            AnchoredToEnd = true
                        }
                    )
                );
            }
        }

        public IEnumerable<Word> Apply(Word input)
        {
            if (!input.IsMorphologicalRuleApplicable(_rule))
                return Enumerable.Empty<Word>();

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
                else if (_morpher.TraceManager.IsTracing)
                {
                    _morpher.TraceManager.MorphologicalRuleNotApplied(_rule, i, input, FailureReason.Pattern, null);
                }
            }

            return output;
        }
    }
}
