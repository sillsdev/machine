using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IWordConfidenceEstimator
	{
		void Estimate(IReadOnlyList<string> sourceSegment, WordGraph wordGraph);
		IReadOnlyList<double> Estimate(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment);
	}
}
