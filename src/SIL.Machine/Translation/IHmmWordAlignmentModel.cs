namespace SIL.Machine.Translation
{
	public interface IHmmWordAlignmentModel : IWordAlignmentModel
	{
		double GetAlignmentProbability(int sourceLen, int prevSourceIndex, int sourceIndex);
	}
}
