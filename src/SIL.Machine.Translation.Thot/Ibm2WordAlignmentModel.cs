namespace SIL.Machine.Translation.Thot
{
	public class Ibm2WordAlignmentModel : ThotWordAlignmentModelBase<Ibm2WordAlignmentModel>,
		IIbm2WordAlignmentModel
	{
		public Ibm2WordAlignmentModel()
			: base(Thot.SmoothedIbm2WordAlignmentClassName)
		{
		}

		public Ibm2WordAlignmentModel(string prefFileName, bool createNew = false)
			: base(Thot.SmoothedIbm2WordAlignmentClassName, prefFileName, createNew)
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
