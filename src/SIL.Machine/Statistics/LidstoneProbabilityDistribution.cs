using System;
using System.Collections.Generic;

namespace SIL.Machine.Statistics
{
    public class LidstoneProbabilityDistribution<TSample> : IProbabilityDistribution<TSample>
    {
        private readonly double _gamma;
        private readonly int _binCount;
        private readonly double _divisor;

        public LidstoneProbabilityDistribution(FrequencyDistribution<TSample> freqDist, double gamma, int binCount)
        {
            if (binCount <= freqDist.ObservedSamples.Count)
                throw new ArgumentOutOfRangeException("binCount");
            if (double.IsInfinity(gamma) || double.IsNaN(gamma) || gamma < 0)
                throw new ArgumentOutOfRangeException("gamma");

            FrequencyDistribution = freqDist;
            _gamma = gamma;
            _binCount = binCount;
            _divisor = FrequencyDistribution.SampleOutcomeCount + (_binCount * gamma);
        }

        public FrequencyDistribution<TSample> FrequencyDistribution { get; }

        public IReadOnlyCollection<TSample> Samples => FrequencyDistribution.ObservedSamples;

        public double this[TSample sample]
        {
            get
            {
                if (FrequencyDistribution.ObservedSamples.Count == 0)
                    return 0;
                int count = FrequencyDistribution[sample];
                return (count + _gamma) / _divisor;
            }
        }

        public double Discount
        {
            get
            {
                if (FrequencyDistribution.ObservedSamples.Count == 0)
                    return 0;

                double gb = _gamma * _binCount;
                return gb / (FrequencyDistribution.SampleOutcomeCount + gb);
            }
        }
    }
}
