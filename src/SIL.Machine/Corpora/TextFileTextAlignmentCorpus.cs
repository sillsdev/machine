using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class TextFileTextAlignmentCorpus : DictionaryTextAlignmentCorpus
	{
		public TextFileTextAlignmentCorpus(params string[] filePatterns)
			: this((IEnumerable<string>) filePatterns)
		{
		}

		public TextFileTextAlignmentCorpus(IEnumerable<string> filePatterns)
			: base(GetAlignmentCollections(filePatterns))
		{
		}

		private static IEnumerable<ITextAlignmentCollection> GetAlignmentCollections(IEnumerable<string> filePatterns)
		{
			foreach ((string id, string fileName) in CorporaHelpers.GetFiles(filePatterns))
				yield return new TextFileTextAlignmentCollection(id, fileName);
		}
	}
}
