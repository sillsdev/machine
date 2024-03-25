using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public enum ScriptureTextType
    {
        NonVerse,
        Verse,
        Note
    }

    public abstract class ScriptureRefUsfmParserHandlerBase : UsfmParserHandlerBase
    {
        private VerseRef _verseRef;
        private readonly Stack<ScriptureElement> _elements;
        private readonly Stack<ScriptureTextType> _curTextType;
        private bool _duplicateVerse = false;

        protected ScriptureRefUsfmParserHandlerBase()
        {
            _elements = new Stack<ScriptureElement>();
            _curTextType = new Stack<ScriptureTextType>();
        }

        protected ScriptureTextType CurrentTextType =>
            _curTextType.Count == 0 ? ScriptureTextType.NonVerse : _curTextType.Peek();

        public override void EndUsfm(UsfmParserState state)
        {
            if (_verseRef.VerseNum != 0)
            {
                EndVerseText(state, CreateVerseRefs());
                _curTextType.Pop();
            }
        }

        public override void Chapter(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            if (_verseRef.VerseNum != 0)
            {
                EndVerseText(state, CreateVerseRefs());
                _curTextType.Pop();
            }
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
            if (state.VerseRef.Equals(_verseRef))
            {
                if (!_duplicateVerse && _verseRef.VerseNum != 0)
                {
                    EndVerseText(state, CreateVerseRefs());
                    _curTextType.Pop();
                }
                _duplicateVerse = true;
                UpdateVerseRef(state.VerseRef, marker);
            }
            else if (VerseRef.AreOverlappingVersesRanges(number, _verseRef.Verse))
            {
                // merge overlapping verse ranges in to one range
                VerseRef verseRef = _verseRef.Clone();
                verseRef.Verse = CorporaUtils.MergeVerseRanges(number, _verseRef.Verse);
                UpdateVerseRef(verseRef, marker);
            }
            else
            {
                if (!_duplicateVerse && _verseRef.VerseNum != 0)
                {
                    EndVerseText(state, CreateVerseRefs());
                    _curTextType.Pop();
                }
                _duplicateVerse = false;
                UpdateVerseRef(state.VerseRef, marker);
                _curTextType.Push(ScriptureTextType.Verse);
                StartVerseText(state, CreateVerseRefs());
            }
        }

        public override void StartPara(
            UsfmParserState state,
            string marker,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            if (_verseRef.IsDefault)
                UpdateVerseRef(state.VerseRef, marker);

            if (!state.IsVerseText)
            {
                StartParentElement(marker);
                _curTextType.Push(ScriptureTextType.NonVerse);
                StartNonVerseText(state, CreateNonVerseRef());
            }
        }

        public override void EndPara(UsfmParserState state, string marker)
        {
            if (CurrentTextType == ScriptureTextType.NonVerse)
            {
                EndParentElement();
                EndNonVerseText(state, CreateNonVerseRef());
                _curTextType.Pop();
            }
        }

        public override void StartRow(UsfmParserState state, string marker)
        {
            if (CurrentTextType == ScriptureTextType.NonVerse)
                StartParentElement(marker);
        }

        public override void EndRow(UsfmParserState state, string marker)
        {
            if (CurrentTextType == ScriptureTextType.NonVerse)
                EndParentElement();
        }

        public override void StartCell(UsfmParserState state, string marker, string align, int colspan)
        {
            if (CurrentTextType == ScriptureTextType.NonVerse)
            {
                StartParentElement(marker);
                _curTextType.Push(ScriptureTextType.NonVerse);
                StartNonVerseText(state, CreateNonVerseRef());
            }
        }

        public override void EndCell(UsfmParserState state, string marker)
        {
            if (CurrentTextType == ScriptureTextType.NonVerse)
            {
                EndParentElement();
                EndNonVerseText(state, CreateNonVerseRef());
                _curTextType.Pop();
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
            NextElement(marker);
            _curTextType.Push(ScriptureTextType.Note);
            StartNoteText(state, CreateNonVerseRef());
        }

        public override void EndNote(UsfmParserState state, string marker, bool closed)
        {
            EndNoteText(state, CreateNonVerseRef());
            _curTextType.Pop();
        }

        public override void Ref(UsfmParserState state, string marker, string display, string target) { }

        protected virtual void StartVerseText(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs) { }

        protected virtual void EndVerseText(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs) { }

        protected virtual void StartNonVerseText(UsfmParserState state, ScriptureRef scriptureRef) { }

        protected virtual void EndNonVerseText(UsfmParserState state, ScriptureRef scriptureRef) { }

        protected virtual void StartNoteText(UsfmParserState state, ScriptureRef scriptureRef) { }

        protected virtual void EndNoteText(UsfmParserState state, ScriptureRef scriptureRef) { }

        private void UpdateVerseRef(VerseRef verseRef, string marker)
        {
            if (!VerseRef.AreOverlappingVersesRanges(verseRef, _verseRef))
            {
                _elements.Clear();
                _elements.Push(new ScriptureElement(0, marker));
            }
            _verseRef = verseRef;
        }

        private void NextElement(string marker)
        {
            ScriptureElement prevElem = _elements.Pop();
            _elements.Push(new ScriptureElement(prevElem.Position + 1, marker));
        }

        private void StartParentElement(string marker)
        {
            NextElement(marker);
            _elements.Push(new ScriptureElement(0, marker));
        }

        private void EndParentElement()
        {
            _elements.Pop();
        }

        private IReadOnlyList<ScriptureRef> CreateVerseRefs()
        {
            return _verseRef.HasMultiple
                ? _verseRef.AllVerses().Select(v => new ScriptureRef(v)).ToArray()
                : new[] { new ScriptureRef(_verseRef) };
        }

        private ScriptureRef CreateNonVerseRef()
        {
            return new ScriptureRef(
                _verseRef.HasMultiple ? _verseRef.AllVerses().Last() : _verseRef,
                _elements.Where(e => e.Position > 0).Reverse()
            );
        }
    }
}
