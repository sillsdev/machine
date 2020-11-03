namespace SIL.Machine.Translation.Thot
{
	public class FastAlignWordAlignmentModel : ThotWordAlignmentModelBase<FastAlignWordAlignmentModel>,
		IIbm2WordAlignmentModel
	{
		public FastAlignWordAlignmentModel()
			: base(Thot.FastAlignWordAlignmentClassName)
		{
		}

		public FastAlignWordAlignmentModel(string prefFileName, bool createNew = false)
			: base(Thot.FastAlignWordAlignmentClassName, prefFileName, createNew)
		{
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
