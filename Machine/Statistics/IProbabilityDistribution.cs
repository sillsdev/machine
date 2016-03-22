using SIL.ObjectModel;

namespace SIL.Machine.Statistics
{
	public interface IProbabilityDistribution<TSample>
	{
		IReadOnlyCollection<TSample> Samples { get; }
		double this[TSample sample] { get; }
	}
}
