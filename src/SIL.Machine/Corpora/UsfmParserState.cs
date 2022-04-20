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
			VerseRef = new VerseRef(versification ?? ScrVers.English);
			VerseOffset = 0;
			Tokens = tokens;
		}

		private UsfmParserState(UsfmParserState state)
		{
			Stylesheet = state.Stylesheet;
			Index = state.Index;
			_stack = new List<UsfmParserElement>(state._stack);
			VerseRef = state.VerseRef;
			VerseOffset = state.VerseOffset;
			Tokens = state.Tokens;
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
		public VerseRef VerseRef { get; internal set; }

		/// <summary>
		/// Offset of start of token in verse
		/// </summary>
		public int VerseOffset { get; internal set; }

		/// <summary>
		/// True if the token processed is part of a special indivisible group 
		/// of tokens (link or chapter/verse alternate/publishable)
		/// </summary>
		public bool SpecialToken { get; internal set; }

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
					e.Type == UsfmElementTypes.Para ||
					e.Type == UsfmElementTypes.Book ||
					e.Type == UsfmElementTypes.Row ||
					e.Type == UsfmElementTypes.Sidebar);
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
				UsfmParserElement elem = Stack.LastOrDefault(e => e.Type == UsfmElementTypes.Note);
				return (elem != null) ? Stylesheet.GetTag(elem.Marker) : null;
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
					if (Stack[i].Type == UsfmElementTypes.Char)
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
				// Sidebars and notes are not verse text 
				if (_stack.Any(e => e.Type == UsfmElementTypes.Sidebar || e.Type == UsfmElementTypes.Note))
					return false;


				if (!IsVersePara)
					return false;

				// All character tags must be verse text
				foreach (UsfmTag charTag in CharTags)
				{
					// Not specified text type is verse text
					if (charTag.TextType != UsfmTextType.VerseText && charTag.TextType != 0)
						return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Determines if text tokens in the current state are publishable
		/// </summary>
		public bool IsPublishable
		{
			get
			{
				// Special tokens not publishable
				if (SpecialToken)
					return false;

				// Non-paragraphs or unknown paragraphs are publishable
				if (ParaTag != null)
				{
					if ((ParaTag.TextProperties & UsfmTextProperties.Nonpublishable) > 0)
						return false;
				}

				if (CharTags.Any(charTag => (charTag.TextProperties & UsfmTextProperties.Nonpublishable) > 0))
					return false;
				return !IsSpecialText;
			}
		}

		/// <summary>
		/// Determines if text tokens in the current state are publishable vernacular
		/// </summary>
		public bool IsPublishableVernacular
		{
			get
			{
				// Non-paragraphs or unknown paragraphs are publishable
				if (ParaTag != null)
				{
					if ((ParaTag.TextProperties & UsfmTextProperties.Nonpublishable) > 0)
						return false;
					if ((ParaTag.TextProperties & UsfmTextProperties.Nonvernacular) > 0)
						return false;
				}
				if (CharTag != null)
				{
					if ((CharTag.TextProperties & UsfmTextProperties.Nonpublishable) > 0)
						return false;
					if ((CharTag.TextProperties & UsfmTextProperties.Nonvernacular) > 0)
						return false;
				}
				return !IsSpecialText;
			}
		}

		/// <summary>
		/// Determines if text is special text like links and figures that are not in the 
		/// vernacular.
		/// </summary>
		public bool IsSpecialText
		{
			get { return SpecialToken; }
		}

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

		internal UsfmParserState Clone()
		{
			return new UsfmParserState(this);
		}
	}

	/// <summary>
	/// Element types on the stack
	/// </summary>
	public enum UsfmElementTypes
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
		public readonly UsfmElementTypes Type;
		public readonly string Marker;
		public IReadOnlyList<UsfmAttribute> Attributes;
		public bool IsClosed;

		public UsfmParserElement(UsfmElementTypes type, string marker, IReadOnlyList<UsfmAttribute> attributes = null)
		{
			Type = type;
			Marker = marker;
			Attributes = attributes;
		}

		public override bool Equals(object obj)
		{
			return obj is UsfmParserElement elm && elm.Type == Type && elm.Marker == Marker;
		}

		public override int GetHashCode()
		{
			return Marker.GetHashCode();
		}
	}
}
