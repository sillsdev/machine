namespace SIL.Machine.Translation
{
	interface IIbm2WordAlignmentModel : IWordAlignmentModel
	{
		double GetAlignmentProbability(int sourceLen, int sourceIndex, int targetLen, int targetIndex);
	}
}
