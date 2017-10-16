using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IWordConfidenceEstimatorFactory
	{
		IWordConfidenceEstimator Create(IReadOnlyList<string> sourceSegment);
	}
}
