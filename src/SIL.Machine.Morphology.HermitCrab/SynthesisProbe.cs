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
        private static long _lexicalLookupTicks;
        private static long _cascadeTicks;
        private static long _templateBatteryTicks;
        private static long _forwardSynthesisTicks;

        internal static long LexicalLookupTicks => Interlocked.Read(ref _lexicalLookupTicks);
        internal static long CascadeTicks => Interlocked.Read(ref _cascadeTicks);
        internal static long TemplateBatteryTicks => Interlocked.Read(ref _templateBatteryTicks);
        internal static long ForwardSynthesisTicks => Interlocked.Read(ref _forwardSynthesisTicks);

        internal static void AddLexicalLookupTicks(long ticks)
        {
            if (Enabled)
                Interlocked.Add(ref _lexicalLookupTicks, ticks);
        }

        internal static void AddCascadeTicks(long ticks)
        {
            if (Enabled)
                Interlocked.Add(ref _cascadeTicks, ticks);
        }

        internal static void AddTemplateBatteryTicks(long ticks)
        {
            if (Enabled)
                Interlocked.Add(ref _templateBatteryTicks, ticks);
        }

        internal static void AddForwardSynthesisTicks(long ticks)
        {
            if (Enabled)
                Interlocked.Add(ref _forwardSynthesisTicks, ticks);
        }

        internal static void ResetWallTime()
        {
            Interlocked.Exchange(ref _lexicalLookupTicks, 0);
            Interlocked.Exchange(ref _cascadeTicks, 0);
            Interlocked.Exchange(ref _templateBatteryTicks, 0);
            Interlocked.Exchange(ref _forwardSynthesisTicks, 0);
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
        private static readonly Dictionary<FoldStepKey, Word> _foldSteps = new Dictionary<FoldStepKey, Word>();
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
        /// Records one successful synthesis morphological-rule/template application: <paramref name="rule"/>
        /// was applied to <paramref name="input"/>, producing <paramref name="output"/>. Also runs the
        /// determinism check: if this exact (fingerprint, rule) pair was already seen with a different
        /// outcome fingerprint, that is a fingerprint-completeness bug (see the class remarks on
        /// <see cref="Word"/>'s callers about the <c>Word.ValueEquals</c> trap this exists to avoid), and is
        /// the single most important thing this probe can find.
        /// </summary>
        internal static void RecordApplication(Word input, IMorphologicalRule rule, Word output)
        {
            if (!Enabled)
                return;

            Interlocked.Increment(ref _totalApplications);
            var key = new FoldStepKey(input, rule);
            lock (_foldSteps)
            {
                if (_foldSteps.TryGetValue(key, out Word priorOutcome))
                {
                    if (!FingerprintEquals(priorOutcome, output))
                        Interlocked.Increment(ref _determinismViolations);
                }
                else
                {
                    _foldSteps[key] = output;
                }
            }
        }

        internal static void ResetFoldSteps()
        {
            lock (_foldSteps)
                _foldSteps.Clear();
            Interlocked.Exchange(ref _totalApplications, 0);
            Interlocked.Exchange(ref _determinismViolations, 0);
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
    }
}
