using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class ConvertUsfmVersificationHandler : ScriptureRefUsfmParserHandlerBase
    {
        private readonly List<UsfmToken> _tokens;
        private VerseRef _prevVerseRef;
        private int _verseBoundary;
        private readonly ScrVers _targetVersification;
        private int _insertChapterIndex;
        private bool _skip;

        public ConvertUsfmVersificationHandler(ScrVers targetVersification)
        {
            _verseBoundary = 0;
            _insertChapterIndex = -1;
            _tokens = new List<UsfmToken>();
            _prevVerseRef = new VerseRef();
            _targetVersification = targetVersification;
            _skip = false;
        }

        public override void Chapter(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            base.Chapter(state, number, marker, altNumber, pubNumber);
            ProcessTokens(state);
            _insertChapterIndex = _tokens.Count;
        }

        public override void Verse(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            base.Verse(state, number, marker, altNumber, pubNumber);

            VerseRef verseRef = state.VerseRef;

            ProcessTokens(state);

            List<VerseRef> verseRefs = state
                .VerseRef.AllVerses()
                .Select(vr => vr.ChangeVersificationWithSegments(_targetVersification))
                .ToList();

            if (
                _prevVerseRef.IsDefault
                || (
                    verseRefs[0].BookNum == _prevVerseRef.BookNum && verseRefs[0].ChapterNum != _prevVerseRef.ChapterNum
                )
            )
            {
                UsfmToken newChapterToken = new UsfmToken(UsfmTokenType.Chapter, "c", "", "", verseRefs[0].Chapter);

                if (_insertChapterIndex == -1)
                    _tokens.Add(newChapterToken);
                else
                    _tokens.Insert(_insertChapterIndex, newChapterToken);
            }

            string start = null;
            for (int i = 0; i < verseRefs.Count; i++)
            {
                if (!_prevVerseRef.IsDefault && verseRefs[i].Book != _prevVerseRef.Book)
                {
                    continue;
                }
                if (start != null)
                {
                    string end = start != _prevVerseRef.Verse ? "-" + _prevVerseRef.Verse : "";
                    if (
                        _prevVerseRef.BookNum == verseRefs[i].BookNum
                        && _prevVerseRef.ChapterNum != verseRefs[i].ChapterNum
                    )
                    {
                        _tokens.Add(new UsfmToken(UsfmTokenType.Verse, "v", "", "", start + end));
                        _tokens.Add(new UsfmToken(UsfmTokenType.Chapter, "c", "", "", verseRefs[i].Chapter));
                        start = verseRefs[i].Verse;
                        _prevVerseRef = verseRefs[i];
                    }
                    else if (_prevVerseRef.VerseNum + 1 != verseRefs[i].VerseNum)
                    {
                        _tokens.Add(new UsfmToken(UsfmTokenType.Verse, "v", "", "", start + end));
                        start = verseRefs[i].Verse;
                        _prevVerseRef = verseRefs[i];
                    }
                    else
                    {
                        _prevVerseRef = verseRefs[i];
                    }
                }
                else
                {
                    start = verseRefs[i].Verse;
                    _prevVerseRef = verseRefs[i];
                }
                verseRef = verseRefs[i];
            }

            if (start != null)
            {
                string end = start != _prevVerseRef.Verse ? "-" + _prevVerseRef.Verse : "";
                _tokens.Add(new UsfmToken(UsfmTokenType.Verse, "v", "", "", start + end));
                _skip = false;
                _insertChapterIndex = -1;
                _prevVerseRef = verseRef;
            }
            else
            {
                _skip = true;
            }
        }

        public override void EndUsfm(UsfmParserState state)
        {
            base.EndUsfm(state);
            ProcessTokens(state);
            if (!_skip && !(state.Token.Type == UsfmTokenType.Chapter || state.Token.Type == UsfmTokenType.Verse))
                _tokens.Add(state.Token);
        }

        public string GetUsfm(UsfmStylesheet stylesheet)
        {
            var tokenizer = new UsfmTokenizer(stylesheet);
            return tokenizer.Detokenize(_tokens);
        }

        private void ProcessTokens(UsfmParserState state)
        {
            int offset = 0;
            if (!_skip)
            {
                while (_verseBoundary + offset < state.Index)
                    _tokens.Add(state.Tokens[_verseBoundary + offset++]);
            }
            _verseBoundary = state.Index + 1;
        }
    }
}
