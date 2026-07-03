using System.Collections.Generic;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Drop-in replacement for the sequential <see cref="CombinationRuleCascade{TData,TOffset}"/> on
    /// Unordered-order analysis strata (parse-optimization.md Phases 2-3). Before expanding a node, checks
    /// whether an earlier expansion elsewhere in the same word's analysis -- reached via a different
    /// unapplication order, but with an equal <see cref="AnalysisStateKey"/> -- already searched this
    /// exact state:
    /// <list type="bullet">
    /// <item>proved empty (Phase 2's "nogood" case) -> skip straight to "no results";</item>
    /// <item>produced results (Phase 3) -> replay them (<see cref="Word.ReplayOnto"/>) instead of
    /// re-searching: clone each stored result and graft the CURRENT arrival's own trail/non-head prefix
    /// onto the stored subtree-local suffix (see <see cref="MemoEntry"/> and <see cref="Word.ReplayOnto"/>
    /// for why only the prefix -- never the suffix -- needs replacing).</item>
    /// </list>
    /// Measured on the real Sena grammar's worst word (cinacemerwa), Phase 2 alone (nogoods only): 523,774
    /// node expansions, 91.1% nogood-cache hits, 254s -> 192s wall clock (~24%) -- real but well below the
    /// node-count reduction, because a nogood hit is a cheap dictionary lookup while the remaining ~9% of
    /// productive nodes still paid full FST-match + clone cost on every revisit. Phase 3 targets exactly
    /// that remaining cost: a state that DOES yield results is typically revisited hundreds to thousands of
    /// times (the single worst atawirambo state was re-expanded 7,200x), and each revisit was a full
    /// re-search before this change.
    ///
    /// Scoped to the sequential cascade only. The parallel cascade
    /// (<c>ParallelCombinationRuleCascade</c>, used when <see cref="Morpher.MaxDegreeOfParallelism"/> > 1)
    /// is a level-by-level breadth-first walk with no natural "this subtree is fully expanded" moment to
    /// hang a memo write on, so it is left unmemoized for now -- callers that want this optimization
    /// should construct their <see cref="Morpher"/> with <c>maxDegreeOfParallelism: 1</c> (the mode the
    /// constructor's own doc comment already recommends for a caller that parallelizes across words
    /// itself, which describes exactly the corpus-batch workloads this optimization targets).
    /// </summary>
    internal class MemoizedCombinationRuleCascade : RuleCascade<Word, int>
    {
        private readonly Morpher _morpher;

        public MemoizedCombinationRuleCascade(
            IEnumerable<IRule<Word, int>> rules,
            IEqualityComparer<Word> comparer,
            Morpher morpher
        )
            : base(rules, true, comparer)
        {
            _morpher = morpher;
        }

        public override IEnumerable<Word> Apply(Word input)
        {
            var output = new HashSet<Word>(Comparer);
            ApplyRules(input, output);
            AddRuleStats(output.Count);
            return output;
        }

        // Returns every result produced strictly within the subtree rooted at `input` (i.e. by applying one
        // or more rules starting from `input`, at any depth) -- NOT including `input` itself. This is both
        // the return value callers use and the value memoized against `input`'s AnalysisStateKey once the
        // subtree finishes, so a later differently-ordered arrival at the same state can replay it via
        // Word.ReplayOnto instead of re-searching.
        private List<Word> ApplyRules(Word input, HashSet<Word> output)
        {
            AnalysisScope scope = input.AnalysisScope;
            // Null while tracing (Morpher.ParseWord skips allocating one) or for words never routed through
            // ParseWord at all (e.g. rule-level unit tests) -- fall back to unmemoized behavior rather than
            // throw, per the ground rule that tracing must stay byte-identical to the unmemoized engine.
            if (scope == null)
                return ApplyRulesRaw(input, output);

            var key = new AnalysisStateKey(input);

            if (scope.Memo.TryGetValue(key, out MemoEntry entry))
            {
                var replayed = new List<Word>(entry.Results.Count);
                foreach (Word storedResult in entry.Results)
                {
                    Word replay = storedResult.ReplayOnto(input, entry.MruleTrailPrefixLength, entry.NonHeadPrefixLength);
                    output.Add(replay);
                    replayed.Add(replay);
                }
                return replayed;
            }

            // In-flight re-entry guard: a multiApp cascade can reach the same state again while its own
            // first expansion is still on the stack (e.g. a self-loop through a rule that returns its input
            // unchanged via a different route). Rather than read a nonexistent/partial entry or deadlock,
            // fall through to a plain unmemoized expansion for just this arrival -- correctness-neutral,
            // it only forgoes memoization for the one re-entrant call.
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

            // OOM guard (parse-optimization.md Phase 3): past the cap, keep searching correctly, just stop
            // growing the table. No "exhausted" bookkeeping is needed here -- this branch has no
            // step/time-budget infrastructure, so `results` always reflects a fully-completed subtree.
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
                    if (Comparer.Equals(input, result))
                        continue;

                    // parse-optimization.md Phase 5: emission above is cheap and deeper strata/templates
                    // may still transform this result, so it always gets recorded regardless -- only
                    // DESCENT (the potentially exponential part) is gated. A result whose shape can't
                    // reach any root in its own or a deeper stratum can never bottom out in a valid
                    // analysis no matter how the rest of the cascade proceeds.
                    if (
                        result.AnalysisScope != null
                        && result.AnalysisScope.LexicalGatingActive
                        && !_morpher.HasReachableRoot(result)
                    )
                    {
                        continue;
                    }

                    local.AddRange(ApplyRules(result, output));
                }
            }
            return local;
        }
    }
}
