using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class TextFileAlignmentCorpus : DictionaryAlignmentCorpus
	{
		public TextFileAlignmentCorpus(params string[] filePatterns)
			: this((IEnumerable<string>)filePatterns)
		{
		}

		public TextFileAlignmentCorpus(IEnumerable<string> filePatterns)
			: base(GetAlignmentCollections(filePatterns))
		{
		}

		private static IEnumerable<IAlignmentCollection> GetAlignmentCollections(IEnumerable<string> filePatterns)
		{
			foreach ((string id, string fileName) in CorporaHelpers.GetFiles(filePatterns))
				yield return new TextFileAlignmentCollection(id, fileName);
		}
	}
}
