using System;
using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Order-independent identity of an analysis-cascade node. Two Words with an equal
    /// key must make identical decisions in every analysis-side rule the cascade can invoke; that is the
    /// memo's correctness contract, so this key-completeness audit of what each rule reads has to be
    /// re-run whenever an <c>Analysis*.cs</c> rule changes:
    /// <list type="bullet">
    /// <item><see cref="MorphologicalRules.AnalysisAffixProcessRule"/>: Shape (FST pattern match),
    /// <see cref="Word.SyntacticFeatureStruct"/> (unifiability gate), per-rule unapplication count.</item>
    /// <item><see cref="MorphologicalRules.AnalysisCompoundingRule"/>: adds <see cref="Word.NonHeadCount"/>
    /// (<c>MaxStemCount</c> gate) -- never the non-heads' own content, only the count.</item>
    /// <item><see cref="MorphologicalRules.AnalysisRealizationalAffixProcessRule"/>: adds
    /// <see cref="Word.RealizationalFeatureStruct"/>.</item>
    /// </list>
    /// No rule reads the order those rules were unapplied in, which is the redundancy this key collapses,
    /// so the trail is reduced to an unordered multiset here. <c>_isLastAppliedRuleFinal</c> and
    /// <c>IsPartial</c> are excluded as well: <c>Word.ValueEquals</c> includes them for result dedup, but
    /// no analysis-side rule reads them.
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

        /// <summary>
        /// Keys <paramref name="word"/>, freezing the fields the key reads. A named factory because that
        /// freeze mutates <paramref name="word"/>: <c>Word.FreezeImpl</c> leaves
        /// <c>SyntacticFeatureStruct</c> unfrozen and <c>AnalysisAffixTemplateRule.Apply</c> mutates it on
        /// already-frozen Words, so pinning it here turns a later mutation into a throw rather than a
        /// corrupted table -- at the cost of freezing it earlier than the unmemoized engine does.
        /// </summary>
        public static AnalysisStateKey PinAndKey(Word word)
        {
            return new AnalysisStateKey(word);
        }

        private AnalysisStateKey(Word word)
        {
            // The cached hash covers live references -- notably Word.UnappliedRuleCounts, the word's own
            // mutable dictionary. Keying an unfrozen word would let a later mutation invalidate a stored
            // key's hash, silently causing permanent misses or entries that no longer match their bucket.
            if (!word.IsFrozen)
                throw new ArgumentException(
                    "The word must be frozen before it can be used as a memo key.",
                    nameof(word)
                );

            _shape = word.Shape;
            _stratum = word.Stratum;
            _syntacticFS = word.SyntacticFeatureStruct;
            _realizationalFS = word.RealizationalFeatureStruct;
            _nonHeadCount = word.NonHeadCount;
            _ruleCounts = word.UnappliedRuleCounts;

            // See PinAndKey for why the key pins these rather than just reading them.
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
                // XOR rather than the usual *31 rolling combine: the multiset is unordered, so entries
                // accumulated in different unapplication orders must still hash identically.
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
