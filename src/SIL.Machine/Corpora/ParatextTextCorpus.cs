using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class ParatextTextCorpus : ScriptureTextCorpus
	{
		public ParatextTextCorpus(ITokenizer<string, int, string> wordTokenizer, string projectDir,
			bool includeMarkers = false) : base(wordTokenizer)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			string settingsFileName = Path.Combine(projectDir, "Settings.xml");
			if (!File.Exists(settingsFileName))
				settingsFileName = Directory.EnumerateFiles(projectDir, "*.ssf").FirstOrDefault();
			if (string.IsNullOrEmpty(settingsFileName))
			{
				throw new ArgumentException("The project directory does not contain a settings file.",
					nameof(projectDir));
			}
			var settingsDoc = XDocument.Load(settingsFileName);
			var encodingStr = (string)settingsDoc.Root.Element("Encoding") ?? "65001";
			if (!int.TryParse(encodingStr, out int codePage))
			{
				throw new NotImplementedException(
					$"The project uses a legacy encoding that requires TECKit, map file: {encodingStr}.");
			}
			var encoding = Encoding.GetEncoding(codePage);

			var scrVersType = (int?)settingsDoc.Root.Element("Versification") ?? (int)ScrVersType.English;
			Versification = new ScrVers((ScrVersType)scrVersType);
			string customVersPath = Path.Combine(projectDir, "custom.vrs");
			if (File.Exists(customVersPath))
			{
				var guid = (string)settingsDoc.Root.Element("Guid");
				string versName = ((ScrVersType)scrVersType).ToString() + "-" + guid;
				using (var reader = new StreamReader(customVersPath))
				{
					Versification = Scripture.Versification.Table.Implementation.Load(reader, customVersPath,
						Versification, versName);
				}
			}

			var stylesheetName = (string)settingsDoc.Root.Element("StyleSheet") ?? "usfm.sty";
			string stylesheetFileName = Path.Combine(projectDir, stylesheetName);
			if (!File.Exists(stylesheetFileName) && stylesheetName != "usfm_sb.sty")
				stylesheetFileName = Path.Combine(projectDir, "usfm.sty");
			var stylesheet = new UsfmStylesheet(stylesheetFileName);

			string prefix = "";
			string suffix = ".SFM";
			XElement namingElem = settingsDoc.Root.Element("Naming");
			if (namingElem != null)
			{
				var prePart = (string)namingElem.Attribute("PrePart");
				if (!string.IsNullOrEmpty(prePart))
					prefix = prePart;
				var postPart = (string)namingElem.Attribute("PostPart");
				if (!string.IsNullOrEmpty(postPart))
					suffix = postPart;
			}

			foreach (string sfmFileName in Directory.EnumerateFiles(projectDir, $"{prefix}*{suffix}"))
			{
				AddText(new UsfmFileText(wordTokenizer, stylesheet, encoding, sfmFileName, Versification,
					includeMarkers));
			}
		}

		public override ScrVers Versification { get; }
	}
}
