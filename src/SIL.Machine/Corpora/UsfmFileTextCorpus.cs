using System.IO;
using System.Text;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class UsfmFileTextCorpus : ScriptureTextCorpus
	{
		public UsfmFileTextCorpus(ITokenizer<string, int, string> wordTokenizer, string stylesheetFileName,
			Encoding encoding, string projectPath, ScrVers versification = null, bool includeMarkers = false,
			string filePattern = "*.SFM")
			: base(wordTokenizer)
		{
			Versification = versification ?? ScrVers.English;
			var stylesheet = new UsfmStylesheet(stylesheetFileName);
			foreach (string sfmFileName in Directory.EnumerateFiles(projectPath, filePattern))
			{
				AddText(new UsfmFileText(wordTokenizer, stylesheet, encoding, sfmFileName, Versification,
					includeMarkers));
			}
		}

		public override ScrVers Versification { get; }
	}
}
