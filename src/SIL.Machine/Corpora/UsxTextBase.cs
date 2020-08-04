using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class UsxTextBase : ScriptureTextBase
	{
		private static readonly HashSet<string> NonVerseParaStyles = new HashSet<string>
		{
			"ms", "mr", "s", "sr", "r", "d", "sp", "rem"
		};

		protected UsxTextBase(ITokenizer<string, int> wordTokenizer, string id, ScrVers versification)
			: base(wordTokenizer, id, versification)
		{	
		}

		public override IEnumerable<TextSegment> Segments
		{
			get
			{
				using (IStreamContainer streamContainer = CreateStreamContainer())
				using (Stream stream = streamContainer.OpenStream())
				{
					var ctxt = new ParseContext();
					var doc = XDocument.Load(stream);
					foreach (XElement elem in doc.Root.Elements())
					{
						switch (elem.Name.LocalName)
						{
							case "chapter":
								if (ctxt.IsInVerse)
									foreach (TextSegment seg in CreateTextSegments(ctxt))
										yield return seg;
								ctxt.Chapter = (string) elem.Attribute("number");
								ctxt.Verse = null;
								break;

							case "para":
								if (!IsVersePara(elem))
									continue;
								foreach (TextSegment segment in ParseElement(elem, ctxt))
									yield return segment;
								if (ctxt.IsInVerse && elem.Nodes().Any())
									ctxt.VerseBuilder.Append(" ");
								break;
						}
					}

					if (ctxt.IsInVerse)
						foreach (TextSegment seg in CreateTextSegments(ctxt))
							yield return seg;

				}
			}
		}

		private IEnumerable<TextSegment> ParseElement(XElement elem, ParseContext ctxt)
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
									foreach (TextSegment seg in CreateTextSegments(ctxt))
										yield return seg;

								ctxt.Verse = (string) e.Attribute("number");
								break;

							case "char":
								foreach (TextSegment segment in ParseElement(e, ctxt))
									yield return segment;
								break;
						}
						break;

					case XText text:
						if (ctxt.IsInVerse)
							ctxt.VerseBuilder.Append(text.Value);
						break;
				}
			}
		}

		private static bool IsVersePara(XElement paraElem)
		{
			var style = (string) paraElem.Attribute("style");
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

		private IEnumerable<TextSegment> CreateTextSegments(ParseContext ctxt)
		{
			string text = ctxt.VerseBuilder.ToString().Trim();
			ctxt.VerseBuilder.Clear();
			return CreateTextSegments(ctxt.Chapter, ctxt.Verse, text);
		}

		private class ParseContext
		{
			public StringBuilder VerseBuilder { get; } = new StringBuilder();
			public string Chapter { get; set; }
			public string Verse { get; set; }
			public bool IsInVerse => Chapter != null && Verse != null;
		}
	}
}
