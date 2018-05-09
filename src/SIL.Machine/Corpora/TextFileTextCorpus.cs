using System.Collections.Generic;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	public class TextFileTextCorpus : DictionaryTextCorpus
	{
		public TextFileTextCorpus(ITokenizer<string, int> wordTokenizer, params string[] filePatterns)
			: this(wordTokenizer, (IEnumerable<string>) filePatterns)
		{
		}

		public TextFileTextCorpus(ITokenizer<string, int> wordTokenizer, IEnumerable<string> filePatterns)
			: base(GetTexts(wordTokenizer, filePatterns))
		{
		}

		private static IEnumerable<IText> GetTexts(ITokenizer<string, int> wordTokenizer,
			IEnumerable<string> filePatterns)
		{
			foreach ((string id, string fileName) in CorporaHelpers.GetFiles(filePatterns))
				yield return new TextFileText(wordTokenizer, id, fileName);
		}
	}
}
