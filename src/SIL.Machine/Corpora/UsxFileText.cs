using SIL.Machine.Tokenization;
using SIL.Scripture;
using System.IO;

namespace SIL.Machine.Corpora
{
	public class UsxFileText : UsxTextBase
	{
		private readonly string _fileName;

		public UsxFileText(ITokenizer<string, int, string> wordTokenizer, string fileName, ScrVers versification = null)
			: base(wordTokenizer, GetId(fileName), versification)
		{
			_fileName = fileName;
		}

		private static string GetId(string fileName)
		{
			string name = Path.GetFileNameWithoutExtension(fileName);
			if (name.Length == 3)
				return name;
			return name.Substring(3, 3);
		}

		protected override IStreamContainer CreateStreamContainer()
		{
			return new FileStreamContainer(_fileName);
		}
	}
}
