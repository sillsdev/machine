using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public enum UsfmVersificationMismatchType
    {
        MissingChapter,
        MissingVerse,
        ExtraVerse,
        InvalidVerseRange,
        MissingVerseSegment,
        ExtraVerseSegment
    }

    public class UsfmVersificationMismatch
    {
        private readonly int _bookNum;
        private readonly int _expectedChapter;
        private readonly int _expectedVerse;
        private readonly int _actualChapter;
        private readonly int _actualVerse;
        private VerseRef? _verseRef = null;

        public UsfmVersificationMismatch(
            int bookNum,
            int expectedChapter,
            int expectedVerse,
            int actualChapter,
            int actualVerse,
            VerseRef? verseRef = null
        )
        {
            _bookNum = bookNum;
            _expectedChapter = expectedChapter;
            _expectedVerse = expectedVerse;
            _actualChapter = actualChapter;
            _actualVerse = actualVerse;
            _verseRef = verseRef;
        }

        public UsfmVersificationMismatchType Type { get; private set; }

        // Returns true if there is a mismatch
        public bool CheckMismatch()
        {
            //A non-empty chapter is expected
            if (_expectedChapter > _actualChapter && _expectedVerse != 0)
            {
                Type = UsfmVersificationMismatchType.MissingChapter;
                return true;
            }
            if (_expectedVerse > _actualVerse && _expectedChapter == _actualChapter)
            {
                Type = UsfmVersificationMismatchType.MissingVerse;
                return true;
            }
            if (_verseRef != null)
            {
                if (string.IsNullOrEmpty(_verseRef.Value.Segment()) && _verseRef.Value.HasSegmentsDefined)
                {
                    Type = UsfmVersificationMismatchType.MissingVerseSegment;
                    return true;
                }
                if (!string.IsNullOrEmpty(_verseRef.Value.Segment()) && !_verseRef.Value.HasSegmentsDefined)
                {
                    Type = UsfmVersificationMismatchType.ExtraVerseSegment;
                    return true;
                }
                if (!_verseRef.Value.Valid)
                {
                    Type = Map(_verseRef.Value.ValidStatus);
                    return true;
                }
            }
            return false;
        }

        private static UsfmVersificationMismatchType Map(VerseRef.ValidStatusType validStatus)
        {
            switch (validStatus)
            {
                case VerseRef.ValidStatusType.OutOfRange:
                    return UsfmVersificationMismatchType.ExtraVerse;
                case VerseRef.ValidStatusType.VerseRepeated:
                case VerseRef.ValidStatusType.VerseOutOfOrder:
                    return UsfmVersificationMismatchType.InvalidVerseRange;
                default:
                    throw new InvalidEnumArgumentException(
                        nameof(validStatus),
                        (int)validStatus,
                        typeof(VerseRef.ValidStatusType)
                    );
            }
        }

        public string ExpectedVerseRef
        {
            get
            {
                if (!VerseRef.TryParse($"{_bookNum} {_expectedChapter}:{_expectedVerse}", out VerseRef defaultVerseRef))
                {
                    return "";
                }
                if (Type == UsfmVersificationMismatchType.ExtraVerse)
                    return "";
                if (
                    Type == UsfmVersificationMismatchType.MissingVerseSegment
                    && VerseRef.TryParse(
                        $"{defaultVerseRef.Book} {defaultVerseRef.Chapter}:{defaultVerseRef.Verse}a",
                        out VerseRef verseWithSegment
                    )
                )
                {
                    return verseWithSegment.ToString();
                }
                if (Type == UsfmVersificationMismatchType.InvalidVerseRange)
                {
                    List<VerseRef> sortedAllUniqueVerses = _verseRef
                        .Value.AllVerses()
                        .Distinct()
                        .OrderBy(v => v)
                        .ToList();
                    VerseRef firstVerse = sortedAllUniqueVerses[0];
                    VerseRef lastVerse = sortedAllUniqueVerses[sortedAllUniqueVerses.Count - 1];
                    if (firstVerse.Equals(lastVerse))
                    {
                        return firstVerse.ToString();
                    }
                    else if (
                        VerseRef.TryParse(
                            $"{firstVerse.Book} {firstVerse.Chapter}:{firstVerse.Verse}-{lastVerse.Verse}",
                            out VerseRef correctedVerseRangeRef
                        )
                    )
                    {
                        return correctedVerseRangeRef.ToString();
                    }
                }
                return defaultVerseRef.ToString();
            }
        }
        public string ActualVerseRef =>
            _verseRef != null
                ? _verseRef.Value.ToString()
                : new VerseRef(_bookNum, _actualChapter, _actualVerse).ToString();
    }

    public class UsfmVersificationMismatchDetector : UsfmParserHandlerBase
    {
        private readonly ScrVers _versification;
        private int _currentBook;
        private int _currentChapter;
        private VerseRef _currentVerse;
        private readonly List<UsfmVersificationMismatch> _errors;

        public UsfmVersificationMismatchDetector(ScrVers versification)
        {
            _versification = versification;
            _currentBook = 0;
            _currentChapter = 0;
            _currentVerse = new VerseRef();
            _errors = new List<UsfmVersificationMismatch>();
        }

        public IReadOnlyList<UsfmVersificationMismatch> Errors => _errors;

        public override void EndUsfm(UsfmParserState state)
        {
            if (_currentBook > 0 && Canon.IsCanonical(_currentBook))
            {
                var versificationMismatch = new UsfmVersificationMismatch(
                    _currentBook,
                    _versification.GetLastChapter(_currentBook),
                    _versification.GetLastVerse(_currentBook, _versification.GetLastChapter(_currentBook)),
                    _currentChapter,
                    _currentVerse.AllVerses().Last().VerseNum
                );
                if (versificationMismatch.CheckMismatch())
                    _errors.Add(versificationMismatch);
            }
        }

        public override void StartBook(UsfmParserState state, string marker, string code)
        {
            _currentBook = state.VerseRef.BookNum;
            _currentChapter = 0;
            _currentVerse = new VerseRef();
        }

        public override void Chapter(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            if (_currentBook > 0 && Canon.IsCanonical(_currentBook) && _currentChapter > 0)
            {
                var versificationMismatch = new UsfmVersificationMismatch(
                    _currentBook,
                    _currentChapter,
                    _versification.GetLastVerse(_currentBook, _currentChapter),
                    _currentChapter,
                    _currentVerse.AllVerses().Last().VerseNum
                );
                if (versificationMismatch.CheckMismatch())
                    _errors.Add(versificationMismatch);
            }

            _currentChapter = state.VerseRef.ChapterNum;
            _currentVerse = new VerseRef();
        }

        public override void Verse(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            _currentVerse = state.VerseRef;
            if (_currentBook > 0 && Canon.IsCanonical(_currentBook) && _currentChapter > 0)
            {
                var versificationMismatch = new UsfmVersificationMismatch(
                    _currentBook,
                    _currentChapter,
                    _currentVerse.AllVerses().Last().VerseNum,
                    _currentChapter,
                    _currentVerse.AllVerses().Last().VerseNum,
                    _currentVerse
                );
                if (versificationMismatch.CheckMismatch())
                    _errors.Add(versificationMismatch);
            }
        }
    }
}
