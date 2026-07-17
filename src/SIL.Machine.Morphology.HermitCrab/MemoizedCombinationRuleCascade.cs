using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Drop-in replacement for the sequential <see cref="CombinationRuleCascade{TData,TOffset}"/> on
    /// Unordered-order analysis strata (memoization.md). Before expanding a node, checks whether an
    /// earlier expansion elsewhere in the same word's analysis -- reached via a different unapplication
    /// order, but with an equal <see cref="AnalysisStateKey"/> -- already searched this exact state:
    /// <list type="bullet">
    /// <item>proved empty (the "nogood" case) -> skip straight to "no results";</item>
    /// <item>produced results (the positive case) -> replay them (<see cref="Word.ReplayOnto"/>) instead
    /// of re-searching: clone each stored result and graft the CURRENT arrival's own trail/non-head
    /// prefix onto the stored subtree-local suffix (see <see cref="MemoEntry"/> and
    /// <see cref="Word.ReplayOnto"/> for why only the prefix -- never the suffix -- needs replacing).</item>
    /// </list>
    /// Scoped to the sequential cascade only. The parallel cascade
    /// (<c>ParallelCombinationRuleCascade</c>, used when <see cref="Morpher.MaxDegreeOfParallelism"/> is
    /// not 1) is a level-by-level breadth-first walk with no natural "this subtree is fully expanded"
    /// moment to hang a memo write on, so it is left unmemoized -- callers that want this optimization
    /// should construct their <see cref="Morpher"/> with <c>maxDegreeOfParallelism: 1</c>.
    /// </summary>
    internal class MemoizedCombinationRuleCascade : RuleCascade<Word, ShapeNode>
    {
        // Test/reporting hooks (memoization.md's standing hit/miss-count requirement): DiagMemoHits counts
        // positive replays (a stored non-empty subtree grafted onto a new arrival); DiagNogoodHits counts
        // nogood hits (a stored EMPTY subtree short-circuited without any replay work). The equivalence
        // tests that cover the replay path assert DiagMemoHits is nonzero so they can never go vacuous --
        // a memo that silently stopped firing would otherwise look exactly like a passing test.
        internal static long DiagMemoHits;
        internal static long DiagNogoodHits;

        public MemoizedCombinationRuleCascade(
            IEnumerable<IRule<Word, ShapeNode>> rules,
            IEqualityComparer<Word> comparer
        )
            : base(rules, true, comparer) { }

        public override IEnumerable<Word> Apply(Word input)
        {
            var output = new HashSet<Word>(Comparer);
            ApplyRules(input, output);
            return output;
        }

        // Returns every result produced strictly within the subtree rooted at `input` (i.e. by applying
        // one or more rules starting from `input`, at any depth) -- NOT including `input` itself. This is
        // both the return value callers use and the value memoized against `input`'s AnalysisStateKey
        // once the subtree finishes, so a later differently-ordered arrival at the same state can replay
        // it via Word.ReplayOnto instead of re-searching.
        private List<Word> ApplyRules(Word input, HashSet<Word> output)
        {
            AnalysisScope scope = input.AnalysisScope;
            // See Word.AnalysisScope's doc for when this is null.
            if (scope == null)
                return ApplyRulesRaw(input, output);

            var key = new AnalysisStateKey(input);

            if (scope.Memo.TryGetValue(key, out MemoEntry entry))
            {
                if (entry.Results.Count == 0)
                {
                    DiagNogoodHits++;
                    return new List<Word>();
                }
                var replayed = new List<Word>(entry.Results.Count);
                foreach (Word storedResult in entry.Results)
                {
                    Word replay = storedResult.ReplayOnto(
                        input,
                        entry.MruleTrailPrefixLength,
                        entry.NonHeadPrefixLength
                    );
                    output.Add(replay);
                    replayed.Add(replay);
                }
                DiagMemoHits++;
                return replayed;
            }

            // In-flight re-entry guard, see AnalysisScope.InProgress.
            if (!scope.InProgress.TryAdd(key, 0))
                return ApplyRulesRaw(input, output);

            List<Word> results;
            try
            {
                results = ApplyRulesRaw(input, output);
            }
            finally
            {
                scope.InProgress.TryRemove(key, out _);
            }

            // Past the cap, keep searching correctly, just stop growing the table (AnalysisScope.HasMemoCapacity).
            if (scope.HasMemoCapacity)
                scope.Memo.TryAdd(key, new MemoEntry(results, input.MorphologicalRuleTrailLength, input.NonHeadCount));

            return results;
        }

        private List<Word> ApplyRulesRaw(Word input, HashSet<Word> output)
        {
            var local = new List<Word>();
            for (int i = 0; i < Rules.Count; i++)
            {
                foreach (Word result in ApplyRule(Rules[i], i, input))
                {
                    local.Add(result);
                    output.Add(result);
                    // avoid infinite loop -- same guard CombinationRuleCascade uses
                    if (!Comparer.Equals(input, result))
                        local.AddRange(ApplyRules(result, output));
                }
            }
            return local;
        }
    }
}
