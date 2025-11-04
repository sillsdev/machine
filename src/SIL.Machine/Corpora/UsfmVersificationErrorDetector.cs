using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public enum UsfmVersificationErrorType
    {
        MissingChapter,
        MissingVerse,
        ExtraVerse,
        InvalidVerseRange,
        MissingVerseSegment,
        ExtraVerseSegment
    }

    public class UsfmVersificationError
    {
        private readonly int _bookNum;
        private readonly int _expectedChapter;
        private readonly int _expectedVerse;
        private readonly int _actualChapter;
        private readonly int _actualVerse;
        private VerseRef? _verseRef = null;

        public UsfmVersificationError(
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

        public UsfmVersificationErrorType Type { get; private set; }

        // Returns true if there is an error
        public bool CheckError()
        {
            //A non-empty chapter is expected
            if (_expectedChapter > _actualChapter && _expectedVerse != 0)
            {
                Type = UsfmVersificationErrorType.MissingChapter;
                return true;
            }
            if (_expectedVerse > _actualVerse && _expectedChapter == _actualChapter)
            {
                Type = UsfmVersificationErrorType.MissingVerse;
                return true;
            }
            if (_verseRef != null)
            {
                if (string.IsNullOrEmpty(_verseRef.Value.Segment()) && _verseRef.Value.HasSegmentsDefined)
                {
                    Type = UsfmVersificationErrorType.MissingVerseSegment;
                    return true;
                }
                if (!string.IsNullOrEmpty(_verseRef.Value.Segment()) && !_verseRef.Value.HasSegmentsDefined)
                {
                    Type = UsfmVersificationErrorType.ExtraVerseSegment;
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

        private static UsfmVersificationErrorType Map(VerseRef.ValidStatusType validStatus)
        {
            switch (validStatus)
            {
                case VerseRef.ValidStatusType.OutOfRange:
                    return UsfmVersificationErrorType.ExtraVerse;
                case VerseRef.ValidStatusType.VerseRepeated:
                case VerseRef.ValidStatusType.VerseOutOfOrder:
                    return UsfmVersificationErrorType.InvalidVerseRange;
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
                // We do not want to throw an exception here, and the VerseRef constructor can throw
                // an exception with certain invalid verse data; use TryParse instead.
                if (!VerseRef.TryParse($"{_bookNum} {_expectedChapter}:{_expectedVerse}", out VerseRef defaultVerseRef))
                {
                    return "";
                }
                if (Type == UsfmVersificationErrorType.ExtraVerse)
                    return "";
                if (
                    Type == UsfmVersificationErrorType.MissingVerseSegment
                    && VerseRef.TryParse(
                        $"{defaultVerseRef.Book} {defaultVerseRef.Chapter}:{defaultVerseRef.Verse}a",
                        out VerseRef verseWithSegment
                    )
                )
                {
                    return verseWithSegment.ToString();
                }
                if (Type == UsfmVersificationErrorType.InvalidVerseRange)
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

    public class UsfmVersificationErrorDetector : UsfmParserHandlerBase
    {
        private readonly ScrVers _versification;
        private int _currentBook;
        private int _currentChapter;
        private VerseRef _currentVerse;
        private readonly List<UsfmVersificationError> _errors;

        public UsfmVersificationErrorDetector(ScrVers versification)
        {
            _versification = versification;
            _currentBook = 0;
            _currentChapter = 0;
            _currentVerse = new VerseRef();
            _errors = new List<UsfmVersificationError>();
        }

        public IReadOnlyList<UsfmVersificationError> Errors => _errors;

        public override void EndUsfm(UsfmParserState state)
        {
            if (_currentBook > 0 && Canon.IsCanonical(_currentBook))
            {
                var versificationError = new UsfmVersificationError(
                    _currentBook,
                    _versification.GetLastChapter(_currentBook),
                    _versification.GetLastVerse(_currentBook, _versification.GetLastChapter(_currentBook)),
                    _currentChapter,
                    _currentVerse.AllVerses().Last().VerseNum
                );
                if (versificationError.CheckError())
                    _errors.Add(versificationError);
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
                var versificationError = new UsfmVersificationError(
                    _currentBook,
                    _currentChapter,
                    _versification.GetLastVerse(_currentBook, _currentChapter),
                    _currentChapter,
                    _currentVerse.AllVerses().Last().VerseNum
                );
                if (versificationError.CheckError())
                    _errors.Add(versificationError);
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
                var versificationError = new UsfmVersificationError(
                    _currentBook,
                    _currentChapter,
                    _currentVerse.AllVerses().Last().VerseNum,
                    _currentChapter,
                    _currentVerse.AllVerses().Last().VerseNum,
                    _currentVerse
                );
                if (versificationError.CheckError())
                    _errors.Add(versificationError);
            }
        }
    }
}
