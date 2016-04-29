using System;
using SIL.ObjectModel;

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
			if (double.IsInfinity(gamma) || double.IsNaN(gamma) || gamma < 0)
				throw new ArgumentOutOfRangeException("gamma");

			_freqDist = freqDist;
			_gamma = gamma;
			_binCount = binCount;
			_divisor = _freqDist.SampleOutcomeCount + (_binCount * gamma);
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
				int count = _freqDist[sample];
				return (count + _gamma) / _divisor;
			}
		}

		public double Discount
		{
			get
			{
				if (_freqDist.ObservedSamples.Count == 0)
					return 0;

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
