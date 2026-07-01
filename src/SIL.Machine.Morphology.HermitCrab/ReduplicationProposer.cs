using System;
using System.Collections.Generic;
using SIL.Machine.Morphology;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// A candidate generator for <b>full reduplication</b> (copy the whole base, surface = base·base) —
    /// the one provably non-regular construct (Dolatian &amp; Heinz 2020), handled <i>beside</i> the FST
    /// rather than inside it (FST_FULL_PLAN.md, Point 3). It does not need to be regular because the
    /// <see cref="VerifiedFstAnalyzer"/> gate re-runs HC to confirm every candidate.
    ///
    /// Mechanism (strip + recurse): if the surface is an adjacent doubling X·X, strip one copy and
    /// <b>recurse the residual X through the FST proposer</b> — so an <i>inflected</i> reduplicant
    /// (e.g. REDUP of an affixed stem) is covered, not just a bare root — then wrap each returned base
    /// analysis with the reduplication morpheme (prepended, matching HC's <c>RED root …</c> order).
    /// Bounded to a single full-copy application (the residual is itself analyzed by the FST, which is
    /// where any further structure lives); "well enough" for the attested cases, and anything it misses
    /// simply fails parity and rides the engine — never a wrong answer.
    ///
    /// Soundness: a coincidental doubling (a word that merely looks like X·X but is not reduplicated)
    /// is proposed but pruned by verify, because HC's synthesis of <c>base + REDUP</c> will not
    /// reproduce it.
    /// </summary>
    public class ReduplicationProposer : IConstructProposer
    {
        private static readonly MorphOp[] _ops = { MorphOp.Reduplication };
        private readonly IMorphologicalAnalyzer _baseProposer;
        private readonly List<MorphemicMorphologicalRule> _redupRules;

        public ReduplicationProposer(Language language, IMorphologicalAnalyzer baseProposer)
        {
            _baseProposer = baseProposer;
            _redupRules = new List<MorphemicMorphologicalRule>();
            foreach (Stratum stratum in language.Strata)
            {
                foreach (IMorphologicalRule mrule in stratum.MorphologicalRules)
                {
                    if (mrule is MorphemicMorphologicalRule rule && IsReduplication(rule))
                    {
                        _redupRules.Add(rule);
                    }
                }
            }
        }

        public IReadOnlyCollection<MorphOp> CoveredOps => _ops;

        public IEnumerable<WordAnalysis> AnalyzeWord(string word)
        {
            // Full-copy detection: an even-length surface whose two halves are identical. The residual
            // (one copy) is recursed through the FST proposer so inflected reduplicants are covered.
            if (_redupRules.Count == 0 || word.Length < 2 || (word.Length & 1) == 1)
            {
                yield break;
            }
            int half = word.Length / 2;
            if (!string.Equals(word.Substring(0, half), word.Substring(half), StringComparison.Ordinal))
            {
                yield break;
            }
            string residual = word.Substring(0, half);
            foreach (WordAnalysis baseAnalysis in _baseProposer.AnalyzeWord(residual))
            {
                foreach (MorphemicMorphologicalRule redup in _redupRules)
                {
                    // Application order: root (and its affixes) then the reduplication rule, matching
                    // HC's WordAnalysis.Morphemes (root·…·RED), so the root index is unchanged.
                    var morphemes = new List<IMorpheme>(baseAnalysis.Morphemes) { redup };
                    yield return new WordAnalysis(morphemes, baseAnalysis.RootMorphemeIndex, null);
                }
            }
        }

        private static bool IsReduplication(MorphemicMorphologicalRule rule)
        {
            if (!(rule is AffixProcessRule affix))
            {
                return false;
            }
            foreach (AffixProcessAllomorph allomorph in affix.Allomorphs)
            {
                if (MorphTokenCodec.ClassifyOp(allomorph, false) == MorphOp.Reduplication)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
