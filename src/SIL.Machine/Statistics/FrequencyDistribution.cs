using System;
using System.Collections.Generic;
using SIL.Extensions;
using SIL.ObjectModel;

namespace SIL.Machine.Statistics
{
	public class FrequencyDistribution<TSample> : ICloneable<FrequencyDistribution<TSample>>
	{
		private readonly Dictionary<TSample, int> _sampleCounts;

		public FrequencyDistribution()
		{
			_sampleCounts = new Dictionary<TSample, int>();
		}

		public FrequencyDistribution(FrequencyDistribution<TSample> fd)
		{
			_sampleCounts = new Dictionary<TSample, int>(fd._sampleCounts);
			SampleOutcomeCount = fd.SampleOutcomeCount;
		}

		public IReadOnlyCollection<TSample> ObservedSamples
		{
			get { return _sampleCounts.Keys; }
		}

		public void Increment(TSample sample)
		{
			Increment(sample, 1);
		}

		public void Increment(TSample sample, int count)
		{
			if (count == 0)
				return;

			_sampleCounts.UpdateValue(sample, () => 0, c => c + count);
			SampleOutcomeCount += count;
		}

		public void Decrement(TSample sample)
		{
			Decrement(sample, 1);
		}

		public void Decrement(TSample sample, int count)
		{
			if (count == 0)
				return;

			int curCount;
			if (_sampleCounts.TryGetValue(sample, out curCount))
			{
				if (curCount < count)
					throw new ArgumentException("The specified sample cannot be decremented.", "sample");
				int newCount = curCount - count;
				if (newCount == 0)
					_sampleCounts.Remove(sample);
				else
					_sampleCounts[sample] = newCount;
			}
			else
			{
				throw new ArgumentException("The specified sample cannot be decremented.", "sample");
			}
			SampleOutcomeCount -= count;
		}

		public int this[TSample sample]
		{
			get
			{
				int count;
				if (_sampleCounts.TryGetValue(sample, out count))
					return count;
				return 0;
			}
		}

		public int SampleOutcomeCount { get; private set; }

		public void Reset()
		{
			_sampleCounts.Clear();
			SampleOutcomeCount = 0;
		}

		public FrequencyDistribution<TSample> Clone()
		{
			return new FrequencyDistribution<TSample>(this);
		}
	}
}
