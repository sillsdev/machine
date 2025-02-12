using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public enum ScriptureTextType
    {
        None,
        NonVerse,
        Verse,
        Embed,
        NoteText
    }

    public abstract class ScriptureRefUsfmParserHandlerBase : UsfmParserHandlerBase
    {
        private VerseRef _curVerseRef;
        private readonly Stack<ScriptureElement> _curElements;
        private readonly Stack<ScriptureTextType> _curTextType;
        private bool _duplicateVerse = false;

        private bool _inEmbed;
        public bool InNoteText { get; private set; }
        private bool _inNestedEmbed;

        protected ScriptureRefUsfmParserHandlerBase()
        {
            _curElements = new Stack<ScriptureElement>();
            _curTextType = new Stack<ScriptureTextType>();
        }

        protected ScriptureTextType CurrentTextType =>
            _curTextType.Count == 0 ? ScriptureTextType.None : _curTextType.Peek();

        private static readonly string[] EmbedStyles = new[] { "f", "fe", "fig", "fm", "x" };
        private static readonly char[] EmbedPartStartCharStyles = new[] { 'f', 'x', 'z' };

        public override void EndUsfm(UsfmParserState state)
        {
            EndVerseText(state);
        }

        public override void Chapter(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            EndVerseText(state);
            UpdateVerseRef(state.VerseRef, marker);
        }

        public override void Verse(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            if (state.VerseRef.Equals(_curVerseRef) && !_duplicateVerse)
            {
                EndVerseText(state, CreateVerseRefs());
                // ignore duplicate verses
                _duplicateVerse = true;
            }
            else if (VerseRef.AreOverlappingVersesRanges(verse1: number, verse2: _curVerseRef.Verse))
            {
                // merge overlapping verse ranges in to one range
                VerseRef verseRef = _curVerseRef.Clone();
                verseRef.Verse = CorporaUtils.MergeVerseRanges(number, _curVerseRef.Verse);
                UpdateVerseRef(verseRef, marker);
            }
            else
            {
                if (CurrentTextType == ScriptureTextType.NonVerse)
                    EndNonVerseText(state);
                else
                    EndVerseText(state);
                UpdateVerseRef(state.VerseRef, marker);
                StartVerseText(state);
            }
        }

        public override void StartPara(
            UsfmParserState state,
            string marker,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            if (_curVerseRef.IsDefault)
                UpdateVerseRef(state.VerseRef, marker);

            if (!state.IsVerseText)
            {
                StartParentElement(marker);
                StartNonVerseText(state);
            }
        }

        public override void EndPara(UsfmParserState state, string marker)
        {
            if (CurrentTextType == ScriptureTextType.NonVerse)
            {
                EndParentElement();
                EndNonVerseText(state);
            }
            else if (CurrentTextType == ScriptureTextType.None)
            {
                // empty verse paragraph
                StartParentElement(marker);
                StartNonVerseText(state);
                EndParentElement();
                EndNonVerseText(state);
            }
        }

        public override void StartRow(UsfmParserState state, string marker)
        {
            if (CurrentTextType == ScriptureTextType.NonVerse || CurrentTextType == ScriptureTextType.None)
                StartParentElement(marker);
        }

        public override void EndRow(UsfmParserState state, string marker)
        {
            if (CurrentTextType == ScriptureTextType.NonVerse || CurrentTextType == ScriptureTextType.None)
                EndParentElement();
        }

        public override void StartCell(UsfmParserState state, string marker, string align, int colspan)
        {
            if (CurrentTextType == ScriptureTextType.NonVerse || CurrentTextType == ScriptureTextType.None)
            {
                StartParentElement(marker);
                StartNonVerseText(state);
            }
        }

        public override void EndCell(UsfmParserState state, string marker)
        {
            if (CurrentTextType == ScriptureTextType.NonVerse)
            {
                EndParentElement();
                EndNonVerseText(state);
            }
        }

        public override void StartSidebar(UsfmParserState state, string marker, string category)
        {
            StartParentElement(marker);
        }

        public override void EndSidebar(UsfmParserState state, string marker, bool closed)
        {
            EndParentElement();
        }

        public override void StartNote(UsfmParserState state, string marker, string caller, string category)
        {
            _inEmbed = true;
            StartEmbed(state, marker);
        }

        public override void EndNote(UsfmParserState state, string marker, bool closed)
        {
            EndNoteText(state);
            EndEmbed(state, marker, null, closed);
            _inEmbed = false;
        }

        public void StartEmbed(UsfmParserState state, string marker)
        {
            if (_curVerseRef.IsDefault)
                UpdateVerseRef(state.VerseRef, marker);

            if (!_duplicateVerse)
            {
                // if we hit a note in a verse paragraph and we aren't in a verse, then start a non-verse segment
                CheckConvertVerseParaToNonVerse(state);
                NextElement(marker);
            }

            StartEmbed(state, CreateNonVerseRef());
        }

        public virtual void StartEmbed(UsfmParserState state, ScriptureRef scriptureRef) { }

        public virtual void EndEmbed(
            UsfmParserState state,
            string marker,
            IReadOnlyList<UsfmAttribute> attributes,
            bool closed
        ) { }

        public override void Text(UsfmParserState state, string text)
        {
            // if we hit text in a verse paragraph and we aren't in a verse, then start a non-verse segment
            if (text.Trim().Length > 0)
                CheckConvertVerseParaToNonVerse(state);
        }

        public override void OptBreak(UsfmParserState state)
        {
            CheckConvertVerseParaToNonVerse(state);
        }

        public override void StartChar(
            UsfmParserState state,
            string markerWithoutPlus,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            if (IsEmbedPartStyle(markerWithoutPlus) & InNoteText)
                _inNestedEmbed = true;

            // if we hit a character marker in a verse paragraph and we aren't in a verse, then start a non-verse
            // segment
            CheckConvertVerseParaToNonVerse(state);

            if (IsEmbedStyle(markerWithoutPlus))
            {
                _inEmbed = true;
                StartEmbed(state, markerWithoutPlus);
            }

            if (IsNoteText(markerWithoutPlus))
            {
                StartNoteTextWrapper(state);
            }
        }

        public override void EndChar(
            UsfmParserState state,
            string marker,
            IReadOnlyList<UsfmAttribute> attributes,
            bool closed
        )
        {
            if (IsEmbedPartStyle(marker))
            {
                if (_inNestedEmbed)
                {
                    _inNestedEmbed = false;
                }
                else
                {
                    EndNoteText(state);
                }
            }
            if (IsEmbedStyle(marker))
            {
                EndEmbed(state, marker, attributes, closed);
                _inEmbed = false;
            }
        }

        protected virtual void StartVerseText(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs) { }

        protected virtual void EndVerseText(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs) { }

        protected virtual void StartNonVerseText(UsfmParserState state, ScriptureRef scriptureRef) { }

        protected virtual void EndNonVerseText(UsfmParserState state, ScriptureRef scriptureRef) { }

        public virtual void StartNoteTextWrapper(UsfmParserState state)
        {
            InNoteText = true;
            _curTextType.Push(ScriptureTextType.NoteText);
            StartNoteText(state);
        }

        protected virtual void StartNoteText(UsfmParserState state) { }

        public virtual void EndNoteText(UsfmParserState state)
        {
            if (_curTextType.Count > 0 && _curTextType.Peek() == ScriptureTextType.NoteText)
            {
                EndNoteText(state, CreateNonVerseRef());
                _curTextType.Pop();
                InNoteText = false;
            }
        }

        protected virtual void EndNoteText(UsfmParserState state, ScriptureRef scriptureRef) { }

        private void StartVerseText(UsfmParserState state)
        {
            _duplicateVerse = false;
            _curTextType.Push(ScriptureTextType.Verse);
            StartVerseText(state, CreateVerseRefs());
        }

        private void EndVerseText(UsfmParserState state)
        {
            if (!_duplicateVerse && _curVerseRef.VerseNum > 0)
                EndVerseText(state, CreateVerseRefs());
            if (_curVerseRef.VerseNum > 0)
                _curTextType.Pop();
        }

        private void StartNonVerseText(UsfmParserState state)
        {
            _curTextType.Push(ScriptureTextType.NonVerse);
            StartNonVerseText(state, CreateNonVerseRef());
        }

        private void EndNonVerseText(UsfmParserState state)
        {
            EndEmbedElements();
            EndNonVerseText(state, CreateNonVerseRef());
            _curTextType.Pop();
        }

        private void UpdateVerseRef(VerseRef verseRef, string marker)
        {
            if (!VerseRef.AreOverlappingVersesRanges(verseRef, _curVerseRef))
            {
                _curElements.Clear();
                _curElements.Push(new ScriptureElement(0, marker));
            }
            _curVerseRef = verseRef;
        }

        private void NextElement(string marker)
        {
            ScriptureElement prevElem = _curElements.Pop();
            _curElements.Push(new ScriptureElement(prevElem.Position + 1, marker));
        }

        private void StartParentElement(string marker)
        {
            NextElement(marker);
            _curElements.Push(new ScriptureElement(0, marker));
        }

        private void EndParentElement()
        {
            _curElements.Pop();
        }

        private void EndEmbedElements()
        {
            if (_curElements.Count > 0 && IsEmbedStyle(_curElements.Peek().Name))
                _curElements.Pop();
        }

        private IReadOnlyList<ScriptureRef> CreateVerseRefs()
        {
            return _curVerseRef.HasMultiple
                ? _curVerseRef.AllVerses().Select(v => new ScriptureRef(v)).ToArray()
                : new[] { new ScriptureRef(_curVerseRef) };
        }

        private ScriptureRef CreateNonVerseRef()
        {
            return new ScriptureRef(
                _curVerseRef.HasMultiple ? _curVerseRef.AllVerses().Last() : _curVerseRef,
                _curElements.Where(e => e.Position > 0).Reverse()
            );
        }

        private void CheckConvertVerseParaToNonVerse(UsfmParserState state)
        {
            UsfmTag paraTag = state.ParaTag;
            if (
                CurrentTextType == ScriptureTextType.None
                && paraTag != null
                && paraTag.Marker != "tr"
                && state.IsVersePara
                && _curVerseRef.VerseNum == 0
            )
            {
                StartParentElement(paraTag.Marker);
                StartNonVerseText(state);
            }
        }

        public bool InEmbed(string marker)
        {
            return _inEmbed || IsEmbedStyle(marker);
        }

        public bool IsInNestedEmbed(string marker)
        {
            return _inNestedEmbed
                || (
                    !(marker is null)
                    && marker.StartsWith("+")
                    && marker.Length > 1
                    && IsEmbedPartStyle(marker.Substring(1))
                );
        }

        private static bool IsNoteText(string marker)
        {
            return marker == "ft";
        }

        public static bool IsEmbedPartStyle(string marker)
        {
            return !(marker is null) && marker.Length > 0 && marker[0].IsOneOf(EmbedPartStartCharStyles);
        }

        private static bool IsEmbedStyle(string marker)
        {
            return !(marker is null) && marker.IsOneOf(EmbedStyles);
        }
    }
}
