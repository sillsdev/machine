using System;
using System.Collections.Generic;
using System.Linq;
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

					UsfmToken attributeToken = HandleAttributes(usfm, preserveWhitespace, tokens, nextMarkerIndex,
						ref text);

					if (text.Length > 0)
						tokens.Add(new UsfmToken(UsfmTokenType.Text, null, text, null));

					if (attributeToken != null)
						tokens.Add(attributeToken);

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

					// don't require a space before the | that starts attributes - mainly for milestones to allow \qt-s|speaker\*
					if (ch == '|')
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
				string tag = usfm.Substring(markerStart, index - markerStart).TrimEnd();
				// Milestone stop/end markers are ended with \*, so marker will just be * and can be skipped
				if (tag == "*")
				{
					// make sure that previous token was a milestone - have to skip space only tokens that may have been added when
					// preserveSpace is true.
					UsfmToken prevToken = tokens.Count > 0 ? tokens.Last(t => t.Type != UsfmTokenType.Text || t.Text.Trim() != "") : null;
					if (prevToken != null && (prevToken.Type == UsfmTokenType.Milestone ||
						prevToken.Type == UsfmTokenType.MilestoneEnd))
					{
						// if the last item is an empty text token, remove it so we don't get extra space.
						if (tokens.Last().Type == UsfmTokenType.Text)
							tokens.RemoveAt(tokens.Count - 1);
						continue;
					}
				}

				// Multiple whitespace after non-end marker is ok
				if (!tag.EndsWith("*", StringComparison.Ordinal) && !preserveWhitespace)
				{
					while ((index < usfm.Length) && IsNonSemanticWhiteSpace(usfm[index]))
						index++;
				}

				bool isNested = tag.StartsWith("+", StringComparison.Ordinal);
				if (!UsfmStylesheet.IsCellRange(tag, out _, out int colSpan))
					colSpan = 0;
				// Lookup marker
				UsfmMarker marker = _stylesheet.GetMarker(tag.TrimStart('+'));

				// If starts with a plus and is not a character style or an end style, it is an unknown tag
				if (isNested && marker.StyleType != UsfmStyleType.Character && marker.StyleType != UsfmStyleType.End)
					marker = _stylesheet.GetMarker(tag);

				switch (marker.StyleType)
				{
					case UsfmStyleType.Character:
						// Handle verse special case
						if ((marker.TextProperties & UsfmTextProperties.Verse) > 0)
						{
							tokens.Add(new UsfmToken(UsfmTokenType.Verse, marker, null,
								GetNextWord(usfm, ref index, preserveWhitespace)));
						}
						else
						{
							tokens.Add(new UsfmToken(UsfmTokenType.Character, marker, null, isNested: isNested,
								colSpan: colSpan));
						}
						break;
					case UsfmStyleType.Paragraph:
						// Handle chapter special case
						if ((marker.TextProperties & UsfmTextProperties.Chapter) > 0)
							tokens.Add(new UsfmToken(UsfmTokenType.Chapter, marker, null,
								GetNextWord(usfm, ref index, preserveWhitespace)));
						else if ((marker.TextProperties & UsfmTextProperties.Book) > 0)
							tokens.Add(new UsfmToken(UsfmTokenType.Book, marker, null,
								GetNextWord(usfm, ref index, preserveWhitespace)));
						else
							tokens.Add(new UsfmToken(UsfmTokenType.Paragraph, marker, null));
						break;
					case UsfmStyleType.Note:
						tokens.Add(new UsfmToken(UsfmTokenType.Note, marker, null,
							GetNextWord(usfm, ref index, preserveWhitespace)));
						break;
					case UsfmStyleType.End:
						tokens.Add(new UsfmToken(UsfmTokenType.End, marker, null, isNested: isNested));
						break;
					case UsfmStyleType.Unknown:
						// End tokens are always end tokens, even if unknown
						if (tag.EndsWith("*", StringComparison.Ordinal))
						{
							tokens.Add(new UsfmToken(UsfmTokenType.End, marker, null, isNested: isNested));
						}
						else
						{
							// Handle special case of esb and esbe which might not be in basic stylesheet
							// but are always sidebars and so should be tokenized as paragraphs
							if (tag == "esb" || tag == "esbe")
							{
								tokens.Add(new UsfmToken(UsfmTokenType.Paragraph, marker, null));
								break;
							}
							// Create unknown token with a corresponding end note
							tokens.Add(new UsfmToken(UsfmTokenType.Unknown, marker, null));
						}
						break;
					case UsfmStyleType.Milestone:
					case UsfmStyleType.MilestoneEnd:
						// if a milestone is not followed by a ending \* treat don't create a milestone token for the begining. Instead create at
						// text token for all the text up to the beginning of the next marker. This will make typing of milestones easiest since
						// the partially typed milestone more be reformatted to have a normal ending even if it hasn't been typed yet.
						if (!MilestoneEnded(usfm, index))
						{
							int endOfText = (index < usfm.Length - 1) ? usfm.IndexOf('\\', index + 1) : -1;
							if (endOfText == -1)
								endOfText = usfm.Length;
							string milestoneText = usfm.Substring(index, endOfText - index);
							// add back space that was removed after marker
							if (milestoneText.Length > 0 && milestoneText[0] != ' ' && milestoneText[0] != '|')
								milestoneText = " " + milestoneText;
							tokens.Add(new UsfmToken(UsfmTokenType.Text, null, @"\" + tag + milestoneText, null));
							index = endOfText;
						}
						else if (marker.StyleType == UsfmStyleType.Milestone)
							tokens.Add(new UsfmToken(UsfmTokenType.Milestone, marker, null));
						else
							tokens.Add(new UsfmToken(UsfmTokenType.MilestoneEnd, marker, null));
						break;
				}
			}

			// Forces a space to be present in tokenization if immediately
			// before a token requiring a preceding CR/LF. This is to ensure
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
								tokens[i - 1].Text = tokens[i - 1].Text + " ";
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

		private UsfmToken HandleAttributes(string usfm, bool preserveWhitespace, List<UsfmToken> tokens,
			int nextMarkerIndex, ref string text)
		{
			int attributeIndex = text.IndexOf('|');
			if (attributeIndex < 0)
				return null;

			UsfmToken attributeToken = null;
			UsfmToken matchingToken = FindMatchingStartMarker(usfm, tokens, nextMarkerIndex);
			if (matchingToken == null)
				return null;

			UsfmMarker matchingMarker = _stylesheet.GetMarker(matchingToken.Marker.Tag);
			if (matchingMarker.StyleType != UsfmStyleType.Character &&
				matchingMarker.StyleType != UsfmStyleType.Milestone &&
				matchingMarker.StyleType != UsfmStyleType.MilestoneEnd)
			{
				return null; // leave attributes of other styles as regular text
			}

			string adjustedText = text.Substring(0, attributeIndex);
			string attributesValue = text.Substring(attributeIndex + 1);
			if (matchingToken.SetAttributes(attributesValue, matchingMarker.DefaultAttributeName, ref adjustedText,
				preserveWhitespace))
			{
				text = adjustedText;

				if (matchingMarker.StyleType == UsfmStyleType.Character) // Don't do this for milestones
				{
					attributeToken = new UsfmToken(UsfmTokenType.Attribute, matchingMarker, null, attributesValue);
					attributeToken.CopyAttributes(matchingToken);
				}
			}

			return attributeToken;
		}

		private static UsfmToken FindMatchingStartMarker(string usfm, List<UsfmToken> tokens, int nextMarkerIndex)
		{
			if (!BeforeEndMarker(usfm, nextMarkerIndex, out string expectedStartMarker))
				return null;

			if (expectedStartMarker == "" && (tokens.Last().Type == UsfmTokenType.Milestone ||
				tokens.Last().Type == UsfmTokenType.MilestoneEnd))
				return tokens.Last();

			int nestingLevel = 0;
			for (int i = tokens.Count - 1; i >= 0; i--)
			{
				UsfmToken token = tokens[i];
				if (token.Type == UsfmTokenType.End)
					nestingLevel++;
				else if (token.Type != UsfmTokenType.Text && token.Type != UsfmTokenType.Attribute)
				{
					if (nestingLevel > 0)
						nestingLevel--;
					else if (nestingLevel == 0)
						return token;
				}
			}

			return null;
		}

		private static bool BeforeEndMarker(string usfm, int nextMarkerIndex, out string startMarker)
		{
			startMarker = null;
			int index = nextMarkerIndex + 1;
			while (index < usfm.Length && usfm[index] != '*' && !char.IsWhiteSpace(usfm[index]))
				index++;

			if (index >= usfm.Length || usfm[index] != '*')
				return false;
			startMarker = usfm.Substring(nextMarkerIndex + 1, index - nextMarkerIndex - 1);
			return true;
		}

		private static bool MilestoneEnded(string usfm, int index)
		{
			int nextMarkerIndex = (index < usfm.Length) ? usfm.IndexOf('\\', index) : -1;
			if (nextMarkerIndex == -1 || nextMarkerIndex > usfm.Length - 2)
				return false;

			return usfm.Substring(nextMarkerIndex, 2) == @"\*";
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
