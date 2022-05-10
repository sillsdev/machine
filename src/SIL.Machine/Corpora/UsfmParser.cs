using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	/// <summary>
	/// Parser for USFM. Sends parse information to an optional sink.
	/// The parser parses one token at a time, looking ahead as necessary
	/// for such elements as figures, links and alternate verses and chapters.
	/// 
	/// The parser first updates the UsfmParserState and then calls the 
	/// parser handler as necessary.
	/// </summary>
	public class UsfmParser
	{
		public static void Parse(UsfmStylesheet stylesheet, string usfm, IUsfmParserHandler handler,
			ScrVers versification = null, bool preserveWhitespace = false)
		{
			var parser = new UsfmParser(stylesheet, usfm, handler, versification, preserveWhitespace);
			parser.ProcessTokens();
		}

		private static readonly Regex OptBreakSplitter = new Regex("(//)", RegexOptions.Compiled);
		private readonly bool _tokensPreserveWhitespace;

		private readonly IUsfmParserHandler _handler;

		/// <summary>
		/// Number of tokens to skip over because have been processed in advance
		/// (i.e. for figures which are three tokens, or links, or chapter/verse alternates)
		/// </summary>
		private int _skip = 0;

		public UsfmParser(UsfmStylesheet stylesheet, IReadOnlyList<UsfmToken> tokens,
			IUsfmParserHandler handler = null, ScrVers versification = null, bool tokensPreserveWhitespace = false)
		{
			State = new UsfmParserState(stylesheet, versification ?? ScrVers.English, tokens);
			_handler = handler;
			_tokensPreserveWhitespace = tokensPreserveWhitespace;
		}

		public UsfmParser(UsfmStylesheet stylesheet, string usfm, IUsfmParserHandler handler = null,
			ScrVers versification = null, bool preserveWhitespace = false)
			: this(stylesheet, GetTokens(stylesheet, usfm, preserveWhitespace), handler, versification,
				  preserveWhitespace)
		{
		}

		private static IReadOnlyList<UsfmToken> GetTokens(UsfmStylesheet stylesheet, string usfm,
			bool preserveWhitespace)
		{
			var tokenizer = new UsfmTokenizer(stylesheet);
			return tokenizer.Tokenize(usfm, preserveWhitespace);
		}

		/// <summary>
		/// Gets the current parser state. Note: Will change with each token parsed
		/// </summary>
		public UsfmParserState State { get; private set; }

		/// <summary>
		/// Processes all tokens
		/// </summary>
		public void ProcessTokens()
		{
			while (ProcessToken())
			{
			}
		}

		/// <summary>
		/// Processes a single token
		/// </summary>
		/// <returns>false if there were no more tokens process</returns>
		public bool ProcessToken()
		{
			// If past end
			if (State.Index >= State.Tokens.Count - 1)
			{
				_handler?.EndUsfm(State);
				return false;
			}
			else if (State.Index < 0)
			{
				_handler?.StartUsfm(State);
			}

			// Move to next token
			State.Index++;

			// Update verse offset with previous token (since verse offset is from start of current token)
			if (State.PrevToken != null)
				State.VerseOffset += State.PrevToken.GetLength(addSpaces: !_tokensPreserveWhitespace);

			// Skip over tokens that are to be skipped, ensuring that 
			// SpecialToken state is true.
			if (_skip > 0)
			{
				_skip--;
				State.SpecialToken = true;
				return true;
			}

			// Reset special token and figure status
			State.SpecialToken = false;

			UsfmToken token = State.Token;

			// Switch unknown types to either character or paragraph
			UsfmTokenType tokenType = token.Type;
			if (tokenType == UsfmTokenType.Unknown)
				tokenType = DetermineUnknownTokenType();

			if (_handler != null && !string.IsNullOrEmpty(token.Marker))
				_handler.GotMarker(State, token.Marker);

			// Close open elements
			switch (tokenType)
			{
				case UsfmTokenType.Book:
				case UsfmTokenType.Chapter:
					CloseAll();
					break;
				case UsfmTokenType.Paragraph:
					// Handle special case of table rows
					if (token.Marker == "tr")
					{
						// Close all but table and sidebar
						while (State.Stack.Count > 0
							   && State.Peek().Type != UsfmElementType.Table
							   && State.Peek().Type != UsfmElementType.Sidebar)
							CloseElement();
						break;
					}

					// Handle special case of sidebars
					if (token.Marker == "esb")
					{
						// Close all
						CloseAll();
						break;
					}

					// Close all but sidebar
					while (State.Stack.Count > 0 && State.Peek().Type != UsfmElementType.Sidebar)
						CloseElement();
					break;
				case UsfmTokenType.Character:
					// Handle special case of table cell
					if (IsCell(token))
					{
						// Close until row
						while (State.Peek().Type != UsfmElementType.Row)
							CloseElement();
						break;
					}

					// Handle refs
					if (IsRef(token))
					{
						// Refs don't close anything
						break;
					}

					// If non-nested character style, close all character styles
					if (!token.Marker.StartsWith("+"))
						CloseCharStyles();
					break;
				case UsfmTokenType.Verse:
					UsfmTag paraTag = State.ParaTag;
					if (paraTag != null && paraTag.TextType != UsfmTextType.VerseText && paraTag.TextType != 0)
						CloseAll();
					else
						CloseNote();
					break;
				case UsfmTokenType.Note:
					CloseNote();
					break;
				case UsfmTokenType.End:
					// If end marker for an active note
					if (State.Stack.Any(e => e.Type == UsfmElementType.Note && (e.Marker + "*" == token.Marker)))
					{
						CloseNote(closed: true);
						break;
					}

					// If end marker for a character style on stack, close it
					// If no matching end marker, close all character styles on top of stack
					UsfmParserElement elem;
					bool unmatched = true;
					while (State.Stack.Count > 0)
					{
						elem = State.Peek();
						if (elem.Type != UsfmElementType.Char)
							break;

						// Determine if a + prefix is needed to close it (was nested char style)
						bool plusPrefix = State.Stack.Count > 1
							&& State.Stack[State.Stack.Count - 2].Type == UsfmElementType.Char;

						// If is a match
						if ((plusPrefix ? "+" : "") + elem.Marker + "*" == token.Marker)
						{
							CloseElement(closed: true);

							unmatched = false;
							break;
						}
						else
						{
							CloseElement();
						}
					}

					// Unmatched end marker
					if (unmatched)
						if (_handler != null) _handler.Unmatched(State, token.Marker);
					break;
			}

			VerseRef vref;
			// Handle tokens
			switch (tokenType)
			{
				case UsfmTokenType.Book:
					State.Push(new UsfmParserElement(UsfmElementType.Book, token.Marker));

					// Code is always upper case
					string code = token.Data.ToUpperInvariant();

					vref = State.VerseRef;
					// Update verse ref. Leave book alone if not empty to prevent parsing errors
					// on books with bad id lines.
					if (vref.Book == "" && Canon.BookIdToNumber(code) != 0)
						vref.Book = code;
					vref.ChapterNum = 1;
					vref.VerseNum = 0;
					State.VerseRef = vref;
					State.VerseOffset = 0;

					// Book start. 
					if (_handler != null) _handler.StartBook(State, token.Marker, code);
					break;
				case UsfmTokenType.Chapter:
					// Get alternate chapter number
					string altChapter = null;
					string pubChapter = null;
					if (State.Index < State.Tokens.Count - 3
						&& State.Tokens[State.Index + 1].Marker == "ca"
						&& State.Tokens[State.Index + 2].Text != null
						&& State.Tokens[State.Index + 3].Marker == "ca*")
					{
						altChapter = State.Tokens[State.Index + 2].Text.Trim();
						_skip += 3;

						// Skip blank space after if present
						if (State.Index + _skip < State.Tokens.Count - 1
							&& State.Tokens[State.Index + _skip + 1].Text != null
							&& State.Tokens[State.Index + _skip + 1].Text.Trim().Length == 0)
							_skip++;
					}

					// Get publishable chapter number
					if (State.Index + _skip < State.Tokens.Count - 2
						&& State.Tokens[State.Index + _skip + 1].Marker == "cp"
						&& State.Tokens[State.Index + _skip + 2].Text != null)
					{
						pubChapter = State.Tokens[State.Index + _skip + 2].Text.Trim();
						_skip += 2;
					}

					// Chapter
					vref = State.VerseRef;
					vref.Chapter = token.Data;
					vref.VerseNum = 0;
					State.VerseRef = vref;
					// Verse offset is not zeroed for chapter 1, as it is part of intro
					if (State.VerseRef.ChapterNum != 1)
						State.VerseOffset = 0;

					if (_handler != null) _handler.Chapter(State, token.Data, token.Marker, altChapter, pubChapter);
					break;
				case UsfmTokenType.Verse:
					string pubVerse = null;
					string altVerse = null;
					if (State.Index < State.Tokens.Count - 3
						&& State.Tokens[State.Index + 1].Marker == "va"
						&& State.Tokens[State.Index + 2].Text != null
						&& State.Tokens[State.Index + 3].Marker == "va*")
					{
						// Get alternate verse number
						altVerse = State.Tokens[State.Index + 2].Text.Trim();
						_skip += 3;
					}
					if (State.Index + _skip < State.Tokens.Count - 3
						&& State.Tokens[State.Index + _skip + 1].Marker == "vp"
						&& State.Tokens[State.Index + _skip + 2].Text != null
						&& State.Tokens[State.Index + _skip + 3].Marker == "vp*")
					{
						// Get publishable verse number
						pubVerse = State.Tokens[State.Index + _skip + 2].Text.Trim();
						_skip += 3;
					}

					// Verse
					vref = State.VerseRef;
					vref.Verse = token.Data;
					State.VerseRef = vref;
					State.VerseOffset = 0;

					if (_handler != null) _handler.Verse(State, token.Data, token.Marker, altVerse, pubVerse);
					break;
				case UsfmTokenType.Paragraph:
					// Handle special case of table rows
					if (token.Marker == "tr")
					{
						// Start table if not open
						if (State.Stack.All(e => e.Type != UsfmElementType.Table))
						{
							State.Push(new UsfmParserElement(UsfmElementType.Table, null));
							if (_handler != null) _handler.StartTable(State);
						}

						State.Push(new UsfmParserElement(UsfmElementType.Row, token.Marker));

						// Row start
						if (_handler != null) _handler.StartRow(State, token.Marker);
						break;
					}

					// Handle special case of sidebars
					if (token.Marker == "esb")
					{
						State.Push(new UsfmParserElement(UsfmElementType.Sidebar, token.Marker));

						// Look for category
						string sidebarCategory = null;
						if (State.Index < State.Tokens.Count - 3
							&& State.Tokens[State.Index + 1].Marker == "cat"
							&& State.Tokens[State.Index + 2].Text != null
							&& State.Tokens[State.Index + 3].Marker == "cat*")
						{
							// Get category
							sidebarCategory = State.Tokens[State.Index + 2].Text.Trim();
							_skip += 3;
						}

						if (_handler != null) _handler.StartSidebar(State, token.Marker, sidebarCategory);
						break;
					}

					// Close sidebar if in sidebar
					if (token.Marker == "esbe")
					{
						if (State.Stack.Any(e => e.Type == UsfmElementType.Sidebar))
						{
							while (State.Stack.Count > 0)
								CloseElement(State.Peek().Type == UsfmElementType.Sidebar);
						}
						else if (_handler != null)
						{
							_handler.Unmatched(State, token.Marker);
						}
						break;
					}

					State.Push(new UsfmParserElement(UsfmElementType.Para, token.Marker));

					// Paragraph opening
					if (_handler != null) _handler.StartPara(State, token.Marker, token.Type == UsfmTokenType.Unknown, token.Attributes);
					break;
				case UsfmTokenType.Character:
					// Handle special case of table cells (treated as special character style)
					if (IsCell(token))
					{
						string align = "start";
						if (token.Marker.Length > 2 && token.Marker[2] == 'c')
							align = "center";
						else if (token.Marker.Length > 2 && token.Marker[2] == 'r')
							align = "end";

						UsfmStylesheet.IsCellRange(token.Marker, out string baseMarker, out int colspan);
						State.Push(new UsfmParserElement(UsfmElementType.Cell, baseMarker));

						if (_handler != null) _handler.StartCell(State, baseMarker, align, colspan);
						break;
					}

					if (IsRef(token))
					{
						// xrefs are special tokens (they do not stand alone)
						State.SpecialToken = true;

						ParseDisplayAndTarget(out string display, out string target);

						_skip += 2;

						if (_handler != null) _handler.Ref(State, token.Marker, display, target);
						break;
					}

					string actualMarker;
					bool invalidMarker = false;
					if (token.Marker.StartsWith("+"))
					{
						// Only strip + if properly nested
						UsfmTag charTag = State.CharTag;
						actualMarker = charTag != null ? token.Marker.TrimStart('+') : token.Marker;
						invalidMarker = charTag == null;
					}
					else
						actualMarker = token.Marker;

					State.Push(new UsfmParserElement(UsfmElementType.Char, actualMarker, token.Attributes));
					if (_handler != null)
					{
						_handler.StartChar(State, actualMarker, token.Type == UsfmTokenType.Unknown || invalidMarker,
							token.Attributes);
					}
					break;
				case UsfmTokenType.Note:
					// Look for category
					string noteCategory = null;
					if (State.Index < State.Tokens.Count - 3
						&& State.Tokens[State.Index + 1].Marker == "cat"
						&& State.Tokens[State.Index + 2].Text != null
						&& State.Tokens[State.Index + 3].Marker == "cat*")
					{
						// Get category
						noteCategory = State.Tokens[State.Index + 2].Text.Trim();
						_skip += 3;
					}

					State.Push(new UsfmParserElement(UsfmElementType.Note, token.Marker));

					if (_handler != null) _handler.StartNote(State, token.Marker, token.Data, noteCategory);
					break;
				case UsfmTokenType.Text:
					string text = token.Text;

					// If last token before a paragraph, book or chapter, esb, esbe (both are paragraph types),
					// or at very end, strip final space
					// This is because USFM requires these to be on a new line, therefore adding whitespace
					if ((State.Index == State.Tokens.Count - 1
						 || State.Tokens[State.Index + 1].Type == UsfmTokenType.Paragraph
						 || State.Tokens[State.Index + 1].Type == UsfmTokenType.Book
						 || State.Tokens[State.Index + 1].Type == UsfmTokenType.Chapter)
						&& text.Length > 0 && text[text.Length - 1] == ' ')
					{
						text = text.Substring(0, text.Length - 1);
					}

					if (_handler != null)
					{
						// Replace ~ with nbsp
						text = text.Replace('~', '\u00A0');

						// Replace // with <optbreak/>
						foreach (string str in OptBreakSplitter.Split(text))
						{
							if (str == "//")
								_handler.OptBreak(State);
							else
								_handler.Text(State, str);
						}
					}
					break;

				case UsfmTokenType.Milestone:
				case UsfmTokenType.MilestoneEnd:
					// currently, parse state doesn't need to be update, so just inform the handler about the milestone.
					_handler?.Milestone(State, token.Marker, token.Type == UsfmTokenType.Milestone, token.Attributes);
					break;
			}

			return true;
		}

		private void ParseDisplayAndTarget(out string display, out string target)
		{
			display = State.Tokens[State.Index + 1].Text.Substring(
				0, State.Tokens[State.Index + 1].Text.IndexOf('|'));
			target = State.Tokens[State.Index + 1].Text.Substring(
				State.Tokens[State.Index + 1].Text.IndexOf('|') + 1);
		}

		/// <summary>
		/// Closes all open elements on stack
		/// </summary>
		public void CloseAll()
		{
			while (State.Stack.Count > 0)
				CloseElement();
		}

		/// <summary>
		/// Determine type that an unknown token should be treated as
		/// </summary>
		/// <returns>character or paragraph type</returns>
		private UsfmTokenType DetermineUnknownTokenType()
		{
			// Unknown inside notes are character
			if (State.Stack.Any(e => e.Type == UsfmElementType.Note))
				return UsfmTokenType.Character;

			return UsfmTokenType.Paragraph;
		}

		private void CloseNote(bool closed = false)
		{
			if (State.Stack.Any(elem => elem.Type == UsfmElementType.Note))
			{
				UsfmParserElement elem;
				do
				{
					if (State.Stack.Count == 0)
						break;

					elem = State.Peek();
					CloseElement(closed && elem.Type == UsfmElementType.Note);
				} while (elem.Type != UsfmElementType.Note);
			}
		}

		private void CloseCharStyles()
		{
			while (State.Stack.Count > 0 && State.Peek().Type == UsfmElementType.Char)
				CloseElement();
		}

		private void CloseElement(bool closed = false)
		{
			UsfmParserElement element = State.Pop();
			switch (element.Type)
			{
				case UsfmElementType.Book:
					if (_handler != null) _handler.EndBook(State, element.Marker);
					break;
				case UsfmElementType.Para:
					if (_handler != null) _handler.EndPara(State, element.Marker);
					break;
				case UsfmElementType.Char:
					if (_handler != null) _handler.EndChar(State, element.Marker, element.Attributes, closed);
					break;
				case UsfmElementType.Note:
					if (_handler != null) _handler.EndNote(State, element.Marker, closed);
					break;
				case UsfmElementType.Table:
					if (_handler != null) _handler.EndTable(State);
					break;
				case UsfmElementType.Row:
					if (_handler != null) _handler.EndRow(State, element.Marker);
					break;
				case UsfmElementType.Cell:
					if (_handler != null) _handler.EndCell(State, element.Marker);
					break;
				case UsfmElementType.Sidebar:
					if (_handler != null) _handler.EndSidebar(State, element.Marker, closed);
					break;
			}
		}

		private bool IsCell(UsfmToken token)
		{
			return token.Type == UsfmTokenType.Character
					&& (token.Marker.StartsWith("th") || token.Marker.StartsWith("tc"))
					&& State.Stack.Any(elem => elem.Type == UsfmElementType.Row);
		}

		private bool IsRef(UsfmToken token)
		{
			return (State.Index < State.Tokens.Count - 2)
				   && (State.Tokens[State.Index + 1].Text != null)
				   && (State.Tokens[State.Index + 1].Text.Contains("|"))
				   && (State.Tokens[State.Index + 2].Type == UsfmTokenType.End)
				   && (State.Tokens[State.Index + 2].Marker == token.EndMarker)
				   && (token.Marker == "ref");
		}
	}
}