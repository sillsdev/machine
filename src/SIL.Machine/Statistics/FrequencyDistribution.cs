using System;
using System.Collections.Generic;
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

        public IReadOnlyCollection<TSample> ObservedSamples => _sampleCounts.Keys;

        public int Increment(TSample sample)
        {
            return Increment(sample, 1);
        }

        public int Increment(TSample sample, int count)
        {
            if (!_sampleCounts.TryGetValue(sample, out int curCount))
                curCount = 0;
            int newCount = curCount + count;
            _sampleCounts[sample] = newCount;
            SampleOutcomeCount += count;
            return newCount;
        }

        public int Decrement(TSample sample)
        {
            return Decrement(sample, 1);
        }

        public int Decrement(TSample sample, int count)
        {
            if (_sampleCounts.TryGetValue(sample, out int curCount))
            {
                if (count == 0)
                    return curCount;
                if (curCount < count)
                    throw new ArgumentException("The specified sample cannot be decremented.", "sample");
                int newCount = curCount - count;
                if (newCount == 0)
                    _sampleCounts.Remove(sample);
                else
                    _sampleCounts[sample] = newCount;
                SampleOutcomeCount -= count;
                return newCount;
            }
            else if (count == 0)
                return 0;
            else
                throw new ArgumentException("The specified sample cannot be decremented.", "sample");
        }

        public int this[TSample sample]
        {
            get
            {
                if (_sampleCounts.TryGetValue(sample, out int count))
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
