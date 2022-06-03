using System.Collections.Generic;

namespace SIL.Machine.Statistics
{
    public class MaxLikelihoodProbabilityDistribution<TSample> : IProbabilityDistribution<TSample>
    {
        public MaxLikelihoodProbabilityDistribution(FrequencyDistribution<TSample> freqDist)
        {
            FrequencyDistribution = freqDist;
        }

        public IReadOnlyCollection<TSample> Samples
        {
            get { return FrequencyDistribution.ObservedSamples; }
        }

        public double this[TSample sample]
        {
            get
            {
                if (FrequencyDistribution.ObservedSamples.Count == 0)
                    return 0;
                return (double)FrequencyDistribution[sample] / FrequencyDistribution.SampleOutcomeCount;
            }
        }

        public FrequencyDistribution<TSample> FrequencyDistribution { get; }
    }
}
