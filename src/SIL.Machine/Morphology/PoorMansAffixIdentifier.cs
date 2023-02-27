using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.DataStructures;

namespace SIL.Machine.Morphology
{
    public class PoorMansAffixIdentifier<TSeq, TItem>
        : PoorMansStemmingAlgorithmBase<TSeq, TItem>,
            IAffixIdentifier<TSeq, TItem>
    {
        public PoorMansAffixIdentifier(Func<TSeq, IEnumerable<TItem>> itemsSelector)
            : this(seq => itemsSelector(seq).ToEnumerable())
        {
            AffixesOccurOnSyllableBoundaries = false;
        }

        public PoorMansAffixIdentifier(Func<TSeq, IEnumerable<IEnumerable<TItem>>> syllablesSelector)
            : base(syllablesSelector)
        {
            Threshold = 5;
        }

        public double Threshold { get; set; }
        public bool NormalizeScores { get; set; }

        public IEnumerable<Affix<TItem>> IdentifyAffixes(IEnumerable<TSeq> sequences, AffixType type)
        {
            IEnumerable<AffixInfo> affixes = ComputeAffixes(sequences.ToArray(), type);

            var results = new List<Affix<TItem>>();
            foreach (
                AffixInfo affix in affixes
                    .Where(a => (NormalizeScores ? a.NormalizedZScore : a.ZScore) >= Threshold)
                    .OrderByDescending(a => a.ZScore)
            )
            {
                if (
                    results.All(
                        a =>
                            !affix.Ngram.StartsWith(
                                a.Ngram,
                                type == AffixType.Prefix ? Direction.LeftToRight : Direction.RightToLeft
                            )
                    )
                )
                    results.Add(
                        new Affix<TItem>(type, affix.Ngram, NormalizeScores ? affix.NormalizedZScore : affix.ZScore)
                    );
            }

            return results;
        }
    }
}
