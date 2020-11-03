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
	}
}
