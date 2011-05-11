using SIL.APRE;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a part of speech category.
	/// </summary>
	public class PartOfSpeech : IDBearer
	{
		public PartOfSpeech(string id, string desc)
			: base(id, desc)
		{
		}
	}
}
