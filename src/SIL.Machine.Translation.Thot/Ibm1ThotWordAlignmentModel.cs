namespace SIL.Machine.Translation.Thot
{
	public class Ibm1ThotWordAlignmentModel : ThotWordAlignmentModelBase<Ibm1ThotWordAlignmentModel>
	{
		public Ibm1ThotWordAlignmentModel()
			: base(Thot.Ibm1WordAlignmentClassName)
		{
		}

		public Ibm1ThotWordAlignmentModel(string prefFileName, bool createNew = false)
			: base(Thot.Ibm1WordAlignmentClassName, prefFileName, createNew)
		{
		}
	}
}
