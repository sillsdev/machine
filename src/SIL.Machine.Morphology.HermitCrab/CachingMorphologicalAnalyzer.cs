using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// The two-path analyzer (HERMITCRAB_FST_PLAN.md §13). The <b>slow path</b> is HC's search engine —
    /// the source of truth, complete — and its result for each word is cached. The <b>fast path</b> is
    /// the verified FST — sound and immediate but possibly under-generating. The default
    /// <see cref="AnalyzeWord(string)"/> returns the <b>guaranteed complete</b> result (cached, or
    /// computed by the engine on a miss and then cached) — backwards-compatible: existing callers get
    /// the same analyses, faster once warm. The opt-in <see cref="AnalyzeWordFast"/> returns the cached
    /// complete result if warm, else the provisional FST result without running the engine.
    ///
    /// For a fixed corpus, <see cref="Warm"/> fills the cache (in parallel) so every word eventually
    /// resolves fast <i>and</i> complete. Thread-safe: the FST is shared, the engine runs from a
    /// <see cref="MorpherPool"/>, and the cache is concurrent.
    /// </summary>
    public class CachingMorphologicalAnalyzer : IMorphologicalAnalyzer
    {
        private readonly IMorphologicalAnalyzer _fast;
        private readonly MorpherPool _enginePool;
        private readonly AnalysisCache _cache;
        private readonly bool _grammarCertified;

        public CachingMorphologicalAnalyzer(
            IMorphologicalAnalyzer fast,
            MorpherPool enginePool,
            AnalysisCache cache,
            bool grammarCertified = false
        )
        {
            _fast = fast;
            _enginePool = enginePool;
            _cache = cache;
            _grammarCertified = grammarCertified;
        }

        /// <summary>
        /// Wire the fast FST, an engine pool, and a (possibly preloaded) cache from a language. If a
        /// <paramref name="certificationCorpus"/> is supplied, the grammar is <b>certified</b> when the
        /// FST's analyses equal the engine's over it (set parity) AND the grammar is FST-closed — in
        /// which case the FST is treated as proven-complete for every word and the engine is never run.
        /// </summary>
        public static CachingMorphologicalAnalyzer FromLanguage(
            TraceManager traceManager,
            Language language,
            IEnumerable<string> certificationCorpus = null,
            AnalysisCache cache = null
        )
        {
            var pool = new MorpherPool(() => new Morpher(new TraceManager(), language));
            var fast = new VerifiedFstAnalyzer(new FstTemplateAnalyzer(language, new Morpher(traceManager, language)), pool);
            bool certified = false;
            if (certificationCorpus != null)
            {
                bool closed = GrammarFstClosure.Analyze(language).FstClosed;
                bool parity = FstVerification.Compare(new Morpher(traceManager, language), fast, certificationCorpus).IsComplete;
                certified = closed && parity;
            }
            return new CachingMorphologicalAnalyzer(fast, pool, cache ?? new AnalysisCache(), certified);
        }

        /// <summary>The underlying cache (for persistence / inspection).</summary>
        public AnalysisCache Cache => _cache;

        /// <summary>True iff the grammar is certified (FST-closed + set-parity) — the FST is then the
        /// complete answer for every word and the full search is never invoked.</summary>
        public bool GrammarCertified => _grammarCertified;

        /// <summary>
        /// Default, guaranteed-complete analysis (backwards-compatible). On a certified grammar the FST
        /// alone is the complete answer (no full search). Otherwise: the cached complete analysis if
        /// present, else the engine (cached). Either way the result is complete.
        /// </summary>
        public IEnumerable<WordAnalysis> AnalyzeWord(string word)
        {
            return _grammarCertified ? _fast.AnalyzeWord(word) : _cache.GetOrAdd(word, EngineAnalyze);
        }

        /// <summary>
        /// Opt-in fast path. On a certified grammar the FST result is proven complete
        /// (<see cref="FastAnalysisResult.IsComplete"/> = true) without any search. Otherwise: the
        /// cached complete set if warm (true), else the provisional verified-FST result (false). Never
        /// runs the slow engine, so it never blocks.
        /// </summary>
        public FastAnalysisResult AnalyzeWordFast(string word)
        {
            if (_grammarCertified)
            {
                return new FastAnalysisResult(_fast.AnalyzeWord(word).ToList(), isComplete: true);
            }
            if (_cache.TryGet(word, out IReadOnlyList<WordAnalysis> complete))
            {
                return new FastAnalysisResult(complete, isComplete: true);
            }
            return new FastAnalysisResult(_fast.AnalyzeWord(word).ToList(), isComplete: false);
        }

        /// <summary>
        /// Populate the cache with the complete analysis of every corpus word (the slow path). Safe to
        /// run in the background; parallelized across words by default.
        /// </summary>
        public void Warm(IEnumerable<string> corpus, bool parallel = true)
        {
            List<string> words = corpus.Distinct().Where(w => !_cache.TryGet(w, out _)).ToList();
            if (parallel)
            {
                Parallel.ForEach(words, w => _cache.GetOrAdd(w, EngineAnalyze));
            }
            else
            {
                foreach (string w in words)
                {
                    _cache.GetOrAdd(w, EngineAnalyze);
                }
            }
        }

        private IReadOnlyList<WordAnalysis> EngineAnalyze(string word)
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
