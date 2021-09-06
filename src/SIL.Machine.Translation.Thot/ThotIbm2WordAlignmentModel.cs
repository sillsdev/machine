namespace SIL.Machine.Translation.Thot
{
	public class ThotIbm2WordAlignmentModel : ThotIbm1WordAlignmentModel, IIbm2WordAlignmentModel
	{
		public ThotIbm2WordAlignmentModel()
		{
		}

		public ThotIbm2WordAlignmentModel(string prefFileName, bool createNew = false)
			: base(prefFileName, createNew)
		{
		}

		public override ThotWordAlignmentModelType Type => ThotWordAlignmentModelType.Ibm2;

		public override double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
			int prevTargetIndex, int targetIndex)
		{
			CheckDisposed();

			return GetAlignmentProbability(sourceLen, sourceIndex, targetLen, targetIndex);
		}

		public double GetAlignmentProbability(int sourceLen, int sourceIndex, int targetLen, int targetIndex)
		{
			CheckDisposed();

			// add 1 to convert the specified indices to Thot position indices, which are 1-based
			return Thot.swAlignModel_getIbm2AlignmentProbability(Handle, (uint)(targetIndex + 1), (uint)sourceLen,
				(uint)targetLen, (uint)(sourceIndex + 1));
		}
	}
}
