using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public enum UsfmVersificationDiagnosticType
    {
        Missing, //Missing content
        Extra, // Extra content
        Invalid, // Invalid verse or chapter reference
        IncorrectVerseSegment, // Verse segment in vrs but not in USFM or segment in USFM but not in vrs
        UnsupportedVerseRange, // Verse range that will cross chapter boundaries when mapped to ScrVers.Original
    }

    public class UsfmVersificationDiagnostic
    {
        public UsfmVersificationDiagnosticType Type { get; internal set; }
        public int NumAffectedVerses => References.Sum(vr => vr.AllVerses().Count());
        public List<VerseRef> References { get; internal set; } //Expected verses for Missing, actual verses for Extra and Invalid
        public string Filename { get; internal set; }
        public List<int> LineNumbers { get; internal set; }

        internal void Extend(VerseRef verseReference)
        {
            if (References.Count > 0) // Combine contiguous references
            {
                VerseRef lastReference = References[References.Count - 1];
                if (verseReference.Book == lastReference.Book && verseReference.ChapterNum == lastReference.ChapterNum)
                {
                    int lastVerseNum = lastReference.AllVerses().Last().VerseNum;
                    int nextVerseNum = verseReference.AllVerses().First().VerseNum;
                    if (
                        nextVerseNum == lastVerseNum + 1
                        && VerseRef.TryParse(
                            $"{verseReference.Book} {verseReference.ChapterNum}:{lastReference.AllVerses().First().VerseNum}-{verseReference.AllVerses().Last().VerseNum}",
                            out VerseRef updatedVerseReference
                        )
                    )
                    {
                        References[References.Count - 1] = updatedVerseReference;
                        return;
                    }
                }

                References.Add(verseReference);
            }
            else
            {
                References.Add(verseReference);
            }
        }

        internal void Extend(VerseRef verseReference, int lineNumber)
        {
            Extend(verseReference);
            LineNumbers.Add(lineNumber);
        }
    }

    public class UsfmVersificationAnalysis
    {
        public int TotalNumAffectedVerses { get; internal set; }
        public int TotalNumEncounteredVerses { get; internal set; }
        public IReadOnlyList<UsfmVersificationDiagnostic> Diagnostics { get; internal set; }
        public ParatextProjectSettings ProjectSettings { get; internal set; }
    }

    public class UsfmVersificationAnalyzerHandler : UsfmParserHandlerBase
    {
        private readonly ParatextProjectSettings _settings;
        private readonly IEnumerator<VerseRef> _expectedVerses;
        private readonly List<UsfmVersificationDiagnostic> _diagnostics;
        private readonly Dictionary<int, HashSet<int>> _onlyChapters;
        private string _filename;
        private bool _lastVerseInError;
        private bool _lastVerseWasExtra;
        private bool _lastVerseWasInvalid;
        private int _totalVersesAnalyzed;
        private int _lastLineNumber;
        private bool _hasMore;
        private VerseRef _nextExpectedVerse;
        private VerseRef _prevEncounteredVerseRef;

        private void GetNextExpectedVerse()
        {
            _nextExpectedVerse = _expectedVerses.Current;
            _hasMore = _expectedVerses.MoveNext();
        }

        private UsfmVersificationDiagnostic CurrentError => _diagnostics[_diagnostics.Count - 1];

        public UsfmVersificationAnalyzerHandler(
            ParatextProjectSettings settings,
            Dictionary<int, HashSet<int>> onlyChapters
        )
        {
            _settings = settings;
            _onlyChapters = onlyChapters;
            _expectedVerses = _settings.Versification.AllIncludedVerses(onlyChapters).GetEnumerator();
            _hasMore = _expectedVerses.MoveNext();
            _prevEncounteredVerseRef = new VerseRef(1, 1, 0);
            _diagnostics = new List<UsfmVersificationDiagnostic>();
            _filename = null;
            _lastVerseInError = false;
            _lastVerseWasExtra = false;
            _lastVerseWasInvalid = false;
            _totalVersesAnalyzed = 0;
            _lastLineNumber = 1;
        }

        public UsfmVersificationAnalysis GetAnalysis()
        {
            while (_hasMore)
            {
                if (!_lastVerseWasInvalid)
                    GetNextExpectedVerse();
                HandleMissingVerse();
                _lastVerseWasInvalid = false;
            }
            return new UsfmVersificationAnalysis
            {
                TotalNumAffectedVerses = _diagnostics.Sum(d => d.NumAffectedVerses),
                Diagnostics = _diagnostics,
                TotalNumEncounteredVerses = _totalVersesAnalyzed,
                ProjectSettings = _settings,
            };
        }

        public override void StartBook(UsfmParserState state, string marker, string code)
        {
            _filename = _settings.GetBookFileName(state.VerseRef.Book);
        }

        public override void Chapter(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            VerseRef verseRef = state.VerseRef;
            if (!Canon.IsCanonical(verseRef.Book))
                return;
            verseRef.Chapter = number;
            if (verseRef.ChapterNum == -1)
            {
                _diagnostics.Add(
                    new UsfmVersificationDiagnostic
                    {
                        Type = UsfmVersificationDiagnosticType.Invalid,
                        References = new List<VerseRef> { verseRef },
                        Filename = _filename,
                        LineNumbers = new List<int> { state.LineNumber },
                    }
                );
                _lastVerseInError = true;
            }
        }

        public override void Verse(
            UsfmParserState state,
            string number,
            string marker,
            string altNumber,
            string pubNumber
        )
        {
            VerseRef currentVerses = state.VerseRef;
            if (
                _onlyChapters != null
                && (
                    !_onlyChapters.TryGetValue(currentVerses.BookNum, out HashSet<int> chapters)
                    || (chapters != null && !chapters.Contains(currentVerses.ChapterNum))
                )
            )
            {
                return;
            }

            VerseRef verseRef = currentVerses;
            if (!Canon.IsCanonical(verseRef.Book))
                return;
            verseRef.Verse = number;
            bool invalidVerseNum = verseRef.VerseNum == -1;
            bool badVerseRange = (
                verseRef.ValidStatus == VerseRef.ValidStatusType.VerseOutOfOrder
                || verseRef.ValidStatus == VerseRef.ValidStatusType.VerseRepeated
            );
            if (invalidVerseNum || badVerseRange)
            {
                HandleInvalidVerse(state, verseRef);
                _lastVerseWasInvalid = true;
            }
            else
            {
                _lastVerseWasInvalid = false;
            }

            bool segmentMismatch = string.IsNullOrEmpty(currentVerses.Segment()) == currentVerses.HasSegmentsDefined;
            if (segmentMismatch)
            {
                HandleIncorrectVerseSegment(state, verseRef);
            }

            if (currentVerses.HasMultiple)
            {
                VerseRef copy = currentVerses;
                bool hasCrossChapterVerseRange = !copy.ChangeVersificationWithRanges(ScrVers.Original);
                if (hasCrossChapterVerseRange)
                {
                    _diagnostics.Add(
                        new UsfmVersificationDiagnostic
                        {
                            Type = UsfmVersificationDiagnosticType.UnsupportedVerseRange,
                            References = new List<VerseRef> { currentVerses },
                            Filename = _filename,
                            LineNumbers = new List<int> { state.LineNumber },
                        }
                    );
                }
            }

            foreach (VerseRef currentVerse in currentVerses.AllVerses().OrderBy(v => v))
            {
                if (_prevEncounteredVerseRef.CompareTo(currentVerse, null, true, compareSegments: false) < 0) //Properly handle verse segments
                {
                    _totalVersesAnalyzed++;
                    if (!_lastVerseWasExtra && _hasMore)
                    {
                        GetNextExpectedVerse();
                    }
                }
                if (_nextExpectedVerse.IsDefault)
                {
                    continue;
                }
                int compare = _nextExpectedVerse.CompareTo(currentVerse, null, true, compareSegments: false);
                if (compare < 0 && _hasMore)
                {
                    HandleMissingVerse();
                    GetNextExpectedVerse();
                    while (
                        _hasMore && _nextExpectedVerse.CompareTo(currentVerse, null, true, compareSegments: false) < 0
                    )
                    {
                        CurrentError.Extend(_nextExpectedVerse);
                        GetNextExpectedVerse();
                    }
                }
                else if ((compare > 0 && !_lastVerseWasInvalid) || (compare < 0 && !_hasMore)) //We want Invalid and Extra to be mutually exclusive to avoid duplicate errors for every Invalid/Extra verse
                {
                    if (!_hasMore && _nextExpectedVerse.CompareTo(_prevEncounteredVerseRef) > 0)
                    {
                        _diagnostics.Add(
                            new UsfmVersificationDiagnostic
                            {
                                Type = UsfmVersificationDiagnosticType.Missing,
                                References = new List<VerseRef> { _nextExpectedVerse },
                                Filename = _filename,
                                LineNumbers = new List<int> { _lastLineNumber },
                            }
                        );
                    }

                    HandleExtraVerse(state.LineNumber, currentVerse);
                }
                else
                {
                    _lastVerseInError = false;
                }
                if (compare <= 0)
                    _lastVerseWasExtra = false;

                _prevEncounteredVerseRef = currentVerse;
            }

            _lastLineNumber = state.LineNumber;
        }

        private void HandleInvalidVerse(UsfmParserState state, VerseRef verseRef)
        {
            _diagnostics.Add(
                new UsfmVersificationDiagnostic
                {
                    Type = UsfmVersificationDiagnosticType.Invalid,
                    References = new List<VerseRef> { verseRef },
                    Filename = _filename,
                    LineNumbers = new List<int> { state.LineNumber },
                }
            );
            _lastVerseInError = true;
        }

        private void HandleIncorrectVerseSegment(UsfmParserState state, VerseRef verseRef)
        {
            _diagnostics.Add(
                new UsfmVersificationDiagnostic
                {
                    Type = UsfmVersificationDiagnosticType.IncorrectVerseSegment,
                    References = new List<VerseRef> { verseRef },
                    Filename = _filename,
                    LineNumbers = new List<int> { state.LineNumber },
                }
            );
            _lastVerseInError = true;
        }

        private void HandleExtraVerse(int lineNumber, VerseRef currentVerse)
        {
            if (!_lastVerseInError || (_lastVerseInError && CurrentError.Type != UsfmVersificationDiagnosticType.Extra))
            {
                _diagnostics.Add(
                    new UsfmVersificationDiagnostic
                    {
                        Type = UsfmVersificationDiagnosticType.Extra,
                        References = new List<VerseRef> { currentVerse },
                        Filename = _filename,
                        LineNumbers = new List<int> { lineNumber },
                    }
                );
                _lastVerseInError = true;
            }
            else
            {
                CurrentError.Extend(currentVerse, lineNumber);
            }
            _lastVerseWasExtra = true;
        }

        private void HandleMissingVerse()
        {
            if (
                !_lastVerseInError
                || (_lastVerseInError && CurrentError.Type != UsfmVersificationDiagnosticType.Missing)
            )
            {
                _diagnostics.Add(
                    new UsfmVersificationDiagnostic
                    {
                        Type = UsfmVersificationDiagnosticType.Missing,
                        References = new List<VerseRef> { _nextExpectedVerse },
                        Filename = _filename,
                        LineNumbers = new List<int> { _lastLineNumber },
                    }
                );
                _lastVerseInError = true;
            }
            else
            {
                CurrentError.Extend(_nextExpectedVerse);
            }
        }
    }
}
