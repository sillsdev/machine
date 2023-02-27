using System.Collections.Generic;
using System.IO;
using System.Text;
using SIL.Machine.Utils;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public abstract class UsfmTextBase : ScriptureText
    {
        private readonly UsfmStylesheet _stylesheet;
        private readonly Encoding _encoding;
        private readonly bool _includeMarkers;

        protected UsfmTextBase(
            string id,
            UsfmStylesheet stylesheet,
            Encoding encoding,
            ScrVers versification,
            bool includeMarkers
        )
            : base(id, versification)
        {
            _stylesheet = stylesheet;
            _encoding = encoding;
            _includeMarkers = includeMarkers;
        }

        protected override IEnumerable<TextRow> GetVersesInDocOrder()
        {
            string usfm = ReadUsfm();
            var rowCollector = new TextRowCollector(this);
            UsfmParser.Parse(_stylesheet, usfm, rowCollector, Versification, preserveWhitespace: _includeMarkers);
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
            private bool _sentenceStart;
            private readonly List<UsfmToken> _nextParaTokens;
            private bool _nextParaTextStarted = false;

            public TextRowCollector(UsfmTextBase text)
            {
                _text = text;
                _rows = new List<TextRow>();
                _verseText = new StringBuilder();
                _nextParaTokens = new List<UsfmToken>();
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

            public override void Text(UsfmParserState state, string text)
            {
                if (_verseRef.IsDefault || !state.IsVersePara)
                    return;

                if (_text._includeMarkers)
                {
                    text = text.TrimEnd('\r', '\n');
                    if (text.Length > 0)
                    {
                        if (!text.IsWhiteSpace())
                        {
                            foreach (UsfmToken token in _nextParaTokens)
                                _verseText.Append(token);
                            _nextParaTokens.Clear();
                            _nextParaTextStarted = true;
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
                if (_verseRef.IsDefault)
                    return;

                string text = _verseText.ToString();
                _rows.AddRange(_text.CreateRows(_verseRef, text, _sentenceStart));
                _sentenceStart = nextSentenceStart ?? text.HasSentenceEnding();
                _verseText.Clear();
            }

            private void HandlePara(UsfmParserState state)
            {
                if (_verseRef.IsDefault)
                    return;

                if (state.IsVersePara)
                {
                    if (_verseText.Length > 0 && !char.IsWhiteSpace(_verseText[_verseText.Length - 1]))
                        _verseText.Append(" ");
                    _nextParaTokens.Add(state.Token);
                    _nextParaTextStarted = false;
                }
            }
        }
    }
}
