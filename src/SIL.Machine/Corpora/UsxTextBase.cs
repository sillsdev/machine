using SIL.Machine.Tokenization;
using System.Collections.Generic;
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
					bool inVerse = false;
					int chapter = 0, verse = 0;
					var sb = new StringBuilder();

					var doc = XDocument.Load(stream);
					foreach (XElement elem in doc.Root.Elements())
					{
						switch (elem.Name.LocalName)
						{
							case "chapter":
								if (inVerse)
								{
									yield return CreateTextSegment(chapter, verse, sb.ToString());
									sb.Clear();
									inVerse = false;
								}
								int nextChapter = (int) elem.Attribute("number");
								if (nextChapter < chapter)
									yield break;
								chapter = nextChapter;
								verse = 0;
								break;

							case "para":
								if (!IsVersePara(elem))
									continue;

								foreach (XNode node in elem.Nodes())
								{
									switch (node)
									{
										case XElement e:
											switch (e.Name.LocalName)
											{
												case "verse":
													if (inVerse)
													{
														yield return CreateTextSegment(chapter, verse, sb.ToString());
														sb.Clear();
													}
													else
													{
														inVerse = true;
													}

													int nextVerse = (int) e.Attribute("number");
													if (nextVerse < verse)
														yield break;
													verse = nextVerse;
													break;

												case "char":
													if (inVerse)
														sb.Append(e.Value);
													break;
											}
											break;

										case XText text:
											if (inVerse)
												sb.Append(text.Value);
											break;
									}
								}
								sb.Append("\n");
								break;
						}
					}

					if (inVerse)
						yield return CreateTextSegment(chapter, verse, sb.ToString());

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
	}
}
