namespace SIL.Machine.Translation.Thot
{
	public class ThotIbm1WordAlignmentModel : ThotWordAlignmentModel
	{
		public ThotIbm1WordAlignmentModel()
		{
		}

		public ThotIbm1WordAlignmentModel(string prefFileName, bool createNew = false)
			: base(prefFileName, createNew)
		{
		}

		public override ThotWordAlignmentModelType Type => ThotWordAlignmentModelType.Ibm1;

		public override double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
			int prevTargetIndex, int targetIndex)
		{
			CheckDisposed();

			return -1;
		}
	}
}
