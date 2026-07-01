using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// The complete analyzer (HERMITCRAB_FST_PLAN.md §12). Completeness is decided by grammar-level
    /// <b>empirical certification</b> — the FST's analyses provably equalled the search engine's over a
    /// representative corpus (set parity, §9.5) — not a per-word heuristic:
    /// <list type="bullet">
    /// <item>certified ⇒ the fast verified FST path is used (complete for every word; certification is
    /// the evidence);</item>
    /// <item>otherwise the proven search engine is used — the known slow path; an uncertified grammar
    /// never silently under-generates.</item>
    /// </list>
    /// Per-word control: <see cref="AnalyzeWord(string,bool)"/> forces the FST on/off for one word, and
    /// <see cref="UseFstFor"/> is an optional policy hook consulted by the plain
    /// <see cref="AnalyzeWord(string)"/>. <b>Thread-safe:</b> the verified FST is thread-safe and the
    /// engine path rents a <see cref="Morpher"/> from a pool, so a corpus can be parsed in parallel.
    /// </summary>
    public class CompleteHybridMorpher : IMorphologicalAnalyzer
    {
        private readonly IMorphologicalAnalyzer _fst;
        private readonly MorpherPool _enginePool;
        private readonly bool _certified;

        public CompleteHybridMorpher(IMorphologicalAnalyzer verifiedFst, MorpherPool enginePool, bool certified)
        {
            _fst = verifiedFst;
            _enginePool = enginePool;
            _certified = certified;
        }

        /// <summary>
        /// Build and certify from a language + corpus: the FST is used only if it empirically matches
        /// the engine on <paramref name="certificationCorpus"/> (set parity); otherwise the engine is
        /// used. The verify and engine paths share one Morpher pool (thread-safe).
        /// </summary>
        public static CompleteHybridMorpher FromLanguage(
            TraceManager traceManager,
            Language language,
            IEnumerable<string> certificationCorpus
        )
        {
            var pool = new MorpherPool(() => new Morpher(new TraceManager(), language));
            var fst = new FstTemplateAnalyzer(language, new Morpher(traceManager, language));
            CompositeProposer proposer = CompositeProposer.ForLanguage(language, fst);
            var verified = new VerifiedFstAnalyzer(proposer, pool);
            var engine = new Morpher(traceManager, language);
            bool parity = FstVerification.Compare(engine, verified, certificationCorpus).IsComplete;
            bool certified = proposer.CoversAllConstructs && parity;
            return new CompleteHybridMorpher(verified, pool, certified);
        }

        /// <summary>True iff the FST passed empirical set-parity for this grammar (the default fast path).</summary>
        public bool Certified => _certified;

        /// <summary>Optional per-word policy: return true to use the FST for a word, false for the engine.
        /// When unset, the plain <see cref="AnalyzeWord(string)"/> uses <see cref="Certified"/>.</summary>
        public Func<string, bool> UseFstFor { get; set; }

        public IEnumerable<WordAnalysis> AnalyzeWord(string word)
        {
            return AnalyzeWord(word, UseFstFor?.Invoke(word) ?? _certified);
        }

        /// <summary>Analyze one word, explicitly choosing the FST fast path or the engine.</summary>
        public IEnumerable<WordAnalysis> AnalyzeWord(string word, bool useFst)
        {
            return useFst ? _fst.AnalyzeWord(word) : AnalyzeWithEngine(word);
        }

        private IEnumerable<WordAnalysis> AnalyzeWithEngine(string word)
        {
            Morpher morpher = _enginePool.Rent();
            try
            {
                return morpher.AnalyzeWord(word).ToList();
            }
            finally
            {
                _enginePool.Return(morpher);
            }
        }
    }
}
