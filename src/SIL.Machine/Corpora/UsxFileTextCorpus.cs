using System.IO;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class UsxFileTextCorpus : DictionaryTextCorpus
	{
		public UsxFileTextCorpus(ITokenizer<string, int> wordTokenizer, string projectPath,
			ScrVers versification = null)
		{
			Versification = versification ?? ScrVers.English;
			foreach (string fileName in Directory.EnumerateFiles(projectPath, "*.usx"))
				AddText(new UsxFileText(wordTokenizer, fileName, Versification));
		}

		public ScrVers Versification { get; }
	}
}
