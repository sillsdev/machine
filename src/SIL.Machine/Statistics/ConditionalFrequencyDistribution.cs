using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.ObjectModel;

namespace SIL.Machine.Statistics
{
    public class ConditionalFrequencyDistribution<TCondition, TSample>
        : ICloneable<ConditionalFrequencyDistribution<TCondition, TSample>>
    {
        private readonly Dictionary<TCondition, FrequencyDistribution<TSample>> _freqDists;

        public ConditionalFrequencyDistribution()
        {
            _freqDists = new Dictionary<TCondition, FrequencyDistribution<TSample>>();
        }

        public ConditionalFrequencyDistribution(ConditionalFrequencyDistribution<TCondition, TSample> cfd)
        {
            _freqDists = cfd._freqDists.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone());
        }

        public IReadOnlyCollection<TCondition> Conditions => _freqDists.Keys.ToReadOnlyCollection();

        public FrequencyDistribution<TSample> this[TCondition condition] =>
            _freqDists.GetOrCreate(condition, () => new FrequencyDistribution<TSample>());

        public int SampleOutcomeCount => _freqDists.Values.Sum(fd => fd.SampleOutcomeCount);

        public ConditionalFrequencyDistribution<TCondition, TSample> Clone()
        {
            return new ConditionalFrequencyDistribution<TCondition, TSample>(this);
        }

        public void Reset()
        {
            _freqDists.Clear();
        }
    }
}
