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
        // OOM guard, since a positive entry holds Word lists rather than just a flag. Past the cap
        // subtrees simply go unmemoized: only the hit rate degrades, never correctness.
        private const int MaxMemoEntries = 100_000;

        public Dictionary<AnalysisStateKey, MemoEntry> Memo { get; } = new Dictionary<AnalysisStateKey, MemoEntry>();

        // Same key space as Memo, different computation: the affix-template battery's result for a state
        // (AnalysisStratumRule.ApplyTemplateBattery). Separate because a state can be memoized in one
        // table but not the other.
        public Dictionary<AnalysisStateKey, MemoEntry> TemplateMemo { get; } =
            new Dictionary<AnalysisStateKey, MemoEntry>();

        // Keys still under expansion somewhere on the call stack. A multiApp cascade can reach the same
        // state again before its first expansion has finished (e.g. via a self-loop), which must fall
        // through to unmemoized expansion rather than read a partial entry or deadlock. The template
        // battery needs no equivalent: its call is eager, with no template/mrule mutual recursion inside.
        public HashSet<AnalysisStateKey> InProgress { get; } = new HashSet<AnalysisStateKey>();

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
        /// Records a fully-expanded result list against <paramref name="key"/>, unless the table is full.
        /// </summary>
        public void Store(
            Dictionary<AnalysisStateKey, MemoEntry> table,
            AnalysisStateKey key,
            Word query,
            List<Word> results
        )
        {
            if (table.Count < MaxMemoEntries)
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
