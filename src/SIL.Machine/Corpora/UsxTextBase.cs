using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class UsxTextBase : ScriptureText
	{
		private static readonly HashSet<string> NonVerseParaStyles = new HashSet<string>
		{
			"ms", "mr", "s", "sr", "r", "d", "sp", "rem"
		};

		protected UsxTextBase(ITokenizer<string, int, string> wordTokenizer, string id, ScrVers versification)
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
					var doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
					XElement bookElem = doc.Descendants("book").First();
					XElement rootElem = bookElem.Parent;
					foreach (XElement elem in rootElem.Elements())
					{
						switch (elem.Name.LocalName)
						{
							case "chapter":
								if (ctxt.IsInVerse)
									foreach (TextSegment seg in CreateTextSegments(ctxt))
										yield return seg;
								ctxt.Chapter = (string) elem.Attribute("number");
								ctxt.Verse = null;
								ctxt.SentenceStart = true;
								break;

							case "para":
								if (!IsVersePara(elem))
								{
									ctxt.SentenceStart = true;
									continue;
								}
								foreach (TextSegment segment in ParseElement(elem, ctxt))
									yield return segment;
								if (ctxt.IsInVerse && elem.Nodes().Any())
									ctxt.Append(" ");
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

								ctxt.Verse = (string)e.Attribute("number") ?? (string)e.Attribute("pubnumber");
								break;

							case "char":
								foreach (TextSegment segment in ParseElement(e, ctxt))
									yield return segment;
								break;

							case "wg":
								if (ctxt.IsInVerse)
									ctxt.Append(e.Value);
								break;
						}
						break;

					case XText text:
						if (ctxt.IsInVerse)
							ctxt.Append(text.Value);
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
			string text = ctxt.GetVerseString();
			ctxt.Clear();
			IEnumerable<TextSegment> segments = CreateTextSegments(ctxt.Chapter, ctxt.Verse, text, ctxt.SentenceStart);
			ctxt.SentenceStart = text.HasSentenceEnding();
			return segments;
		}

		private class ParseContext
		{
			private readonly StringBuilder _verseBuilder = new StringBuilder();
			public string Chapter { get; set; }
			public string Verse { get; set; }
			public bool IsInVerse => Chapter != null && Verse != null;
			public bool SentenceStart { get; set; } = true;

			public void Append(string str)
			{
				_verseBuilder.Append(str);
			}

			public string GetVerseString()
			{
				return _verseBuilder.ToString().Trim();
			}

			public void Clear()
			{
				_verseBuilder.Clear();
			}
		}
	}
}
