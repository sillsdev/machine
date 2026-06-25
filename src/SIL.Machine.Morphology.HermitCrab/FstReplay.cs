using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Verification by <b>restricted re-analysis</b> (HERMITCRAB_FST_PLAN.md §11.8, Route A): confirm
    /// an FST candidate by running HC's own <see cref="Morpher.AnalyzeWord"/> with the rule/lexicon
    /// selectors pinned to <i>just this candidate's root and rules</i>. That prunes HC's combinatorial
    /// fan-out to the single path the FST already found — a few ms, not the full search — while reusing
    /// HC's real analysis+synthesis validation end to end (no reimplemented constraints).
    ///
    /// A candidate is valid iff HC's restricted analysis of the surface yields it: restriction can only
    /// remove paths HC would not take, never fabricate one (HC still runs full synthesis + surface
    /// match), so membership in the restricted result is exactly "is a valid HC analysis". The Morpher
    /// is <see cref="MorpherPool.Rent"/>ed so concurrent verification is thread-safe (the selectors are
    /// mutable instance state). The <b>matched HC analysis is returned</b> (not the FST candidate) so
    /// the caller emits a genuine engine <see cref="WordAnalysis"/> — with its real category — rather
    /// than the category-less proposal.
    ///
    /// Compounds (Phase G2, FST_FULL_GRAMMAR_PLAN.md): a candidate may contain a SECOND <see cref="LexEntry"/>
    /// at a non-head position (the FST's compound loop proposes one candidate per head choice — see
    /// <c>FstTemplateAnalyzer.ToWordAnalyses</c>). That second root is admitted into the lexicon
    /// selector alongside the head, and <see cref="CompoundingRule"/> is opened in the rule selector
    /// (only when a compound is actually present, so an ordinary word's fan-out stays exactly as tight
    /// as before). This was the ONLY real blocker for compounding — earlier documentation claiming a
    /// cross-cutting `WordAnalysis`/`MorphToken` data-model lift was needed was wrong; both types already
    /// represent compounds (see `MorphOp.Compound`), as this fix demonstrates.
    /// </summary>
    internal static class FstReplay
    {
        /// <summary>The matched HC analysis of <paramref name="word"/> equal to <paramref name="candidate"/>, or null if HC does not produce it.</summary>
        public static WordAnalysis Confirm(MorpherPool pool, WordAnalysis candidate, string word)
        {
            int rootIndex = candidate.RootMorphemeIndex;
            IReadOnlyList<IMorpheme> morphemes = candidate.Morphemes;
            if (rootIndex < 0 || rootIndex >= morphemes.Count || !(morphemes[rootIndex] is LexEntry root))
            {
                return null;
            }

            var rules = new HashSet<IHCRule>();
            var extraRoots = new HashSet<LexEntry>();
            for (int i = 0; i < morphemes.Count; i++)
            {
                if (i == rootIndex)
                {
                    continue;
                }
                if (morphemes[i] is LexEntry nonHeadRoot)
                {
                    extraRoots.Add(nonHeadRoot); // a compound's non-head root, not a rule
                    continue;
                }
                if (!(morphemes[i] is IHCRule rule))
                {
                    return null;
                }
                rules.Add(rule);
            }

            Morpher morpher = pool.Rent();
            try
            {
                // Pin HC to this candidate's path: only this root (plus any compound non-head roots),
                // only its morphological rules. Templates and strata stay open (they are containers the
                // path threads through), and phonological rules ALWAYS stay open — they are obligatory,
                // deterministic rewrites, not a fan-out choice, and un-applying them is exactly how a
                // phonologically-altered surface (e.g. an FST candidate proposed from a surface
                // allomorph) reduces back to its root. Gating only the leaf morphological rules + the
                // root(s) is what collapses the fan-out.
                morpher.LexEntrySelector = e => e == root || extraRoots.Contains(e);
                morpher.RuleSelector = r =>
                    r is AffixTemplate
                    || r is Stratum
                    || r is IPhonologicalRule
                    || rules.Contains(r)
                    || (extraRoots.Count > 0 && r is CompoundingRule);

                var ids = new Dictionary<IMorpheme, int>();
                string target = Signature(candidate, ids);
                foreach (WordAnalysis analysis in morpher.AnalyzeWord(word))
                {
                    if (Signature(analysis, ids) == target)
                    {
                        return analysis; // the genuine HC analysis (carries the real category)
                    }
                }
                return null;
            }
            finally
            {
                pool.Return(morpher);
            }
        }

        /// <summary>Signature by per-morpheme identity (affix Morpheme.Id is empty, so shape-only would
        /// falsely match a same-shape but different-morpheme analysis); same objects on both sides.</summary>
        private static string Signature(WordAnalysis analysis, Dictionary<IMorpheme, int> ids)
        {
            return string.Join("+", analysis.Morphemes.Select(m => Id(m, ids))) + ":" + analysis.RootMorphemeIndex;
        }

        private static int Id(IMorpheme morpheme, Dictionary<IMorpheme, int> ids)
        {
            if (!ids.TryGetValue(morpheme, out int id))
            {
                id = ids.Count;
                ids[morpheme] = id;
            }
            return id;
        }
    }
}
