namespace SIL.Machine.Translation.Thot
{
	public class ThotFastAlignWordAlignmentModel : ThotWordAlignmentModelBase<ThotFastAlignWordAlignmentModel>,
		IIbm2WordAlignmentModel
	{
		public ThotFastAlignWordAlignmentModel()
			: base(Thot.FastAlignWordAlignmentClassName)
		{
			TrainingIterationCount = 4;
		}

		public ThotFastAlignWordAlignmentModel(string prefFileName, bool createNew = false)
			: base(Thot.FastAlignWordAlignmentClassName, prefFileName, createNew)
		{
			TrainingIterationCount = 4;
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
