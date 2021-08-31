namespace SIL.Machine.Translation.Thot
{
	public class ThotIbm1WordAlignmentModel : ThotWordAlignmentModel
	{
		public ThotIbm1WordAlignmentModel()
			: base(Thot.Ibm1WordAlignmentClassName)
		{
		}

		public ThotIbm1WordAlignmentModel(string prefFileName, bool createNew = false)
			: base(Thot.Ibm1WordAlignmentClassName, prefFileName, createNew)
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
