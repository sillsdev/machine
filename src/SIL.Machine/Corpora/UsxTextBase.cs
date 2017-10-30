using SIL.Machine.Tokenization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace SIL.Machine.Corpora
{
	public abstract class UsxTextBase : StreamTextBase
	{
		private static readonly HashSet<string> NonVerseParaStyles = new HashSet<string>
		{
			"ms", "mr", "s", "sr", "r", "d", "sp"
		};

		protected UsxTextBase(ITokenizer<string, int> wordTokenizer, string id)
			: base(wordTokenizer, id)
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
									yield return CreateTextSegment(ctxt);
								int nextChapter = (int) elem.Attribute("number");
								if (nextChapter < ctxt.Chapter)
									throw new InvalidOperationException("The chapters occurred out of order.");
								ctxt.Chapter = nextChapter;
								ctxt.Verse = 0;
								break;

							case "para":
								if (!IsVersePara(elem))
									continue;
								foreach (TextSegment segment in ParseElement(elem, ctxt))
									yield return segment;
								if (ctxt.IsInVerse)
									ctxt.VerseBuilder.Append("\n");
								break;
						}
					}

					if (ctxt.IsInVerse)
						yield return CreateTextSegment(ctxt);

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
									yield return CreateTextSegment(ctxt);

								var verseNumberStr = (string) e.Attribute("number");
								int index = verseNumberStr.IndexOf("-", StringComparison.Ordinal);
								if (index > -1)
									verseNumberStr = verseNumberStr.Substring(0, index);
								int nextVerse = int.Parse(verseNumberStr,
									CultureInfo.InvariantCulture);
								if (nextVerse < ctxt.Verse)
									throw new InvalidOperationException("The verses occurred out of order.");
								ctxt.Verse = nextVerse;
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

		private TextSegment CreateTextSegment(ParseContext ctxt)
		{
			TextSegment segment = CreateTextSegment(ctxt.Chapter, ctxt.Verse, ctxt.VerseBuilder.ToString());
			ctxt.VerseBuilder.Clear();
			return segment;
		}

		private class ParseContext
		{
			public StringBuilder VerseBuilder { get; } = new StringBuilder();
			public int Chapter { get; set; }
			public int Verse { get; set; }
			public bool IsInVerse => Chapter > 0 && Verse > 0;
		}
	}
}
