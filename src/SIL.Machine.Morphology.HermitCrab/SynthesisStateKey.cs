using System;
using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Identity of a synthesis fold-step input, for <see cref="SynthesisFoldScope"/>
    /// (docs/hermitcrab-synthesis-fold-probes.md). Two Words with an equal key must make an identical
    /// decision -- same output set -- for the same rule in every synthesis-side class the fold can invoke;
    /// that is the memo's correctness contract, so this key-completeness audit has to be re-run whenever a
    /// <c>Synthesis*.cs</c> rule or <c>Allomorph</c>/<c>Morpher.IsWordValid</c> changes.
    /// <para>
    /// <b>Not a starting point: <c>SynthesisProbe</c>'s P1c fingerprint.</b> That fingerprint is a
    /// *measurement* key (docs/hermitcrab-synthesis-fold-probes.md section 3) and is unsound as a memo key
    /// for exactly one reason, recorded as a trap in the plan doc and reconfirmed by the N1 census
    /// retraction (section 7): it carries <c>PendingTrailPosition</c>, an integer index, and no
    /// remaining-trail *content*. Two candidates at the same index with different pending rule sequences
    /// compare equal under it. For a per-step measurement that is fine -- the "applied rule" half of
    /// <c>FoldStepKey</c> already tells you which single rule was attempted. For a memo that *skips real
    /// work and hands back a stored result*, it silently drops the distinct continuation the omitted trail
    /// content represents: two states that will diverge on the very next step get merged, and the rarer
    /// path's parse is lost. This key fixes exactly that gap (see <see cref="_pendingTrail"/> below) and is
    /// otherwise structurally close to the P1c fingerprint, because that fingerprint's own field list is
    /// itself the result of an audit against these same classes.
    /// </para>
    /// <para>
    /// <b>Not <c>Word.ValueEquals</c>.</b> (<c>Word.cs</c>) compares shape, realizational FS, non-heads,
    /// stratum, root allomorph, trail, index and the final-rule flag, and omits
    /// <c>SyntacticFeatureStruct</c>, MPR features, and disjunctive allomorph indices -- all three read
    /// below. It also includes non-heads, which nothing this memo covers reads or writes (see field list).
    /// </para>
    /// <para>
    /// Field-by-field justification, audited against <see cref="MorphologicalRules.SynthesisAffixProcessRule"/>,
    /// <see cref="MorphologicalRules.SynthesisRealizationalAffixProcessRule"/>, <see cref="SynthesisStratumRule"/>,
    /// <see cref="SynthesisAffixTemplateRule"/>, <see cref="SynthesisAffixTemplatesRule"/>,
    /// <see cref="Allomorph.IsWordValid(Morpher, Word)"/>, and the private <c>Morpher.IsWordValid(Word)</c>:
    /// <list type="bullet">
    /// <item><b>Shape</b> (<see cref="Word.Shape"/>, compared/hashed via <c>ValueEquals</c>/
    /// <c>GetFrozenHashCode</c>) -- every rule's pattern match reads it
    /// (<c>SynthesisAffixProcessAllomorphRuleSpec</c>), and <c>Allomorph.IsWordValid</c>'s environment and
    /// disjunctive-allomorph checks read the surrounding shape context. <c>Shape.ValueEquals</c> recurses
    /// into every annotation's <c>FeatureStruct</c>, including each "Morph" annotation's MorphID and
    /// Allomorph feature values -- so two candidates whose shape differs only in which
    /// <c>_mruleAppCount</c>-derived MorphID got stamped on an otherwise-identical morph (a real hazard:
    /// realizational rules are trail-exempt and can fire a different number of times along two paths that
    /// otherwise converge) are already caught here, with no separate field needed. This is also why
    /// <c>Allomorphs</c>/<c>ObligatorySyntacticFeatures</c> (both read by <c>Morpher.IsWordValid</c>) do not
    /// need their own field: the allomorph-ID set embedded in every Morph annotation's FeatureStruct pins
    /// <c>_allomorphs</c>' key set (and, since <c>Allomorph</c> objects are grammar-level singletons per ID,
    /// its values too), and <c>ObligatorySyntacticFeatures</c> is a deterministic union of
    /// <c>rule.ObligatorySyntacticFeatures</c> over the rules with a positive count in
    /// <see cref="_appliedRuleCounts"/>, which is already a field below.</item>
    /// <item><b>SyntacticFeatureStruct</b> -- read by every rule's <c>RequiredSyntacticFeatureStruct.Unify</c>
    /// (<c>SynthesisAffixProcessRule.cs</c>, <c>SynthesisRealizationalAffixProcessRule.cs</c>), by
    /// <c>SynthesisAffixTemplatesRule</c>'s <c>IsUnifiable</c>/<c>ChooseInflectionalStem</c> checks, and by
    /// <c>Morpher.IsWordValid</c>'s <c>RealizationalFeatureStruct.IsUnifiable</c> and obligatory-feature
    /// checks. Hashed with a deliberately weak, freeze-free hash (mirroring
    /// <c>SynthesisProbe.SyntacticFeatureStructWeakHash</c>): <c>Word.FreezeImpl</c> does not freeze this
    /// field (<c>AnalysisAffixTemplateRule.Apply</c> mutates it on already-frozen Words), so
    /// <c>GetFrozenHashCode</c> is unavailable and must not be forced by freezing it here as a side effect.
    /// Correctness lives entirely in <see cref="Equals"/>, which always does the real
    /// <c>FeatureStruct.ValueEquals</c>; a hash collision only costs a linear bucket scan.</item>
    /// <item><b>RealizationalFeatureStruct</b> -- read by <c>SynthesisRealizationalAffixProcessRule</c>'s
    /// <c>Subsumes</c>/<c>IsBlocked</c> checks, by <c>SynthesisAffixTemplatesRule.ChooseInflectionalStem</c>
    /// and its own <c>IsUnifiable</c> gate, and by <c>Morpher.IsWordValid</c>'s <c>IsUnifiable</c> check.</item>
    /// <item><b>MprFeatures</b> -- read by every allomorph's <c>RequiredMprFeatures</c>/<c>ExcludedMprFeatures</c>
    /// check in both memoized rule classes.</item>
    /// <item><b>RootAllomorph</b> (reference identity) -- <c>SynthesisAffixProcessRule</c>'s
    /// <c>RequiredStemName</c> check reads <c>input.RootAllomorph.StemName</c> directly, and
    /// <c>SynthesisAffixTemplatesRule.ChooseInflectionalStem</c> reads
    /// <c>input.RootAllomorph.Morpheme</c>'s family/stratum.</item>
    /// <item><b>DisjunctiveAllomorphIndices</b> -- read by <c>Allomorph.IsWordValid</c> via
    /// <c>GetDisjunctiveAllomorphApplications</c>; two words differing only here can validly disagree on
    /// whether a later allomorph is blocked. Compared as a dictionary of sets (order-independent both
    /// within each set and across morph IDs), matching how it is written
    /// (<c>Word.MorphologicalRuleApplied</c>'s <c>UnionWith</c>).</item>
    /// <item><b>AppliedRuleCounts</b> -- backs every rule's <c>MaxApplicationCount</c> gate
    /// (<c>SynthesisAffixProcessRule.cs</c>) and realizational's "at most once" gate
    /// (<c>SynthesisRealizationalAffixProcessRule.cs</c>), and -- see the Shape item above -- transitively
    /// pins <c>ObligatorySyntacticFeatures</c>. Compared as a dictionary (order-independent), matching how
    /// unapplication counts are compared on the analysis side.</item>
    /// <item><b>IsPartial</b> -- read by <c>SynthesisAffixProcessRule</c>'s final-template-adjacency gates
    /// and by <c>SynthesisAffixTemplatesRule</c>'s applicable-template / no-template-fired branches.</item>
    /// <item><b>IsLastAppliedRuleFinal</b> -- read by the same final-template-adjacency gates and by
    /// <c>SynthesisStratumRule.Apply</c>'s own final-rule check
    /// (<c>mruleOutWord.IsLastAppliedRuleFinal ?? false</c>).</item>
    /// <item><b>Stratum</b> -- <c>SynthesisStratumRule.Apply</c> gates on
    /// <c>input.RootAllomorph.Morpheme.Stratum.Depth &gt; _stratum.Depth</c>, and
    /// <c>HasRemainingRulesFromStratum</c> reads it via <c>curRule.Stratum</c>.</item>
    /// <item><b>Pending trail content</b> (<see cref="_pendingTrail"/>, an ORDERED sequence, not a
    /// multiset -- unlike <see cref="AnalysisStateKey"/>'s unapplication counts, order here is exactly what
    /// determines which rule fires next, so two equal-length pending trails with the rules in a different
    /// order are genuinely different states) -- <c>IsMorphologicalRuleApplicable</c> and
    /// <c>HasRemainingRulesFromStratum</c> read only its first (current) entry to decide *this* step, but
    /// the memo also has to guarantee the *next* step -- run against this step's stored output, replayed
    /// onto a different query candidate -- sees the correct rest of the trail. This is the field the P1c
    /// fingerprint deliberately omits; see the class remarks above.</item>
    /// </list>
    /// Deliberately excluded, with the reason a rule-read audit does not license including them:
    /// <list type="bullet">
    /// <item>The already-consumed trail suffix (entries past the pending prefix) and the non-head list --
    /// never read by any rule again once passed, only by <c>MorphemesInApplicationOrder</c> on a finished
    /// result. <see cref="Word.ReanchorSynthesisStep"/> splices both from the query candidate rather than
    /// the stored one, exactly because the key does not (and must not, to keep sharing meaningful) pin them
    /// down.</item>
    /// <item><c>_mruleAppCount</c> and MorphID strings -- bookkeeping for a per-word morph-annotation
    /// ordinal, not a decision input to any rule; already transitively pinned by Shape equality (see the
    /// Shape item above), so adding it explicitly would be redundant, not more complete.</item>
    /// <item>Compounding-rule state (<c>CurrentNonHead</c>/non-heads) -- read only by
    /// <see cref="MorphologicalRules.SynthesisCompoundingRule"/>, which this memo does not intercept (it is
    /// out of the audited class list above, and out of what <c>SynthesisProbe</c>'s P1c ratio measured).
    /// Compounding-driven fold steps always fall through unmemoized.</item>
    /// </list>
    /// </para>
    /// </summary>
    internal readonly struct SynthesisStateKey : IEquatable<SynthesisStateKey>
    {
        private readonly Shape _shape;
        private readonly FeatureStruct _syntacticFS;
        private readonly FeatureStruct _realizationalFS;
        private readonly MprFeatureSet _mprFeatures;
        private readonly RootAllomorph _rootAllomorph;
        private readonly IReadOnlyDictionary<string, HashSet<int>> _disjunctiveAllomorphIndices;
        private readonly IReadOnlyDictionary<IMorphologicalRule, int> _appliedRuleCounts;
        private readonly bool _isPartial;
        private readonly bool? _isLastAppliedRuleFinal;
        private readonly Stratum _stratum;
        private readonly IMorphologicalRule[] _pendingTrail;
        private readonly int _hashCode;

        /// <summary>
        /// Keys <paramref name="word"/>. A named factory to match <c>AnalysisStateKey.PinAndKey</c>'s
        /// style, though unlike that key this one never mutates <paramref name="word"/>: it deliberately
        /// avoids freezing <c>SyntacticFeatureStruct</c> (see the class remarks).
        /// </summary>
        public static SynthesisStateKey PinAndKey(Word word)
        {
            return new SynthesisStateKey(word);
        }

        private SynthesisStateKey(Word word)
        {
            if (!word.IsFrozen)
                throw new ArgumentException(
                    "The word must be frozen before it can be used as a memo key.",
                    nameof(word)
                );

            _shape = word.Shape;
            _syntacticFS = word.SyntacticFeatureStruct;
            _realizationalFS = word.RealizationalFeatureStruct;
            _mprFeatures = word.MprFeatures;
            _rootAllomorph = word.RootAllomorph;
            _disjunctiveAllomorphIndices = word.DisjunctiveAllomorphIndices;
            _appliedRuleCounts = word.AppliedRuleCounts;
            _isPartial = word.IsPartial;
            _isLastAppliedRuleFinal = word.IsLastAppliedRuleFinal;
            _stratum = word.Stratum;

            int pendingLength = word.PendingTrailPosition + 1;
            _pendingTrail = pendingLength <= 0 ? Array.Empty<IMorphologicalRule>() : new IMorphologicalRule[pendingLength];
            for (int i = 0; i < pendingLength; i++)
                _pendingTrail[i] = word.MorphologicalRuleTrail[i];

            _realizationalFS.Freeze();

            int hash = 17;
            hash = hash * 31 + _shape.GetFrozenHashCode();
            hash = hash * 31 + SyntacticFeatureStructWeakHash(_syntacticFS);
            hash = hash * 31 + _realizationalFS.GetFrozenHashCode();
            hash = hash * 31 + UnorderedSetHash(_mprFeatures);
            hash = hash * 31 + (_rootAllomorph?.GetHashCode() ?? 0);
            hash = hash * 31 + UnorderedDictHash(_disjunctiveAllomorphIndices, UnorderedSetHash);
            hash = hash * 31 + UnorderedDictHash(_appliedRuleCounts, v => v);
            hash = hash * 31 + _isPartial.GetHashCode();
            hash = hash * 31 + _isLastAppliedRuleFinal.GetHashCode();
            hash = hash * 31 + (_stratum?.GetHashCode() ?? 0);
            foreach (IMorphologicalRule rule in _pendingTrail)
                hash = hash * 31 + (rule?.GetHashCode() ?? 0);
            _hashCode = hash;
        }

        public override int GetHashCode() => _hashCode;

        public override bool Equals(object obj) => obj is SynthesisStateKey other && Equals(other);

        public bool Equals(SynthesisStateKey other)
        {
            if (_hashCode != other._hashCode)
                return false;
            if (_isPartial != other._isPartial || _isLastAppliedRuleFinal != other._isLastAppliedRuleFinal)
                return false;
            if (!ReferenceEquals(_stratum, other._stratum) || !ReferenceEquals(_rootAllomorph, other._rootAllomorph))
                return false;
            if (!PendingTrailEqual(_pendingTrail, other._pendingTrail))
                return false;
            if (!_shape.ValueEquals(other._shape))
                return false;
            if (!_syntacticFS.ValueEquals(other._syntacticFS) || !_realizationalFS.ValueEquals(other._realizationalFS))
                return false;
            if (!_mprFeatures.SetEquals(other._mprFeatures))
                return false;
            if (!DictEquals(_appliedRuleCounts, other._appliedRuleCounts, (x, y) => x == y))
                return false;
            return DictEquals(
                _disjunctiveAllomorphIndices,
                other._disjunctiveAllomorphIndices,
                (x, y) => x.SetEquals(y)
            );
        }

        private static bool PendingTrailEqual(IMorphologicalRule[] a, IMorphologicalRule[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }

        // FeatureStruct.GetFrozenHashCode() throws unless frozen, and SyntacticFeatureStruct is
        // deliberately never frozen by this key (see class remarks). Weak but always-safe: Equals always
        // does the real ValueEquals, so a collision only costs a linear bucket scan, never a false merge.
        private static int SyntacticFeatureStructWeakHash(FeatureStruct fs)
        {
            int acc = 0;
            foreach (Feature f in fs.Features)
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
    }
}
