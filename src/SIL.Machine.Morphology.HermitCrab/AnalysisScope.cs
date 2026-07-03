using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Per-parse cache carrier threaded through <see cref="Word"/> clones exactly like
    /// <see cref="Word.CurrentTrace"/> -- reference-shared, excluded from
    /// <c>Word.FreezeImpl</c>/<c>Word.ValueEquals</c> so existing dedup semantics are unchanged. Holds the
    /// analysis-cascade memo table from parse-optimization.md Phases 2 (nogoods) and 3 (positive memo with
    /// trail replay) -- see <see cref="MemoizedCombinationRuleCascade"/>.
    /// One instance per <see cref="Morpher.ParseWord(string, out object)"/> call: entries are state facts
    /// about a specific parse (a state key does not encode the target surface word), so sharing this
    /// across concurrent parses of different words would be unsound without also scoping the key to the
    /// word -- that cross-word extension is explicitly deferred (parse-optimization.md §4.6).
    /// Thread-safe because a single word's analysis can itself run in parallel
    /// (<c>ParallelCombinationRuleCascade</c>, used when <see cref="Morpher.MaxDegreeOfParallelism"/> > 1) --
    /// though today only the sequential cascade actually reads/writes this (see
    /// <see cref="MemoizedCombinationRuleCascade"/>'s doc comment).
    /// </summary>
    internal sealed class AnalysisScope
    {
        public AnalysisScope(bool lexicalGatingActive)
        {
            LexicalGatingActive = lexicalGatingActive;
        }

        // parse-optimization.md Phase 5: precomputed once per parse (Morpher.ParseWord already knows
        // whether tracing/guessRoot/the grammar's own qualification allow the gate for this call) rather
        // than re-checked per candidate -- cheap, and keeps MemoizedCombinationRuleCascade from needing a
        // direct Morpher reference just to read three unrelated conditions.
        public bool LexicalGatingActive { get; }

        // OOM guard (parse-optimization.md Phase 3): cinacemerwa-class words have crashed a test host on
        // memory before budgets existed to stop them, and a positive memo holds actual Word lists (not
        // just a boolean like the nogood case), so it is the one that can grow unboundedly. Past the cap,
        // new subtrees are simply not memoized -- correctness is unaffected, only the hit rate degrades.
        private const int MaxMemoEntries = 100_000;

        public ConcurrentDictionary<AnalysisStateKey, MemoEntry> Memo { get; } =
            new ConcurrentDictionary<AnalysisStateKey, MemoEntry>();

        // Same discipline, different battery: template unapplication (AnalysisStratumRule.ApplyTemplates)
        // reads exactly what AnalysisStateKey captures (shape, syntactic FS, stratum, non-heads) and
        // nothing trail-order-dependent, so equal-keyed arrivals get equal template outputs modulo the
        // trail prefix -- the same Word.ReplayOnto graft Phase 3 uses. Kept as a separate table from
        // Memo because the two record different computations over the same key space (a state's mrule
        // subtree vs. its one-level template outputs); merging them would conflate a "no mrule results"
        // nogood with "no template outputs". Measured motivation (2026-07-03, atawirambo): the template
        // battery was invoked 38,840 times against ~2,581 unique keys -- 93% of total parse wall time --
        // because Phases 2/3 memoized only the mrule cascade and templates sat outside it.
        public ConcurrentDictionary<AnalysisStateKey, MemoEntry> TemplateMemo { get; } =
            new ConcurrentDictionary<AnalysisStateKey, MemoEntry>();

        // Keys currently under expansion on some call stack -- guards the in-flight re-entry case (a
        // multiApp cascade can reach the same state again before its own first expansion has completed,
        // e.g. via a self-loop). A hit here must fall through to plain, unmemoized expansion rather than
        // read a nonexistent/partial entry or deadlock; see MemoizedCombinationRuleCascade.ApplyRules.
        public ConcurrentDictionary<AnalysisStateKey, byte> InProgress { get; } =
            new ConcurrentDictionary<AnalysisStateKey, byte>();

        public bool HasMemoCapacity => Memo.Count < MaxMemoEntries;
    }

    /// <summary>
    /// A memoized analysis-cascade subtree (parse-optimization.md Phase 3). <see cref="Results"/> empty =
    /// the Phase 2 "nogood" case (subtree proved to yield nothing); non-empty = the Phase 3 positive case,
    /// replayable onto a differently-ordered arrival at the same <see cref="AnalysisStateKey"/> via
    /// <see cref="Word.ReplayOnto"/>, using <see cref="MruleTrailPrefixLength"/>/
    /// <see cref="NonHeadPrefixLength"/> to split each stored result's trail/non-heads into the
    /// (discarded, replaced) prefix and the (kept) subtree-local suffix. There is no "budget exhausted /
    /// incomplete" flag: this branch has no step/time budget infrastructure (see parse-optimization.md
    /// "Branch context"), so every recorded subtree was explored to full completion.
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
