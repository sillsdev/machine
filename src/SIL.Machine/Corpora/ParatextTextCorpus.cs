using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class ParatextTextCorpus : ScriptureTextCorpusBase
	{
		public ParatextTextCorpus(ITokenizer<string, int> wordTokenizer, string projectDir)
		{
			string settingsFileName = Path.Combine(projectDir, "Settings.xml");
			var settingsDoc = XDocument.Load(settingsFileName);
			var codePage = (int?)settingsDoc.Root.Element("Encoding") ?? 65001;
			EncodingInfo encodingInfo = Encoding.GetEncodings().FirstOrDefault(ei => ei.CodePage == codePage);
			if (encodingInfo == null)
				throw new InvalidOperationException("The Paratext project contains an unknown encoding.");
			Encoding encoding = encodingInfo.GetEncoding();

			var scrVersType = (int?)settingsDoc.Root.Element("Versification") ?? (int)ScrVersType.English;
			Versification = new ScrVers((ScrVersType)scrVersType);

			var stylesheetName = (string)settingsDoc.Root.Element("StyleSheet") ?? "usfm.sty";
			string stylesheetFileName = Path.Combine(projectDir, stylesheetName);
			var stylesheet = new UsfmStylesheet(stylesheetFileName);

			string suffix = ".SFM";
			XElement namingElem = settingsDoc.Root.Element("Naming");
			if (namingElem != null)
			{
				var postPart = (string)namingElem.Attribute("PostPart");
				if (!string.IsNullOrEmpty(postPart))
					suffix = postPart;
			}

			foreach (string sfmFileName in Directory.EnumerateFiles(projectDir, $"*{suffix}"))
				AddText(new UsfmFileText(wordTokenizer, stylesheet, encoding, sfmFileName, Versification));
		}

		public ScrVers Versification { get; }
	}
}
