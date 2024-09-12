using System;
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

        public string Project { get; set; }

        protected override IEnumerable<TextRow> GetVersesInDocOrder()
        {
            string usfm = ReadUsfm();
            var rowCollector = new TextRowCollector(this);

            var tokenizer = new UsfmTokenizer(_stylesheet);
            IReadOnlyList<UsfmToken> tokens;
            try
            {
                tokens = tokenizer.Tokenize(usfm, _includeMarkers);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append($"An error occurred while tokenizing the text '{Id}'");
                if (!string.IsNullOrEmpty(Project))
                    sb.Append($" in project '{Project}'");
                sb.Append($". Error: '{ex.Message}'");
                throw new InvalidOperationException(sb.ToString(), ex);
            }

            var parser = new UsfmParser(
                tokens,
                rowCollector,
                _stylesheet,
                Versification,
                tokensPreserveWhitespace: _includeMarkers
            );
            try
            {
                parser.ProcessTokens();
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append($"An error occurred while parsing the text '{Id}'");
                if (!string.IsNullOrEmpty(Project))
                    sb.Append($" in project '{Project}'");
                sb.Append($". Verse: {parser.State.VerseRef}, line: {parser.State.LineNumber}, ");
                sb.Append($"character: {parser.State.ColumnNumber}, error: '{ex.Message}'");
                throw new InvalidOperationException(sb.ToString(), ex);
            }
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

        private class TextRowCollector : ScriptureRefUsfmParserHandlerBase
        {
            private readonly UsfmTextBase _text;
            private readonly List<TextRow> _rows;
            private readonly Stack<StringBuilder> _rowTexts;
            private bool _sentenceStart;
            private readonly List<UsfmToken> _nextParaTokens;
            private bool _nextParaTextStarted = false;

            public TextRowCollector(UsfmTextBase text)
            {
                _text = text;
                _rows = new List<TextRow>();
                _rowTexts = new Stack<StringBuilder>();
                _nextParaTokens = new List<UsfmToken>();
            }

            public IEnumerable<TextRow> Rows => _rows;

            public override void Verse(
                UsfmParserState state,
                string number,
                string marker,
                string altNumber,
                string pubNumber
            )
            {
                base.Verse(state, number, marker, altNumber, pubNumber);

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
                base.StartPara(state, marker, unknown, attributes);

                HandlePara(state);
            }

            public override void StartRow(UsfmParserState state, string marker)
            {
                base.StartRow(state, marker);

                HandlePara(state);
            }

            public override void StartCell(UsfmParserState state, string marker, string align, int colspan)
            {
                base.StartCell(state, marker, align, colspan);

                if (_text._includeMarkers)
                {
                    OutputMarker(state);
                }
                else if (CurrentTextType == ScriptureTextType.Verse)
                {
                    if (_rowTexts.Count == 0)
                        return;
                    StringBuilder verseText = _rowTexts.Peek();
                    if (verseText.Length > 0 && !char.IsWhiteSpace(verseText[verseText.Length - 1]))
                        verseText.Append(" ");
                }
            }

            public override void Ref(UsfmParserState state, string marker, string display, string target)
            {
                base.Ref(state, marker, display, target);

                OutputMarker(state);
            }

            public override void StartChar(
                UsfmParserState state,
                string markerWithoutPlus,
                bool unknown,
                IReadOnlyList<UsfmAttribute> attributes
            )
            {
                base.StartChar(state, markerWithoutPlus, unknown, attributes);

                OutputMarker(state);
            }

            public override void EndChar(
                UsfmParserState state,
                string marker,
                IReadOnlyList<UsfmAttribute> attributes,
                bool closed
            )
            {
                base.EndChar(state, marker, attributes, closed);

                if (_rowTexts.Count == 0)
                    return;

                if (_text._includeMarkers && attributes != null && state.PrevToken?.Type == UsfmTokenType.Attribute)
                    _rowTexts.Peek().Append(state.PrevToken);

                if (closed)
                    OutputMarker(state);

                if (!_text._includeMarkers && marker == "rq")
                    _rowTexts.Peek().TrimEnd();
            }

            public override void StartNote(UsfmParserState state, string marker, string caller, string category)
            {
                base.StartNote(state, marker, caller, category);

                OutputMarker(state);
            }

            public override void EndNote(UsfmParserState state, string marker, bool closed)
            {
                base.EndNote(state, marker, closed);

                if (closed)
                    OutputMarker(state);
            }

            public override void OptBreak(UsfmParserState state)
            {
                base.OptBreak(state);

                if (_rowTexts.Count == 0)
                    return;

                if (_text._includeMarkers)
                {
                    _rowTexts.Peek().Append("//");
                }
                else if (CurrentTextType != ScriptureTextType.Verse || state.IsVerseText)
                {
                    _rowTexts.Peek().TrimEnd();
                }
            }

            public override void Text(UsfmParserState state, string text)
            {
                base.Text(state, text);

                if (_rowTexts.Count == 0)
                    return;

                StringBuilder rowText = _rowTexts.Peek();
                if (_text._includeMarkers)
                {
                    text = text.TrimEnd('\r', '\n');
                    if (text.Length > 0)
                    {
                        if (!text.IsWhiteSpace())
                        {
                            foreach (UsfmToken token in _nextParaTokens)
                                rowText.Append(token);
                            _nextParaTokens.Clear();
                            _nextParaTextStarted = true;
                        }
                        if (rowText.Length == 0 || char.IsWhiteSpace(rowText[rowText.Length - 1]))
                            text = text.TrimStart();
                        rowText.Append(text);
                    }
                }
                else if (text.Length > 0 && (CurrentTextType != ScriptureTextType.Verse || state.IsVerseText))
                {
                    if (
                        state.PrevToken?.Type == UsfmTokenType.End
                        && (rowText.Length == 0 || char.IsWhiteSpace(rowText[rowText.Length - 1]))
                    )
                    {
                        text = text.TrimStart();
                    }
                    rowText.Append(text);
                }
            }

            protected override void StartVerseText(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs)
            {
                _rowTexts.Push(new StringBuilder());
            }

            protected override void EndVerseText(UsfmParserState state, IReadOnlyList<ScriptureRef> scriptureRefs)
            {
                string text = _rowTexts.Pop().ToString();
                _rows.AddRange(_text.CreateRows(scriptureRefs, text, _sentenceStart));
                _sentenceStart = state.Token.Marker == "c" || text.HasSentenceEnding();
            }

            protected override void StartNonVerseText(UsfmParserState state, ScriptureRef scriptureRef)
            {
                _rowTexts.Push(new StringBuilder());
            }

            protected override void EndNonVerseText(UsfmParserState state, ScriptureRef scriptureRef)
            {
                string text = _rowTexts.Pop().ToString();
                if (_text._includeAllText)
                    _rows.Add(_text.CreateRow(scriptureRef, text, _sentenceStart));
            }

            protected override void StartNoteText(UsfmParserState state, ScriptureRef scriptureRef)
            {
                if (_text._includeMarkers)
                    return;

                _rowTexts.Push(new StringBuilder());
            }

            protected override void EndNoteText(UsfmParserState state, ScriptureRef scriptureRef)
            {
                if (_text._includeMarkers)
                    return;

                string text = _rowTexts.Pop().ToString();
                if (_text._includeAllText)
                    _rows.Add(_text.CreateRow(scriptureRef, text, _sentenceStart));
            }

            private void OutputMarker(UsfmParserState state)
            {
                if (!_text._includeMarkers || _rowTexts.Count == 0)
                    return;

                if (_nextParaTextStarted)
                    _rowTexts.Peek().Append(state.Token);
                else
                    _nextParaTokens.Add(state.Token);
            }

            private void HandlePara(UsfmParserState state)
            {
                if (_rowTexts.Count == 0)
                    return;

                foreach (StringBuilder rowText in _rowTexts)
                {
                    if (rowText.Length > 0 && !char.IsWhiteSpace(rowText[rowText.Length - 1]))
                        rowText.Append(" ");
                }
                if (CurrentTextType == ScriptureTextType.Verse)
                {
                    _nextParaTokens.Add(state.Token);
                    _nextParaTextStarted = false;
                }
                if (!state.IsVersePara)
                    _sentenceStart = true;
            }
        }
    }
}
