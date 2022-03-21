using System.IO;
using System.Text;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class UsfmFileTextCorpus : ScriptureTextCorpus
	{
		public UsfmFileTextCorpus(string stylesheetFileName, Encoding encoding, string projectPath,
			ScrVers versification = null, bool includeMarkers = false, string filePattern = "*.SFM")
		{
			Versification = versification ?? ScrVers.English;
			var stylesheet = new UsfmStylesheet(stylesheetFileName);
			foreach (string sfmFileName in Directory.EnumerateFiles(projectPath, filePattern))
				AddText(new UsfmFileText(stylesheet, encoding, sfmFileName, Versification, includeMarkers));
		}

		public override ScrVers Versification { get; }
	}
}
