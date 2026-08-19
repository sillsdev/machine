using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Carrier for the analysis-cascade memo, threaded through <see cref="Word"/> clones
    /// like <see cref="Word.CurrentTrace"/> and likewise excluded from <c>Word.FreezeImpl</c>/
    /// <c>Word.ValueEquals</c>, so dedup semantics are unchanged.
    /// <para>
    /// One instance per <see cref="Morpher.ParseWord(string, out object)"/> call. A state key does not
    /// encode the target surface word, so sharing a scope across parses of different words would be
    /// unsound.
    /// </para>
    /// <para>
    /// Not thread-safe, hence the plain collections: a scope is only installed when
    /// <see cref="Morpher.MaxDegreeOfParallelism"/> is 1. Memoizing the parallel cascade would require
    /// concurrent ones.
    /// </para>
    /// </summary>
    internal sealed class AnalysisScope
    {
        // OOM guards; past either cap subtrees simply go unmemoized, degrading hit rate but never
        // correctness. The Word budget is the load-bearing one: entry size is unbounded (a node's list
        // holds every descendant, undeduplicated) and storing them keeps every intermediate of the search
        // alive for the whole parse. Both tables share it. It is a coarse backstop, not a figure derived
        // from measured memory.
        private const int MaxMemoEntries = 100_000;
        private const int MaxMemoWords = 1_000_000;

        private int _storedWordCount;

        public Dictionary<AnalysisStateKey, MemoEntry> Memo { get; } = new Dictionary<AnalysisStateKey, MemoEntry>();

        // Same key space as Memo, different computation: the affix-template battery's result for a state
        // (AnalysisStratumRule.ApplyTemplateBattery). Separate because a state can be memoized in one
        // table but not the other.
        public Dictionary<AnalysisStateKey, MemoEntry> TemplateMemo { get; } =
            new Dictionary<AnalysisStateKey, MemoEntry>();

        // Keys still under expansion on the call stack; a re-arrival at one must fall through to
        // unmemoized expansion rather than read a partial entry. Defensive only -- no path reaches it
        // today, since every unapplication grows the multiset the key hashes, so a key cannot recur while
        // still on the stack. The template battery needs no equivalent: its call is eager.
        public HashSet<AnalysisStateKey> InProgress { get; } = new HashSet<AnalysisStateKey>();

        // Per-parse hit counts, folded into the owning Morpher when the parse ends. Equivalence tests
        // assert on them: a memo that silently stopped firing looks exactly like a passing test.
        public int MemoHits { get; set; }
        public int NogoodHits { get; set; }
        public int TemplateMemoHits { get; set; }
        public int TemplateNogoodHits { get; set; }

        /// <summary>
        /// Replay shared by both memo consumers. False on a miss; on a hit
        /// <paramref name="replayed"/> holds the stored results grafted onto <paramref name="query"/>, or
        /// is empty for a stored-empty ("nogood") entry. The query's non-head prefix is cloned once and
        /// shared across this hit's replays, which is safe because each replay freezes immediately and
        /// every non-head mutation path is CheckFrozen-guarded.
        /// </summary>
        public bool TryReplay(
            Dictionary<AnalysisStateKey, MemoEntry> table,
            AnalysisStateKey key,
            Word query,
            out List<Word> replayed
        )
        {
            if (!table.TryGetValue(key, out MemoEntry entry))
            {
                replayed = null;
                return false;
            }
            if (entry.Results.Count == 0)
            {
                replayed = new List<Word>();
                return true;
            }
            List<Word> queryNonHeadPrefix = query.CloneNonHeadsForReplay();
            replayed = new List<Word>(entry.Results.Count);
            foreach (Word stored in entry.Results)
            {
                replayed.Add(
                    stored.ReplayOnto(
                        query,
                        entry.MruleTrailPrefixLength,
                        entry.NonHeadPrefixLength,
                        queryNonHeadPrefix
                    )
                );
            }
            return true;
        }

        /// <summary>
        /// Records a fully-expanded result list against <paramref name="key"/>, unless either the table is
        /// full or the parse's retained-Word budget cannot absorb it.
        /// </summary>
        public void Store(
            Dictionary<AnalysisStateKey, MemoEntry> table,
            AnalysisStateKey key,
            Word query,
            List<Word> results
        )
        {
            if (table.Count >= MaxMemoEntries || _storedWordCount > MaxMemoWords - results.Count)
                return;
            _storedWordCount += results.Count;
            table[key] = new MemoEntry(results, query.MorphologicalRuleTrailLength, query.NonHeadCount);
        }
    }

    /// <summary>
    /// A memoized subtree or template-battery result. An empty <see cref="Results"/> means the state was
    /// proved to yield nothing. The two prefix lengths are the trail/non-head counts at the moment of the
    /// write, which is where <see cref="Word.ReplayOnto"/> splits a stored result when grafting it onto a
    /// new arrival.
    /// <para>
    /// There is deliberately no "incomplete" flag: only fully-explored subtrees may be recorded. Should a
    /// step or time budget ever be added, an interrupted subtree must not be stored.
    /// </para>
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
