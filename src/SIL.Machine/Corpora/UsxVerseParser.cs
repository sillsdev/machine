using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Corpora
{
	public class UsxVerseParser
	{
		private static readonly HashSet<string> NonVerseParaStyles = new HashSet<string>
		{
			"ms", "mr", "s", "sr", "r", "d", "sp", "rem"
		};

		public IEnumerable<UsxVerse> Parse(Stream stream)
		{
			var ctxt = new ParseContext();
			var doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
			XElement bookElem = doc.Descendants("book").First();
			XElement rootElem = bookElem.Parent;
			foreach (XElement elem in rootElem.Elements())
			{
				switch (elem.Name.LocalName)
				{
					case "chapter":
						if (ctxt.IsInVerse)
							yield return CreateVerse(ctxt);
						ctxt.Chapter = (string)elem.Attribute("number");
						ctxt.Verse = null;
						ctxt.SentenceStart = true;
						break;

					case "para":
						if (!IsVersePara(elem))
						{
							ctxt.SentenceStart = true;
							continue;
						}
						foreach (UsxVerse evt in ParseElement(elem, ctxt))
							yield return evt;
						break;
				}
			}

			if (ctxt.IsInVerse)
				yield return CreateVerse(ctxt);
		}

		private IEnumerable<UsxVerse> ParseElement(XElement elem, ParseContext ctxt)
		{
			foreach (XNode node in elem.Nodes())
			{
				switch (node)
				{
					case XElement e:
						switch (e.Name.LocalName)
						{
							case "verse":
								if (ctxt.IsInVerse)
									yield return CreateVerse(ctxt);
								ctxt.Verse = (string)e.Attribute("number") ?? (string)e.Attribute("pubnumber");
								break;

							case "char":
								foreach (UsxVerse evt in ParseElement(e, ctxt))
									yield return evt;
								break;

							case "wg":
								if (ctxt.IsInVerse)
									ctxt.VerseNodes.Add(e);
								break;
						}
						break;

					case XText text:
						if (ctxt.IsInVerse)
							ctxt.VerseNodes.Add(text);
						break;
				}
			}
		}

		private static UsxVerse CreateVerse(ParseContext ctxt)
		{
			var verse = new UsxVerse(ctxt.Chapter, ctxt.Verse, ctxt.SentenceStart, ctxt.VerseNodes);
			ctxt.SentenceStart = verse.Text.HasSentenceEnding();
			ctxt.VerseNodes.Clear();
			return verse;
		}

		private static bool IsVersePara(XElement paraElem)
		{
			var style = (string)paraElem.Attribute("style");
			if (NonVerseParaStyles.Contains(style))
				return false;

			if (IsNumberedStyle("ms", style))
				return false;

			if (IsNumberedStyle("s", style))
				return false;

			return true;
		}

		private static bool IsNumberedStyle(string stylePrefix, string style)
		{
			return style.StartsWith(stylePrefix) && int.TryParse(style.Substring(stylePrefix.Length), out _);
		}

		private class ParseContext
		{
			public IList<XNode> VerseNodes { get; } = new List<XNode>();
			public string Chapter { get; set; }
			public string Verse { get; set; }
			public bool IsInVerse => Chapter != null && Verse != null;
			public bool SentenceStart { get; set; } = true;
		}
	}
}
