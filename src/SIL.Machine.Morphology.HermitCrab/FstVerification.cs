using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.Morphology;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// One word on which a candidate analyzer's analysis set differs from the reference's:
    /// <see cref="MissingFromCandidate"/> are analyses the reference found but the candidate did not
    /// (completeness failures), <see cref="ExtraInCandidate"/> are analyses the candidate produced
    /// that the reference rejects (soundness / over-generation failures).
    /// </summary>
    public sealed class AnalysisDivergence
    {
        public AnalysisDivergence(
            string word,
            IReadOnlyList<string> missingFromCandidate,
            IReadOnlyList<string> extraInCandidate
        )
        {
            Word = word;
            MissingFromCandidate = missingFromCandidate;
            ExtraInCandidate = extraInCandidate;
        }

        public string Word { get; }
        public IReadOnlyList<string> MissingFromCandidate { get; }
        public IReadOnlyList<string> ExtraInCandidate { get; }
    }

    /// <summary>The result of an FST-vs-search corpus comparison — a manual gap-inspection report,
    /// not a gate. Nothing in this codebase treats a clean result as a license to stop running the
    /// real engine; it exists purely to show a grammar engineer where the fast path currently
    /// diverges from the reference and why.</summary>
    public sealed class AnalysisComparison
    {
        public AnalysisComparison(int wordsChecked, IReadOnlyList<AnalysisDivergence> divergences)
        {
            WordsChecked = wordsChecked;
            Divergences = divergences;
        }

        public int WordsChecked { get; }
        public IReadOnlyList<AnalysisDivergence> Divergences { get; }

        /// <summary>Words whose analysis sets matched exactly.</summary>
        public int Matches => WordsChecked - Divergences.Count;

        /// <summary>True iff the candidate's analysis SET equals the reference's for every word
        /// checked — no missing and no spurious analyses, on this corpus only. Not a proof, and not
        /// a signal to disable the reference engine; see <see cref="AnalysisComparison"/>.</summary>
        public bool MatchesReferenceExactly => Divergences.Count == 0;

        /// <summary>A readable dump.</summary>
        public string Format()
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                $"checked {WordsChecked}, {Matches} match, {Divergences.Count} diverge — "
                    + (MatchesReferenceExactly ? "EXACT MATCH (set parity)" : "DIVERGENCES")
            );
            foreach (AnalysisDivergence d in Divergences)
            {
                sb.AppendLine(
                    $"  {d.Word}: missing=[{string.Join(" | ", d.MissingFromCandidate)}] "
                        + $"extra=[{string.Join(" | ", d.ExtraInCandidate)}]"
                );
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// A manual divergence-inspection tool for benchmarks and ad hoc investigation: run a candidate
    /// analyzer (e.g. <see cref="VerifiedFstAnalyzer"/>) beside the reference (<see cref="Morpher"/>)
    /// over a corpus and report, per word, where their analysis SETS differ — missing analyses
    /// (fast-path gaps) and extra analyses (would be a soundness bug in the candidate, since
    /// <see cref="VerifiedFstAnalyzer"/> is supposed to never over-generate). This is diagnostic
    /// only: nothing in this codebase gates behavior on its result, and it is not intended to run in
    /// CI (the reference <see cref="Morpher"/> can be extremely slow on some words) — use it from
    /// <c>[Explicit]</c> benchmarks with a capped corpus.
    /// </summary>
    public static class FstVerification
    {
        public static AnalysisComparison Compare(
            IMorphologicalAnalyzer reference,
            IMorphologicalAnalyzer candidate,
            IEnumerable<string> words
        )
        {
            // Identity key per distinct morpheme object: affix Morpheme.Id is empty in many grammars,
            // so a name/id-string signature would collapse different affixes of the same shape (e.g. the
            // subject markers 3P+2 / 3S+1 / 6) into one key and hide same-shape under-generation. Both
            // analyzers reference the SAME Morpheme instances from the Language, so object identity is a
            // faithful, shared discriminator.
            var ids = new Dictionary<IMorpheme, int>();
            string Sig(WordAnalysis a) => Signature(a, ids);

            var divergences = new List<AnalysisDivergence>();
            int count = 0;
            foreach (string word in words)
            {
                count++;
                var referenceSet = new HashSet<string>(reference.AnalyzeWord(word).Select(Sig));
                var candidateSet = new HashSet<string>(candidate.AnalyzeWord(word).Select(Sig));
                if (referenceSet.SetEquals(candidateSet))
                {
                    continue;
                }
                List<string> missing = referenceSet
                    .Except(candidateSet)
                    .OrderBy(s => s, StringComparer.Ordinal)
                    .ToList();
                List<string> extra = candidateSet.Except(referenceSet).OrderBy(s => s, StringComparer.Ordinal).ToList();
                divergences.Add(new AnalysisDivergence(word, missing, extra));
            }
            return new AnalysisComparison(count, divergences);
        }

        /// <summary>A signature of one analysis: per-morpheme identity ids (in morph order) + root index.</summary>
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
