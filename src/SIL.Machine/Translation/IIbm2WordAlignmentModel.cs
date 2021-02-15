namespace SIL.Machine.Translation
{
	public interface IIbm2WordAlignmentModel : IIbm1WordAlignmentModel
	{
		double GetAlignmentProbability(int sourceLen, int sourceIndex, int targetLen, int targetIndex);
	}
}
