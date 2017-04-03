using System.Collections.Generic;
using System.IO;
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

		private static IEnumerable<IText> GetTexts(ITokenizer<string, int> wordTokenizer, IEnumerable<string> filePatterns)
		{
			foreach (string filePattern in filePatterns)
			{
				string path = filePattern;
				string searchPattern = "*";
				if (!filePattern.EndsWith(Path.PathSeparator.ToString()))
				{
					path = Path.GetDirectoryName(filePattern);
					searchPattern = Path.GetFileName(filePattern);
				}

				foreach (string fileName in Directory.EnumerateFiles(path, searchPattern))
					yield return new TextFileText(Path.GetFileNameWithoutExtension(fileName), fileName, wordTokenizer);
			}
		}
	}
}
