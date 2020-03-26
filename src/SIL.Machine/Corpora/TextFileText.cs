using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	public class TextFileText : TextBase
	{
		public static ITextCorpus CreateSingleFileCorpus(ITokenizer<string, int> wordTokenizer, string fileName)
		{
			return new DictionaryTextCorpus(new TextFileText(wordTokenizer, "*all*", fileName));
		}

		private readonly string _fileName;

		public TextFileText(ITokenizer<string, int> wordTokenizer, string id, string fileName)
			: base(wordTokenizer, id, id)
		{
			_fileName = fileName;
		}

		public override IEnumerable<TextSegment> Segments
		{
			get
			{
				using (var reader = new StreamReader(_fileName))
				{
					int sectionNum = 1;
					int segmentNum = 1;
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						if (line.StartsWith("//"))
						{
							string sectionNumStr = line.Substring(2).Trim();
							if (!string.IsNullOrEmpty(sectionNumStr))
							{
								sectionNum = int.Parse(sectionNumStr, CultureInfo.InvariantCulture);
								segmentNum = 1;
							}
						}
						else
						{
							yield return CreateTextSegment(line, sectionNum, segmentNum);
							segmentNum++;
						}
					}
				}
			}
		}
	}
}
