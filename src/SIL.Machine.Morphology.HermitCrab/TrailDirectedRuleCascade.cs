using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Synthesis-side replacement for <see cref="CombinationRuleCascade{TData,TOffset}"/> on
    /// <see cref="MorphologicalRuleOrder.Unordered"/> strata. Unlike analysis -- where any subset/order of
    /// morphological rules is a live hypothesis and every rule genuinely must be tried -- synthesis already
    /// knows, from the trail recorded during analysis (<see cref="Word.TryGetNextMorphologicalRuleToApply"/>),
    /// exactly which single rule (or, for an unresolved compounding rule, which subset) can possibly apply
    /// next. The unmodified cascade still probes the entire rule battery at every node and lets
    /// <see cref="Word.IsMorphologicalRuleApplicable"/> reject the misses -- reject calls that are pure
    /// overhead: <see cref="MorphologicalRules.SynthesisAffixProcessRule.Apply"/> and
    /// <see cref="SynthesisCompoundingRule.Apply"/> both return empty on that check with no trace call, so
    /// skipping the attempt entirely changes neither the result set nor trace output. Realizational affix
    /// rules are excluded from the trail (<see cref="Word.MorphologicalRuleUnapplied"/>) and self-govern via
    /// feature-structure checks instead, so they are always attempted, exactly as before.
    /// </summary>
    internal class TrailDirectedRuleCascade : InstrumentedRule<Word, int>
    {
        // Preserves the stratum's original rule order: when more than one rule can apply at a single node
        // (a realizational rule alongside the trail-directed rule, or multiple compounding rules), trace
        // calls must fire in the same relative order the unmodified all-rules cascade produced.
        private readonly List<(IMorphologicalRule MorphologicalRule, IRule<Word, int> CompiledRule)> _rules;
        private readonly IEqualityComparer<Word> _comparer;

        public TrailDirectedRuleCascade(
            IEnumerable<(IMorphologicalRule MorphologicalRule, IRule<Word, int> CompiledRule)> rules,
            IEqualityComparer<Word> comparer
        )
        {
            Name = "TrailDirectedRuleCascade";
            _rules = new List<(IMorphologicalRule, IRule<Word, int>)>(rules);
            _comparer = comparer;
            AddSubRules(_rules.Select(p => p.CompiledRule));
        }

        public override IEnumerable<Word> Apply(Word input)
        {
            var output = new HashSet<Word>(_comparer);
            ApplyRules(input, output);
            AddRuleStats(output.Count);
            return output;
        }

        private void ApplyRules(Word input, HashSet<Word> output)
        {
            bool hasNext = input.TryGetNextMorphologicalRuleToApply(out IMorphologicalRule next);
            foreach ((IMorphologicalRule mrule, IRule<Word, int> compiled) in _rules)
            {
                bool attempt;
                if (mrule is RealizationalAffixProcessRule)
                    attempt = true;
                else if (!hasNext)
                    attempt = false;
                else if (next == null)
                    attempt = mrule is CompoundingRule;
                else
                    attempt = ReferenceEquals(mrule, next);

                if (!attempt)
                    continue;

                foreach (Word result in compiled.Apply(input))
                {
                    if (!_comparer.Equals(input, result))
                        ApplyRules(result, output);
                    output.Add(result);
                }
            }
        }
    }
}
