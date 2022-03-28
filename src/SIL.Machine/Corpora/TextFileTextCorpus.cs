using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class TextFileTextCorpus : DictionaryTextCorpus
	{
		public TextFileTextCorpus(params string[] filePatterns)
			: this((IEnumerable<string>)filePatterns)
		{
		}

		public TextFileTextCorpus(IEnumerable<string> filePatterns)
			: base(GetTexts(filePatterns))
		{
		}

		private static IEnumerable<IText> GetTexts(IEnumerable<string> filePatterns)
		{
			foreach ((string id, string fileName) in CorporaUtils.GetFiles(filePatterns))
				yield return new TextFileText(id, fileName);
		}
	}
}
