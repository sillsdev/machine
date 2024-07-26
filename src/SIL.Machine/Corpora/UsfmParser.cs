using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public static void Parse(
            string usfm,
            IUsfmParserHandler handler,
            string stylesheetFileName = "usfm.sty",
            ScrVers versification = null,
            bool preserveWhitespace = false
        )
        {
            Parse(usfm, handler, new UsfmStylesheet(stylesheetFileName), versification, preserveWhitespace);
        }

        public static void Parse(
            string usfm,
            IUsfmParserHandler handler,
            UsfmStylesheet stylesheet,
            ScrVers versification = null,
            bool preserveWhitespace = false
        )
        {
            var parser = new UsfmParser(
                usfm,
                handler,
                stylesheet ?? new UsfmStylesheet("usfm.sty"),
                versification,
                preserveWhitespace
            );
            try
            {
                parser.ProcessTokens();
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append(
                    $"An error occurred while parsing the USFM text in Verse: {parser.State.VerseRef}, line: {parser.State.LineNumber}, "
                );
                sb.Append($"column: {parser.State.ColumnNumber}, error: '{ex.Message}'");
                throw new InvalidOperationException(sb.ToString(), ex);
            }
        }

        private static readonly Regex OptBreakSplitter = new Regex("(//)", RegexOptions.Compiled);

        public UsfmParser(
            IReadOnlyList<UsfmToken> tokens,
            IUsfmParserHandler handler = null,
            string stylesheetFileName = "usfm.sty",
            ScrVers versification = null,
            bool tokensPreserveWhitespace = false
        )
            : this(tokens, handler, new UsfmStylesheet(stylesheetFileName), versification, tokensPreserveWhitespace) { }

        public UsfmParser(
            IReadOnlyList<UsfmToken> tokens,
            IUsfmParserHandler handler,
            UsfmStylesheet stylesheet,
            ScrVers versification = null,
            bool tokensPreserveWhitespace = false
        )
            : this(
                new UsfmParserState(
                    stylesheet ?? new UsfmStylesheet("usfm.sty"),
                    versification ?? ScrVers.English,
                    tokens
                ),
                handler,
                tokensPreserveWhitespace
            ) { }

        public UsfmParser(
            string usfm,
            IUsfmParserHandler handler = null,
            string stylesheetFileName = "usfm.sty",
            ScrVers versification = null,
            bool tokensPreserveWhitespace = false
        )
            : this(usfm, handler, new UsfmStylesheet(stylesheetFileName), versification, tokensPreserveWhitespace) { }

        public UsfmParser(
            string usfm,
            IUsfmParserHandler handler,
            UsfmStylesheet stylesheet,
            ScrVers versification = null,
            bool tokensPreserveWhitespace = false
        )
            : this(
                new UsfmParserState(
                    stylesheet ?? new UsfmStylesheet("usfm.sty"),
                    versification ?? ScrVers.English,
                    GetTokens(stylesheet, usfm, tokensPreserveWhitespace)
                ),
                handler,
                tokensPreserveWhitespace
            ) { }

        private UsfmParser(UsfmParserState state, IUsfmParserHandler handler, bool tokensPreserveWhitespace)
        {
            State = state;
            Handler = handler;
            TokensPreserveWhitespace = tokensPreserveWhitespace;
        }

        private static IReadOnlyList<UsfmToken> GetTokens(
            UsfmStylesheet stylesheet,
            string usfm,
            bool preserveWhitespace
        )
        {
            var tokenizer = new UsfmTokenizer(stylesheet);
            return tokenizer.Tokenize(usfm, preserveWhitespace);
        }

        public IUsfmParserHandler Handler { get; }

        public bool TokensPreserveWhitespace { get; }

        /// <summary>
        /// Gets the current parser state. Note: Will change with each token parsed
        /// </summary>
        public UsfmParserState State { get; private set; }

        /// <summary>
        /// Processes all tokens
        /// </summary>
        public void ProcessTokens()
        {
            while (ProcessToken()) { }
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
                CloseAll();
                Handler?.EndUsfm(State);
                return false;
            }
            else if (State.Index < 0)
            {
                Handler?.StartUsfm(State);
            }

            // Move to next token
            State.Index++;

            State.LineNumber = State.Token.LineNumber;
            State.ColumnNumber = State.Token.ColumnNumber;

            // Update verse offset with previous token (since verse offset is from start of current token)
            if (State.PrevToken != null)
                State.VerseOffset += State.PrevToken.GetLength(addSpaces: !TokensPreserveWhitespace);

            // Skip over tokens that are to be skipped, ensuring that
            // SpecialToken state is true.
            if (State.SpecialTokenCount > 0)
            {
                State.SpecialTokenCount--;
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

            if (Handler != null && !string.IsNullOrEmpty(token.Marker))
                Handler.GotMarker(State, token.Marker);

            // Close open elements
            switch (tokenType)
            {
                case UsfmTokenType.Book:
                case UsfmTokenType.Chapter:
                    throw new Exception("Book and chapter tokens should not be encountered here.");
                case UsfmTokenType.Paragraph:
                    // Handle special case of table rows
                    if (token.Marker == "tr")
                    {
                        // Close all but table and sidebar
                        while (
                            State.Stack.Count > 0
                            && State.Peek().Type != UsfmElementType.Table
                            && State.Peek().Type != UsfmElementType.Sidebar
                        )
                        {
                            CloseElement();
                        }

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
                        bool plusPrefix =
                            State.Stack.Count > 1 && State.Stack[State.Stack.Count - 2].Type == UsfmElementType.Char;

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
                        Handler?.Unmatched(State, token.Marker);
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
                    Handler?.StartBook(State, token.Marker, code);
                    break;
                case UsfmTokenType.Chapter:
                    // Get alternate chapter number
                    string altChapter = null;
                    string pubChapter = null;
                    if (
                        State.Index < State.Tokens.Count - 3
                        && State.Tokens[State.Index + 1].Marker == "ca"
                        && State.Tokens[State.Index + 2].Text != null
                        && State.Tokens[State.Index + 3].Marker == "ca*"
                    )
                    {
                        altChapter = State.Tokens[State.Index + 2].Text.Trim();
                        State.SpecialTokenCount += 3;

                        // Skip blank space after if present
                        if (
                            State.Index + State.SpecialTokenCount < State.Tokens.Count - 1
                            && State.Tokens[State.Index + State.SpecialTokenCount + 1].Text != null
                            && State.Tokens[State.Index + State.SpecialTokenCount + 1].Text.Trim().Length == 0
                        )
                        {
                            State.SpecialTokenCount++;
                        }
                    }

                    // Get publishable chapter number
                    if (
                        State.Index + State.SpecialTokenCount < State.Tokens.Count - 2
                        && State.Tokens[State.Index + State.SpecialTokenCount + 1].Marker == "cp"
                        && State.Tokens[State.Index + State.SpecialTokenCount + 2].Text != null
                    )
                    {
                        pubChapter = State.Tokens[State.Index + State.SpecialTokenCount + 2].Text.Trim();
                        State.SpecialTokenCount += 2;
                    }

                    // Chapter
                    vref = State.VerseRef;
                    vref.Chapter = token.Data;
                    vref.VerseNum = 0;
                    State.VerseRef = vref;
                    // Verse offset is not zeroed for chapter 1, as it is part of intro
                    if (State.VerseRef.ChapterNum != 1)
                        State.VerseOffset = 0;

                    Handler?.Chapter(State, token.Data, token.Marker, altChapter, pubChapter);
                    break;
                case UsfmTokenType.Verse:
                    string pubVerse = null;
                    string altVerse = null;
                    if (
                        State.Index < State.Tokens.Count - 3
                        && State.Tokens[State.Index + 1].Marker == "va"
                        && State.Tokens[State.Index + 2].Text != null
                        && State.Tokens[State.Index + 3].Marker == "va*"
                    )
                    {
                        // Get alternate verse number
                        altVerse = State.Tokens[State.Index + 2].Text.Trim();
                        State.SpecialTokenCount += 3;
                    }
                    if (
                        State.Index + State.SpecialTokenCount < State.Tokens.Count - 3
                        && State.Tokens[State.Index + State.SpecialTokenCount + 1].Marker == "vp"
                        && State.Tokens[State.Index + State.SpecialTokenCount + 2].Text != null
                        && State.Tokens[State.Index + State.SpecialTokenCount + 3].Marker == "vp*"
                    )
                    {
                        // Get publishable verse number
                        pubVerse = State.Tokens[State.Index + State.SpecialTokenCount + 2].Text.Trim();
                        State.SpecialTokenCount += 3;
                    }

                    // Verse
                    vref = State.VerseRef;
                    vref.Verse = token.Data;
                    State.VerseRef = vref;
                    State.VerseOffset = 0;

                    Handler?.Verse(State, token.Data, token.Marker, altVerse, pubVerse);
                    break;
                case UsfmTokenType.Paragraph:
                    // Handle special case of table rows
                    if (token.Marker == "tr")
                    {
                        // Start table if not open
                        if (State.Stack.All(e => e.Type != UsfmElementType.Table))
                        {
                            State.Push(new UsfmParserElement(UsfmElementType.Table, null));
                            Handler?.StartTable(State);
                        }

                        State.Push(new UsfmParserElement(UsfmElementType.Row, token.Marker));

                        // Row start
                        Handler?.StartRow(State, token.Marker);
                        break;
                    }

                    // Handle special case of sidebars
                    if (token.Marker == "esb")
                    {
                        State.Push(new UsfmParserElement(UsfmElementType.Sidebar, token.Marker));

                        // Look for category
                        string sidebarCategory = null;
                        if (
                            State.Index < State.Tokens.Count - 3
                            && State.Tokens[State.Index + 1].Marker == "cat"
                            && State.Tokens[State.Index + 2].Text != null
                            && State.Tokens[State.Index + 3].Marker == "cat*"
                        )
                        {
                            // Get category
                            sidebarCategory = State.Tokens[State.Index + 2].Text.Trim();
                            State.SpecialTokenCount += 3;
                        }

                        Handler?.StartSidebar(State, token.Marker, sidebarCategory);
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
                        else
                        {
                            Handler?.Unmatched(State, token.Marker);
                        }
                        break;
                    }

                    State.Push(new UsfmParserElement(UsfmElementType.Para, token.Marker));

                    // Paragraph opening
                    Handler?.StartPara(State, token.Marker, token.Type == UsfmTokenType.Unknown, token.Attributes);
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

                        Handler?.StartCell(State, baseMarker, align, colspan);
                        break;
                    }

                    if (IsRef(token))
                    {
                        // xrefs are special tokens (they do not stand alone)
                        State.SpecialToken = true;

                        ParseDisplayAndTarget(out string display, out string target);

                        State.SpecialTokenCount += 2;

                        Handler?.Ref(State, token.Marker, display, target);
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
                    {
                        actualMarker = token.Marker;
                    }

                    State.Push(new UsfmParserElement(UsfmElementType.Char, actualMarker, token.Attributes));
                    Handler?.StartChar(
                        State,
                        actualMarker,
                        token.Type == UsfmTokenType.Unknown || invalidMarker,
                        token.Attributes
                    );
                    break;
                case UsfmTokenType.Note:
                    // Look for category
                    string noteCategory = null;
                    if (
                        State.Index < State.Tokens.Count - 3
                        && State.Tokens[State.Index + 1].Marker == "cat"
                        && State.Tokens[State.Index + 2].Text != null
                        && State.Tokens[State.Index + 3].Marker == "cat*"
                    )
                    {
                        // Get category
                        noteCategory = State.Tokens[State.Index + 2].Text.Trim();
                        State.SpecialTokenCount += 3;
                    }

                    State.Push(new UsfmParserElement(UsfmElementType.Note, token.Marker));

                    Handler?.StartNote(State, token.Marker, token.Data, noteCategory);
                    break;
                case UsfmTokenType.Text:
                    string text = token.Text;

                    // If last token before a paragraph, book or chapter, esb, esbe (both are paragraph types),
                    // or at very end, strip final space
                    // This is because USFM requires these to be on a new line, therefore adding whitespace
                    if (
                        (
                            State.Index == State.Tokens.Count - 1
                            || State.Tokens[State.Index + 1].Type == UsfmTokenType.Paragraph
                            || State.Tokens[State.Index + 1].Type == UsfmTokenType.Book
                            || State.Tokens[State.Index + 1].Type == UsfmTokenType.Chapter
                        )
                        && text.Length > 0
                        && text[text.Length - 1] == ' '
                    )
                    {
                        text = text.Substring(0, text.Length - 1);
                    }

                    if (Handler != null)
                    {
                        // Replace ~ with nbsp
                        text = text.Replace('~', '\u00A0');

                        // Replace // with <optbreak/>
                        foreach (string str in OptBreakSplitter.Split(text))
                        {
                            if (str == "//")
                                Handler.OptBreak(State);
                            else
                                Handler.Text(State, str);
                        }
                    }
                    break;

                case UsfmTokenType.Milestone:
                case UsfmTokenType.MilestoneEnd:
                    // currently, parse state doesn't need to be update, so just inform the handler about the milestone.
                    Handler?.Milestone(State, token.Marker, token.Type == UsfmTokenType.Milestone, token.Attributes);
                    break;
            }

            return true;
        }

        private void ParseDisplayAndTarget(out string display, out string target)
        {
            display = State.Tokens[State.Index + 1].Text.Substring(0, State.Tokens[State.Index + 1].Text.IndexOf('|'));
            target = State.Tokens[State.Index + 1].Text.Substring(State.Tokens[State.Index + 1].Text.IndexOf('|') + 1);
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
                    Handler?.EndBook(State, element.Marker);
                    break;
                case UsfmElementType.Para:
                    Handler?.EndPara(State, element.Marker);
                    break;
                case UsfmElementType.Char:
                    Handler?.EndChar(State, element.Marker, element.Attributes, closed);
                    break;
                case UsfmElementType.Note:
                    Handler?.EndNote(State, element.Marker, closed);
                    break;
                case UsfmElementType.Table:
                    Handler?.EndTable(State);
                    break;
                case UsfmElementType.Row:
                    Handler?.EndRow(State, element.Marker);
                    break;
                case UsfmElementType.Cell:
                    Handler?.EndCell(State, element.Marker);
                    break;
                case UsfmElementType.Sidebar:
                    Handler?.EndSidebar(State, element.Marker, closed);
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
