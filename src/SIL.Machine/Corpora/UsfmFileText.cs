using SIL.Machine.Tokenization;
using System.IO;
using System.Text;

namespace SIL.Machine.Corpora
{
	public class UsfmFileText : UsfmTextBase
	{
		private readonly string _fileName;

		public UsfmFileText(ITokenizer<string, int> wordTokenizer, UsfmStylesheet stylesheet, Encoding encoding,
			string fileName)
			: base(wordTokenizer, GetId(fileName), stylesheet, encoding)
		{
			_fileName = fileName;
		}

		private static string GetId(string fileName)
		{
			string name = Path.GetFileNameWithoutExtension(fileName);
			return name.Substring(2, 3);
		}

		protected override IStreamContainer CreateStreamContainer()
		{
			return new FileStreamContainer(_fileName);
		}
	}
}
