using System;
using System.Collections.Generic;
using System.Threading;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// The die point a candidate that entered forward synthesis was rejected at, for the P1b histogram
    /// (docs/hermitcrab-synthesis-fold-probes.md section 3). Named after the table in that section rather
    /// than <see cref="FailureReason"/> directly: several <see cref="FailureReason"/> values collapse into
    /// one die point here because the plan's table groups them (e.g. a rule's own applicability gate and
    /// its pattern-match failure are one row, "morphological rule not applicable / pattern match
    /// failure"), and two sites -- the mrule-trail gate at <c>SynthesisAffixProcessRule.cs:43</c> and
    /// realizational subsumption/blocking -- have no <see cref="FailureReason"/> at all today because they
    /// return empty without ever calling <see cref="ITraceManager"/> (that call is gated on
    /// <c>IsTracing</c>, which the memoized synthesis path this probe measures always runs with off).
    /// </summary>
    internal enum SynthesisDiePoint
    {
        LexicalLookupMiss,
        ApplicationCount,
        RuleNotApplicableOrPatternMismatch,
        AllomorphEnvironment,
        RealizationalSubsumptionOrBlocking,
        FeatureUnification,
        MprFeatures,
        IsWordValid,
        SurfaceFormMismatch,
    }

    /// <summary>
    /// Static instrumentation hub for the P1 synthesis-fold probes (P1a wall-time split, P1b die-point
    /// histogram, P1c fold-step fingerprint ratio; see docs/hermitcrab-synthesis-fold-probes.md section 3).
    /// Measurement only: every counter here is write-only from the engine's point of view, and
    /// <see cref="Enabled"/> is the single gate that decides whether any of it runs at all -- nothing
    /// downstream of that flag ever feeds back into which analyses or syntheses a parse returns, so no
    /// engine control flow depends on it. Defaults to <c>false</c> so the ordinary test suite (several
    /// non-Explicit tests construct a sequential Morpher, e.g. MorpherTests, AnalysisStratumRuleTests) never
    /// pays for the P1c fold-step table, which pins Word references and would otherwise grow for the whole
    /// process lifetime with nothing ever the wiser to reset it. Only the P1 harness
    /// (SynthesisFoldProbe.cs, [Explicit]) turns this on.
    /// <para>
    /// Not lock-free: the harness always runs the sequential (maxDegreeOfParallelism: 1) path one word at a
    /// time, but the locks below are cheap insurance against NUnit fixture-level parallelism sharing this
    /// process, not a performance-sensitive path.
    /// </para>
    /// </summary>
    internal static class SynthesisProbe
    {
        internal static volatile bool Enabled;

        // ---- P1a: wall-time split ----
        // Two sides, kept as separate labelled buckets per the P1a follow-up (docs/hermitcrab-synthesis-fold-probes.md
        // section 3): the "syn*" buckets bracket disjoint regions inside Morpher.Synthesize/SynthesisStratumRule
        // (unchanged from the original P1a cut, just renamed so they read unambiguously next to the analysis
        // side). The "an*" buckets are the new analysis-side instrumentation this follow-up adds. AnTotalTicks
        // is a NESTED/INCLUSIVE total -- it brackets the whole of Morpher.ParseWord's `_analysisRule.Apply(input)`
        // call, and AnCascadeTicks/AnBatteryTicks/AnPhonoTicks are disjoint slices taken from calls *within* that
        // same call tree (see AnalysisStratumRule), so AnTotalTicks >= AnCascadeTicks + AnBatteryTicks + AnPhonoTicks.
        // The three "an*" slice buckets are mutually disjoint from each other and from the "syn*"/lookup buckets,
        // by the same non-overlapping-call-site construction the original synthesis-side buckets already used
        // (see SynthesisStratumRule's ApplyMorphologicalRules/ApplyTemplates remarks).
        private static long _lexicalLookupTicks;
        private static long _synCascadeTicks;
        private static long _synBatteryTicks;
        private static long _synForwardTicks;
        private static long _synExpandTicks;
        private static long _anTotalTicks;
        private static long _anCascadeTicks;
        private static long _anBatteryTicks;
        private static long _anPhonoTicks;

        internal static long LexicalLookupTicks => Interlocked.Read(ref _lexicalLookupTicks);
        internal static long SynCascadeTicks => Interlocked.Read(ref _synCascadeTicks);
        internal static long SynBatteryTicks => Interlocked.Read(ref _synBatteryTicks);
        internal static long SynForwardTicks => Interlocked.Read(ref _synForwardTicks);

        // ---- N1: ExpandAlternatives bracket ----
        // docs/hermitcrab-synthesis-fold-probes.md section 6.4's "one honest gap": Word.ExpandAlternatives
        // (Word.cs:470) is called per synthesis word in Morpher.SynthesizeSequential OUTSIDE every timed
        // region that existed before N1, doing recursive Clone/Unify/Subtract/Freeze work per alternative.
        // This is a new EXCLUSIVE top-level slice (bracketed around the ExpandAlternatives() call itself,
        // not around anything already covered by lookup/synCascade/synBattery/synForward/anTotal), so
        // "unaccounted" in SynthesisFoldProbe shrinks by exactly what this bucket gains.
        internal static long SynExpandTicks => Interlocked.Read(ref _synExpandTicks);
        internal static long AnTotalTicks => Interlocked.Read(ref _anTotalTicks);
        internal static long AnCascadeTicks => Interlocked.Read(ref _anCascadeTicks);
        internal static long AnBatteryTicks => Interlocked.Read(ref _anBatteryTicks);
        internal static long AnPhonoTicks => Interlocked.Read(ref _anPhonoTicks);

        internal static void AddLexicalLookupTicks(long ticks)
        {
            if (Enabled)
                Interlocked.Add(ref _lexicalLookupTicks, ticks);
        }

        internal static void AddSynCascadeTicks(long ticks)
        {
            if (Enabled)
                Interlocked.Add(ref _synCascadeTicks, ticks);
        }

        internal static void AddSynBatteryTicks(long ticks)
        {
            if (Enabled)
                Interlocked.Add(ref _synBatteryTicks, ticks);
        }

        internal static void AddSynForwardTicks(long ticks)
        {
            if (Enabled)
                Interlocked.Add(ref _synForwardTicks, ticks);
        }

        internal static void AddSynExpandTicks(long ticks)
        {
            if (Enabled)
                Interlocked.Add(ref _synExpandTicks, ticks);
        }

        internal static void AddAnTotalTicks(long ticks)
        {
            if (Enabled)
                Interlocked.Add(ref _anTotalTicks, ticks);
        }

        internal static void AddAnCascadeTicks(long ticks)
        {
            if (Enabled)
                Interlocked.Add(ref _anCascadeTicks, ticks);
        }

        internal static void AddAnBatteryTicks(long ticks)
        {
            if (Enabled)
                Interlocked.Add(ref _anBatteryTicks, ticks);
        }

        internal static void AddAnPhonoTicks(long ticks)
        {
            if (Enabled)
                Interlocked.Add(ref _anPhonoTicks, ticks);
        }

        internal static void ResetWallTime()
        {
            Interlocked.Exchange(ref _lexicalLookupTicks, 0);
            Interlocked.Exchange(ref _synCascadeTicks, 0);
            Interlocked.Exchange(ref _synBatteryTicks, 0);
            Interlocked.Exchange(ref _synForwardTicks, 0);
            Interlocked.Exchange(ref _synExpandTicks, 0);
            Interlocked.Exchange(ref _anTotalTicks, 0);
            Interlocked.Exchange(ref _anCascadeTicks, 0);
            Interlocked.Exchange(ref _anBatteryTicks, 0);
            Interlocked.Exchange(ref _anPhonoTicks, 0);
        }

        // ---- P1b: die-point histogram ----
        // Counts rejection EVENTS, not distinct top-level candidates: a single alternative can branch into
        // many internal rule/allomorph attempts, each independently able to die at a different check, and
        // there is no tractable way to attribute "the" one reason a whole subtree failed without changing
        // the traversal. This is what the table in section 3 is measuring anyway -- "which checks kill
        // candidates" -- and it is exactly what cinacemerwa's 218,847-candidate figure counts.
        private static readonly long[] _diePoints = new long[Enum.GetValues(typeof(SynthesisDiePoint)).Length];

        internal static void RecordDie(SynthesisDiePoint point)
        {
            if (Enabled)
                Interlocked.Increment(ref _diePoints[(int)point]);
        }

        internal static long GetDieCount(SynthesisDiePoint point) => Interlocked.Read(ref _diePoints[(int)point]);

        internal static void ResetDiePoints()
        {
            for (int i = 0; i < _diePoints.Length; i++)
                Interlocked.Exchange(ref _diePoints[i], 0);
        }

        // ---- P1c: fold-step fingerprint ratio ----
        // Keyed on the INPUT to a synthesis step (everything a step reads, see FingerprintHash/Equals
        // below) plus the rule that was applied. Persists across a whole fixture (not reset per word) so
        // the distinct-pair count is a true count over the combined stream, not a sum of per-word counts
        // that would double-count a pair recurring across words of the same fixture.
        private static readonly Dictionary<FoldStepKey, List<Word>> _foldSteps = new Dictionary<FoldStepKey, List<Word>>();
        private static long _totalApplications;
        private static long _determinismViolations;

        internal static long TotalApplications => Interlocked.Read(ref _totalApplications);

        internal static long DistinctFoldSteps
        {
            get
            {
                lock (_foldSteps)
                    return _foldSteps.Count;
            }
        }

        internal static long DeterminismViolations => Interlocked.Read(ref _determinismViolations);

        /// <summary>
        /// Records one call's worth of successful synthesis morphological-rule/template applications:
        /// <paramref name="rule"/> was applied to <paramref name="input"/>, producing every word in
        /// <paramref name="outputs"/>. Grouped as one call, not one per output, because several allomorphs
        /// of the same rule can legitimately all pattern-match the same input before a disjunctive break --
        /// (fingerprint, rule) is properly a SET-valued fold step, exactly the shape the plan doc's second
        /// trap already calls out for realizational rules ("any stored partial must be a set, like
        /// MemoEntry.Results, not a value"). Recording per output instead of per set would make ordinary
        /// disjunctive fan-out look like a determinism violation on every single-input, multi-allomorph
        /// rule -- this was tried first and produced exactly that false-positive flood.
        /// <para>
        /// The determinism check: if this exact (fingerprint, rule) pair was already seen (from a different
        /// call -- a different top-level candidate reaching the same state) with a different OUTCOME SET,
        /// that is a fingerprint-completeness bug (see the class remarks on <see cref="Word"/>'s callers
        /// about the <c>Word.ValueEquals</c> trap this exists to avoid), and is the single most important
        /// thing this probe can find.
        /// </para>
        /// </summary>
        internal static void RecordApplications(Word input, IMorphologicalRule rule, IReadOnlyList<Word> outputs)
        {
            if (!Enabled || outputs.Count == 0)
                return;

            Interlocked.Add(ref _totalApplications, outputs.Count);
            var key = new FoldStepKey(input, rule);
            lock (_foldSteps)
            {
                if (_foldSteps.TryGetValue(key, out List<Word> priorOutcomes))
                {
                    if (!OutcomeSetEquals(priorOutcomes, outputs))
                        Interlocked.Increment(ref _determinismViolations);
                }
                else
                {
                    _foldSteps[key] = new List<Word>(outputs);
                }
            }
        }

        // Bipartite multiset comparison via FingerprintEquals membership. Outcome sets here are small (at
        // most the number of allomorphs one rule declares), so the O(n*m) cost is negligible.
        private static bool OutcomeSetEquals(List<Word> a, IReadOnlyList<Word> b)
        {
            if (a.Count != b.Count)
                return false;

            var matched = new bool[b.Count];
            foreach (Word x in a)
            {
                bool found = false;
                for (int j = 0; j < b.Count; j++)
                {
                    if (!matched[j] && FingerprintEquals(x, b[j]))
                    {
                        matched[j] = true;
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
        }

        internal static void ResetFoldSteps()
        {
            lock (_foldSteps)
                _foldSteps.Clear();
            Interlocked.Exchange(ref _totalApplications, 0);
            Interlocked.Exchange(ref _determinismViolations, 0);
            ResetFoldEntries();
        }

        // ---- N1: dedupe census at fold entry ----
        // docs/hermitcrab-synthesis-fold-probes.md section "What to build", item 2. For every Word
        // ExpandAlternatives() produces that is about to enter _synthesisRule.Apply
        // (Morpher.SynthesizeSequential), tracks how many are literal duplicates -- by the SAME P1c
        // fingerprint used above, not a second one -- of an earlier alternative, and for each duplicate,
        // whether its first occurrence traces back to the same outer analysis word or a different one.
        // That split is the decisive one for the gates: same-analysis-word duplication is interceptable
        // BEFORE the Clone/Unify/Freeze work ExpandAlternatives just did (a dedupe could sit ahead of
        // ExpandAlternatives, keyed on the pre-expansion input); cross-analysis-word duplication is only
        // detectable by fingerprinting the post-expansion output, so by the time it is caught the expensive
        // work is already spent.
        // <para>
        // Same persistence lifecycle as _foldSteps (reset together, see ResetFoldSteps): accumulates across
        // a whole fixture/corpus so DistinctAlternatives is a true count over the combined stream, not a sum
        // of per-word counts that would double-count an alternative recurring across words.
        // </para>
        private static readonly Dictionary<AlternativeKey, Word> _foldEntries = new Dictionary<AlternativeKey, Word>();
        private static long _totalAlternatives;
        private static long _dupeSameAnalysisWord;
        private static long _dupeDifferentAnalysisWord;

        internal static long TotalAlternatives => Interlocked.Read(ref _totalAlternatives);

        internal static long DistinctAlternatives
        {
            get
            {
                lock (_foldEntries)
                    return _foldEntries.Count;
            }
        }

        internal static long DupeSameAnalysisWord => Interlocked.Read(ref _dupeSameAnalysisWord);
        internal static long DupeDifferentAnalysisWord => Interlocked.Read(ref _dupeDifferentAnalysisWord);

        /// <summary>
        /// Records one alternative arriving at the fold entry point (about to be passed to
        /// <c>_synthesisRule.Apply</c>), with <paramref name="analysisWord"/> the outer analysis word whose
        /// <c>LexicalLookup</c>/<c>ExpandAlternatives</c> chain produced it. Provenance is decided by
        /// reference identity against the analysis word recorded on first sight of this fingerprint --
        /// exactly the loop variable identity <c>Morpher.SynthesizeSequential</c> already has in scope, no
        /// separate ID scheme needed.
        /// </summary>
        internal static void RecordFoldEntry(Word analysisWord, Word alternative)
        {
            if (!Enabled)
                return;

            Interlocked.Increment(ref _totalAlternatives);
            var key = new AlternativeKey(alternative);
            lock (_foldEntries)
            {
                if (_foldEntries.TryGetValue(key, out Word firstAnalysisWord))
                {
                    if (ReferenceEquals(firstAnalysisWord, analysisWord))
                        Interlocked.Increment(ref _dupeSameAnalysisWord);
                    else
                        Interlocked.Increment(ref _dupeDifferentAnalysisWord);
                }
                else
                {
                    _foldEntries[key] = analysisWord;
                }
            }
        }

        internal static void ResetFoldEntries()
        {
            lock (_foldEntries)
                _foldEntries.Clear();
            Interlocked.Exchange(ref _totalAlternatives, 0);
            Interlocked.Exchange(ref _dupeSameAnalysisWord, 0);
            Interlocked.Exchange(ref _dupeDifferentAnalysisWord, 0);
        }

        internal static void ResetAll()
        {
            ResetWallTime();
            ResetDiePoints();
            ResetFoldSteps();
        }

        // ---- fingerprint machinery ----

        /// <summary>
        /// Everything a synthesis step reads, per docs/hermitcrab-synthesis-fold-probes.md section 3:
        /// shape+annotations, syntactic FS, realizational FS, MPR feature set, root allomorph, disjunctive
        /// allomorph indices, application counts, IsPartial, IsLastAppliedRuleFinal, stratum, and
        /// pending-trail position. Deliberately NOT <see cref="Word.ValueEquals"/> -- see Word.cs:600's
        /// remarks and the plan doc's trap section: that comparer omits SyntacticFeatureStruct, MPR
        /// features, and disjunctive allomorph indices, all three of which are checked here precisely
        /// because omitting them is what produced an inflated, invalid ratio on the predecessor probe.
        /// <para>
        /// Field-by-field justification against what Synthesis*.cs actually reads:
        /// <list type="bullet">
        /// <item>Shape+annotations (<see cref="Word.Shape"/>) -- every rule's pattern match
        /// (SynthesisAffixProcessAllomorphRuleSpec) reads the shape and its morph annotations.</item>
        /// <item>SyntacticFeatureStruct -- read by every rule's RequiredSyntacticFeatureStruct.Unify
        /// (SynthesisAffixProcessRule.cs, SynthesisRealizationalAffixProcessRule.cs) and by
        /// IsUnifiable/RealizationalFeatureStruct checks in SynthesisAffixTemplatesRule.</item>
        /// <item>RealizationalFeatureStruct -- read by SynthesisRealizationalAffixProcessRule's Subsumes
        /// and IsBlocked checks, and by SynthesisAffixTemplatesRule.ChooseInflectionalStem.</item>
        /// <item>MprFeatures -- read by every allomorph's RequiredMprFeatures/ExcludedMprFeatures check in
        /// both SynthesisAffixProcessRule and SynthesisRealizationalAffixProcessRule.</item>
        /// <item>RootAllomorph -- pattern matching and StemName/environment checks are root-allomorph
        /// dependent (SynthesisAffixProcessRule's RequiredStemName check reads
        /// <c>input.RootAllomorph.StemName</c> directly).</item>
        /// <item>Disjunctive allomorph indices -- read by Allomorph.IsWordValid via
        /// GetDisjunctiveAllomorphApplications; two words differing only here can validly disagree on
        /// whether a later allomorph is blocked.</item>
        /// <item>Application counts -- GetApplicationCount backs every rule's MaxApplicationCount gate
        /// (SynthesisAffixProcessRule.cs:46, SynthesisRealizationalAffixProcessRule's "at most once"
        /// gate).</item>
        /// <item>IsPartial -- read by SynthesisAffixProcessRule's final-template-adjacency gates and by
        /// SynthesisAffixTemplatesRule's applicability check.</item>
        /// <item>IsLastAppliedRuleFinal -- read by the same final-template-adjacency gates and by
        /// SynthesisStratumRule.Apply's own final-rule check.</item>
        /// <item>Stratum -- SynthesisStratumRule.Apply gates on
        /// <c>input.RootAllomorph.Morpheme.Stratum.Depth &gt; _stratum.Depth</c>, and HasRemainingRulesFromStratum
        /// elsewhere reads it.</item>
        /// <item>Pending-trail position (PendingTrailPosition / _mruleAppIndex) -- IsMorphologicalRuleApplicable
        /// reads exactly this index; paired with the "applied rule" half of the key (see
        /// <see cref="RecordApplication"/>), which rule was actually attempted is already captured there, so
        /// the index alone -- not the full trail content -- is what a step reads before deciding.</item>
        /// </list>
        /// </para>
        /// </summary>
        private static int FingerprintHash(Word w)
        {
            int hash = 17;
            hash = hash * 31 + w.Shape.GetFrozenHashCode();
            hash = hash * 31 + w.RealizationalFeatureStruct.GetFrozenHashCode();
            hash = hash * 31 + SyntacticFeatureStructWeakHash(w.SyntacticFeatureStruct);
            hash = hash * 31 + UnorderedSetHash(w.MprFeatures);
            hash = hash * 31 + (w.RootAllomorph?.GetHashCode() ?? 0);
            hash = hash * 31 + UnorderedDictHash(w.DisjunctiveAllomorphIndices, UnorderedSetHash);
            hash = hash * 31 + UnorderedDictHash(w.AppliedRuleCounts, v => v);
            hash = hash * 31 + w.IsPartial.GetHashCode();
            hash = hash * 31 + w.IsLastAppliedRuleFinal.GetHashCode();
            hash = hash * 31 + (w.Stratum?.GetHashCode() ?? 0);
            hash = hash * 31 + w.PendingTrailPosition.GetHashCode();
            return hash;
        }

        /// <summary>
        /// The real, exact comparison backing <see cref="FoldStepKey"/> equality (and the determinism
        /// check's outcome comparison). <see cref="FingerprintHash"/> is deliberately allowed to be a weak
        /// hash for the SyntacticFeatureStruct component (see
        /// <see cref="SyntacticFeatureStructWeakHash"/>) because correctness lives entirely here: a hash
        /// collision only costs a linear scan within a bucket, it can never merge two states that this
        /// method would call unequal.
        /// </summary>
        private static bool FingerprintEquals(Word a, Word b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null)
                return false;

            return a.Shape.ValueEquals(b.Shape)
                && a.RealizationalFeatureStruct.ValueEquals(b.RealizationalFeatureStruct)
                && a.SyntacticFeatureStruct.ValueEquals(b.SyntacticFeatureStruct)
                && a.MprFeatures.SetEquals(b.MprFeatures)
                && a.RootAllomorph == b.RootAllomorph
                && DictEquals(a.DisjunctiveAllomorphIndices, b.DisjunctiveAllomorphIndices, (x, y) => x.SetEquals(y))
                && DictEquals(a.AppliedRuleCounts, b.AppliedRuleCounts, (x, y) => x == y)
                && a.IsPartial == b.IsPartial
                && a.IsLastAppliedRuleFinal == b.IsLastAppliedRuleFinal
                && a.Stratum == b.Stratum
                && a.PendingTrailPosition == b.PendingTrailPosition;
        }

        // FeatureStruct.GetFrozenHashCode() throws unless the struct is frozen, and Word.Freeze() does NOT
        // freeze SyntacticFeatureStruct -- that is exactly why Word.ValueEquals omits it (Word.cs:600).
        // Freezing it here as a side effect, even just to hash it, would risk breaking later engine
        // mutation of that same object (e.g. SynthesisAffixProcessRule's
        // outWord.SyntacticFeatureStruct.PriorityUnion(...) on a different candidate that happens to share
        // the reference) -- exactly the kind of behavior change this probe must never cause. So this is a
        // deliberately weak, always-safe hash over the top-level feature keys only; FingerprintEquals above
        // always does the real FeatureStruct.ValueEquals, which needs no freeze.
        private static int SyntacticFeatureStructWeakHash(FeatureModel.FeatureStruct fs)
        {
            int acc = 0;
            foreach (FeatureModel.Feature f in fs.Features)
                acc ^= f.GetHashCode();
            return acc;
        }

        private static int UnorderedSetHash<T>(IEnumerable<T> items)
        {
            int acc = 0;
            foreach (T item in items)
                acc ^= item?.GetHashCode() ?? 0;
            return acc;
        }

        private static int UnorderedDictHash<TKey, TValue>(
            IReadOnlyDictionary<TKey, TValue> dict,
            Func<TValue, int> valueHash
        )
        {
            int acc = 0;
            foreach (KeyValuePair<TKey, TValue> kvp in dict)
                acc ^= ((kvp.Key?.GetHashCode() ?? 0) * 397) ^ valueHash(kvp.Value);
            return acc;
        }

        private static bool DictEquals<TKey, TValue>(
            IReadOnlyDictionary<TKey, TValue> a,
            IReadOnlyDictionary<TKey, TValue> b,
            Func<TValue, TValue, bool> valueEquals
        )
        {
            if (a.Count != b.Count)
                return false;
            foreach (KeyValuePair<TKey, TValue> kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out TValue otherValue) || !valueEquals(kvp.Value, otherValue))
                    return false;
            }
            return true;
        }

        private readonly struct FoldStepKey : IEquatable<FoldStepKey>
        {
            private readonly Word _word;
            private readonly IMorphologicalRule _rule;
            private readonly int _hash;

            public FoldStepKey(Word word, IMorphologicalRule rule)
            {
                _word = word;
                _rule = rule;
                _hash = (FingerprintHash(word) * 397) ^ (rule?.GetHashCode() ?? 0);
            }

            public bool Equals(FoldStepKey other) => _rule == other._rule && FingerprintEquals(_word, other._word);

            public override bool Equals(object obj) => obj is FoldStepKey k && Equals(k);

            public override int GetHashCode() => _hash;
        }

        // Same fingerprint as FoldStepKey, minus the rule component: N1's dedupe census keys on the
        // alternative alone, since it runs at fold entry, before any rule has been chosen/applied.
        private readonly struct AlternativeKey : IEquatable<AlternativeKey>
        {
            private readonly Word _word;
            private readonly int _hash;

            public AlternativeKey(Word word)
            {
                _word = word;
                _hash = FingerprintHash(word);
            }

            public bool Equals(AlternativeKey other) => FingerprintEquals(_word, other._word);

            public override bool Equals(object obj) => obj is AlternativeKey k && Equals(k);

            public override int GetHashCode() => _hash;
        }
    }
}
