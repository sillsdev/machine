using System;
using System.Collections.Generic;
using SIL.Machine.DataStructures;
using SIL.Machine.Statistics;

namespace SIL.Machine.NgramModeling
{
    public class ModifiedKneserNeySmoother<TSeq, TItem> : INgramModelSmoother<TSeq, TItem>
    {
        private double _discount1,
            _discount2,
            _discount3;
        private readonly Dictionary<Ngram<TItem>, Tuple<int, int, int>> _bigNs;
        private ConditionalFrequencyDistribution<Ngram<TItem>, TItem> _cfd;
        private Direction _dir;

        public ModifiedKneserNeySmoother()
        {
            _bigNs = new Dictionary<Ngram<TItem>, Tuple<int, int, int>>();
        }

        public void Smooth(
            int ngramSize,
            TSeq[] sequences,
            Func<TSeq, IEnumerable<TItem>> itemsSelector,
            Direction dir,
            ConditionalFrequencyDistribution<Ngram<TItem>, TItem> cfd
        )
        {
            _cfd = cfd;
            _dir = dir;

            int totalN1 = 0,
                totalN2 = 0,
                totalN3 = 0,
                totalN4 = 0;
            _bigNs.Clear();
            foreach (Ngram<TItem> cond in cfd.Conditions)
            {
                int n1 = 0,
                    n2 = 0,
                    n3 = 0,
                    n4 = 0;
                int nGreater = 0;
                FrequencyDistribution<TItem> freqDist = cfd[cond];
                foreach (TItem item in freqDist.ObservedSamples)
                {
                    if (freqDist[item] == 1)
                        n1++;
                    else if (freqDist[item] == 2)
                        n2++;
                    else if (freqDist[item] > 2)
                    {
                        if (freqDist[item] == 3)
                            n3++;
                        else if (freqDist[item] == 4)
                            n4++;
                        nGreater++;
                    }
                }

                totalN1 += n1;
                totalN2 += n2;
                totalN3 += n3;
                totalN4 += n4;

                _bigNs[cond] = Tuple.Create(n1, n2, nGreater);
            }

            _discount1 = 0;
            _discount2 = 0;
            _discount3 = 0;
            double y = 0;
            if (totalN1 > 0)
            {
                y = (double)totalN1 / (totalN1 + (2 * totalN2));
                _discount1 = 1 - (2 * y * ((double)totalN2 / totalN1));
            }
            if (totalN2 > 0)
                _discount2 = 2 - (3 * y * ((double)totalN3 / totalN2));
            if (totalN3 > 0)
                _discount3 = 3 - (4 * y * ((double)totalN4 / totalN3));

            if (ngramSize > 1)
                LowerOrderModel = new NgramModel<TSeq, TItem>(
                    ngramSize - 1,
                    sequences,
                    itemsSelector,
                    dir,
                    new ModifiedKneserNeySmoother<TSeq, TItem>()
                );
        }

        public double GetProbability(TItem item, Ngram<TItem> context)
        {
            FrequencyDistribution<TItem> freqDist = _cfd[context];
            if (freqDist.ObservedSamples.Count == 0)
                return 0;

            if (context.Length == 0)
                return (double)freqDist[item] / freqDist.SampleOutcomeCount;

            int count = freqDist[item];
            Tuple<int, int, int> bigN = _bigNs[context];
            double gamma =
                ((_discount1 * bigN.Item1) + (_discount2 * bigN.Item2) + (_discount3 * bigN.Item3))
                / freqDist.SampleOutcomeCount;
            double d = 0;
            if (count == 1)
                d = _discount1;
            else if (count == 2)
                d = _discount2;
            else if (count > 2)
                d = _discount3;

            double prob = (count - d) / freqDist.SampleOutcomeCount;
            return prob + (gamma * LowerOrderModel.GetProbability(item, context.SkipFirst(_dir)));
        }

        public NgramModel<TSeq, TItem> LowerOrderModel { get; private set; }
    }
}
