using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    /***
     * This is a USFM parser handler that can be used to replace the existing text in a USFM file with the specified
     * text.
     */
    public class UsfmVerseTextUpdater : UsfmParserHandlerBase
    {
        private readonly IReadOnlyList<(IReadOnlyList<VerseRef>, string)> _rows;
        private readonly List<UsfmToken> _tokens;
        private readonly string _idText;
        private readonly bool _stripAllText;
        private int _rowIndex;
        private int _tokenIndex;
        private bool _replaceText;

        public UsfmVerseTextUpdater(
            IReadOnlyList<(IReadOnlyList<VerseRef>, string)> rows = null,
            string idText = null,
            bool stripAllText = false
        )
        {
            _rows = rows ?? Array.Empty<(IReadOnlyList<VerseRef>, string)>();
            _tokens = new List<UsfmToken>();
            _idText = idText;
            _stripAllText = stripAllText;
        }

        public IReadOnlyList<UsfmToken> Tokens => _tokens;

        public override void StartBook(UsfmParserState state, string marker, string code)
        {
            CollectTokens(state);
            if (_idText != null)
            {
                _tokens.Add(new UsfmToken(_idText + " "));
                _replaceText = true;
            }
        }

        public override void EndBook(UsfmParserState state, string marker)
        {
            _replaceText = false;
        }

        public override void StartPara(
            UsfmParserState state,
            string marker,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            if (!state.IsVersePara)
                _replaceText = false;
            CollectTokens(state);
        }

        public override void StartRow(UsfmParserState state, string marker)
        {
            CollectTokens(state);
        }

        public override void StartCell(UsfmParserState state, string marker, string align, int colSpan)
        {
            CollectTokens(state);
        }

        public override void EndCell(UsfmParserState state, string marker)
        {
            CollectTokens(state);
        }

        public override void StartSidebar(UsfmParserState state, string marker, string category)
        {
            _replaceText = false;
            CollectTokens(state);
        }

        public override void EndSidebar(UsfmParserState state, string marker, bool closed)
        {
            _replaceText = false;
            if (closed)
                CollectTokens(state);
        }

        public override void Chapter(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            _replaceText = false;
            CollectTokens(state);
        }

        public override void Milestone(
            UsfmParserState state,
            string marker,
            bool startMilestone,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            CollectTokens(state);
        }

        public override void Verse(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            _replaceText = false;
            CollectTokens(state);

            while (_rowIndex < _rows.Count)
            {
                (IReadOnlyList<VerseRef> verseRefs, string text) = _rows[_rowIndex];
                bool stop = false;
                foreach (VerseRef verseRef in verseRefs)
                {
                    int compare = verseRef.CompareTo(state.VerseRef, compareAllVerses: true);
                    if (compare == 0)
                    {
                        _tokens.Add(new UsfmToken(text + " "));
                        _replaceText = true;
                        break;
                    }
                    else
                    {
                        if (state.VerseRef.AllVerses().Any(v => v.Equals(verseRef)))
                        {
                            _tokens.Add(new UsfmToken(text + " "));
                            _replaceText = true;
                            break;
                        }
                        if (compare > 0)
                        {
                            stop = true;
                            break;
                        }
                    }
                }

                if (stop)
                    break;
                else
                    _rowIndex++;
            }
        }

        public override void StartChar(
            UsfmParserState state,
            string markerWithoutPlus,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            // strip out char-style markers in verses that are being replaced
            if (_stripAllText || (_replaceText && state.IsVersePara))
                SkipTokens(state);
            else
                CollectTokens(state);
        }

        public override void EndChar(
            UsfmParserState state,
            string marker,
            IReadOnlyList<UsfmAttribute> attributes,
            bool closed
        )
        {
            // strip out char-style markers in verses that are being replaced
            if (closed && (_stripAllText || (_replaceText && state.IsVersePara)))
                SkipTokens(state);
        }

        public override void StartNote(UsfmParserState state, string marker, string caller, string category)
        {
            // strip out notes in verses that are being replaced
            if (_stripAllText || (_replaceText && state.IsVersePara))
                SkipTokens(state);
            else
                CollectTokens(state);
        }

        public override void EndNote(UsfmParserState state, string marker, bool closed)
        {
            // strip out notes in verses that are being replaced
            if (closed && (_stripAllText || (_replaceText && state.IsVersePara)))
                SkipTokens(state);
        }

        public override void Ref(UsfmParserState state, string marker, string display, string target)
        {
            // strip out ref in verses that are being replaced
            if (_stripAllText || (_replaceText && state.IsVersePara))
                SkipTokens(state);
            else
                CollectTokens(state);
        }

        public override void Text(UsfmParserState state, string text)
        {
            // strip out text in verses that are being replaced
            if (_stripAllText || (_replaceText && (state.IsVersePara || state.ParaTag.Marker == "id")))
                SkipTokens(state);
            else
                CollectTokens(state);
        }

        public override void OptBreak(UsfmParserState state)
        {
            // strip out optBreaks in verses that are being replaced
            if (_stripAllText || (_replaceText && state.IsVersePara))
                SkipTokens(state);
            else
                CollectTokens(state);
        }

        public override void Unmatched(UsfmParserState state, string marker)
        {
            // strip out unmatched end markers in verses that are being replaced
            if (_stripAllText || (_replaceText && state.IsVersePara))
                SkipTokens(state);
            else
                CollectTokens(state);
        }

        public string GetUsfm(string stylesheetFileName = "usfm.sty")
        {
            return GetUsfm(new UsfmStylesheet(stylesheetFileName));
        }

        public string GetUsfm(UsfmStylesheet stylesheet)
        {
            var tokenizer = new UsfmTokenizer(stylesheet);
            return tokenizer.Detokenize(_tokens);
        }

        private void CollectTokens(UsfmParserState state)
        {
            while (_tokenIndex <= state.Index + state.SpecialTokenCount)
            {
                _tokens.Add(state.Tokens[_tokenIndex]);
                _tokenIndex++;
            }
        }

        private void SkipTokens(UsfmParserState state)
        {
            _tokenIndex = state.Index + 1 + state.SpecialTokenCount;
        }
    }
}
