using System;
using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// The role/operation of a morpheme in a derivation — the high 8-bit field of a packed
    /// <see cref="MorphToken"/>. It is the "ordered operation connected to the letters": it lets a
    /// consumer rebuild the gloss/bracketing of an analysis without re-running any rule.
    /// </summary>
    public enum MorphOp : byte
    {
        /// <summary>Unset / not a morpheme boundary.</summary>
        None = 0,

        /// <summary>The root (stem) morpheme.</summary>
        Root = 1,

        /// <summary>A prefix.</summary>
        Prefix = 2,

        /// <summary>A suffix.</summary>
        Suffix = 3,

        /// <summary>An infix (inserted inside the stem).</summary>
        Infix = 4,

        /// <summary>Reduplication.</summary>
        Reduplication = 5,

        /// <summary>The prefixal half of a circumfix.</summary>
        CircumfixPrefix = 6,

        /// <summary>The suffixal half of a circumfix.</summary>
        CircumfixSuffix = 7,

        /// <summary>A compounding element (a non-head stem).</summary>
        Compound = 8,

        /// <summary>A clitic.</summary>
        Clitic = 9,

        /// <summary>A process / simulfix (a ModifyFromInput-style change, no added segments).</summary>
        Process = 10,

        /// <summary>A zero (null) morph.</summary>
        Null = 11,
    }

    /// <summary>
    /// A 32-bit packed analysis token: high 8 bits = <see cref="MorphOp"/>, low 24 bits = a
    /// morpheme index into the grammar's compiled morpheme table. The analyzer transducer emits
    /// one token per morpheme, in application order; the resulting <c>uint[]</c> IS the structured
    /// analysis. It is self-describing — the morpheme order is the array order, and the root
    /// position is the index of the <see cref="MorphOp.Root"/> token, so no separate
    /// RootMorphemeIndex field is needed. See HERMITCRAB_FST_PLAN.md §8.
    /// </summary>
    public static class MorphToken
    {
        /// <summary>Number of low bits reserved for the morpheme index.</summary>
        public const int MorphemeIdBits = 24;

        /// <summary>Largest encodable morpheme index (16,777,215).</summary>
        public const int MaxMorphemeId = (1 << MorphemeIdBits) - 1;

        private const uint MorphemeIdMask = (1u << MorphemeIdBits) - 1;

        /// <summary>Pack a (role, morpheme index) pair into one 32-bit token.</summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="morphemeId"/> does not fit in <see cref="MorphemeIdBits"/> bits.
        /// </exception>
        public static uint Encode(MorphOp op, int morphemeId)
        {
            if (morphemeId < 0 || morphemeId > MaxMorphemeId)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(morphemeId),
                    morphemeId,
                    $"morpheme index must be in [0, {MaxMorphemeId}] to fit in {MorphemeIdBits} bits"
                );
            }
            return ((uint)op << MorphemeIdBits) | (uint)morphemeId;
        }

        /// <summary>The morpheme's role/operation.</summary>
        public static MorphOp GetOp(uint token) => (MorphOp)(token >> MorphemeIdBits);

        /// <summary>The morpheme index into the grammar's compiled morpheme table.</summary>
        public static int GetMorphemeId(uint token) => (int)(token & MorphemeIdMask);

        /// <summary>
        /// Index of the <see cref="MorphOp.Root"/> token in a derivation array, or -1 if none.
        /// This recovers <c>WordAnalysis.RootMorphemeIndex</c> from the token array itself.
        /// </summary>
        public static int RootIndex(IReadOnlyList<uint> tokens)
        {
            if (tokens == null)
            {
                return -1;
            }
            for (int i = 0; i < tokens.Count; i++)
            {
                if (GetOp(tokens[i]) == MorphOp.Root)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
