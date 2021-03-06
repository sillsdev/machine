﻿using System.IO;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class UsxFileTextCorpus : ScriptureTextCorpus
	{
		public UsxFileTextCorpus(ITokenizer<string, int, string> wordTokenizer, string projectPath,
			ScrVers versification = null)
		{
			string versificationFileName = Path.Combine(projectPath, "versification.vrs");
			if (versification == null && File.Exists(versificationFileName))
			{
				string vrsName = Path.GetFileName(projectPath);
				versification = Scripture.Versification.Table.Implementation.Load(versificationFileName, vrsName);
			}
			Versification = versification ?? ScrVers.English;
			foreach (string fileName in Directory.EnumerateFiles(projectPath, "*.usx"))
				AddText(new UsxFileText(wordTokenizer, fileName, Versification));
		}

		public override ScrVers Versification { get; }
	}
}
