namespace SIL.Machine.Translation.Thot
{
	public class Ibm1WordAlignmentModel : ThotWordAlignmentModelBase<Ibm1WordAlignmentModel>
	{
		public Ibm1WordAlignmentModel()
			: base(Thot.SmoothedIbm1WordAlignmentClassName)
		{
		}

		public Ibm1WordAlignmentModel(string prefFileName, bool createNew = false)
			: base(Thot.SmoothedIbm1WordAlignmentClassName, prefFileName, createNew)
		{
		}

		public override double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
			int prevTargetIndex, int targetIndex)
		{
			CheckDisposed();

			return -1;
		}
	}
}
