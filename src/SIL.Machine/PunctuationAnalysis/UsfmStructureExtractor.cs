using System.Collections.Generic;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace SIL.Machine.PunctuationAnalysis
{
    public class UsfmStructureExtractor : IUsfmParserHandler
    {
        private readonly List<TextSegment> _textSegments;
        private TextSegment.Builder _nextTextSegmentBuilder;

        public UsfmStructureExtractor()
        {
            _textSegments = new List<TextSegment>();
            _nextTextSegmentBuilder = new TextSegment.Builder();
        }

        public void StartBook(UsfmParserState state, string marker, string code)
        {
            _nextTextSegmentBuilder.SetBook(code);
        }

        public void Chapter(UsfmParserState state, string number, string marker, string altNumber, string pubNumber)
        {
            _nextTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Chapter);
            _nextTextSegmentBuilder.SetChapter(int.Parse(number));
        }

        public void EndBook(UsfmParserState state, string marker) { }

        public void EndCell(UsfmParserState state, string marker) { }

        public void EndChar(UsfmParserState state, string marker, IReadOnlyList<UsfmAttribute> attributes, bool closed)
        {
            _nextTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Character);
        }

        public void EndNote(UsfmParserState state, string marker, bool closed)
        {
            _nextTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Embed);
        }

        public void EndPara(UsfmParserState state, string marker) { }

        public void EndRow(UsfmParserState state, string marker) { }

        public void EndSidebar(UsfmParserState state, string marker, bool closed)
        {
            _nextTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Embed);
        }

        public void EndTable(UsfmParserState state)
        {
            _nextTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Embed);
        }

        public void EndUsfm(UsfmParserState state) { }

        public void GotMarker(UsfmParserState state, string marker) { }

        public void Milestone(
            UsfmParserState state,
            string marker,
            bool startMilestone,
            IReadOnlyList<UsfmAttribute> attributes
        ) { }

        public void OptBreak(UsfmParserState state) { }

        public void Ref(UsfmParserState state, string marker, string display, string target)
        {
            _nextTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Embed);
        }

        public void StartCell(UsfmParserState state, string marker, string align, int colspan) { }

        public void StartChar(
            UsfmParserState state,
            string markerWithoutPlus,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            _nextTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Character);
        }

        public void StartNote(UsfmParserState state, string marker, string caller, string category) { }

        public void StartPara(
            UsfmParserState state,
            string marker,
            bool unknown,
            IReadOnlyList<UsfmAttribute> attributes
        )
        {
            _nextTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Paragraph);
        }

        public void StartRow(UsfmParserState state, string marker) { }

        public void StartSidebar(UsfmParserState state, string marker, string category) { }

        public void StartTable(UsfmParserState state) { }

        public void StartUsfm(UsfmParserState state) { }

        public void Text(UsfmParserState state, string text)
        {
            if (!state.IsVerseText)
                return;
            if (text.Length > 0)
            {
                _nextTextSegmentBuilder.SetText(text);
                TextSegment textSegment = _nextTextSegmentBuilder.Build();
                // Don't look past verse boundaries, to enable identical functionality in the
                // online one-verse-at-a-time (QuotationMarkDenormalizationScriptureUpdateBlockHandler)
                // and offline whole-book-at-once settings (QuoteConventionDetector)
                if (_textSegments.Count > 0 && !textSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Verse))
                {
                    _textSegments[_textSegments.Count - 1].NextSegment = textSegment;
                    textSegment.PreviousSegment = _textSegments[_textSegments.Count - 1];
                }
                _textSegments.Add(textSegment);
            }
            _nextTextSegmentBuilder = new TextSegment.Builder();
        }

        public void Unmatched(UsfmParserState state, string marker) { }

        public void Verse(UsfmParserState state, string number, string marker, string altNumber, string pubNumber)
        {
            _nextTextSegmentBuilder.AddPrecedingMarker(UsfmMarkerType.Verse);
        }

        public List<Chapter> GetChapters(Dictionary<int, List<int>> includeChapters = null)
        {
            var chapters = new List<Chapter>();
            int currentBook = 0;
            int currentChapter = 0;
            var currentChapterVerses = new List<Verse>();
            var currentVerseSegments = new List<TextSegment>();
            foreach (TextSegment textSegment in _textSegments)
            {
                if (textSegment.Book != null)
                    currentBook = Canon.BookIdToNumber(textSegment.Book);
                if (textSegment.Chapter > 0)
                    currentChapter = textSegment.Chapter;
                if (includeChapters != null && currentBook > 0)
                {
                    if (!includeChapters.TryGetValue(currentBook, out List<int> bookChapters))
                        continue;
                    if (currentChapter > 0 && bookChapters.Count > 0 && !bookChapters.Contains(currentChapter))
                        continue;
                }
                if (textSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Verse))
                {
                    if (currentVerseSegments.Count > 0)
                    {
                        currentChapterVerses.Add(new Verse(currentVerseSegments));
                    }
                    currentVerseSegments = new List<TextSegment>();
                }
                if (textSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Chapter))
                {
                    if (currentChapterVerses.Count > 0)
                    {
                        chapters.Add(new Chapter(currentChapterVerses));
                    }
                    currentChapterVerses = new List<Verse>();
                }
                currentVerseSegments.Add(textSegment);
            }
            if (currentVerseSegments.Count > 0)
            {
                currentChapterVerses.Add(new Verse(currentVerseSegments));
            }
            if (currentChapterVerses.Count > 0)
            {
                chapters.Add(new Chapter(currentChapterVerses));
            }
            return chapters;
        }
    }
}
