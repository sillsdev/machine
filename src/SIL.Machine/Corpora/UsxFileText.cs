using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class UsxFileText : UsxTextBase
	{
		private readonly string _fileName;

		public UsxFileText(ITokenizer<string, int, string> wordTokenizer, string fileName, ScrVers versification = null)
			: base(wordTokenizer, CorporaHelpers.GetUsxId(fileName), versification)
		{
			_fileName = fileName;
		}

		protected override IStreamContainer CreateStreamContainer()
		{
			return new FileStreamContainer(_fileName);
		}
	}
}
