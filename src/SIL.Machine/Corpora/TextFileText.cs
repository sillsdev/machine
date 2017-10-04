using System.Collections.Generic;
using System.IO;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	public class TextFileText : TextBase
	{
		private readonly string _fileName;

		public TextFileText(ITokenizer<string, int> wordTokenizer, string id, string fileName)
			: base(wordTokenizer, id)
		{
			_fileName = fileName;
		}

		public override IEnumerable<TextSegment> Segments
		{
			get
			{
				using (var reader = new StreamReader(_fileName))
				{
					int lineNum = 1;
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						yield return CreateTextSegment(1, lineNum, line);
						lineNum++;
					}
				}
			}
		}
	}
}
