using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.DataStructures;
using SIL.Machine.NgramModeling;
using SIL.Machine.Statistics;

namespace SIL.Machine.Morphology
{
    public abstract class PoorMansStemmingAlgorithmBase<TSeq, TItem>
    {
        private readonly Func<TSeq, IEnumerable<IEnumerable<TItem>>> _syllablesSelector;

        protected PoorMansStemmingAlgorithmBase(Func<TSeq, IEnumerable<IEnumerable<TItem>>> syllablesSelector)
        {
            _syllablesSelector = syllablesSelector;
            MaxAffixLength = 3;
            AffixesOccurOnSyllableBoundaries = true;
        }

        public int MaxAffixLength { get; set; }
        public bool AffixesOccurOnSyllableBoundaries { get; set; }

        protected Func<TSeq, IEnumerable<IEnumerable<TItem>>> SyllablesSelector
        {
            get { return _syllablesSelector; }
        }

        protected IEnumerable<AffixInfo> ComputeAffixes(ICollection<TSeq> sequences, AffixType type)
        {
            var dir = Direction.LeftToRight;
            switch (type)
            {
                case AffixType.Prefix:
                    dir = Direction.LeftToRight;
                    break;

                case AffixType.Suffix:
                    dir = Direction.RightToLeft;
                    break;
            }

            var affixFreqDist = new ConditionalFrequencyDistribution<int, Ngram<TItem>>();
            var ngramFreqDist = new ConditionalFrequencyDistribution<int, Ngram<TItem>>();
            var itemFreqDist = new FrequencyDistribution<TItem>();

            var affixes = new Dictionary<Ngram<TItem>, AffixInfo>();
            var nullAffix = new AffixInfo(sequences.Count, new Ngram<TItem>());
            foreach (TSeq seq in sequences)
            {
                var wordNgram = new Ngram<TItem>(_syllablesSelector(seq).SelectMany(s => s));
                nullAffix.Stems.Add(wordNgram);
                foreach (TItem item in wordNgram)
                    itemFreqDist.Increment(item);
                if (wordNgram.Length <= 1)
                    continue;

                var items = new List<TItem>();
                var syllableStart = new HashSet<int>();
                foreach (IEnumerable<TItem> syllable in _syllablesSelector(seq).Items(dir))
                {
                    items.AddRange(syllable.Items(dir));
                    syllableStart.Add(items.Count - 1);
                }
                var affix = new Ngram<TItem>();
                var stem = new Ngram<TItem>(items, dir);
                for (int i = 0; i < Math.Min(MaxAffixLength + 1, items.Count); i++)
                {
                    affix = affix.Concat(items[i], dir);
                    affixFreqDist[affix.Length].Increment(affix);
                    if (i < items.Count - 1 && affix.Length <= MaxAffixLength)
                    {
                        AffixInfo ai = affixes.GetOrCreate(affix, () => new AffixInfo(sequences.Count, affix));
                        stem = stem.SkipFirst(dir);
                        ai.Stems.Add(stem);
                        if (syllableStart.Contains(i))
                            ai.SyllableBreakCount++;
                    }
                }

                for (int i = 0; i < items.Count; i++)
                {
                    var ngram = new Ngram<TItem>();
                    for (int j = i; j < Math.Min(MaxAffixLength + i, items.Count); j++)
                    {
                        ngram = ngram.Concat(items[j], dir);
                        ngramFreqDist[ngram.Length].Increment(ngram);
                    }
                }
            }

            var itemProbDist = new MaxLikelihoodProbabilityDistribution<TItem>(itemFreqDist);
            var affixProbDist = new ConditionalProbabilityDistribution<int, Ngram<TItem>>(
                affixFreqDist,
                (c, fd) =>
                {
                    if (c == 1)
                        return new MaxLikelihoodProbabilityDistribution<Ngram<TItem>>(fd);
                    int binCount;
                    try
                    {
                        binCount = checked((int)Math.Pow(itemFreqDist.ObservedSamples.Count, c));
                    }
                    catch (OverflowException)
                    {
                        binCount = int.MaxValue;
                    }
                    return new WittenBellProbabilityDistribution<Ngram<TItem>>(fd, binCount);
                }
            );
            var ngramProbDist = new ConditionalProbabilityDistribution<int, Ngram<TItem>>(
                ngramFreqDist,
                (c, fd) =>
                {
                    if (c == 1)
                        return new MaxLikelihoodProbabilityDistribution<Ngram<TItem>>(fd);
                    int binCount;
                    try
                    {
                        binCount = checked((int)Math.Pow(itemFreqDist.ObservedSamples.Count, c));
                    }
                    catch (OverflowException)
                    {
                        binCount = int.MaxValue;
                    }
                    return new WittenBellProbabilityDistribution<Ngram<TItem>>(fd, binCount);
                }
            );

            foreach (AffixInfo affix in affixes.Values)
            {
                int freq = affixFreqDist[affix.Ngram.Length][affix.Ngram];

                var maxCurveItem = itemFreqDist
                    .ObservedSamples.Select(item => new
                    {
                        Item = item,
                        Curve = (double)affixFreqDist[affix.Ngram.Length + 1][affix.Ngram.Concat(item, dir)] / freq
                    })
                    .MaxBy(item => item.Curve);
                double curveDrop = (1 - maxCurveItem.Curve) / (1 - itemProbDist[maxCurveItem.Item]);

                double pw = affixProbDist[affix.Ngram.Length][affix.Ngram];
                double npw = ngramProbDist[affix.Ngram.Length][affix.Ngram];
                double randomAdj = npw == 0 ? 1.0 : pw / npw;

                double normalizedFreq = affix.Ngram.Length * Math.Log(freq);

                double syllableScore = AffixesOccurOnSyllableBoundaries
                    ? (0.5 * ((double)affix.SyllableBreakCount / freq)) + 0.5
                    : 1.0;

                affix.ZScore = curveDrop * randomAdj * normalizedFreq * syllableScore;
                yield return affix;
            }

            yield return nullAffix;
        }

        protected static ICollection<AffixInfo> ComputeParadigm(
            AffixInfo mainAffix,
            IEnumerable<AffixInfo> affixes,
            int maxSearchDepth
        )
        {
            AffixInfo[] affixArray = affixes.ToArray();
            var paradigm = new HashSet<AffixInfo>(mainAffix.ToEnumerable());
            AffixInfo lastChanged = null;
            double paradigmScore = 0;
            for (int i = 0; i < maxSearchDepth; i++)
            {
                double maxScore = paradigmScore;
                HashSet<AffixInfo> bestParadigm = paradigm;
                AffixInfo bestChanged = null;

                foreach (AffixInfo affix in affixArray.Where(a => a != mainAffix && a != lastChanged))
                {
                    var newParadigm = new HashSet<AffixInfo>(paradigm);
                    if (paradigm.Contains(affix))
                        newParadigm.Remove(affix);
                    else
                        newParadigm.Add(affix);
                    double score = CalcVIScore(affixArray, newParadigm);
                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestParadigm = newParadigm;
                        bestChanged = affix;
                        if (Math.Abs(maxScore - 1.0) < double.Epsilon)
                            return bestParadigm;
                    }
                }

                if (bestParadigm == paradigm)
                    return bestParadigm;

                paradigm = bestParadigm;
                paradigmScore = maxScore;
                lastChanged = bestChanged;
            }

            return paradigm;
        }

        protected static double MaxZScore(int wordCount)
        {
            return 10 * Math.Log((double)wordCount / 2);
        }

        protected static double CalcAScore(IEnumerable<AffixInfo> affixes, ICollection<AffixInfo> paradigm)
        {
            return CalcVIScore(affixes, paradigm) * paradigm.Sum(affix => affix.ZScore);
        }

        protected static double CalcVIScore(IEnumerable<AffixInfo> affixes, ICollection<AffixInfo> paradigm)
        {
            if (paradigm.Count == 1)
                return 0;

            double sum = 0;
            int rank = 0;
            int numFound = 0;
            foreach (AffixInfo y in affixes.OrderByDescending(y => CalcVpScore(paradigm, y)))
            {
                if (paradigm.Contains(y))
                {
                    sum += rank;
                    numFound++;
                    if (numFound == paradigm.Count)
                        break;
                }
                rank++;
            }

            return (paradigm.Count * (paradigm.Count - 1)) / (2.0 * sum);
        }

        protected static double CalcVpScore(ICollection<AffixInfo> paradigm, AffixInfo y)
        {
            return paradigm.Where(x => x != y).Sum(x => CalcHScore(x, y));
        }

        protected static double CalcHScore(AffixInfo x, AffixInfo y)
        {
            return (double)IntersectionCount(x.Stems, y.Stems) / x.Stems.Count;
        }

        protected static int IntersectionCount(ISet<Ngram<TItem>> x, ISet<Ngram<TItem>> y)
        {
            ISet<Ngram<TItem>> shorterSet,
                longerSet;
            if (x.Count < y.Count)
            {
                shorterSet = x;
                longerSet = y;
            }
            else
            {
                shorterSet = y;
                longerSet = x;
            }
            return shorterSet.Count(item => longerSet.Contains(item));
        }

        protected class AffixInfo
        {
            private readonly Ngram<TItem> _ngram;
            private readonly HashSet<Ngram<TItem>> _stems;
            private readonly int _wordCount;

            public AffixInfo(int wordCount, Ngram<TItem> ngram)
            {
                _ngram = ngram;
                _stems = new HashSet<Ngram<TItem>>();
                _wordCount = wordCount;
            }

            public Ngram<TItem> Ngram
            {
                get { return _ngram; }
            }

            public double ZScore { get; set; }

            public double NormalizedZScore
            {
                get { return Math.Min(ZScore / MaxZScore(_wordCount), 1.0); }
            }

            public ISet<Ngram<TItem>> Stems
            {
                get { return _stems; }
            }

            public int SyllableBreakCount { get; set; }

            public override string ToString()
            {
                return _ngram.ToString();
            }
        }
    }
}
