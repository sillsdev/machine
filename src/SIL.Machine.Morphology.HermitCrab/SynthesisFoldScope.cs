using System;
using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Carrier for the synthesis fold-step memo (docs/hermitcrab-synthesis-fold-probes.md). Same shape as
    /// <see cref="AnalysisScope"/>: one instance per <see cref="Morpher.ParseWord(string, out object)"/>
    /// call, threaded through <see cref="Word.SynthesisFoldScope"/>, not installed while tracing or when
    /// running with more than one degree of parallelism (plain <see cref="Dictionary{TKey,TValue}"/>, not
    /// thread-safe).
    /// <para>
    /// A memo entry is keyed on (<see cref="SynthesisStateKey"/>, applied rule) and holds a SET of output
    /// Words -- not one value -- because several allomorphs of one rule can legitimately all pattern-match
    /// one input before a disjunctive break (see the doc comment on <c>SynthesisAffixProcessRule.Apply</c>'s
    /// call to <c>SynthesisProbe.RecordApplications</c>, which records fold steps the same way for the same
    /// reason). Stored Words are never handed to a caller directly: every read goes through
    /// <see cref="Word.ReanchorSynthesisStep"/>, which re-parents the stored result onto the querying
    /// candidate's own trail/non-head identity.
    /// </para>
    /// <para>
    /// Only successful steps are stored, mirroring <see cref="SynthesisProbe.RecordApplications"/>: a
    /// trail-position mismatch (<c>IsMorphologicalRuleApplicable</c> false) is an O(1) index-plus-reference
    /// check the plan doc itself characterizes as "not 95% of anything" at ~29ns, so memoizing it would
    /// spend a scarce entry slot buying almost nothing. Empty results *past* that cheap gate (a rule that
    /// was trail-eligible but whose allomorphs all failed to unify/pattern-match) ARE stored, because
    /// reaching that verdict is exactly the expensive work (unification, pattern matching) this memo exists
    /// to share.
    /// </para>
    /// </summary>
    internal sealed class SynthesisFoldScope
    {
        // Same cap philosophy as AnalysisScope.MaxMemoEntries: a coarse backstop, not a figure derived from
        // measured memory. Correctness never depends on it -- past the cap, new fold steps are simply
        // computed and returned unmemoized, same as when the scope itself is null.
        private const int MaxMemoEntries = 100_000;

        private readonly Dictionary<SynthesisFoldStepKey, IReadOnlyList<Word>> _memo =
            new Dictionary<SynthesisFoldStepKey, IReadOnlyList<Word>>();

        /// <summary>Per-parse hit count, folded into the owning Morpher when the parse ends.</summary>
        public int Hits { get; set; }

        // Diagnostic totals for the A/B harness: distinguishes "the memo never hits" (the idea is dead)
        // from "the memo hits but the key costs more than the step it saves" (the key is the problem).
        // Free-running; a harness reads the delta.
        internal static long DiagHits;
        internal static long DiagStores;
        internal static long DiagLookups;

        public bool TryGet(SynthesisStateKey key, IMorphologicalRule rule, out IReadOnlyList<Word> outputs)
        {
            DiagLookups++;
            bool hit = _memo.TryGetValue(new SynthesisFoldStepKey(key, rule), out outputs);
            if (hit)
                DiagHits++;
            return hit;
        }

        public void Store(SynthesisStateKey key, IMorphologicalRule rule, IReadOnlyList<Word> outputs)
        {
            if (_memo.Count >= MaxMemoEntries)
                return;
            DiagStores++;
            _memo[new SynthesisFoldStepKey(key, rule)] = outputs;
        }

        private readonly struct SynthesisFoldStepKey : IEquatable<SynthesisFoldStepKey>
        {
            private readonly SynthesisStateKey _state;
            private readonly IMorphologicalRule _rule;
            private readonly int _hash;

            public SynthesisFoldStepKey(SynthesisStateKey state, IMorphologicalRule rule)
            {
                _state = state;
                _rule = rule;
                _hash = (state.GetHashCode() * 397) ^ (rule?.GetHashCode() ?? 0);
            }

            public bool Equals(SynthesisFoldStepKey other) => _rule == other._rule && _state.Equals(other._state);

            public override bool Equals(object obj) => obj is SynthesisFoldStepKey k && Equals(k);

            public override int GetHashCode() => _hash;
        }
    }
}
