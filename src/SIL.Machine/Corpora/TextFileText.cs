using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	public class TextFileText : IText
	{
		private readonly string _fileName;
		private readonly ITokenizer<string, int> _tokenizer;

		public TextFileText(string id, string fileName, ITokenizer<string, int> tokenizer)
		{
			Id = id;
			_fileName = fileName;
			_tokenizer = tokenizer;
		}

		public string Id { get; }

		public IEnumerable<TextSegment> Segments
		{
			get
			{
				using (var reader = new StreamReader(File.Open(_fileName, FileMode.Open)))
				{
					int lineNum = 1;
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						yield return new TextSegment(new TextSegmentRef(1, lineNum), _tokenizer.TokenizeToStrings(line).ToArray());
						lineNum++;
					}
				}
			}
		}
	}
}
