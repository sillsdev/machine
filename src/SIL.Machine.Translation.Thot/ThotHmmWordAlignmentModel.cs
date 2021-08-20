namespace SIL.Machine.Translation.Thot
{
	public class ThotHmmWordAlignmentModel : ThotWordAlignmentModelBase<ThotHmmWordAlignmentModel>,
		IHmmWordAlignmentModel
	{
		public ThotHmmWordAlignmentModel()
			: base(Thot.HmmWordAlignmentClassName)
		{
		}

		public ThotHmmWordAlignmentModel(string prefFileName, bool createNew = false)
			: base(Thot.HmmWordAlignmentClassName, prefFileName, createNew)
		{
		}

		public override double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
			int prevTargetIndex, int targetIndex)
		{
			CheckDisposed();

			return GetAlignmentProbability(sourceLen, prevSourceIndex, sourceIndex);
		}

		/// <summary>
		/// Gets the alignment probability from the HMM single word alignment model. Use -1 for unaligned indices that
		/// occur before the first aligned index. Other unaligned indices are indicated by adding the source length to
		/// the previously aligned index.
		/// </summary>
		public double GetAlignmentProbability(int sourceLen, int prevSourceIndex, int sourceIndex)
		{
			CheckDisposed();

			// add 1 to convert the specified indices to Thot position indices, which are 1-based
			return Thot.swAlignModel_getHmmAlignmentProbability(Handle, (uint)(prevSourceIndex + 1), (uint)sourceLen,
				(uint)(sourceIndex + 1));
		}
	}
}
