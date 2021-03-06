﻿using System.IO;
using System.Text;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class UsfmFileTextCorpus : ScriptureTextCorpus
	{
		public UsfmFileTextCorpus(ITokenizer<string, int, string> wordTokenizer, string stylesheetFileName,
			Encoding encoding, string projectPath, ScrVers versification = null)
		{
			Versification = versification ?? ScrVers.English;
			var stylesheet = new UsfmStylesheet(stylesheetFileName);
			foreach (string sfmFileName in Directory.EnumerateFiles(projectPath, "*.SFM"))
				AddText(new UsfmFileText(wordTokenizer, stylesheet, encoding, sfmFileName, Versification));
		}

		public override ScrVers Versification { get; }
	}
}
