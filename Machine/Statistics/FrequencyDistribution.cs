using System;
using System.Collections.Generic;
using SIL.Collections;

namespace SIL.Machine.Statistics
{
	public class FrequencyDistribution<TSample> : IDeepCloneable<FrequencyDistribution<TSample>>
	{
		private readonly Dictionary<TSample, int> _sampleCounts;
		private int _sampleOutcomeCount;
 
		public FrequencyDistribution()
		{
			_sampleCounts = new Dictionary<TSample, int>();
		}

		public FrequencyDistribution(FrequencyDistribution<TSample> fd)
		{
			_sampleCounts = new Dictionary<TSample, int>(fd._sampleCounts);
			_sampleOutcomeCount = fd._sampleOutcomeCount;
		}

		public IReadOnlyCollection<TSample> ObservedSamples
		{
			get { return _sampleCounts.Keys.ToReadOnlyCollection(); }
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
			_sampleOutcomeCount += count;
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
			_sampleOutcomeCount -= count;
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

		public int SampleOutcomeCount
		{
			get { return _sampleOutcomeCount; }
		}

		public void Reset()
		{
			_sampleCounts.Clear();
			_sampleOutcomeCount = 0;
		}

		public FrequencyDistribution<TSample> DeepClone()
		{
			return new FrequencyDistribution<TSample>(this);
		}
	}
}
