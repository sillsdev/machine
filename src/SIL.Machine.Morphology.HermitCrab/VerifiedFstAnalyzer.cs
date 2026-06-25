using System.Collections.Generic;
using SIL.Machine.Morphology;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// The FST proposes candidates fast; each is confirmed by <b>restricted re-analysis</b>
    /// (<see cref="FstReplay"/>) — HC's own <see cref="Morpher.AnalyzeWord"/> pinned to the candidate's
    /// root and rules — and a candidate HC does not confirm is <b>discarded</b> (not a fallback). The
    /// confirmed, genuine HC analysis is emitted. Because verification runs HC's real analysis +
    /// synthesis, this enforces every constraint (category, MPR, co-occurrence, obligatoriness) without
    /// reimplementing any of them.
    ///
    /// Sound by construction (a kept analysis is a real HC analysis) and lossless (a valid candidate is
    /// never false-rejected). It does not add analyses the proposer never produced, so under-generation
    /// (coverage) must be closed in the proposer. <b>Thread-safe:</b> the immutable proposer is shared
    /// and each verification rents a <see cref="Morpher"/> from the pool, so many words can be analyzed
    /// in parallel.
    /// </summary>
    public class VerifiedFstAnalyzer : IMorphologicalAnalyzer
    {
        private readonly IMorphologicalAnalyzer _proposer;
        private readonly MorpherPool _pool;

        public VerifiedFstAnalyzer(IMorphologicalAnalyzer proposer, MorpherPool pool)
        {
            _proposer = proposer;
            _pool = pool;
        }

        /// <summary>Build the proposer and a verify Morpher pool from a language.</summary>
        public VerifiedFstAnalyzer(TraceManager traceManager, Language language)
            : this(
                new FstTemplateAnalyzer(language, new Morpher(traceManager, language)),
                new MorpherPool(() => new Morpher(new TraceManager(), language))
            ) { }

        public IEnumerable<WordAnalysis> AnalyzeWord(string word)
        {
            foreach (WordAnalysis candidate in _proposer.AnalyzeWord(word))
            {
                WordAnalysis confirmed = FstReplay.Confirm(_pool, candidate, word);
                if (confirmed != null)
                {
                    yield return confirmed;
                }
            }
        }
    }
}
