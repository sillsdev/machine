using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class UsfmFileTextCorpus : DictionaryTextCorpus
	{
		public UsfmFileTextCorpus(ITokenizer<string, int> wordTokenizer, string stylesheetFileName, Encoding encoding,
			string projectPath, ScrVers versification = null)
		{
			Versification = versification ?? ScrVers.English;
			var stylesheet = new UsfmStylesheet(stylesheetFileName);
			foreach (string sfmFileName in Directory.EnumerateFiles(projectPath, "*.SFM"))
				AddText(new UsfmFileText(wordTokenizer, stylesheet, encoding, sfmFileName, Versification));
		}

		public ScrVers Versification { get; }

		public override IEnumerable<IText> Texts => TextDictionary.Values.OrderBy(t => Canon.BookIdToNumber(t.Id));
	}
}
