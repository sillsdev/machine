using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// A thread-safe store of <b>complete</b> (engine-computed) analyses, keyed by surface word
    /// (HERMITCRAB_FST_PLAN.md §13). The FST fast path is sound but not guaranteed complete, so the
    /// slow engine is the source of truth; its result is cached here once and reused. For a fixed
    /// corpus the cache is warmed (in the background) until every word has its complete analysis, after
    /// which queries are both fast and complete. Persisted across sessions via
    /// <see cref="MorphemeRegistry"/> (the analyses reference grammar morpheme objects, which are
    /// rehydrated against the same grammar; a grammar-version guard rejects a stale cache).
    /// </summary>
    public sealed class AnalysisCache
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<
            string,
            IReadOnlyList<WordAnalysis>
        > _store = new System.Collections.Concurrent.ConcurrentDictionary<string, IReadOnlyList<WordAnalysis>>();

        /// <summary>Number of words with a stored complete analysis.</summary>
        public int Count => _store.Count;

        /// <summary>The cached words.</summary>
        public IEnumerable<string> Words => _store.Keys;

        /// <summary>True (with the complete analyses) iff this word's complete analysis is cached.</summary>
        public bool TryGet(string word, out IReadOnlyList<WordAnalysis> analyses)
        {
            return _store.TryGetValue(word, out analyses);
        }

        /// <summary>Store the complete analysis for a word (overwrites).</summary>
        public void Set(string word, IReadOnlyList<WordAnalysis> analyses)
        {
            _store[word] = analyses;
        }

        /// <summary>Return the cached complete analysis, or compute it once via <paramref name="compute"/> and cache it.</summary>
        public IReadOnlyList<WordAnalysis> GetOrAdd(
            string word,
            System.Func<string, IReadOnlyList<WordAnalysis>> compute
        )
        {
            return _store.GetOrAdd(word, compute);
        }

        /// <summary>Snapshot of (word, analyses) pairs — for persistence.</summary>
        public IEnumerable<KeyValuePair<string, IReadOnlyList<WordAnalysis>>> Entries => _store.ToArray();
    }

    /// <summary>
    /// The result of the opt-in fast query: the analyses plus whether they are the <b>complete</b>
    /// (cached, engine-verified) set or a <b>provisional</b> FST result that may under-generate. A
    /// consumer should only treat "no analyses" as "not a word" when <see cref="IsComplete"/> is true.
    /// </summary>
    public readonly struct FastAnalysisResult
    {
        public FastAnalysisResult(IReadOnlyList<WordAnalysis> analyses, bool isComplete)
        {
            Analyses = analyses;
            IsComplete = isComplete;
        }

        public IReadOnlyList<WordAnalysis> Analyses { get; }

        /// <summary>True if these are the cached complete analyses; false if a provisional FST result.</summary>
        public bool IsComplete { get; }
    }
}
