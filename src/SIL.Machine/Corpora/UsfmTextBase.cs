using System.Collections.Generic;
using System.IO;
using System.Text;
using SIL.Machine.Tokenization;
using SIL.Machine.Utils;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public abstract class UsfmTextBase : ScriptureText
	{
		private static readonly HashSet<string> NonVerseParaStyles = new HashSet<string>
		{
			"ms", "mr", "s", "sr", "r", "d", "sp", "rem", "restore", "cl", "cp"
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

		protected override IEnumerable<TextSegment> GetSegmentsInDocOrder(bool includeText = true)
		{
			string usfm = ReadUsfm();
			UsfmMarker curEmbedMarker = null;
			UsfmMarker curSpanMarker = null;
			var sb = new StringBuilder();
			string chapter = null, verse = null;
			bool sentenceStart = true;
			UsfmToken prevToken = null;
			bool isVersePara = false;
			foreach (UsfmToken token in _parser.Parse(usfm))
			{
				switch (token.Type)
				{
					case UsfmTokenType.Chapter:
						if (chapter != null && verse != null)
						{
							string text = sb.ToString();
							foreach (TextSegment seg in CreateTextSegments(includeText, chapter, verse, text,
								sentenceStart))
							{
								yield return seg;
							}
							sentenceStart = true;
							sb.Clear();
						}
						chapter = token.Text;
						verse = null;
						break;

					case UsfmTokenType.Verse:
						if (chapter != null && verse != null)
						{
							if (token.Text == verse)
							{
								string text = sb.ToString();
								foreach (TextSegment seg in CreateTextSegments(includeText, chapter, verse, text,
									sentenceStart))
								{
									yield return seg;
								}
								sentenceStart = text.HasSentenceEnding();
								sb.Clear();

								// ignore duplicate verse
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
								foreach (TextSegment seg in CreateTextSegments(includeText, chapter, verse, text,
									sentenceStart))
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
							verse = token.Text;
						}
						isVersePara = true;
						break;

					case UsfmTokenType.Paragraph:
						isVersePara = IsVersePara(token);
						break;

					case UsfmTokenType.Note:
						curEmbedMarker = token.Marker;
						if (chapter != null && verse != null && _includeMarkers)
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
						if (curSpanMarker != null && token.Marker.Marker == curSpanMarker.EndMarker)
							curSpanMarker = null;
						if (isVersePara && chapter != null && verse != null && _includeMarkers)
							sb.Append(token);
						break;

					case UsfmTokenType.Character:
						if (token.Marker.Marker == "w" || token.Marker.Marker == "jmp")
						{
							curSpanMarker = token.Marker;
						}
						else if (token.Marker.Marker != "qac" && token.Marker.TextType == UsfmTextType.Other)
						{
							curEmbedMarker = token.Marker;
							if (!_includeMarkers && token.Marker.Marker == "rq")
								sb.TrimEnd();
						}
						if (isVersePara && chapter != null && verse != null && _includeMarkers)
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
						if (isVersePara && chapter != null && verse != null && !string.IsNullOrEmpty(token.Text))
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
								if (curSpanMarker != null)
								{
									int index = text.IndexOf("|");
									if (index >= 0)
										text = text.Substring(0, index);
								}
								if (prevToken?.Type == UsfmTokenType.End
									&& (sb.Length == 0 || char.IsWhiteSpace(sb[sb.Length - 1])))
								{
									text = text.TrimStart();
								}
								sb.Append(text);
							}
						}
						break;
				}
				prevToken = token;
			}

			if (chapter != null && verse != null)
			{
				foreach (TextSegment seg in CreateTextSegments(includeText, chapter, verse, sb.ToString(),
					sentenceStart))
				{
					yield return seg;
				}
			}
		}

		protected abstract IStreamContainer CreateStreamContainer();

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
