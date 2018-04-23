using SIL.Machine.Tokenization;
using System.Collections.Generic;
using System.IO;

namespace SIL.Machine.Corpora
{
	public class UsxFileTextCorpus : DictionaryTextCorpus
	{
		public UsxFileTextCorpus(ITokenizer<string, int> wordTokenizer, string projectPath)
			: base(GetTexts(wordTokenizer, projectPath))
		{
		}

		private static IEnumerable<IText> GetTexts(ITokenizer<string, int> wordTokenizer, string projectPath)
		{
			foreach (string fileName in Directory.EnumerateFiles(projectPath, "*.usx"))
				yield return new UsxFileText(wordTokenizer, fileName);
		}
	}
}
