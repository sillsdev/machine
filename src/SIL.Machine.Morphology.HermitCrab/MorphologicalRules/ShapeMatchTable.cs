using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    /// <summary>
    /// What each affix or realizational rule allomorph gives on each shape one <see cref="Morpher.ParseWord(string)"/>
    /// reaches. Matching an allomorph's pattern reads the input's <see cref="Shape"/> and nothing else, so the
    /// output shapes are a function of (rule, allomorph, shape) and are computed once per parse; every caller still
    /// runs its own per-path gates and builds its own output words. An empty row records "no match". Compounding
    /// rules are not stored here: their match also reads the non-head root.
    /// </summary>
    internal sealed class ShapeMatchTable
    {
        private readonly struct Key : IEquatable<Key>
        {
            private readonly IMorphologicalRule _rule;
            private readonly int _allomorph;
            private readonly Shape _shape;

            public Key(IMorphologicalRule rule, int allomorph, Shape shape)
            {
                _rule = rule;
                _allomorph = allomorph;
                _shape = shape;
            }

            public bool Equals(Key other) =>
                ReferenceEquals(_rule, other._rule)
                && _allomorph == other._allomorph
                && (ReferenceEquals(_shape, other._shape) || _shape.ValueEquals(other._shape));

            public override bool Equals(object obj) => obj is Key key && Equals(key);

            public override int GetHashCode() =>
                (System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(_rule) * 31 + _allomorph) * 31
                + _shape.GetFrozenHashCode();
        }

        private readonly ConcurrentDictionary<Key, Shape[]> _rows = new ConcurrentDictionary<Key, Shape[]>();

        /// <summary>
        /// Returns the words the pattern rule gives on the input, in the order and with the duplicates removed
        /// exactly as <c>patternRule.Apply(input).RemoveDuplicates()</c> does. The caller owns the returned words.
        /// </summary>
        public static List<Word> Match(
            ShapeMatchTable table,
            IMorphologicalRule rule,
            int allomorph,
            PatternRule<Word, ShapeNode> patternRule,
            Word input,
            Action<IMorphologicalRule, Word> matched
        )
        {
            // an unfrozen shape has no value hash, so it cannot be a key
            if (table == null || !input.Shape.IsFrozen)
                return Compute(rule, patternRule, input, matched);

            var key = new Key(rule, allomorph, input.Shape);
            if (table._rows.TryGetValue(key, out Shape[] shapes))
                return shapes.Select(shape => input.CloneWithShape(shape)).ToList();

            List<Word> words = Compute(rule, patternRule, input, matched);
            // a frozen shape is never mutated, so every word built from this row may share it
            foreach (Word word in words)
                word.Shape.Freeze();
            table._rows.TryAdd(key, words.Select(word => word.Shape).ToArray());
            return words;
        }

        private static List<Word> Compute(
            IMorphologicalRule rule,
            PatternRule<Word, ShapeNode> patternRule,
            Word input,
            Action<IMorphologicalRule, Word> matched
        )
        {
            matched?.Invoke(rule, input);
            return patternRule.Apply(input).RemoveDuplicates().ToList();
        }
    }
}
