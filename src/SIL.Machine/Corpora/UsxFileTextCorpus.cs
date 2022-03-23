using System.IO;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class UsxFileTextCorpus : ScriptureTextCorpus
	{
		public UsxFileTextCorpus(string projectPath, ScrVers versification = null)
		{
			Versification = GetVersification(projectPath, versification);
			foreach (string fileName in Directory.EnumerateFiles(projectPath, "*.usx"))
				AddText(new UsxFileText(fileName, Versification));
		}

		public override ScrVers Versification { get; }

		private static ScrVers GetVersification(string projectPath, ScrVers versification)
		{
			string versificationFileName = Path.Combine(projectPath, "versification.vrs");
			if (versification == null && File.Exists(versificationFileName))
			{
				string vrsName = Path.GetFileName(projectPath);
				versification = Scripture.Versification.Table.Implementation.Load(versificationFileName, vrsName);
			}
			return versification ?? ScrVers.English;
		}
	}
}
