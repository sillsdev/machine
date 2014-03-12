using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Machine.Statistics
{
	public class ConditionalFrequencyDistribution<TCondition, TSample> : IDeepCloneable<ConditionalFrequencyDistribution<TCondition, TSample>>
	{
		private readonly Dictionary<TCondition, FrequencyDistribution<TSample>> _freqDists;

		public ConditionalFrequencyDistribution()
		{
			_freqDists = new Dictionary<TCondition, FrequencyDistribution<TSample>>();
		}

		public ConditionalFrequencyDistribution(ConditionalFrequencyDistribution<TCondition, TSample> cfd)
		{
			_freqDists = cfd._freqDists.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.DeepClone());
		}

		public IReadOnlyCollection<TCondition> Conditions
		{
			get { return _freqDists.Keys.ToReadOnlyCollection(); }
		}

		public FrequencyDistribution<TSample> this[TCondition condition]
		{
			get { return _freqDists.GetValue(condition, () => new FrequencyDistribution<TSample>()); }
		}

		public int SampleOutcomeCount
		{
			get { return _freqDists.Values.Sum(fd => fd.SampleOutcomeCount); }
		}

		public ConditionalFrequencyDistribution<TCondition, TSample> DeepClone()
		{
			return new ConditionalFrequencyDistribution<TCondition, TSample>(this);
		}
	}
}
