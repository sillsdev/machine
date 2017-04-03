using System.Collections.Generic;
using System.IO;

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
					yield return new TextFileTextAlignmentCollection(Path.GetFileNameWithoutExtension(fileName), fileName);
			}
		}
	}
}
