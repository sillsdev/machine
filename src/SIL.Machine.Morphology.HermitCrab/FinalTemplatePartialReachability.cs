using System.Collections.Generic;
using System.Linq;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Finds the affix templates that a partial word can reach in synthesis. A word is partial at a template only
    /// if a partial affix-process rule applied before it (a partial root takes no template), in the template's
    /// stratum or an earlier one. Its part of speech is then whatever that rule leaves, changed by any rules
    /// applied since. The search over-approximates: it tracks only the part-of-speech feature, treats an
    /// unspecified or variable part of speech as every part of speech, ignores application counts, rule order,
    /// template gating and every other feature, and lets every rule of a stratum apply any number of times. A
    /// template it does not return can therefore never see a partial word.
    /// </summary>
    internal static class FinalTemplatePartialReachability
    {
        public static HashSet<AffixTemplate> Compute(Language lang)
        {
            var result = new HashSet<AffixTemplate>();
            SymbolicFeature posFeature = lang.SyntacticFeatureSystem.PartOfSpeechFeature;
            HashSet<FeatureSymbol> all =
                posFeature == null
                    ? new HashSet<FeatureSymbol>()
                    : new HashSet<FeatureSymbol>(posFeature.PossibleSymbols);

            // Part-of-speech values a partial word may carry on leaving the strata processed so far.
            var reach = new HashSet<FeatureSymbol>();
            bool anyPartial = false;
            foreach (Stratum stratum in lang.Strata) // synthesis order
            {
                List<IMorphologicalRule> rules = stratum
                    .MorphologicalRules.Concat(stratum.AffixTemplates.SelectMany(t => t.Slots).SelectMany(s => s.Rules))
                    .Distinct()
                    .ToList();

                foreach (AffixProcessRule rule in rules.OfType<AffixProcessRule>().Where(r => r.IsPartial))
                {
                    anyPartial = true;
                    reach.UnionWith(
                        Apply(all, all, rule.RequiredSyntacticFeatureStruct, rule.OutSyntacticFeatureStruct)
                    );
                }
                if (!anyPartial)
                    continue;
                if (all.Count == 0)
                {
                    // No parts of speech to tell templates apart: every later template is reachable.
                    result.UnionWith(stratum.AffixTemplates);
                    continue;
                }

                // Close the set under every rule of this stratum.
                bool changed = true;
                while (changed)
                {
                    changed = false;
                    foreach (IMorphologicalRule rule in rules)
                    {
                        if (reach.Count == 0)
                            break;
                        IEnumerable<FeatureSymbol> next = ApplyRule(all, reach, rule);
                        foreach (FeatureSymbol symbol in next.ToList())
                            changed |= reach.Add(symbol);
                    }
                }

                foreach (AffixTemplate template in stratum.AffixTemplates)
                {
                    if (reach.Count == 0)
                        continue;
                    HashSet<FeatureSymbol> required = Constraint(all, template.RequiredSyntacticFeatureStruct);
                    if (required == null || required.Overlaps(reach))
                        result.Add(template);
                }
            }
            return result;
        }

        private static IEnumerable<FeatureSymbol> ApplyRule(
            HashSet<FeatureSymbol> all,
            HashSet<FeatureSymbol> input,
            IMorphologicalRule rule
        )
        {
            switch (rule)
            {
                case AffixProcessRule apr:
                    return Apply(all, input, apr.RequiredSyntacticFeatureStruct, apr.OutSyntacticFeatureStruct);
                case CompoundingRule cr:
                    return Apply(all, input, cr.HeadRequiredSyntacticFeatureStruct, cr.OutSyntacticFeatureStruct);
                case RealizationalAffixProcessRule rr:
                    return Apply(all, input, rr.RequiredSyntacticFeatureStruct, rr.RealizationalFeatureStruct);
                default:
                    // Unknown rule kind: assume it can produce any part of speech.
                    return all;
            }
        }

        /// <summary>Unify with <paramref name="required"/>, then priority-union <paramref name="output"/>.</summary>
        private static IEnumerable<FeatureSymbol> Apply(
            HashSet<FeatureSymbol> all,
            HashSet<FeatureSymbol> input,
            FeatureStruct required,
            FeatureStruct output
        )
        {
            HashSet<FeatureSymbol> req = Constraint(all, required);
            bool applicable = req == null ? input.Count > 0 : req.Overlaps(input);
            if (!applicable)
                return Enumerable.Empty<FeatureSymbol>();
            HashSet<FeatureSymbol> outPos = Constraint(all, output);
            if (outPos != null)
                return outPos;
            if (req == null)
                return input;
            return input.Where(req.Contains);
        }

        /// <summary>The part-of-speech values a feature structure allows, or null if it leaves them open.</summary>
        private static HashSet<FeatureSymbol> Constraint(HashSet<FeatureSymbol> all, FeatureStruct fs)
        {
            if (fs == null)
                return null;
            if (!fs.TryGetValue(SyntacticFeatureSystem.PartOfSpeechID, out SymbolicFeatureValue value))
                return null;
            if (value.IsVariable)
                return null;
            var symbols = new HashSet<FeatureSymbol>(value.Values);
            // A value that names no known symbol is treated as unconstrained, to stay conservative.
            return symbols.Count == 0 ? null : symbols;
        }
    }
}
