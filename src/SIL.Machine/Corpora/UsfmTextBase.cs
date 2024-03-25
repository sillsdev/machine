using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.Machine.Utils;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public abstract class UsfmTextBase : ScriptureText
    {
        private readonly UsfmStylesheet _stylesheet;
        private readonly Encoding _encoding;
        private readonly bool _includeMarkers;
        private readonly bool _includeAllText;

        protected UsfmTextBase(
            string id,
            UsfmStylesheet stylesheet,
            Encoding encoding,
            ScrVers versification,
            bool includeMarkers,
            bool includeAllText
        )
            : base(id, versification)
        {
            _stylesheet = stylesheet;
            _encoding = encoding;
            _includeMarkers = includeMarkers;
            _includeAllText = includeAllText;
        }

        protected override IEnumerable<TextRow> GetVersesInDocOrder()
        {
            string usfm = ReadUsfm();
            var rowCollector = new TextRowCollector(this);
            UsfmParser.Parse(usfm, rowCollector, _stylesheet, Versification, preserveWhitespace: _includeMarkers);
            return rowCollector.Rows;
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

        private class TextRowCollector : UsfmParserHandlerBase
        {
            private readonly UsfmTextBase _text;
            private readonly List<TextRow> _rows;
            private VerseRef _verseRef;
            private readonly StringBuilder _verseText;
            private readonly StringBuilder _nonVerseText;
            private bool _sentenceStart;
            private readonly List<UsfmToken> _nextParaTokens;
            private bool _nextParaTextStarted = false;
            private bool _inNonVerse = false;
            private readonly List<int> _positions;

            public TextRowCollector(UsfmTextBase text)
            {
                _text = text;
                _rows = new List<TextRow>();
                _verseText = new StringBuilder();
                _nonVerseText = new StringBuilder();
                _nextParaTokens = new List<UsfmToken>();
                _positions = new List<int> { 0 };
            }

            public IEnumerable<TextRow> Rows => _rows;

            public override void Chapter(
                UsfmParserState state,
                string number,
                string marker,
                string altNumber,
                string pubNumber
            )
            {
                VerseCompleted(nextSentenceStart: true);
                _verseRef = default;
            }

            public override void Verse(
                UsfmParserState state,
                string number,
                string marker,
                string altNumber,
                string pubNumber
            )
            {
                if (_verseRef.IsDefault)
                {
                    _verseRef = state.VerseRef;
                }
                else if (state.VerseRef.Equals(_verseRef))
                {
                    VerseCompleted();

                    // ignore duplicate verse
                    _verseRef = default;
                }
                else if (VerseRef.AreOverlappingVersesRanges(number, _verseRef.Verse))
                {
                    // merge overlapping verse ranges in to one range
                    _verseRef.Verse = CorporaUtils.MergeVerseRanges(number, _verseRef.Verse);
                }
                else
                {
                    VerseCompleted();
                    _verseRef = state.VerseRef;
                }
                _nextParaTextStarted = true;
                _nextParaTokens.Clear();
            }

            public override void StartPara(
                UsfmParserState state,
                string marker,
                bool unknown,
                IReadOnlyList<UsfmAttribute> attributes
            )
            {
                HandlePara(state);
            }

            public override void StartRow(UsfmParserState state, string marker)
            {
                HandlePara(state);
            }

            public override void StartSidebar(UsfmParserState state, string marker, string category)
            {
                _positions[_positions.Count - 1]++;
                _positions.Add(0);
            }

            public override void EndSidebar(UsfmParserState state, string marker, bool closed)
            {
                _positions.RemoveAt(_positions.Count - 1);
            }

            public override void EndPara(UsfmParserState state, string marker)
            {
                if (_verseRef.IsDefault || !_text._includeAllText || !_inNonVerse)
                    return;

                string text = _nonVerseText.ToString();
                _rows.Add(
                    _text.CreateRow(
                        _verseRef.AllVerses().Last(),
                        _positions.Zip(state.Stack.Select(e => e.Marker).Concat(marker), (p, m) => (p, m)),
                        text,
                        _sentenceStart
                    )
                );
                _nonVerseText.Clear();
                _inNonVerse = false;
            }

            public override void StartCell(UsfmParserState state, string marker, string align, int colspan)
            {
                if (_verseRef.IsDefault)
                    return;

                if (_text._includeMarkers)
                {
                    OutputMarker(state);
                }
                else
                {
                    if (_verseText.Length > 0 && !char.IsWhiteSpace(_verseText[_verseText.Length - 1]))
                        _verseText.Append(" ");
                }
            }

            public override void Ref(UsfmParserState state, string marker, string display, string target)
            {
                OutputMarker(state);
            }

            public override void StartChar(
                UsfmParserState state,
                string markerWithoutPlus,
                bool unknown,
                IReadOnlyList<UsfmAttribute> attributes
            )
            {
                OutputMarker(state);
            }

            public override void EndChar(
                UsfmParserState state,
                string marker,
                IReadOnlyList<UsfmAttribute> attributes,
                bool closed
            )
            {
                if (_text._includeMarkers && attributes != null && state.PrevToken?.Type == UsfmTokenType.Attribute)
                    _verseText.Append(state.PrevToken);

                if (closed)
                    OutputMarker(state);
                if (!_text._includeMarkers && marker == "rq")
                    _verseText.TrimEnd();
            }

            public override void StartNote(UsfmParserState state, string marker, string caller, string category)
            {
                OutputMarker(state);
            }

            public override void EndNote(UsfmParserState state, string marker, bool closed)
            {
                if (closed)
                    OutputMarker(state);
            }

            public override void OptBreak(UsfmParserState state)
            {
                if (!_text._includeMarkers)
                    _verseText.TrimEnd();
            }

            public override void Text(UsfmParserState state, string text)
            {
                if (_verseRef.IsDefault)
                    return;

                if (state.IsVersePara)
                {
                    if (_text._includeMarkers)
                    {
                        text = text.TrimEnd('\r', '\n');
                        if (text.Length > 0 && !state.Stack.Any(e => e.Type == UsfmElementType.Sidebar))
                        {
                            if (!text.IsWhiteSpace())
                            {
                                foreach (UsfmToken token in _nextParaTokens)
                                    _verseText.Append(token);
                                _nextParaTokens.Clear();
                                _nextParaTextStarted = true;
                            }
                            if (_verseText.Length == 0 || char.IsWhiteSpace(_verseText[_verseText.Length - 1]))
                            {
                                text = text.TrimStart();
                            }
                            _verseText.Append(text);
                        }
                    }
                    else if (state.IsVerseText && text.Length > 0)
                    {
                        if (
                            state.PrevToken?.Type == UsfmTokenType.End
                            && (_verseText.Length == 0 || char.IsWhiteSpace(_verseText[_verseText.Length - 1]))
                        )
                        {
                            text = text.TrimStart();
                        }
                        _verseText.Append(text);
                    }
                }

                if (_text._includeAllText && _inNonVerse && text.Length > 0)
                {
                    if (
                        state.PrevToken?.Type == UsfmTokenType.End
                        && (_verseText.Length == 0 || char.IsWhiteSpace(_verseText[_verseText.Length - 1]))
                    )
                    {
                        text = text.TrimStart();
                    }
                    _nonVerseText.Append(text);
                }
            }

            public override void EndUsfm(UsfmParserState state)
            {
                VerseCompleted();
            }

            private void OutputMarker(UsfmParserState state)
            {
                if (_verseRef.IsDefault || !_text._includeMarkers)
                    return;

                if (_nextParaTextStarted)
                    _verseText.Append(state.Token);
                else
                    _nextParaTokens.Add(state.Token);
            }

            private void VerseCompleted(bool? nextSentenceStart = null)
            {
                if (_verseRef.VerseNum == 0)
                    return;

                string text = _verseText.ToString();
                _rows.AddRange(_text.CreateRows(_verseRef, text, _sentenceStart));
                _sentenceStart = nextSentenceStart ?? text.HasSentenceEnding();
                _verseText.Clear();
                _positions.Clear();
                _positions.Add(0);
            }

            private void HandlePara(UsfmParserState state)
            {
                if (_verseRef.VerseNum != 0)
                {
                    if (_verseText.Length > 0 && !char.IsWhiteSpace(_verseText[_verseText.Length - 1]))
                        _verseText.Append(" ");
                    _nextParaTokens.Add(state.Token);
                    _nextParaTextStarted = false;
                }

                if (!state.IsVerseText && _text._includeAllText)
                {
                    if (_verseRef.IsDefault)
                        _verseRef = state.VerseRef;
                    _positions[_positions.Count - 1]++;
                    _inNonVerse = true;
                    _sentenceStart = true;
                }
            }
        }
    }
}
