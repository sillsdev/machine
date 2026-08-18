using System.Collections.Generic;
using System.Threading;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// The sequential <see cref="CombinationRuleCascade{TData,TOffset}"/> plus memoization of each
    /// expanded subtree, for Unordered-order analysis strata. A node whose
    /// <see cref="AnalysisStateKey"/> was already searched earlier in this word's analysis, via a
    /// different unapplication order, is not searched again: an empty stored result short-circuits, and a
    /// non-empty one is replayed onto the current arrival (<see cref="Word.ReplayOnto"/>).
    /// <para>
    /// The parallel cascade is left unmemoized -- its breadth-first walk never reaches a point where a
    /// given subtree is known to be fully expanded, so there is nowhere to hang a memo write.
    /// </para>
    /// </summary>
    internal class MemoizedCombinationRuleCascade : CombinationRuleCascade<Word, ShapeNode>
    {
        // Read by the equivalence tests to prove the memo actually fired: one that silently stopped
        // firing would otherwise look exactly like a passing test.
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

        // Returns the results produced strictly within `input`'s subtree, at any depth, excluding `input`
        // itself -- both what callers consume and what gets memoized against `input`'s key.
        private List<Word> ApplyRules(Word input, HashSet<Word> output)
        {
            AnalysisScope scope = input.AnalysisScope;
            // See Word.AnalysisScope's doc for when this is null.
            if (scope == null)
                return ApplyRulesRaw(input, output);

            var key = new AnalysisStateKey(input);

            if (scope.TryReplay(scope.Memo, key, input, out List<Word> replayed))
            {
                if (replayed.Count == 0)
                {
                    Interlocked.Increment(ref DiagNogoodHits);
                    return replayed;
                }
                foreach (Word replay in replayed)
                {
                    output.Add(replay);
                    CheckMaxAlternatives(output.Count);
                }
                Interlocked.Increment(ref DiagMemoHits);
                return replayed;
            }

            // In-flight re-entry guard, see AnalysisScope.InProgress.
            if (!scope.InProgress.Add(key))
                return ApplyRulesRaw(input, output);

            List<Word> results;
            try
            {
                results = ApplyRulesRaw(input, output);
            }
            finally
            {
                scope.InProgress.Remove(key);
            }

            scope.Store(scope.Memo, key, input, results);
            return results;
        }

        // Mirrors the base's multiApp expansion, including its recurse-before-add ordering: the HashSet
        // keeps whichever of two comparer-equal results lands first, and Word.ValueEquals ignores
        // SyntacticFeatureStruct, so which one survives is observable downstream. Delegating to the base
        // is not possible -- it collects into one globally-deduped set, so a subtree result another branch
        // already contributed is missing from it, yet must still be recorded here or a later replay of
        // this key would return too few results.
        private List<Word> ApplyRulesRaw(Word input, HashSet<Word> output)
        {
            var local = new List<Word>();
            for (int i = 0; i < Rules.Count; i++)
            {
                foreach (Word result in ApplyRule(Rules[i], i, input))
                {
                    // avoid infinite loop -- same guard CombinationRuleCascade uses
                    if (!Comparer.Equals(input, result))
                        local.AddRange(ApplyRules(result, output));
                    local.Add(result);
                    output.Add(result);
                    CheckMaxAlternatives(output.Count);
                }
            }
            return local;
        }
    }
}
