using SIL.Collections;

namespace SIL.Machine.Statistics
{
	public class MaxLikelihoodProbabilityDistribution<TSample> : IProbabilityDistribution<TSample>
	{
		private readonly FrequencyDistribution<TSample> _freqDist; 

		public MaxLikelihoodProbabilityDistribution(FrequencyDistribution<TSample> freqDist)
		{
			_freqDist = freqDist;
		}

		public IReadOnlyCollection<TSample> Samples
		{
			get { return _freqDist.ObservedSamples; }
		}

		public double this[TSample sample]
		{
			get
			{
				if (_freqDist.ObservedSamples.Count == 0)
					return 0;
				return (double) _freqDist[sample] / _freqDist.SampleOutcomeCount;
			}
		}

		public FrequencyDistribution<TSample> FrequencyDistribution
		{
			get { return _freqDist; }
		}
	}
}
