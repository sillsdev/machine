using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Morphology;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// A candidate generator for <b>reduplication</b> — full copy (surface = base·base) or partial
    /// prefix/suffix copy (e.g. Tagalog CV-reduplication: surface = CV·base) — the one provably
    /// non-regular construct (Dolatian &amp; Heinz 2020), handled <i>beside</i> the FST rather than
    /// inside it (FST_FULL_PLAN.md, Point 3). It does not need to be regular because the
    /// <see cref="VerifiedFstAnalyzer"/> gate re-runs HC to confirm every candidate.
    ///
    /// Mechanism (strip + recurse): for every copy length from 1 up to half the word, check both a
    /// PREFIX copy (surface starts with its own next `len` characters repeated — full reduplication is
    /// just the `len == word.Length / 2` case of this) and a SUFFIX copy (surface ends with its own
    /// preceding `len` characters repeated). For each match, strip the copy and <b>recurse the residual
    /// base through the FST proposer</b> — so an <i>inflected</i> reduplicant (e.g. REDUP of an affixed
    /// stem) is covered, not just a bare root — then wrap each returned base analysis with the
    /// reduplication morpheme (prepended, matching HC's <c>RED root …</c> order). This is a bounded,
    /// O(word length²) scan (trivial); "well enough" for the attested cases, and anything it misses
    /// simply fails parity and rides the engine — never a wrong answer.
    ///
    /// A THIRD shape (Phase C/D, FST_FULL_GRAMMAR_PLAN.md): a single-character SEPARATOR between base
    /// and copy, where the copy is a TAIL of the base rather than an identical adjacent repeat — e.g.
    /// Indonesian's <c>-Cont</c> gives <c>menulis-nulis</c> (`nulis` = the last 5 characters of
    /// `menulis`, not a copy of the WHOLE prefixed word). HC's own morph-boundary bookkeeping makes the
    /// copy exclude fixed-inserted prefix text like `me` in a way this generator does not try to model
    /// underlyingly; empirically the copy is always a genuine surface TAIL, so scanning for that shape
    /// directly (any single separator character, any tail length) recovers it — a wrong guess (an
    /// unrelated character that happens to precede a coincidental tail match) is pruned by verify like
    /// any other candidate here, so trying every position costs time, never soundness.
    ///
    /// Soundness: a coincidental repeat (a word that merely looks copied at some length but is not
    /// reduplicated) is proposed but pruned by verify, because HC's synthesis of <c>base + REDUP</c> (or
    /// <c>REDUP + base</c>) will not reproduce it.
    ///
    /// A FOURTH shape (Phase G1, FST_FULL_GRAMMAR_PLAN.md): a suffix stacked OUTSIDE the reduplication
    /// itself, on the copy side only — e.g. Indonesian's <c>mengamat-amati</c> is
    /// <c>meng+amat</c> → <c>-Cont</c> → <c>mengamat-amat</c> → <c>-i</c>(LOC) → <c>mengamat-amati</c>,
    /// so the copy (<c>amati</c>) is the base's tail PLUS a known suffix surface, not a plain tail. When
    /// the plain separator-scan match fails, this generator additionally tries peeling each grammar
    /// suffix's own surface text off the END of the copy and re-testing the remainder as a tail — a
    /// single layer only (this corpus needs no more), and a wrong peel is pruned by verify exactly like
    /// every other candidate here.
    /// </summary>
    public class ReduplicationProposer : IConstructProposer
    {
        private static readonly MorphOp[] _ops = { MorphOp.Reduplication };
        private readonly IMorphologicalAnalyzer _baseProposer;
        private readonly List<MorphemicMorphologicalRule> _redupRules;
        private readonly List<(string SurfaceText, MorphemicMorphologicalRule Rule)> _suffixSurfaces;

        public ReduplicationProposer(Language language, IMorphologicalAnalyzer baseProposer)
        {
            _baseProposer = baseProposer;
            _redupRules = new List<MorphemicMorphologicalRule>();
            _suffixSurfaces = new List<(string, MorphemicMorphologicalRule)>();
            CharacterDefinitionTable table = language.SurfaceStratum.CharacterDefinitionTable;
            foreach (Stratum stratum in language.Strata)
            {
                foreach (IMorphologicalRule mrule in stratum.MorphologicalRules)
                {
                    if (!(mrule is MorphemicMorphologicalRule rule))
                    {
                        continue;
                    }
                    if (IsReduplication(rule))
                    {
                        _redupRules.Add(rule);
                        continue;
                    }
                    foreach (AffixProcessAllomorph allomorph in Allomorphs(rule))
                    {
                        if (MorphTokenCodec.ClassifyOp(allomorph, false) != MorphOp.Suffix)
                        {
                            continue;
                        }
                        InsertSegments insert = allomorph.Rhs.OfType<InsertSegments>().FirstOrDefault();
                        if (insert == null)
                        {
                            continue;
                        }
                        // The underlying representation may include boundary characters (e.g.
                        // Indonesian's "-i" LOC suffix is underlyingly "+i") that never appear on the
                        // surface — strip them by keeping only Segment-type nodes when rendering.
                        string surfaceText = RenderSurfaceOnly(table, insert.Segments.Shape);
                        if (!string.IsNullOrEmpty(surfaceText))
                        {
                            _suffixSurfaces.Add((surfaceText, rule));
                        }
                    }
                }
            }
        }

        private static IEnumerable<AffixProcessAllomorph> Allomorphs(MorphemicMorphologicalRule rule)
        {
            switch (rule)
            {
                case AffixProcessRule affix:
                    return affix.Allomorphs;
                case RealizationalAffixProcessRule realizational:
                    return realizational.Allomorphs;
                default:
                    return Enumerable.Empty<AffixProcessAllomorph>();
            }
        }

        private static string RenderSurfaceOnly(CharacterDefinitionTable table, Shape shape)
        {
            var sb = new System.Text.StringBuilder();
            foreach (ShapeNode node in shape)
            {
                if (node.Annotation.Type() != HCFeatureSystem.Segment)
                {
                    continue;
                }
                string rep = table.GetMatchingStrReps(node).FirstOrDefault();
                if (string.IsNullOrEmpty(rep))
                {
                    return null;
                }
                sb.Append(rep);
            }
            return sb.ToString();
        }

        public IReadOnlyCollection<MorphOp> CoveredOps => _ops;

        public IEnumerable<WordAnalysis> AnalyzeWord(string word)
        {
            if (_redupRules.Count == 0)
            {
                yield break;
            }
            int maxCopyLen = word.Length / 2;
            for (int len = 1; len <= maxCopyLen; len++)
            {
                // Prefix copy: the first `len` characters repeat immediately (surface = copy·base, and
                // the base itself starts with that same `len`-character prefix). Strip the copy.
                if (string.Equals(word.Substring(0, len), word.Substring(len, len), StringComparison.Ordinal))
                {
                    foreach (WordAnalysis analysis in ProposeForResidual(word.Substring(len)))
                    {
                        yield return analysis;
                    }
                }
                // Suffix copy: the last `len` characters repeat the `len` characters before them
                // (surface = base·copy). Strip the trailing copy.
                if (
                    string.Equals(
                        word.Substring(word.Length - len, len),
                        word.Substring(word.Length - (2 * len), len),
                        StringComparison.Ordinal
                    )
                )
                {
                    foreach (WordAnalysis analysis in ProposeForResidual(word.Substring(0, word.Length - len)))
                    {
                        yield return analysis;
                    }
                }
            }
            // Separator + tail copy: base + one literal character + a TAIL of base (not necessarily the
            // whole base) — see class remarks. `sepPos` is the separator's index; everything after it is
            // the candidate copy, everything before it is the candidate base.
            for (int sepPos = 1; sepPos < word.Length - 1; sepPos++)
            {
                string before = word.Substring(0, sepPos);
                string copy = word.Substring(sepPos + 1);
                if (copy.Length == 0)
                {
                    continue;
                }
                if (before.Length >= copy.Length && before.EndsWith(copy, StringComparison.Ordinal))
                {
                    foreach (WordAnalysis analysis in ProposeForResidual(before))
                    {
                        yield return analysis;
                    }
                    continue; // plain tail matched — no need to also try peeling a suffix off this copy
                }
                // Fourth shape (Phase G1): the copy didn't match as a plain tail — try peeling a known
                // suffix surface off the END of the copy and re-testing the remainder as a tail.
                foreach ((string suffixText, MorphemicMorphologicalRule suffixRule) in _suffixSurfaces)
                {
                    if (!copy.EndsWith(suffixText, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    string strippedCopy = copy.Substring(0, copy.Length - suffixText.Length);
                    if (
                        strippedCopy.Length > 0
                        && before.Length >= strippedCopy.Length
                        && before.EndsWith(strippedCopy, StringComparison.Ordinal)
                    )
                    {
                        foreach (WordAnalysis analysis in ProposeForResidual(before, suffixRule))
                        {
                            yield return analysis;
                        }
                    }
                }
            }
        }

        private IEnumerable<WordAnalysis> ProposeForResidual(
            string residual,
            MorphemicMorphologicalRule extraSuffix = null
        )
        {
            foreach (WordAnalysis baseAnalysis in _baseProposer.AnalyzeWord(residual))
            {
                foreach (MorphemicMorphologicalRule redup in _redupRules)
                {
                    // Application order: root (and its affixes), then the reduplication rule, then (if
                    // present) a suffix stacked OUTSIDE the reduplication (Phase G1) — matching HC's
                    // WordAnalysis.Morphemes order (root·…·RED·suffix), so the root index is unchanged.
                    var morphemes = new List<IMorpheme>(baseAnalysis.Morphemes) { redup };
                    if (extraSuffix != null)
                    {
                        morphemes.Add(extraSuffix);
                    }
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
