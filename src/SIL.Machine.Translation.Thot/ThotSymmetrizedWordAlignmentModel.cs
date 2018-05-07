namespace SIL.Machine.Translation.Thot
{
	public class ThotSymmetrizedWordAlignmentModel : SymmetrizedWordAlignmentModel
	{
		public ThotSymmetrizedWordAlignmentModel()
			: base(new ThotWordAlignmentModel(), new ThotWordAlignmentModel())
		{
		}

		public ThotSymmetrizedWordAlignmentModel(string directPrefFileName, string inversePrefFileName,
			bool createNew = false)
			: base(new ThotWordAlignmentModel(directPrefFileName, createNew),
				  new ThotWordAlignmentModel(inversePrefFileName, createNew))
		{
		}
	}
}
