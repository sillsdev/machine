namespace SIL.Machine.Translation
{
	public interface IWordConfidenceEstimator
	{
		double EstimateConfidence(string targetWord);
	}
}
