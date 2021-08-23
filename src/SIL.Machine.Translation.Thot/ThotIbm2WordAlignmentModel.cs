namespace SIL.Machine.Translation.Thot
{
	public class ThotIbm2WordAlignmentModel : ThotWordAlignmentModel, IIbm2WordAlignmentModel
	{
		public ThotIbm2WordAlignmentModel()
			: base(Thot.SmoothedIbm2WordAlignmentClassName)
		{
		}

		public ThotIbm2WordAlignmentModel(string prefFileName, bool createNew = false)
			: base(Thot.SmoothedIbm2WordAlignmentClassName, prefFileName, createNew)
		{
		}

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
