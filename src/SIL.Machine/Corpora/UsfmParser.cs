using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.Machine.Corpora
{
	public class UsfmParser
	{
		private const char ZeroWidthSpace = '\u200B';

		private readonly UsfmStylesheet _stylesheet;

		public UsfmParser(UsfmStylesheet stylesheet)
		{
			_stylesheet = stylesheet;
		}

		public IEnumerable<UsfmToken> Parse(string usfm, bool preserveWhitespace = false)
		{
			List<UsfmToken> tokens = new List<UsfmToken>();

			int index = 0;      // Current position
			while (index < usfm.Length)
			{
				int nextMarkerIndex = (index < usfm.Length - 1) ? usfm.IndexOf('\\', index + 1) : -1;
				if (nextMarkerIndex == -1)
					nextMarkerIndex = usfm.Length;

				// If text, create text token until end or next \
				var ch = usfm[index];
				if (ch != '\\')
				{
					string text = usfm.Substring(index, nextMarkerIndex - index);
					if (!preserveWhitespace)
						text = RegularizeSpaces(text);

					tokens.Add(new UsfmToken(UsfmTokenType.Text, null, text));

					index = nextMarkerIndex;
					continue;
				}

				// Get marker (and move past whitespace or star ending)
				index++;
				int markerStart = index;
				while (index < usfm.Length)
				{
					ch = usfm[index];

					// Backslash starts a new marker
					if (ch == '\\')
						break;

					// End star is part of marker
					if (ch == '*')
					{
						index++;
						break;
					}

					if (IsNonSemanticWhiteSpace(ch))
					{
						// Preserve whitespace if needed, otherwise skip
						if (!preserveWhitespace)
							index++;
						break;
					}
					index++;
				}
				string markerStr = usfm.Substring(markerStart, index - markerStart).TrimEnd();

				// Multiple whitespace after non-end marker is ok
				if (!markerStr.EndsWith("*", StringComparison.Ordinal) && !preserveWhitespace)
				{
					while ((index < usfm.Length) && IsNonSemanticWhiteSpace(usfm[index]))
						index++;
				}

				// Lookup marker
				UsfmMarker marker = _stylesheet.GetMarker(markerStr.TrimStart('+'));

				// If starts with a plus and is not a character style, it is an unknown marker
				if (markerStr.StartsWith("+", StringComparison.Ordinal) && marker.StyleType != UsfmStyleType.Character)
					marker = _stylesheet.GetMarker(markerStr);

				switch (marker.StyleType)
				{
					case UsfmStyleType.Character:
						// Handle verse special case
						if ((marker.TextProperties & UsfmTextProperties.Verse) > 0)
							tokens.Add(new UsfmToken(UsfmTokenType.Verse, marker, GetNextWord(usfm, ref index, preserveWhitespace)));
						else
							tokens.Add(new UsfmToken(UsfmTokenType.Character, marker, null));
						break;
					case UsfmStyleType.Paragraph:
						// Handle chapter special case
						if ((marker.TextProperties & UsfmTextProperties.Chapter) > 0)
							tokens.Add(new UsfmToken(UsfmTokenType.Chapter, marker, GetNextWord(usfm, ref index, preserveWhitespace)));
						else if ((marker.TextProperties & UsfmTextProperties.Book) > 0)
							tokens.Add(new UsfmToken(UsfmTokenType.Book, marker, GetNextWord(usfm, ref index, preserveWhitespace)));
						else
							tokens.Add(new UsfmToken(UsfmTokenType.Paragraph, marker, null));
						break;
					case UsfmStyleType.Note:
						tokens.Add(new UsfmToken(UsfmTokenType.Note, marker, GetNextWord(usfm, ref index, preserveWhitespace)));
						break;
					case UsfmStyleType.End:
						tokens.Add(new UsfmToken(UsfmTokenType.End, marker, null));
						break;
					case UsfmStyleType.Unknown:
						// End tokens are always end tokens, even if unknown
						if (markerStr.EndsWith("*", StringComparison.Ordinal))
						{
							tokens.Add(new UsfmToken(UsfmTokenType.End, marker, null));
						}
						else
						{
							// Handle special case of esb and esbe which might not be in basic stylesheet
							// but are always sidebars and so should be tokenized as paragraphs
							if (markerStr == "esb" || markerStr == "esbe")
							{
								tokens.Add(new UsfmToken(UsfmTokenType.Paragraph, marker, null));
								break;
							}
							// Create unknown token with a corresponding end note
							tokens.Add(new UsfmToken(UsfmTokenType.Unknown, marker, null));
						}
						break;
				}
			}

			// Forces a space to be present in tokenization if immediately
			// before a token requiring a preceeding CR/LF. This is to ensure 
			// that when written to disk and re-read, that tokenization 
			// will match. For example, "\p test\p here" requires a space
			// after "test". Also, "\p \em test\em*\p here" requires a space
			// token inserted after \em*
			if (!preserveWhitespace)
			{
				for (int i = 1; i < tokens.Count; i++)
				{
					// If requires newline (verses do, except when after '(' or '[')
					if (tokens[i].Type == UsfmTokenType.Book ||
						tokens[i].Type == UsfmTokenType.Chapter ||
						tokens[i].Type == UsfmTokenType.Paragraph ||
						(tokens[i].Type == UsfmTokenType.Verse &&
							!(tokens[i - 1].Type == UsfmTokenType.Text &&
							(tokens[i - 1].Text.EndsWith("(", StringComparison.Ordinal) || tokens[i - 1].Text.EndsWith("[", StringComparison.Ordinal)))))
					{
						// Add space to text token
						if (tokens[i - 1].Type == UsfmTokenType.Text)
						{
							if (!tokens[i - 1].Text.EndsWith(" ", StringComparison.Ordinal))
								tokens[i - 1] = new UsfmToken(tokens[i - 1].Text + " ");
						}
						else if (tokens[i - 1].Type == UsfmTokenType.End)
						{
							// Insert space token after * of end marker
							tokens.Insert(i, new UsfmToken(UsfmTokenType.Text, null, " "));
							i++;
						}
					}
				}
			}

			return tokens;
		}

		/// <summary>
		/// Gets the next word in the usfm and advances the index past it 
		/// </summary>
		/// <param name="usfm"></param>
		/// <param name="index"></param>
		/// <param name="preserveWhitespace">true to preserve all whitespace in tokens</param>
		/// <returns></returns>
		private static string GetNextWord(string usfm, ref int index, bool preserveWhitespace)
		{
			// Skip over leading spaces
			while ((index < usfm.Length) && IsNonSemanticWhiteSpace(usfm[index]))
				index++;

			int dataStart = index;
			while ((index < usfm.Length) && !IsNonSemanticWhiteSpace(usfm[index]) && (usfm[index] != '\\'))
				index++;

			string data = usfm.Substring(dataStart, index - dataStart);

			// Skip over trailing spaces
			if (!preserveWhitespace)
			{
				while ((index < usfm.Length) && IsNonSemanticWhiteSpace(usfm[index]))
					index++;
			}

			return data;
		}

		/// <summary>
		/// Converts all control characters, carriage returns and tabs into
		/// spaces, and then strips duplicate spaces. 
		/// </summary>
		private static string RegularizeSpaces(string str)
		{
			StringBuilder sb = new StringBuilder(str.Length);
			bool wasSpace = false;
			for (int i = 0; i < str.Length; i++)
			{
				var ch = str[i];
				// Control characters and CR/LF and TAB become spaces
				if (ch < 32)
				{
					if (!wasSpace)
						sb.Append(' ');
					wasSpace = true;
				}
				else if (!wasSpace && ch == ZeroWidthSpace && i + 1 < str.Length && IsNonSemanticWhiteSpace(str[i + 1]))
				{
					// ZWSP is redundant if followed by a space
				}
				else if (IsNonSemanticWhiteSpace(ch))
				{
					// Keep other kinds of spaces
					if (!wasSpace)
						sb.Append(ch);
					wasSpace = true;
				}
				else
				{
					sb.Append(ch);
					wasSpace = false;
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Checks if is whitespace, but not U+3000 (IDEOGRAPHIC SPACE).
		/// Note: ~ is not included as it is considered punctuation, not whitespace for simplicity.
		/// </summary>
		/// <param name="c">character</param>
		/// <returns>true if non-meaningful whitespace</returns>
		private static bool IsNonSemanticWhiteSpace(char c)
		{
			// Consider \u200B (ZERO-WIDTH SPACE), 
			// FB 18842 -- ZWJ and ZWNJ are not whitespace
			return (c != '\u3000' && char.IsWhiteSpace(c)) || (c == ZeroWidthSpace);
		}
	}
}
