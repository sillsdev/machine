namespace SIL.Machine.Translation.Thot
{
	public class Ibm2ThotWordAlignmentModel : ThotWordAlignmentModelBase<Ibm2ThotWordAlignmentModel>,
		IIbm2WordAlignmentModel
	{
		public Ibm2ThotWordAlignmentModel()
			: base(Thot.Ibm2WordAlignmentClassName)
		{
		}

		public Ibm2ThotWordAlignmentModel(string prefFileName, bool createNew = false)
			: base(Thot.Ibm2WordAlignmentClassName, prefFileName, createNew)
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
