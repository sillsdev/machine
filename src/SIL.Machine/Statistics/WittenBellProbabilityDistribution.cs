using System;
using System.Collections.Generic;

namespace SIL.Machine.Statistics
{
    public class WittenBellProbabilityDistribution<TSample> : IProbabilityDistribution<TSample>
    {
        private readonly double _probZero;

        public WittenBellProbabilityDistribution(FrequencyDistribution<TSample> freqDist, int binCount)
        {
            if (binCount <= freqDist.ObservedSamples.Count)
                throw new ArgumentOutOfRangeException("binCount");

            FrequencyDistribution = freqDist;
            if (freqDist.ObservedSamples.Count > 0)
            {
                int z = binCount - FrequencyDistribution.ObservedSamples.Count;
                _probZero =
                    (double)FrequencyDistribution.ObservedSamples.Count
                    / (z * (FrequencyDistribution.SampleOutcomeCount + FrequencyDistribution.ObservedSamples.Count));
            }
        }

        public FrequencyDistribution<TSample> FrequencyDistribution { get; }

        public IReadOnlyCollection<TSample> Samples => FrequencyDistribution.ObservedSamples;

        public double this[TSample sample]
        {
            get
            {
                int count = FrequencyDistribution[sample];
                if (count == 0)
                    return _probZero;
                return (double)count
                    / (FrequencyDistribution.SampleOutcomeCount + FrequencyDistribution.ObservedSamples.Count);
            }
        }
    }
}
