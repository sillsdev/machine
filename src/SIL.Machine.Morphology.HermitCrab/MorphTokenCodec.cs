using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Converts a parsed <see cref="Word"/> into the packed 32-bit morpheme-token array
    /// (HERMITCRAB_FST_PLAN.md §8) and assigns each morpheme a stable 24-bit index. This is the
    /// reference encoder the FST compiler will emit as arc outputs; it also proves the schema
    /// faithfully reproduces a real HC analysis — encoding a <see cref="Word"/> and decoding it
    /// yields the same morphemes (and root) that <c>WordAnalysis</c> carries, with the operation
    /// of each morpheme recovered from the rule that introduced it.
    /// </summary>
    public class MorphTokenCodec
    {
        private readonly Dictionary<Morpheme, int> _indexByMorpheme = new Dictionary<Morpheme, int>();
        private readonly List<Morpheme> _morphemesByIndex = new List<Morpheme>();

        /// <summary>Number of distinct morphemes that have been assigned an index.</summary>
        public int MorphemeCount => _morphemesByIndex.Count;

        /// <summary>The morpheme assigned a given 24-bit index.</summary>
        public Morpheme GetMorpheme(int index) => _morphemesByIndex[index];

        /// <summary>
        /// Encode a parsed word as its derivation token array: one <see cref="MorphToken"/> per
        /// morpheme in application order, the head root tagged <see cref="MorphOp.Root"/>. Mirrors
        /// the morpheme order and root choice that <c>Morpher.CreateWordAnalysis</c> produces.
        /// </summary>
        public uint[] Encode(Word word)
        {
            var tokens = new List<uint>();
            foreach (Allomorph allo in word.AllomorphsInMorphOrder)
            {
                MorphOp op = ClassifyOp(allo, allo == word.RootAllomorph);
                tokens.Add(MorphToken.Encode(op, GetOrAddIndex(allo.Morpheme)));
            }
            return tokens.ToArray();
        }

        /// <summary>Assign (or look up) the stable 24-bit index for a morpheme.</summary>
        public int GetOrAddIndex(Morpheme morpheme)
        {
            if (!_indexByMorpheme.TryGetValue(morpheme, out int index))
            {
                index = _morphemesByIndex.Count;
                _indexByMorpheme[morpheme] = index;
                _morphemesByIndex.Add(morpheme);
            }
            return index;
        }

        /// <summary>
        /// Determine the role/operation of an applied allomorph: the head root is
        /// <see cref="MorphOp.Root"/>; any other root (a compound stem) is
        /// <see cref="MorphOp.Compound"/>; an affix is classified from its output actions.
        /// </summary>
        public static MorphOp ClassifyOp(Allomorph allomorph, bool isHeadRoot)
        {
            if (isHeadRoot)
            {
                return MorphOp.Root;
            }
            if (allomorph is RootAllomorph)
            {
                return MorphOp.Compound;
            }
            if (allomorph is AffixProcessAllomorph affix)
            {
                return ClassifyAffix(affix.Rhs);
            }
            return MorphOp.None;
        }

        private static MorphOp ClassifyAffix(IList<MorphologicalOutputAction> rhs)
        {
            // Reduplication: the same input part is copied two or more times.
            bool reduplication = rhs.OfType<CopyFromInput>().GroupBy(c => c.PartName).Any(g => g.Count() >= 2);
            if (reduplication)
            {
                return MorphOp.Reduplication;
            }

            int firstCopy = -1;
            int lastCopy = -1;
            for (int i = 0; i < rhs.Count; i++)
            {
                if (rhs[i] is CopyFromInput)
                {
                    if (firstCopy < 0)
                    {
                        firstCopy = i;
                    }
                    lastCopy = i;
                }
            }

            if (firstCopy < 0)
            {
                // No copy of the stem: a pure insertion, or a process (ModifyFromInput) change.
                return rhs.OfType<ModifyFromInput>().Any() ? MorphOp.Process : MorphOp.None;
            }

            // Inserted material BETWEEN two copies of the stem = infixation.
            for (int i = firstCopy + 1; i < lastCopy; i++)
            {
                if (!(rhs[i] is CopyFromInput))
                {
                    return MorphOp.Infix;
                }
            }

            bool leadingInsert = firstCopy > 0;
            bool trailingInsert = lastCopy < rhs.Count - 1;
            if (leadingInsert && trailingInsert)
            {
                return MorphOp.CircumfixPrefix;
            }
            if (leadingInsert)
            {
                return MorphOp.Prefix;
            }
            if (trailingInsert)
            {
                return MorphOp.Suffix;
            }
            return MorphOp.None;
        }
    }
}
