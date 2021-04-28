using System.Collections.Generic;
using System.IO;
using System.Text;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class UsfmTextBase : ScriptureText
	{
		private static readonly HashSet<string> NonVerseParaStyles = new HashSet<string>
		{
			"ms", "mr", "s", "sr", "r", "d", "sp", "rem", "restore"
		};

		private readonly UsfmParser _parser;
		private readonly Encoding _encoding;
		private readonly bool _includeMarkers;

		protected UsfmTextBase(ITokenizer<string, int, string> wordTokenizer, string id, UsfmStylesheet stylesheet,
			Encoding encoding, ScrVers versification, bool includeMarkers)
			: base(wordTokenizer, id, versification)
		{
			_parser = new UsfmParser(stylesheet);
			_encoding = encoding;
			_includeMarkers = includeMarkers;
		}

		public override IEnumerable<TextSegment> Segments
		{
			get
			{
				string usfm = ReadUsfm();
				bool inVerse = false;
				UsfmMarker curEmbedMarker = null;
				var sb = new StringBuilder();
				string chapter = null, verse = null;
				bool sentenceStart = true;
				UsfmToken prevToken = null;
				var prevVerseRef = new VerseRef();
				foreach (UsfmToken token in _parser.Parse(usfm))
				{
					switch (token.Type)
					{
						case UsfmTokenType.Chapter:
							if (inVerse)
							{
								string text = sb.ToString();
								foreach (TextSegment seg in CreateTextSegments(ref prevVerseRef, chapter, verse, text,
									sentenceStart))
								{
									yield return seg;
								}
								sentenceStart = true;
								sb.Clear();
								inVerse = false;
							}
							chapter = token.Text;
							verse = null;
							break;

						case UsfmTokenType.Verse:
							if (inVerse)
							{
								if (token.Text == verse)
								{
									string text = sb.ToString();
									foreach (TextSegment seg in CreateTextSegments(ref prevVerseRef, chapter, verse,
										text, sentenceStart))
									{
										yield return seg;
									}
									sentenceStart = text.HasSentenceEnding();
									sb.Clear();

									// ignore duplicate verse
									inVerse = false;
									verse = null;
								}
								else if (VerseRef.AreOverlappingVersesRanges(token.Text, verse))
								{
									// merge overlapping verse ranges in to one range
									verse = CorporaHelpers.MergeVerseRanges(token.Text, verse);
								}
								else
								{
									string text = sb.ToString();
									foreach (TextSegment seg in CreateTextSegments(ref prevVerseRef, chapter, verse,
										text, sentenceStart))
									{
										yield return seg;
									}
									sentenceStart = text.HasSentenceEnding();
									sb.Clear();
									verse = token.Text;
								}
							}
							else
							{
								inVerse = true;
								verse = token.Text;
							}
							break;

						case UsfmTokenType.Paragraph:
							if (inVerse && !IsVersePara(token))
							{
								string text = sb.ToString();
								foreach (TextSegment seg in CreateTextSegments(ref prevVerseRef, chapter, verse, text,
									sentenceStart))
								{
									yield return seg;
								}
								sentenceStart = true;
								sb.Clear();
								inVerse = false;
								verse = null;
							}
							break;

						case UsfmTokenType.Note:
							curEmbedMarker = token.Marker;
							if (inVerse && _includeMarkers)
							{
								if (prevToken?.Type == UsfmTokenType.Paragraph && IsVersePara(prevToken))
								{
									sb.Append(prevToken);
									sb.Append(" ");
								}
								sb.Append(token);
								sb.Append(" ");
							}
							break;

						case UsfmTokenType.End:
							if (curEmbedMarker != null && token.Marker.Marker == curEmbedMarker.EndMarker)
								curEmbedMarker = null;
							if (inVerse && _includeMarkers)
								sb.Append(token);
							break;

						case UsfmTokenType.Character:
							switch (token.Marker.Marker)
							{
								case "fig":
								case "va":
								case "vp":
									curEmbedMarker = token.Marker;
									break;
							}
							if (inVerse && _includeMarkers)
							{
								if (prevToken?.Type == UsfmTokenType.Paragraph && IsVersePara(prevToken))
								{
									sb.Append(prevToken);
									sb.Append(" ");
								}
								sb.Append(token);
								sb.Append(" ");
							}
							break;

						case UsfmTokenType.Text:
							if (inVerse && !string.IsNullOrEmpty(token.Text))
							{
								if (_includeMarkers)
								{
									if (prevToken?.Type == UsfmTokenType.Paragraph && IsVersePara(prevToken))
									{
										sb.Append(prevToken);
										sb.Append(" ");
									}
									sb.Append(token);
								}
								else if (curEmbedMarker == null)
								{
									string text = token.Text;
									if (prevToken?.Type == UsfmTokenType.End)
										text = text.TrimStart();
									sb.Append(text);
								}
							}
							break;
					}
					prevToken = token;
				}

				if (inVerse)
				{
					foreach (TextSegment seg in CreateTextSegments(ref prevVerseRef, chapter, verse, sb.ToString(),
						sentenceStart))
					{
						yield return seg;
					}
				}
			}
		}

		private string ReadUsfm()
		{
			using (IStreamContainer streamContainer = CreateStreamContainer())
			using (var reader = new StreamReader(streamContainer.OpenStream(), _encoding))
			{
				return reader.ReadToEnd();
			}
		}

		private static bool IsVersePara(UsfmToken paraToken)
		{
			string style = paraToken.Marker.Marker;
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
