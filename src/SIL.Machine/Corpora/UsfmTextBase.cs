using System.Collections.Generic;
using System.IO;
using System.Text;
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

		private static readonly HashSet<string> SpanMarkers = new HashSet<string>
		{
			"w", "jmp"
		};

		private static readonly HashSet<string> EmbedMarkers = new HashSet<string>
		{
			"fig", "va", "vp", "pro", "rq", "fm"
		};

		private readonly UsfmParser _parser;
		private readonly Encoding _encoding;
		private readonly bool _includeMarkers;

		protected UsfmTextBase(string id, UsfmStylesheet stylesheet, Encoding encoding, ScrVers versification,
			bool includeMarkers)
			: base(id, versification)
		{
			_parser = new UsfmParser(stylesheet);
			_encoding = encoding;
			_includeMarkers = includeMarkers;
		}

		protected override IEnumerable<TextRow> GetVersesInDocOrder()
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
							foreach (TextRow seg in CreateRows(chapter, verse, text, sentenceStart))
								yield return seg;
							sentenceStart = true;
							sb.Clear();
						}
						chapter = token.Data;
						verse = null;
						break;

					case UsfmTokenType.Verse:
						if (chapter != null && verse != null)
						{
							if (token.Data == verse)
							{
								string text = sb.ToString();
								foreach (TextRow seg in CreateRows(chapter, verse, text, sentenceStart))
									yield return seg;
								sentenceStart = text.HasSentenceEnding();
								sb.Clear();

								// ignore duplicate verse
								verse = null;
							}
							else if (VerseRef.AreOverlappingVersesRanges(token.Data, verse))
							{
								// merge overlapping verse ranges in to one range
								verse = CorporaUtils.MergeVerseRanges(token.Data, verse);
							}
							else
							{
								string text = sb.ToString();
								foreach (TextRow seg in CreateRows(chapter, verse, text, sentenceStart))
									yield return seg;
								sentenceStart = text.HasSentenceEnding();
								sb.Clear();
								verse = token.Data;
							}
						}
						else
						{
							verse = token.Data;
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
						if (curEmbedMarker != null && token.Marker.Tag == curEmbedMarker.EndTag)
							curEmbedMarker = null;
						if (curSpanMarker != null && token.Marker.Tag == curSpanMarker.EndTag)
							curSpanMarker = null;
						if (isVersePara && chapter != null && verse != null && _includeMarkers)
							sb.Append(token);
						break;

					case UsfmTokenType.Character:
						if (SpanMarkers.Contains(token.Marker.Tag))
						{
							curSpanMarker = token.Marker;
						}
						else if (token.Marker.Tag != "qac"
							&& (token.Marker.TextType == UsfmTextType.Other
								|| EmbedMarkers.Contains(token.Marker.Tag)))
						{
							curEmbedMarker = token.Marker;
							if (!_includeMarkers && token.Marker.Tag == "rq")
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
						else if (IsTableCellStyle(token))
						{
							if (!char.IsWhiteSpace(sb[sb.Length - 1]))
								sb.Append(" ");
						}
						break;

					case UsfmTokenType.Attribute:
						if (isVersePara && chapter != null && verse != null && _includeMarkers)
						{
							sb.Append(token);
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
				foreach (TextRow seg in CreateRows(chapter, verse, sb.ToString(), sentenceStart))
					yield return seg;
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
			string style = paraToken.Marker.Tag;
			if (NonVerseParaStyles.Contains(style))
				return false;

			if (IsNumberedStyle("ms", style))
				return false;

			if (IsNumberedStyle("s", style))
				return false;

			return true;
		}

		private static bool IsTableCellStyle(UsfmToken charToken)
		{
			string style = charToken.Marker.Tag;
			return IsNumberedStyle("th", style) || IsNumberedStyle("thc", style) || IsNumberedStyle("thr", style)
				|| IsNumberedStyle("tc", style) || IsNumberedStyle("tcc", style) || IsNumberedStyle("tcr", style);
		}

		private static bool IsNumberedStyle(string stylePrefix, string style)
		{
			return style.StartsWith(stylePrefix) && int.TryParse(style.Substring(stylePrefix.Length), out _);
		}
	}
}
