using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// A candidate generator for <b>boundary-conditioned morphophonemics</b> — the case the phonology
    /// inverse cannot handle (FST_FULL_PLAN.md, Point 1/4 reconciliation). Indonesian <c>meN-</c> nasal
    /// substitution (tulis → me<b>n</b>ulis, the prefix nasal assimilates AND the root-initial deletes)
    /// is conditioned on the morpheme boundary, so un-applying phonology on the bare surface
    /// over-generates and cannot be cleanly composed. <b>Forward synthesis</b>, however, is
    /// boundary-correct: <see cref="Morpher.GenerateWords"/> applies the rules with the boundary present
    /// and yields the true surface.
    ///
    /// So at build time this enumerates each root × every bounded combination of morphological rules,
    /// synthesizes the surface form(s), and tabulates <c>surface → (root + affixes)</c>. Analysis is then
    /// a dictionary lookup; <see cref="VerifiedFstAnalyzer"/> still confirms each candidate (and supplies
    /// the real category). Sound by construction (a tabulated entry is a real synthesized word) and
    /// covers every construct synthesis handles — including reduplication and infixation, which it gets
    /// for free. The cost is build-time enumeration (root × affix-combos), bounded by
    /// <c>maxAffixes</c> and a hard <c>budget</c>; it trades the FST's compactness for coverage, which
    /// fits languages with bounded productive affixation (it does not scale to heavily-inflecting
    /// templatic systems — those keep riding the engine via the parity gate).
    /// </summary>
    public class ForwardSynthesisProposer : IConstructProposer
    {
        private readonly Dictionary<string, List<WordAnalysis>> _table =
            new Dictionary<string, List<WordAnalysis>>(StringComparer.Ordinal);
        private readonly MorphOp[] _coveredOps;

        /// <summary>Build the surface→analysis table. <paramref name="maxAffixes"/> bounds how many
        /// morphological rules may co-occur on a word; <paramref name="budget"/> hard-caps the number of
        /// tabulated entries so a productive grammar degrades (fewer combos) rather than exploding.</summary>
        public ForwardSynthesisProposer(Language language, Morpher morpher, int maxAffixes = 2, int budget = 500_000)
        {
            var rules = language
                .Strata.SelectMany(s => s.MorphologicalRules)
                .OfType<MorphemicMorphologicalRule>()
                .ToList();
            var roots = language.Strata.SelectMany(s => s.Entries).ToList();
            var covered = new HashSet<MorphOp>();
            int entries = 0;
            bool capped = false;

            foreach (LexEntry root in roots)
            {
                foreach (List<MorphemicMorphologicalRule> combo in Combinations(rules, maxAffixes))
                {
                    if (entries >= budget)
                    {
                        capped = true;
                        break;
                    }
                    IReadOnlyCollection<string> surfaces;
                    try
                    {
                        surfaces = morpher
                            .GenerateWords(root, combo, new FeatureStruct())
                            .Select(Normalize)
                            .Distinct()
                            .ToList();
                    }
                    catch (Exception)
                    {
                        continue; // an invalid combo (category clash, obligatoriness) — synthesis declines
                    }
                    if (surfaces.Count == 0)
                    {
                        continue;
                    }
                    WordAnalysis analysis = BuildAnalysis(root, combo);
                    foreach (string surface in surfaces)
                    {
                        if (!_table.TryGetValue(surface, out List<WordAnalysis> list))
                        {
                            list = new List<WordAnalysis>();
                            _table[surface] = list;
                        }
                        list.Add(analysis);
                        entries++;
                    }
                    foreach (MorphemicMorphologicalRule rule in combo)
                    {
                        covered.Add(RuleOp(rule));
                    }
                }
                if (capped)
                {
                    break;
                }
            }
            // Only the constructs the FST proposer cannot build are worth claiming; concatenative
            // prefix/suffix are already covered there. Parity remains the real certification arbiter.
            _coveredOps = covered
                .Where(o => o == MorphOp.Reduplication || o == MorphOp.Infix || o == MorphOp.Process)
                .ToArray();
            WasCapped = capped;
            EntryCount = entries;
        }

        /// <summary>True if the entry budget was hit (coverage is partial; more combos were skipped).</summary>
        public bool WasCapped { get; }

        /// <summary>Number of tabulated surface→analysis entries.</summary>
        public int EntryCount { get; }

        public IReadOnlyCollection<MorphOp> CoveredOps => _coveredOps;

        public IEnumerable<WordAnalysis> AnalyzeWord(string word)
        {
            return _table.TryGetValue(Normalize(word), out List<WordAnalysis> list)
                ? list
                : Enumerable.Empty<WordAnalysis>();
        }

        /// <summary>The candidate analysis in HC application order: prefixes, then the root, then the
        /// remaining affixes (suffix/reduplication/infix). Verify confirms the order against the engine.</summary>
        private static WordAnalysis BuildAnalysis(LexEntry root, List<MorphemicMorphologicalRule> combo)
        {
            var prefixes = new List<IMorpheme>();
            var rest = new List<IMorpheme>();
            foreach (MorphemicMorphologicalRule rule in combo)
            {
                if (RuleOp(rule) == MorphOp.Prefix)
                {
                    prefixes.Add(rule);
                }
                else
                {
                    rest.Add(rule);
                }
            }
            var morphemes = new List<IMorpheme>(prefixes.Count + 1 + rest.Count);
            morphemes.AddRange(prefixes);
            morphemes.Add(root);
            morphemes.AddRange(rest);
            return new WordAnalysis(morphemes, prefixes.Count, null);
        }

        private static MorphOp RuleOp(MorphemicMorphologicalRule rule)
        {
            IEnumerable<AffixProcessAllomorph> allomorphs;
            switch (rule)
            {
                case AffixProcessRule affix:
                    allomorphs = affix.Allomorphs;
                    break;
                case RealizationalAffixProcessRule realizational:
                    allomorphs = realizational.Allomorphs;
                    break;
                default:
                    return MorphOp.None;
            }
            foreach (AffixProcessAllomorph allomorph in allomorphs)
            {
                return MorphTokenCodec.ClassifyOp(allomorph, false);
            }
            return MorphOp.None;
        }

        private static string Normalize(string s) => s.Normalize(System.Text.NormalizationForm.FormD);

        /// <summary>All ORDERED sequences of 0..<paramref name="max"/> distinct rules. Order matters:
        /// <see cref="Morpher.GenerateWords"/> is sensitive to the morpheme-list order (meN·Cont yields
        /// the real "menulis-nulis"; Cont·meN yields a different form), so every permutation is tried —
        /// the wrong orders simply tabulate forms that are never queried.</summary>
        private static IEnumerable<List<MorphemicMorphologicalRule>> Combinations(
            List<MorphemicMorphologicalRule> rules,
            int max
        )
        {
            yield return new List<MorphemicMorphologicalRule>();
            for (int size = 1; size <= max; size++)
            {
                foreach (List<MorphemicMorphologicalRule> seq in PermutationsOfSize(rules, size, new bool[rules.Count]))
                {
                    yield return seq;
                }
            }
        }

        private static IEnumerable<List<MorphemicMorphologicalRule>> PermutationsOfSize(
            List<MorphemicMorphologicalRule> rules,
            int size,
            bool[] used
        )
        {
            if (size == 0)
            {
                yield return new List<MorphemicMorphologicalRule>();
                yield break;
            }
            for (int i = 0; i < rules.Count; i++)
            {
                if (used[i])
                {
                    continue;
                }
                used[i] = true;
                foreach (List<MorphemicMorphologicalRule> tail in PermutationsOfSize(rules, size - 1, used))
                {
                    tail.Insert(0, rules[i]);
                    yield return tail;
                }
                used[i] = false;
            }
        }
    }
}
