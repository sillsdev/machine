using System;
using SIL.Collections;

namespace SIL.Machine.Statistics
{
	public class LidstoneProbabilityDistribution<TSample> : IProbabilityDistribution<TSample>
	{
		private readonly FrequencyDistribution<TSample> _freqDist;
		private readonly double _gamma;
		private readonly int _binCount;
		private readonly double _divisor;

		public LidstoneProbabilityDistribution(FrequencyDistribution<TSample> freqDist, double gamma, int binCount)
		{
			if (binCount <= freqDist.ObservedSamples.Count)
				throw new ArgumentOutOfRangeException("binCount");

			_freqDist = freqDist;
			_gamma = gamma;
			_binCount = binCount;
			_divisor = _freqDist.SampleOutcomeCount + _binCount * gamma;
		}

		public IReadOnlyCollection<TSample> Samples
		{
			get { return _freqDist.ObservedSamples; }
		}

		public double this[TSample sample]
		{
			get
			{
				int count = _freqDist[sample];
				return (count + _gamma) / _divisor;
			}
		}

		public double Discount
		{
			get
			{
				double gb = _gamma * _binCount;
				return gb / (_freqDist.SampleOutcomeCount + gb);
			}
		}

		public FrequencyDistribution<TSample> FrequencyDistribution
		{
			get { return _freqDist; }
		}
	}
}
