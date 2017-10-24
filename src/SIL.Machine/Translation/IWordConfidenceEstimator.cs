using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IWordConfidenceEstimator
	{
		IWordConfidences Estimate(IReadOnlyList<string> sourceSegment, WordGraph wordGraph = null);
	}
}
