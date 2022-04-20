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
		public static void Parse(UsfmStylesheet stylesheet, ScrVers versification, string usfm,
			UsfmParserHandlerBase handler, bool preserveWhitespace = false)
		{
			var parser = new UsfmParser(stylesheet, versification, usfm, handler, preserveWhitespace);
			parser.ProcessTokens();
		}

		private static readonly Regex OptBreakSplitter = new Regex("(//)", RegexOptions.Compiled);
		private readonly bool _tokensPreserveWhitespace;

		private readonly UsfmStylesheet _stylesheet;
		private readonly IUsfmParserHandler _handler;
		private UsfmParser _tokenClosedParser;

		/// <summary>
		/// Number of tokens to skip over because have been processed in advance
		/// (i.e. for figures which are three tokens, or links, or chapter/verse alternates)
		/// </summary>
		private int _skip = 0;

		public UsfmParser(UsfmStylesheet stylesheet, ScrVers versification, IReadOnlyList<UsfmToken> tokens,
			IUsfmParserHandler handler = null, bool tokensPreserveWhitespace = false)
		{
			_stylesheet = stylesheet;
			State = new UsfmParserState(stylesheet, versification, tokens);
			_handler = handler;
			_tokensPreserveWhitespace = tokensPreserveWhitespace;
		}

		public UsfmParser(UsfmStylesheet stylesheet, ScrVers versification, string usfm,
			IUsfmParserHandler handler = null, bool preserveWhitespace = false)
			: this(stylesheet, versification, GetTokens(stylesheet, usfm, preserveWhitespace), handler,
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
		/// Constructor for making a duplicate for looking ahead to find closing
		/// tokens of notes and character styles.
		/// </summary>
		private UsfmParser(UsfmParser usfmParser)
		{
			_stylesheet = usfmParser._stylesheet;
		}

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
				State.VerseOffset += State.PrevToken.GetLength(false, !_tokensPreserveWhitespace);

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
							   && State.Peek().Type != UsfmElementTypes.Table
							   && State.Peek().Type != UsfmElementTypes.Sidebar)
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
					while (State.Stack.Count > 0 && State.Peek().Type != UsfmElementTypes.Sidebar)
						CloseElement();
					break;
				case UsfmTokenType.Character:
					// Handle special case of table cell
					if (IsCell(token))
					{
						// Close until row
						while (State.Peek().Type != UsfmElementTypes.Row)
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
					if (paraTag != null && State.ParaTag.TextType != UsfmTextType.VerseText && paraTag.TextType != 0)
						CloseAll();
					else
						CloseNote();
					break;
				case UsfmTokenType.Note:
					CloseNote();
					break;
				case UsfmTokenType.End:
					// If end marker for an active note
					if (State.Stack.Any(e => e.Type == UsfmElementTypes.Note && (e.Marker + "*" == token.Marker)))
					{
						CloseNote();
						break;
					}

					// If end marker for a character style on stack, close it
					// If no matching end marker, close all character styles on top of stack
					UsfmParserElement elem;
					bool unmatched = true;
					while (State.Stack.Count > 0)
					{
						elem = State.Peek();
						if (elem.Type != UsfmElementTypes.Char)
							break;
						CloseElement();

						// Determine if a + prefix is needed to close it (was nested char style)
						bool plusPrefix = (State.Stack.Count > 0 && State.Peek().Type == UsfmElementTypes.Char);

						// If is a match
						if ((plusPrefix ? "+" : "") + elem.Marker + "*" == token.Marker)
						{
							unmatched = false;
							break;
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
					State.Push(new UsfmParserElement(UsfmElementTypes.Book, token.Marker));

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
						if (State.Stack.All(e => e.Type != UsfmElementTypes.Table))
						{
							State.Push(new UsfmParserElement(UsfmElementTypes.Table, null));
							if (_handler != null) _handler.StartTable(State);
						}

						State.Push(new UsfmParserElement(UsfmElementTypes.Row, token.Marker));

						// Row start
						if (_handler != null) _handler.StartRow(State, token.Marker);
						break;
					}

					// Handle special case of sidebars
					if (token.Marker == "esb")
					{
						bool isClosed = IsStudyBibleItemClosed("esb", "esbe");
						State.Push(new UsfmParserElement(UsfmElementTypes.Sidebar, token.Marker));

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

						if (_handler != null) _handler.StartSidebar(State, token.Marker, sidebarCategory, isClosed);
						break;
					}

					// Close sidebar if in sidebar
					if (token.Marker == "esbe")
					{
						if (State.Stack.Any(e => e.Type == UsfmElementTypes.Sidebar))
							CloseAll();
						else if (_handler != null)
							_handler.Unmatched(State, token.Marker);
						break;
					}

					State.Push(new UsfmParserElement(UsfmElementTypes.Para, token.Marker));

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
						State.Push(new UsfmParserElement(UsfmElementTypes.Cell, baseMarker));

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
						actualMarker = State.CharTag != null ? token.Marker.TrimStart('+') : token.Marker;
						invalidMarker = State.CharTag == null;
					}
					else
						actualMarker = token.Marker;

					State.Push(new UsfmParserElement(UsfmElementTypes.Char, actualMarker, State.Token.Attributes));
					if (_handler != null)
					{
						bool charIsClosed = IsTokenClosed();
						State.Stack.Last().IsClosed = charIsClosed; // save for attribute check in Text method
						_handler.StartChar(State, actualMarker, charIsClosed,
							token.Type == UsfmTokenType.Unknown || invalidMarker, State.Token.Attributes);
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

					State.Push(new UsfmParserElement(UsfmElementTypes.Note, token.Marker));

					if (_handler != null) _handler.StartNote(State, token.Marker, token.Data, noteCategory, IsTokenClosed());
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
					// currently, parse state doesn't need to be update, so just inform the sink about the milestone.
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
		/// Updates the state of this parser to be the same as the state of the specified parser.
		/// </summary>
		private void UpdateParser(UsfmParser usfmParser)
		{
			State = usfmParser.State.Clone();
			_skip = 0;
		}

		/// <summary>
		/// Determine if Study Bible item closed (ending marker before book or chapter)
		/// </summary>
		private bool IsStudyBibleItemClosed(string startMarker, string endingMarker)
		{
			for (int i = State.Index + 1; i < State.Tokens.Count; i++)
			{
				if (State.Tokens[i].Marker == endingMarker)
					return true;

				if (State.Tokens[i].Marker == startMarker
					|| State.Tokens[i].Type == UsfmTokenType.Book
					|| State.Tokens[i].Type == UsfmTokenType.Chapter)
					return false;
			}
			return false;
		}

		/// <summary>
		/// Determine type that an unknown token should be treated as
		/// </summary>
		/// <returns>character or paragraph type</returns>
		private UsfmTokenType DetermineUnknownTokenType()
		{
			// Unknown inside notes are character
			if (State.Stack.Any(e => e.Type == UsfmElementTypes.Note))
				return UsfmTokenType.Character;

			return UsfmTokenType.Paragraph;
		}


		private bool IsTokenClosed()
		{
			// Clone current parser
			if (_tokenClosedParser == null)
				_tokenClosedParser = new UsfmParser(this);
			_tokenClosedParser.UpdateParser(this);

			string marker = State.Token.Marker;
			LookaheadParser(State, _tokenClosedParser, marker, out bool isTokenClosed);
			return isTokenClosed;
		}

		private static void LookaheadParser(UsfmParserState state, UsfmParser lookaheadParser, string marker,
			out bool isTokenClosed)
		{
			// BEWARE: This method is fairly performance-critical
			// Determine current marker
			string endMarker = marker + "*";

			// Process tokens until either the start of the stack doesn't match (it was closed
			// improperly) or a matching close marker is found
			while (lookaheadParser.ProcessToken())
			{
				UsfmToken currentToken = lookaheadParser.State.Token;

				// Check if same marker was reopened without a close
				bool reopened = currentToken.Marker == marker &&
					lookaheadParser.State.Stack.SequenceEqual(state.Stack);
				if (reopened)
				{
					isTokenClosed = false;
					return;
				}

				// Check if beginning of stack is unchanged. If token is unclosed, it will be unchanged
				bool markerStillOpen = lookaheadParser.State.Stack.Take(state.Stack.Count).SequenceEqual(state.Stack);
				if (!markerStillOpen)
				{
					// Record whether marker is an end for this marker 
					isTokenClosed = currentToken.Marker == endMarker && currentToken.Type == UsfmTokenType.End;
					return;
				}
			}
			isTokenClosed = false;
		}

		private void CloseNote()
		{
			if (State.Stack.Any(elem => elem.Type == UsfmElementTypes.Note))
			{
				UsfmParserElement elem;
				do
				{
					if (State.Stack.Count == 0)
						break;

					elem = State.Peek();
					CloseElement();
				} while (elem.Type != UsfmElementTypes.Note);
			}
		}

		private void CloseCharStyles()
		{
			while (State.Stack.Count > 0 && State.Peek().Type == UsfmElementTypes.Char)
				CloseElement();
		}

		private void CloseElement()
		{
			UsfmParserElement element = State.Pop();
			switch (element.Type)
			{
				case UsfmElementTypes.Book:
					if (_handler != null) _handler.EndBook(State, element.Marker);
					break;
				case UsfmElementTypes.Para:
					if (_handler != null) _handler.EndPara(State, element.Marker);
					break;
				case UsfmElementTypes.Char:
					if (_handler != null) _handler.EndChar(State, element.Marker, element.Attributes);
					break;
				case UsfmElementTypes.Note:
					if (_handler != null) _handler.EndNote(State, element.Marker);
					break;
				case UsfmElementTypes.Table:
					if (_handler != null) _handler.EndTable(State);
					break;
				case UsfmElementTypes.Row:
					if (_handler != null) _handler.EndRow(State, element.Marker);
					break;
				case UsfmElementTypes.Cell:
					if (_handler != null) _handler.EndCell(State, element.Marker);
					break;
				case UsfmElementTypes.Sidebar:
					if (_handler != null) _handler.EndSidebar(State, element.Marker);
					break;
			}
		}

		private bool IsCell(UsfmToken token)
		{
			return token.Type == UsfmTokenType.Character
					&& (token.Marker.StartsWith("th") || token.Marker.StartsWith("tc"))
					&& State.Stack.Any(elem => elem.Type == UsfmElementTypes.Row);
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