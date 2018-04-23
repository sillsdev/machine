using SIL.ObjectModel;

namespace SIL.Machine.Statistics
{
	public interface IConditionalProbabilityDistribution<TCondition, TSample>
	{
		ReadOnlyCollection<TCondition> Conditions { get; }
		IProbabilityDistribution<TSample> this[TCondition condition] { get; }
		bool TryGetProbabilityDistribution(TCondition condition, out IProbabilityDistribution<TSample> probDist);
	}
}
