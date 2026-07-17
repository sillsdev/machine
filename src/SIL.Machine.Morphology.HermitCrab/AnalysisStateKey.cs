using System;
using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Order-independent identity of an analysis-cascade node, used by the memo cache in
    /// <see cref="AnalysisStratumRule"/>'s Unordered-mode morphological rule cascade (memoization.md). Two
    /// Words with an equal key are guaranteed to make identical decisions in every analysis-side
    /// morphological rule that cascade can invoke -- verified by inspecting what each one reads from its
    /// input (key-completeness audit, re-run whenever an <c>Analysis*.cs</c> rule changes):
    /// <list type="bullet">
    /// <item><see cref="MorphologicalRules.AnalysisAffixProcessRule"/>: Shape (FST pattern match) and
    /// <see cref="Word.SyntacticFeatureStruct"/> (unifiability gate) plus a per-rule unapplication count.</item>
    /// <item><see cref="MorphologicalRules.AnalysisCompoundingRule"/>: adds <see cref="Word.NonHeadCount"/>
    /// (<c>MaxStemCount</c> gate) -- it never reads the non-heads' own content, only the count.</item>
    /// <item><see cref="MorphologicalRules.AnalysisRealizationalAffixProcessRule"/>: adds
    /// <see cref="Word.RealizationalFeatureStruct"/>.</item>
    /// </list>
    /// but never the ORDER those rules were unapplied in -- exactly the redundancy this key collapses.
    /// Deliberately excludes fields <c>Word.ValueEquals</c> includes for a different purpose (result
    /// dedup): the unapplication trail as an ordered SEQUENCE (replaced here by an order-independent
    /// multiset) and <c>_isLastAppliedRuleFinal</c>/<c>IsPartial</c>, which are not read by any
    /// analysis-side rule (grep-verified against every file matching <c>MorphologicalRules/Analysis*.cs</c>
    /// and <c>PhonologicalRules/Analysis*.cs</c>).
    /// </summary>
    internal readonly struct AnalysisStateKey : IEquatable<AnalysisStateKey>
    {
        private readonly Shape _shape;
        private readonly Stratum _stratum;
        private readonly FeatureStruct _syntacticFS;
        private readonly FeatureStruct _realizationalFS;
        private readonly int _nonHeadCount;
        private readonly IReadOnlyDictionary<IMorphologicalRule, int> _ruleCounts;
        private readonly int _hashCode;

        public AnalysisStateKey(Word word)
        {
            _shape = word.Shape;
            _stratum = word.Stratum;
            _syntacticFS = word.SyntacticFeatureStruct;
            _realizationalFS = word.RealizationalFeatureStruct;
            _nonHeadCount = word.NonHeadCount;
            // Null means "no rule unapplied yet" -- Word never allocates this non-null-but-empty.
            _ruleCounts = word.UnappliedRuleCounts;

            // Defensive: AnalysisAffixTemplateRule.Apply reassigns SyntacticFeatureStruct to a fresh,
            // unfrozen clone AFTER the owning Word is already frozen (no CheckFrozen() guard on that
            // setter). Freeze() is idempotent and safe to call again here.
            _shape.Freeze();
            _syntacticFS.Freeze();
            _realizationalFS.Freeze();

            int hash = 17;
            hash = hash * 31 + _shape.GetFrozenHashCode();
            hash = hash * 31 + (_stratum?.GetHashCode() ?? 0);
            hash = hash * 31 + _syntacticFS.GetFrozenHashCode();
            hash = hash * 31 + _realizationalFS.GetFrozenHashCode();
            hash = hash * 31 + _nonHeadCount;
            if (_ruleCounts != null)
            {
                // XOR, not the usual *31 rolling combine: the multiset is unordered, so the combination
                // must be commutative -- two dictionaries with the same entries built up in different
                // unapplication orders must hash identically.
                int multisetHash = 0;
                foreach (KeyValuePair<IMorphologicalRule, int> kvp in _ruleCounts)
                    multisetHash ^= (kvp.Key.GetHashCode() * 397) ^ kvp.Value;
                hash = hash * 31 + multisetHash;
            }
            _hashCode = hash;
        }

        public override int GetHashCode() => _hashCode;

        public override bool Equals(object obj) => obj is AnalysisStateKey other && Equals(other);

        public bool Equals(AnalysisStateKey other)
        {
            if (_hashCode != other._hashCode)
                return false;
            if (_nonHeadCount != other._nonHeadCount || !ReferenceEquals(_stratum, other._stratum))
                return false;
            if (!_shape.ValueEquals(other._shape))
                return false;
            if (!_syntacticFS.ValueEquals(other._syntacticFS) || !_realizationalFS.ValueEquals(other._realizationalFS))
                return false;
            return RuleCountsEqual(_ruleCounts, other._ruleCounts);
        }

        private static bool RuleCountsEqual(
            IReadOnlyDictionary<IMorphologicalRule, int> a,
            IReadOnlyDictionary<IMorphologicalRule, int> b
        )
        {
            int aCount = a?.Count ?? 0;
            int bCount = b?.Count ?? 0;
            if (aCount != bCount)
                return false;
            if (aCount == 0)
                return true;
            foreach (KeyValuePair<IMorphologicalRule, int> kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out int otherCount) || otherCount != kvp.Value)
                    return false;
            }
            return true;
        }
    }
}
