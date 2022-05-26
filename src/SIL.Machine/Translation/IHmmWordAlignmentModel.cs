namespace SIL.Machine.Translation
{
    public interface IHmmWordAlignmentModel : IIbm1WordAlignmentModel
    {
        double GetAlignmentProbability(int sourceLen, int prevSourceIndex, int sourceIndex);
    }
}
