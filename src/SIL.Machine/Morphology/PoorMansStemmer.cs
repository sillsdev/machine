using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.DataStructures;
using SIL.Machine.NgramModeling;

namespace SIL.Machine.Morphology
{
    public class PoorMansStemmer<TSeq, TItem> : PoorMansStemmingAlgorithmBase<TSeq, TItem>, IStemmer<TSeq>
    {
        private Dictionary<Ngram<TItem>, AffixInfo> _prefixes;
        private Dictionary<Ngram<TItem>, AffixInfo> _suffixes;
        private int _wordCount;

        public PoorMansStemmer(Func<TSeq, IEnumerable<TItem>> itemsSelector)
            : this(seq => itemsSelector(seq).ToEnumerable())
        {
            AffixesOccurOnSyllableBoundaries = false;
        }

        public PoorMansStemmer(Func<TSeq, IEnumerable<IEnumerable<TItem>>> syllablesSelector)
            : base(syllablesSelector)
        {
            Threshold = 5;
            WeightScores = true;
        }

        public double Threshold { get; set; }
        public bool NormalizeScores { get; set; }
        public bool WeightScores { get; set; }

        public void Train(IEnumerable<TSeq> sequences)
        {
            TSeq[] seqArray = sequences.ToArray();
            _prefixes = ComputeAffixes(seqArray, AffixType.Prefix).ToDictionary(a => a.Ngram);
            _suffixes = ComputeAffixes(seqArray, AffixType.Suffix).ToDictionary(a => a.Ngram);
            _wordCount = seqArray.Length;
        }

        public bool HaveSameStem(TSeq x, TSeq y)
        {
            return HaveSameStem(x, y, out double _);
        }

        public bool HaveSameStem(TSeq x, TSeq y, out double score)
        {
            var ngram1 = new Ngram<TItem>(SyllablesSelector(x).SelectMany(s => s));
            var ngram2 = new Ngram<TItem>(SyllablesSelector(y).SelectMany(s => s));

            if (ngram1.Equals(ngram2))
            {
                score = NormalizeScores ? 1.0 : MaxZScore(_wordCount);
                return true;
            }

            Tuple<AffixInfo, Ngram<TItem>, AffixInfo>[] analyses1 = GetAnalyses(ngram1).ToArray();
            Tuple<AffixInfo, Ngram<TItem>, AffixInfo>[] analyses2 = GetAnalyses(ngram2).ToArray();

            score = 0;
            foreach (Tuple<AffixInfo, Ngram<TItem>, AffixInfo> analysis1 in analyses1)
            {
                foreach (Tuple<AffixInfo, Ngram<TItem>, AffixInfo> analysis2 in analyses2)
                {
                    if (
                        (analysis1.Item1.Ngram.Length == 0 || analysis1.Item1 != analysis2.Item1)
                        && analysis1.Item2.Equals(analysis2.Item2)
                        && (analysis1.Item3.Ngram.Length == 0 || analysis1.Item3 != analysis2.Item3)
                    )
                    {
                        double prefixScore = CalcAffixScore(analysis1.Item1, analysis2.Item1);
                        double suffixScore = CalcAffixScore(analysis1.Item3, analysis2.Item3);

                        double minScore = Math.Min(prefixScore, suffixScore);
                        if (minScore >= Threshold && minScore > score)
                            score = minScore;
                    }
                }
            }

            return score > 0;
        }

        private double CalcAffixScore(AffixInfo x, AffixInfo y)
        {
            if (x.Ngram.Length == 0 && y.Ngram.Length == 0)
                return NormalizeScores ? 1.0 : MaxZScore(_wordCount);

            double score;
            if (x.Ngram.Length == 0 || y.Ngram.Length == 0)
            {
                score = NormalizeScores
                    ? Math.Max(x.NormalizedZScore, y.NormalizedZScore)
                    : Math.Max(x.ZScore, y.ZScore);
                if (WeightScores)
                    score *= (double)IntersectionCount(x.Stems, y.Stems) / Math.Min(x.Stems.Count, y.Stems.Count);
            }
            else
            {
                score = NormalizeScores
                    ? Math.Min(x.NormalizedZScore, y.NormalizedZScore)
                    : Math.Min(x.ZScore, y.ZScore);
                if (WeightScores)
                    score *= (2.0 * IntersectionCount(x.Stems, y.Stems)) / (x.Stems.Count + y.Stems.Count);
            }

            return score;
        }

        private IEnumerable<Tuple<AffixInfo, Ngram<TItem>, AffixInfo>> GetAnalyses(Ngram<TItem> word)
        {
            return GetAffixes(word, AffixType.Prefix)
                .SelectMany(
                    prefix => GetAffixes(prefix.Item2, AffixType.Suffix),
                    (prefix, suffix) => Tuple.Create(prefix.Item1, suffix.Item2, suffix.Item1)
                );
        }

        private IEnumerable<Tuple<AffixInfo, Ngram<TItem>>> GetAffixes(Ngram<TItem> word, AffixType type)
        {
            Direction dir;
            Dictionary<Ngram<TItem>, AffixInfo> affixes;
            if (type == AffixType.Prefix)
            {
                dir = Direction.LeftToRight;
                affixes = _prefixes;
            }
            else
            {
                dir = Direction.RightToLeft;
                affixes = _suffixes;
            }

            var affix = new Ngram<TItem>();
            yield return Tuple.Create(affixes[affix], word);
            foreach (TItem item in word.GetItems(dir).Take(Math.Min(MaxAffixLength, word.Length - 1)))
            {
                affix = affix.Concat(item, dir);
                word = word.SkipFirst(dir);
                AffixInfo ai;
                if (affixes.TryGetValue(affix, out ai))
                    yield return Tuple.Create(ai, word);
            }
        }
    }
}
