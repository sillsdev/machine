using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class ConvertUsfmVersificationHandler : ScriptureRefUsfmParserHandlerBase
    {
        private static readonly Regex TrailingParagraphMarkerPatterns = new Regex(
            @"^(?:mte?\d*|ms\d*|sd?\d*|mr|sr|sp|d|r)$",
            RegexOptions.Compiled
        );
        private readonly List<UsfmToken> _tokens;
        private List<(int Index, UsfmToken Token)> _trailingVerseTokens;
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
            _trailingVerseTokens = new List<(int Index, UsfmToken Token)>();
            _prevVerseRef = new VerseRef();
            _targetVersification = targetVersification;
            _skip = false;
        }

        public IReadOnlyList<UsfmToken> Tokens => _tokens;

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
            VerseRef vr = state.VerseRef;
            // The versification of verse 0 cannot properly be changed
            vr.Verse = "1";
            if (
                !_prevVerseRef.IsDefault
                && (
                    vr.ChangeVersificationWithSegments(_targetVersification).Book != _prevVerseRef.Book
                    || vr.ChapterNum == -1
                )
            )
            {
                _skip = true;
            }
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
                (
                    _prevVerseRef.IsDefault
                    || (
                        verseRefs[0].BookNum == _prevVerseRef.BookNum
                        && verseRefs[0].ChapterNum != _prevVerseRef.ChapterNum
                    )
                ) && (verseRefs[0].ChapterNum != -1)
            )
            {
                UsfmToken newChapterToken = new UsfmToken(UsfmTokenType.Chapter, "c", "", "", verseRefs[0].Chapter);

                if (_insertChapterIndex == -1)
                {
                    int chapterIndex = _tokens.Count;
                    _tokens.Add(newChapterToken);
                    List<UsfmToken> trailingAtChapter = _trailingVerseTokens
                        .Where(tup => tup.Index == chapterIndex)
                        .Select(tup => tup.Token)
                        .ToList();
                    if (trailingAtChapter.Count == 0)
                    {
                        // The chapter break falls mid-paragraph, so the paragraph continues across it.
                        _tokens.Add(new UsfmToken(UsfmTokenType.Paragraph, "nb", "", "", ""));
                    }
                    else
                    {
                        // The trailing markers follow the new chapter and break the paragraph. If
                        // they do not open a paragraph of their own, the verse still needs one.
                        UsfmToken lastParagraph = trailingAtChapter.LastOrDefault(t =>
                            t.Type == UsfmTokenType.Paragraph
                        );
                        if (lastParagraph == null || TrailingParagraphMarkerPatterns.IsMatch(lastParagraph.Marker))
                        {
                            _trailingVerseTokens.Add(
                                (chapterIndex, new UsfmToken(UsfmTokenType.Paragraph, "nb", "", "", ""))
                            );
                        }
                        _trailingVerseTokens = _trailingVerseTokens
                            .Select(tup => tup.Index == chapterIndex ? (tup.Index + 1, tup.Token) : tup)
                            .ToList();
                    }
                }
                else
                {
                    _tokens.Insert(_insertChapterIndex, newChapterToken);
                    _trailingVerseTokens = _trailingVerseTokens
                        .Select(tup => tup.Index >= _insertChapterIndex ? (tup.Index + 1, tup.Token) : tup)
                        .ToList();
                }
            }

            bool addedVerseText = false;

            string start = null;
            for (int i = 0; i < verseRefs.Count; i++)
            {
                if (
                    (!_prevVerseRef.IsDefault && verseRefs[i].Book != _prevVerseRef.Book)
                    || verseRefs[i].ChapterNum == -1
                )
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
                        AddTrailingTokens();
                        _tokens.Add(new UsfmToken(UsfmTokenType.Verse, "v", "", "", start + end));
                        if (!addedVerseText && state.Index + 1 < state.Tokens.Count)
                        {
                            UsfmToken nextToken = state.Tokens[state.Index + 1];
                            if (nextToken.Type == UsfmTokenType.Text)
                            {
                                _tokens.Add(nextToken);
                                _verseBoundary++;
                                addedVerseText = true;
                            }
                        }
                        _tokens.Add(new UsfmToken(UsfmTokenType.Chapter, "c", "", "", verseRefs[i].Chapter));
                        _tokens.Add(new UsfmToken(UsfmTokenType.Paragraph, "nb", "", "", ""));
                        start = verseRefs[i].Verse;
                        _prevVerseRef = verseRefs[i];
                    }
                    else if (_prevVerseRef.VerseNum + 1 != verseRefs[i].VerseNum)
                    {
                        AddTrailingTokens();
                        _tokens.Add(new UsfmToken(UsfmTokenType.Verse, "v", "", "", start + end));
                        if (!addedVerseText && state.Index + 1 < state.Tokens.Count)
                        {
                            UsfmToken nextToken = state.Tokens[state.Index + 1];
                            if (nextToken.Type == UsfmTokenType.Text)
                            {
                                _tokens.Add(nextToken);
                                _verseBoundary++;
                                addedVerseText = true;
                            }
                        }
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
                AddTrailingTokens();
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
            bool inPreservedParagraph = false;
            while (_verseBoundary + offset < state.Index)
            {
                UsfmToken token = state.Tokens[_verseBoundary + offset];
                if (
                    IsPreservedTrailingParagraphMarker(
                        token,
                        _verseBoundary + offset + 1 < state.Tokens.Count
                            ? state.Tokens[_verseBoundary + offset + 1]
                            : null
                    )
                )
                {
                    inPreservedParagraph = true;
                }
                else if (inPreservedParagraph)
                {
                    inPreservedParagraph = token.Type != UsfmTokenType.Paragraph;
                }
                else
                {
                    inPreservedParagraph = false;
                }

                if (inPreservedParagraph)
                    _trailingVerseTokens.Add((_tokens.Count, token));
                else if (!_skip)
                    _tokens.Add(token);

                offset++;
            }
            _verseBoundary = state.Index + 1;
        }

        private void AddTrailingTokens()
        {
            foreach (
                (int index, List<UsfmToken> tokens) in _trailingVerseTokens
                    .GroupBy(tup => tup.Index)
                    .Select(g => (g.Key, g.Select(tup => tup.Token).ToList()))
                    .OrderBy(tup => -tup.Key)
            )
            {
                _tokens.InsertRange(index, tokens);
            }
            _trailingVerseTokens.Clear();
        }

        private bool IsPreservedTrailingParagraphMarker(UsfmToken token, UsfmToken nextToken)
        {
            return (token.Marker == "p" && nextToken != null && nextToken.Type == UsfmTokenType.Verse)
                || token.Type == UsfmTokenType.Paragraph && TrailingParagraphMarkerPatterns.IsMatch(token.Marker);
        }
    }
}
