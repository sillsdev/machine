using SIL.ObjectModel;

namespace SIL.Machine.Statistics
{
	public interface IProbabilityDistribution<TSample>
	{
		ReadOnlyCollection<TSample> Samples { get; }
		double this[TSample sample] { get; }
	}
}
