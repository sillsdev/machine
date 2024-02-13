using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Statistics
{
    public class SimpleGoodTuringProbabilityDistribution<TSample> : IProbabilityDistribution<TSample>
    {
        private readonly double _slope;
        private readonly double _intercept;
        private readonly Dictionary<int, double> _probs;
        private readonly double _probZero;
        private readonly int _binCount;

        public SimpleGoodTuringProbabilityDistribution(FrequencyDistribution<TSample> freqDist, int binCount)
        {
            if (binCount <= freqDist.ObservedSamples.Count)
                throw new ArgumentOutOfRangeException("binCount");

            FrequencyDistribution = freqDist;
            _binCount = binCount;
            _probs = new Dictionary<int, double>();

            if (freqDist.ObservedSamples.Count == 0)
                return;

            var r = new List<int>();
            var nr = new List<int>();
            int b = 0,
                i = 0;
            while (b != FrequencyDistribution.ObservedSamples.Count)
            {
                int nri = FrequencyDistribution.ObservedSamples.Count(s => FrequencyDistribution[s] == i);
                if (nri > 0)
                {
                    b += nri;
                    r.Add(i);
                    nr.Add(nri);
                }
                i++;
            }

            double[] zr = new double[r.Count];
            double[] logR = new double[r.Count];
            double[] logZR = new double[r.Count];
            for (int j = 0; j < r.Count; j++)
            {
                i = j > 0 ? r[j - 1] : 0;
                int k = j == r.Count - 1 ? 2 * r[j] - i : r[j + 1];
                zr[j] = 2.0 * nr[j] / (k - i);
                logR[j] = Math.Log(r[j]);
                logZR[j] = Math.Log(zr[j]);
            }

            double xyCov = 0,
                xVar = 0,
                xMean = 0,
                yMean = 0;
            for (int j = 0; j < r.Count; j++)
            {
                xMean += logR[j];
                yMean += logZR[j];
            }
            xMean /= r.Count;
            yMean /= r.Count;
            for (int j = 0; j < logR.Length; j++)
            {
                xyCov += (logR[j] - xMean) * (logZR[j] - yMean);
                xVar += Math.Pow(logR[j] - xMean, 2);
            }
            _slope = Math.Abs(xVar - 0) > double.Epsilon ? xyCov / xVar : 0;
            _intercept = yMean - _slope * xMean;

            double[] rStar = new double[r.Count];
            for (int j = 0; j < r.Count; j++)
            {
                double smoothRStar = (r[j] + 1) * GetSmoothedSamplesCount(r[j] + 1) / GetSmoothedSamplesCount(r[j]);
                if (r.Count == j + 1 || r[j + 1] != r[j] + 1)
                    rStar[j] = smoothRStar;
                else
                {
                    double unsmoothRStar = (double)(r[j] + 1) * nr[j + 1] / nr[j];
                    double std = Math.Sqrt(GetVariance(r[j], nr[j], nr[j + 1]));
                    if (Math.Abs(unsmoothRStar - smoothRStar) <= 1.96 * std)
                        rStar[j] = smoothRStar;
                    else
                        rStar[j] = unsmoothRStar;
                }
            }

            double samplesCountPrime = 0;
            for (int j = 0; j < r.Count; j++)
                samplesCountPrime += nr[j] * rStar[j];

            _probZero =
                (double)FrequencyDistribution.ObservedSamples.Count(s => FrequencyDistribution[s] == 1)
                / FrequencyDistribution.SampleOutcomeCount;
            for (int j = 0; j < r.Count; j++)
                _probs[r[j]] = (1.0 - _probZero) * rStar[j] / samplesCountPrime;
        }

        private double GetSmoothedSamplesCount(int r)
        {
            return Math.Exp(_intercept + _slope * Math.Log(r));
        }

        private double GetVariance(int r, int nr, int nr1)
        {
            return Math.Pow(r + 1, 2) * (nr1 / Math.Pow(nr, 2)) * (1.0 + (double)nr1 / nr);
        }

        public FrequencyDistribution<TSample> FrequencyDistribution { get; }

        public IReadOnlyCollection<TSample> Samples => FrequencyDistribution.ObservedSamples;

        public double this[TSample sample]
        {
            get
            {
                int count = FrequencyDistribution[sample];
                double prob;
                if (_probs.TryGetValue(count, out prob))
                    return prob;
                return _probZero / (_binCount - FrequencyDistribution.ObservedSamples.Count);
            }
        }

        public double Discount
        {
            get
            {
                if (FrequencyDistribution.ObservedSamples.Count == 0)
                    return 0;
                return GetSmoothedSamplesCount(1) / FrequencyDistribution.SampleOutcomeCount;
            }
        }
    }
}
