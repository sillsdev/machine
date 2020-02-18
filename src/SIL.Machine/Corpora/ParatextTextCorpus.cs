using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class ParatextTextCorpus : DictionaryTextCorpus
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

			foreach (string sfmFileName in Directory.EnumerateFiles(projectDir, "*.SFM"))
				AddText(new UsfmFileText(wordTokenizer, stylesheet, encoding, sfmFileName, Versification));
		}

		public ScrVers Versification { get; }

		public override IEnumerable<IText> Texts => TextDictionary.Values.OrderBy(t => Canon.BookIdToNumber(t.Id));
	}
}
