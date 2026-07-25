using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    /// <summary>
    /// Rewrite and metathesis rule specs assume that every top-level child of a rule's target/replacement
    /// pattern (or a metathesis switch group) is a simple <see cref="Constraint{TData, TOffset}"/> (a
    /// segment, natural class, or boundary marker). <see cref="Constraint{TData, TOffset}"/> and
    /// <see cref="Quantifier{TData, TOffset}"/> are both direct subclasses of
    /// <see cref="PatternNode{TData, TOffset}"/> -- siblings, not related by inheritance -- but the DTD and
    /// <c>XmlLanguageLoader</c> allow an <c>OptionalSegmentSequence</c> (loaded as a <see cref="Quantifier{TData, TOffset}"/>)
    /// in exactly the same positions as a plain segment. These helpers replace unchecked
    /// <c>Cast&lt;Constraint&lt;Word, ShapeNode&gt;&gt;()</c> calls and direct casts with a checked
    /// conversion that raises a clear, actionable <see cref="CompileException"/> naming the unsupported
    /// construct and where it appeared, instead of an opaque <see cref="System.InvalidCastException"/>.
    /// </summary>
    internal static class PatternNodeCastExtensions
    {
        public static IEnumerable<Constraint<Word, ShapeNode>> CastToConstraints(
            this IEnumerable<PatternNode<Word, ShapeNode>> nodes,
            string context
        )
        {
            foreach (PatternNode<Word, ShapeNode> node in nodes)
                yield return node.AsConstraint(context);
        }

        public static Constraint<Word, ShapeNode> AsConstraint(this PatternNode<Word, ShapeNode> node, string context)
        {
            if (node is Constraint<Word, ShapeNode> constraint)
                return constraint;

            throw new CompileException(
                $"{context} contains a '{node.GetType().Name}', which is not supported there. Only simple "
                    + "segments, natural classes, and boundary markers are supported; optional or repeated "
                    + "segment sequences (quantifiers) are only supported within a rule's left/right environment."
            );
        }
    }
}
