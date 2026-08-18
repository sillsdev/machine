using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Per-parse cache carrier threaded through <see cref="Word"/> clones exactly like
    /// <see cref="Word.CurrentTrace"/> -- reference-shared, excluded from <c>Word.FreezeImpl</c>/
    /// <c>Word.ValueEquals</c> so existing dedup semantics are unchanged. Holds the analysis-cascade memo
    /// (memoization.md) -- see <see cref="MemoizedCombinationRuleCascade"/>.
    /// One instance per <see cref="Morpher.ParseWord(string, out object)"/> call: entries are state facts
    /// about a specific parse (a state key does not encode the target surface word), so sharing this
    /// across concurrent parses of different words would be unsound without also scoping the key to the
    /// word -- that cross-word extension is explicitly out of scope.
    /// Thread-safe because a single word's analysis can itself run in parallel
    /// (<c>ParallelCombinationRuleCascade</c>, used when <see cref="Morpher.MaxDegreeOfParallelism"/> is not
    /// 1) -- though today only the sequential cascade actually reads/writes this.
    /// </summary>
    internal sealed class AnalysisScope
    {
        // OOM guard: a positive memo holds actual Word lists (not just a boolean like the nogood case), so
        // it is the one that can grow unboundedly on a pathological word. Past the cap, new subtrees are
        // simply not memoized -- correctness is unaffected, only the hit rate degrades. No corpus word
        // seen so far has come close to this cap; deliberately untested against an actual overflow.
        private const int MaxMemoEntries = 100_000;

        public ConcurrentDictionary<AnalysisStateKey, MemoEntry> Memo { get; } =
            new ConcurrentDictionary<AnalysisStateKey, MemoEntry>();

        // Second table, same key space, different computation: the affix-template battery result for a
        // state (AnalysisStratumRule.ApplyTemplateBattery), as opposed to the mrule-cascade subtree result
        // above. Kept separate because a state can be memoized in one table but not (yet) the other.
        public ConcurrentDictionary<AnalysisStateKey, MemoEntry> TemplateMemo { get; } =
            new ConcurrentDictionary<AnalysisStateKey, MemoEntry>();

        // Keys currently under expansion on some call stack -- guards the in-flight re-entry case (a
        // multiApp cascade can reach the same state again before its own first expansion has completed,
        // e.g. via a self-loop). A hit here must fall through to plain, unmemoized expansion rather than
        // read a nonexistent/partial entry or deadlock; see MemoizedCombinationRuleCascade.ApplyRules. The
        // template battery needs no equivalent guard: ApplyTemplateBattery's call is eager and
        // self-contained (no template<->mrule mutual recursion happens inside it).
        public ConcurrentDictionary<AnalysisStateKey, byte> InProgress { get; } =
            new ConcurrentDictionary<AnalysisStateKey, byte>();

        public bool HasMemoCapacity => Memo.Count < MaxMemoEntries;

        public bool HasTemplateMemoCapacity => TemplateMemo.Count < MaxMemoEntries;
    }

    /// <summary>
    /// A memoized analysis-cascade subtree or template-battery result (memoization.md). <see cref="Results"/>
    /// empty = the "nogood" case (subtree/battery proved to yield nothing); non-empty = the positive case,
    /// replayable onto a differently-ordered arrival at the same <see cref="AnalysisStateKey"/> via
    /// <see cref="Word.ReplayOnto"/>, using <see cref="MruleTrailPrefixLength"/>/<see cref="NonHeadPrefixLength"/>
    /// to split each stored result's trail/non-heads into the (discarded, replaced) prefix and the (kept)
    /// subtree-local suffix. There is no "budget exhausted / incomplete" flag: this engine has no step/time
    /// budget infrastructure, so every recorded subtree was explored to full completion.
    /// </summary>
    internal sealed class MemoEntry
    {
        public MemoEntry(IReadOnlyList<Word> results, int mruleTrailPrefixLength, int nonHeadPrefixLength)
        {
            Results = results;
            MruleTrailPrefixLength = mruleTrailPrefixLength;
            NonHeadPrefixLength = nonHeadPrefixLength;
        }

        public IReadOnlyList<Word> Results { get; }
        public int MruleTrailPrefixLength { get; }
        public int NonHeadPrefixLength { get; }
    }
}
