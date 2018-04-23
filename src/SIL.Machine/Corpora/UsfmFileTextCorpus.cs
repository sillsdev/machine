using System.Collections.Generic;
using System.IO;
using System.Text;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Corpora
{
	public class UsfmFileTextCorpus : DictionaryTextCorpus
	{
		public UsfmFileTextCorpus(ITokenizer<string, int> wordTokenizer, string stylesheetFileName, Encoding encoding,
			string projectPath)
			: base(GetTexts(wordTokenizer, stylesheetFileName, encoding, projectPath))
		{
		}

		private static IEnumerable<IText> GetTexts(ITokenizer<string, int> wordTokenizer, string stylesheetFileName,
			Encoding encoding, string projectPath)
		{
			var stylesheet = new UsfmStylesheet(stylesheetFileName);
			foreach (string sfmFileName in Directory.EnumerateFiles(projectPath, "*.SFM"))
				yield return new UsfmFileText(wordTokenizer, stylesheet, encoding, sfmFileName);
		}
	}
}
