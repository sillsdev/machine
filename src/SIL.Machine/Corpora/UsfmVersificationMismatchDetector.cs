using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public enum UsfmVersificationMismatchType
    {
        MissingChapter,
        ExtraChapter,
        MissingVerse,
        ExtraVerse,
        MissingSegment,
        ExtraSegment
    }

    public class UsfmVersificationMismatch //TODO Better name
    {
        public int BookNum { get; set; }
        public int ExpectedChapter { get; set; }
        public int ExpectedVerse { get; set; }
        public int ActualChapter { get; set; }
        public int ActualVerse { get; set; }

        public string ExpectedSegment { get; set; }
        public string ActualSegment { get; set; }
        public UsfmVersificationMismatchType Type { get; set; }

        public bool IsMismatch()
        {
            if (ExpectedChapter != ActualChapter) //TODO set type
                return true;
            if (ExpectedVerse != ActualVerse)
                return true;
            if (ExpectedSegment != ActualSegment && ExpectedSegment != null)
                return true;
            return false;
        }

        public string ExpectedVerseRef => new VerseRef(BookNum, ExpectedChapter, ExpectedVerse).ToString();
        public string ActualVerseRef => new VerseRef(BookNum, ActualChapter, ActualVerse).ToString();
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

        public bool HasError => _errors.Count > 0;
        public IReadOnlyList<UsfmVersificationMismatch> Errors => _errors;

        public override void StartBook(UsfmParserState state, string marker, string code)
        {
            if (_currentBook > 0 && Canon.IsCanonical(_currentBook))
            {
                var versificationMismatch = new UsfmVersificationMismatch()
                {
                    BookNum = _currentBook,
                    ExpectedChapter = _versification.GetLastChapter(_currentBook),
                    ExpectedVerse = _versification.GetLastVerse(_currentBook, _currentChapter),
                    ActualChapter = _currentChapter,
                    ActualVerse = _currentVerse.AllVerses().Last().VerseNum,
                };
                if (versificationMismatch.IsMismatch())
                    _errors.Add(versificationMismatch);
            }

            _currentBook = state.VerseRef.BookNum;
            _currentChapter = 0;
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
        }

        public override void Chapter(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            if (_currentChapter != 0)
            {
                var versificationMismatch = new UsfmVersificationMismatch()
                {
                    BookNum = _currentBook,
                    ExpectedChapter = _currentChapter,
                    ExpectedVerse = _versification.GetLastVerse(_currentBook, _currentChapter),
                    ActualChapter = _currentChapter,
                    ActualVerse = _currentVerse.AllVerses().Last().VerseNum,
                };
                if (versificationMismatch.IsMismatch())
                    _errors.Add(versificationMismatch);
            }

            _currentChapter = state.VerseRef.ChapterNum;
            _currentVerse = new VerseRef();
        }
    }
}
