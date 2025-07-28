using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    /// <summary>
    /// Class for maintaining the state when parsing scripture.
    /// Maintains the current verse reference, paragraph, character and note styles.
    /// Note that book of verse reference is not updated unless blank
    /// </summary>
    public class UsfmParserState
    {
        private readonly List<UsfmParserElement> _stack;

        public UsfmParserState(UsfmStylesheet stylesheet, ScrVers versification, IReadOnlyList<UsfmToken> tokens)
        {
            Stylesheet = stylesheet;
            _stack = new List<UsfmParserElement>();
            VerseRef = new VerseRef(versification);
            VerseOffset = 0;
            LineNumber = 1;
            ColumnNumber = 0;
            Tokens = tokens;
        }

        public UsfmStylesheet Stylesheet { get; }

        /// <summary>
        /// USFM tokens
        /// </summary>
        public IReadOnlyList<UsfmToken> Tokens { get; }

        /// <summary>
        /// Returns index of current token
        /// </summary>
        public int Index { get; internal set; } = -1;

        /// <summary>
        /// Returns current token
        /// </summary>
        public UsfmToken Token => Index >= 0 ? Tokens[Index] : null;

        /// <summary>
        /// Returns previous token
        /// </summary>
        public UsfmToken PrevToken => Index >= 1 ? Tokens[Index - 1] : null;

        /// <summary>
        /// Stack of elements that are open
        /// </summary>
        public IReadOnlyList<UsfmParserElement> Stack => _stack;

        /// <summary>
        /// Current verse reference
        /// </summary>
        public VerseRef VerseRef { get; protected internal set; }

        /// <summary>
        /// Offset of start of token in verse
        /// </summary>
        public int VerseOffset { get; internal set; }

        public int LineNumber { get; internal set; }
        public int ColumnNumber { get; internal set; }

        /// <summary>
        /// True if the token processed is part of a special indivisible group
        /// of tokens (link or chapter/verse alternate/publishable)
        /// </summary>
        public bool SpecialToken { get; internal set; }

        /// <summary>
        /// Number of tokens to skip over because have been processed in advance
        /// (i.e. for figures which are three tokens, or links, or chapter/verse alternates)
        /// </summary>
        public int SpecialTokenCount { get; internal set; }

        /// <summary>
        /// True if the token processed is a figure.
        /// </summary>
        public bool IsFigure => CharTag?.Marker == "fig";

        /// <summary>
        /// Current paragraph tag or null for none.
        /// Note that book and table rows are considered paragraphs for legacy checking reasons.
        /// </summary>
        public UsfmTag ParaTag
        {
            get
            {
                UsfmParserElement elem = _stack.LastOrDefault(e =>
                    e.Type == UsfmElementType.Para
                    || e.Type == UsfmElementType.Book
                    || e.Type == UsfmElementType.Row
                    || e.Type == UsfmElementType.Sidebar
                );
                if (elem != null)
                    return Stylesheet.GetTag(elem.Marker);
                return null;
            }
        }

        /// <summary>
        /// Innermost character tag or null for none
        /// </summary>
        public UsfmTag CharTag
        {
            get { return CharTags.FirstOrDefault(); }
        }

        /// <summary>
        /// Current note tag or null for none
        /// </summary>
        public UsfmTag NoteTag
        {
            get
            {
                UsfmParserElement elem = Stack.LastOrDefault(e => e.Type == UsfmElementType.Note);
                return elem != null ? Stylesheet.GetTag(elem.Marker) : null;
            }
        }

        /// <summary>
        /// Character tags, starting with innermost
        /// </summary>
        public IEnumerable<UsfmTag> CharTags
        {
            get
            {
                for (int i = Stack.Count - 1; i >= 0; i--)
                {
                    if (Stack[i].Type == UsfmElementType.Char)
                        yield return Stylesheet.GetTag(Stack[i].Marker);
                    else
                        break;
                }
            }
        }

        public bool IsVersePara
        {
            get
            {
                // If the user enters no markers except just \c and \v we want the text to be
                // considered verse text. This is covered by the empty stack that makes ParaTag null.
                // Not specified text type is verse text
                UsfmTag paraTag = ParaTag;
                return paraTag == null || paraTag.TextType == UsfmTextType.VerseText || paraTag.TextType == 0;
            }
        }

        /// <summary>
        /// Determines if text tokens in the current state are verse text
        /// </summary>
        public bool IsVerseText
        {
            get
            {
                // Anything before verse 1 is not verse text
                if (VerseRef.VerseNum == 0)
                    return false;

                // Sidebars and notes are not verse text
                if (_stack.Any(e => e.Type == UsfmElementType.Sidebar || e.Type == UsfmElementType.Note))
                    return false;

                if (!IsVersePara)
                    return false;

                // All character tags must be verse text
                foreach (UsfmTag charTag in CharTags)
                {
                    // Not specified text type is verse text
                    if (charTag.TextType != UsfmTextType.VerseText && charTag.TextType != UsfmTextType.NotSpecified)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Determines if text is special text like links and figures that are not in the vernacular.
        /// </summary>
        public bool IsSpecialText => SpecialToken;

        internal UsfmParserElement Peek()
        {
            return _stack[_stack.Count - 1];
        }

        internal void Push(UsfmParserElement elem)
        {
            _stack.Add(elem);
        }

        internal UsfmParserElement Pop()
        {
            UsfmParserElement element = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            return element;
        }
    }

    /// <summary>
    /// Element types on the stack
    /// </summary>
    public enum UsfmElementType
    {
        Book,
        Para,
        Char,
        Table,
        Row,
        Cell,
        Note,
        Sidebar
    };

    /// <summary>
    /// Element that can be on the parser stack
    /// </summary>
    public sealed class UsfmParserElement
    {
        public UsfmParserElement(UsfmElementType type, string marker, IReadOnlyList<UsfmAttribute> attributes = null)
        {
            Type = type;
            Marker = marker;
            Attributes = attributes;
        }

        public UsfmElementType Type { get; }
        public string Marker { get; }
        public IReadOnlyList<UsfmAttribute> Attributes { get; set; }
    }
}
