using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.Machine.Corpora.Analysis
{
    public class ApostropheProportionStatistics
    {
        private int _numCharacters;
        private int _numApostrophes;

        public ApostropheProportionStatistics()
        {
            Reset();
        }

        public void Reset()
        {
            _numCharacters = 0;
            _numApostrophes = 0;
        }

        public void CountCharacters(TextSegment textSegment)
        {
            _numCharacters += textSegment.Length;
        }

        public void AddApostrophe()
        {
            _numApostrophes++;
        }

        public bool IsApostropheProportionGreaterThan(double threshold)
        {
            if (_numCharacters == 0)
                return false;
            return (_numApostrophes / _numCharacters) > threshold;
        }
    }

    public class QuotationMarkWordPositions
    {
        private Dictionary<string, int> _wordInitialOccurrences;
        private Dictionary<string, int> _midWordOccurrences;
        private Dictionary<string, int> _wordFinalOccurrences;

        public QuotationMarkWordPositions()
        {
            Reset();
        }

        public void Reset()
        {
            _wordInitialOccurrences = new Dictionary<string, int>();
            _midWordOccurrences = new Dictionary<string, int>();
            _wordFinalOccurrences = new Dictionary<string, int>();
        }

        public void CountWordInitialApostrophe(string quotationMark)
        {
            if (!_wordInitialOccurrences.ContainsKey(quotationMark))
            {
                _wordInitialOccurrences[quotationMark] = 0;
            }
            _wordInitialOccurrences[quotationMark]++;
        }

        public void CountMidWordApostrophe(string quotationMark)
        {
            if (!_midWordOccurrences.ContainsKey(quotationMark))
            {
                _midWordOccurrences[quotationMark] = 0;
            }
            _midWordOccurrences[quotationMark]++;
        }

        public void CountWordFinalApostrophe(string quotationMark)
        {
            if (!_wordFinalOccurrences.ContainsKey(quotationMark))
            {
                _wordFinalOccurrences[quotationMark] = 0;
            }
            _wordFinalOccurrences[quotationMark]++;
        }

        private int GetWordInitialOccurrences(string quotationMark)
        {
            return _wordInitialOccurrences.TryGetValue(quotationMark, out int count) ? count : 0;
        }

        private int GetMidWordOccurrences(string quotationMark)
        {
            return _midWordOccurrences.TryGetValue(quotationMark, out int count) ? count : 0;
        }

        private int GetWordFinalOccurrences(string quotationMark)
        {
            return _wordFinalOccurrences.TryGetValue(quotationMark, out int count) ? count : 0;
        }

        private int GetTotalOccurrences(string quotationMark)
        {
            return GetWordInitialOccurrences(quotationMark)
                + GetMidWordOccurrences(quotationMark)
                + GetWordFinalOccurrences(quotationMark);
        }

        public bool IsMarkRarelyInitial(string quotationMark)
        {
            int numInitialMarks = GetWordInitialOccurrences(quotationMark);
            int numTotalMarks = GetTotalOccurrences(quotationMark);
            return numTotalMarks > 0 && (numInitialMarks / numTotalMarks) < 0.1;
        }

        public bool IsMarkRarelyFinal(string quotationMark)
        {
            int numFinalMarks = GetWordFinalOccurrences(quotationMark);
            int numTotalMarks = GetTotalOccurrences(quotationMark);
            return numTotalMarks > 0 && (numFinalMarks / numTotalMarks) < 0.1;
        }

        public bool AreInitialAndFinalRatesSimilar(string quotationMark)
        {
            int numInitialMarks = GetWordInitialOccurrences(quotationMark);
            int numFinalMarks = GetWordFinalOccurrences(quotationMark);
            int numTotalMarks = GetTotalOccurrences(quotationMark);
            return numTotalMarks > 0 && (Math.Abs(numInitialMarks - numFinalMarks) / numTotalMarks) < 0.3;
        }

        public bool IsMarkCommonlyMidWord(string quotationMark)
        {
            int numMidWordMarks = GetMidWordOccurrences(quotationMark);
            int numTotalMarks = GetTotalOccurrences(quotationMark);
            return numTotalMarks > 0 && (numMidWordMarks / numTotalMarks) > 0.3;
        }
    }

    public class QuotationMarkSequences
    {
        private Dictionary<string, int> _earlierQuotationMarkCounts;
        private Dictionary<string, int> _laterQuotationMarkCounts;

        public QuotationMarkSequences()
        {
            Reset();
        }

        public void Reset()
        {
            _earlierQuotationMarkCounts = new Dictionary<string, int>();
            _laterQuotationMarkCounts = new Dictionary<string, int>();
        }

        public void RecordEarlierQuotationMark(string quotationMark)
        {
            if (!_earlierQuotationMarkCounts.ContainsKey(quotationMark))
            {
                _earlierQuotationMarkCounts[quotationMark] = 0;
            }
            _earlierQuotationMarkCounts[quotationMark] += 1;
        }

        public void RecordLaterQuotationMark(string quotationMark)
        {
            if (!_laterQuotationMarkCounts.ContainsKey(quotationMark))
            {
                _laterQuotationMarkCounts[quotationMark] = 0;
            }
            _laterQuotationMarkCounts[quotationMark] += 1;
        }

        private int GetEarlierOccurrences(string quotationMark)
        {
            return _earlierQuotationMarkCounts.TryGetValue(quotationMark, out int count) ? count : 0;
        }

        private int GetLaterOccurrences(string quotationMark)
        {
            return _laterQuotationMarkCounts.TryGetValue(quotationMark, out int count) ? count : 0;
        }

        public bool IsMarkMuchMoreCommonEarlier(string quotationMark)
        {
            int numEarlyOccurrences = GetEarlierOccurrences(quotationMark);
            int numLateOccurrences = GetLaterOccurrences(quotationMark);
            return (numLateOccurrences == 0 && numEarlyOccurrences > 5)
                || numEarlyOccurrences > numLateOccurrences * 10;
        }

        public bool IsMarkMuchMoreCommonLater(string quotationMark)
        {
            int numEarlyOccurrences = GetEarlierOccurrences(quotationMark);
            int numLateOccurrences = GetLaterOccurrences(quotationMark);
            return (numEarlyOccurrences == 0 && numLateOccurrences > 5)
                || numLateOccurrences > numEarlyOccurrences * 10;
        }

        public bool IsMarkCommonEarlyAndLate(string quotationMark)
        {
            int numEarlyOccurrences = GetEarlierOccurrences(quotationMark);
            int numLateOccurrences = GetLaterOccurrences(quotationMark);
            return numEarlyOccurrences > 0
                && (Math.Abs(numLateOccurrences - numEarlyOccurrences) / numEarlyOccurrences) < 0.2;
        }
    }

    public class QuotationMarkGrouper
    {
        private readonly QuoteConventionSet _quoteConventionSet;
        private Dictionary<string, List<QuotationMarkStringMatch>> _groupedQuotationMarks;

        public QuotationMarkGrouper(
            List<QuotationMarkStringMatch> quotationMarks,
            QuoteConventionSet quoteConventionSet
        )
        {
            _quoteConventionSet = quoteConventionSet;
            GroupQuotationMarks(quotationMarks);
        }

        private void GroupQuotationMarks(List<QuotationMarkStringMatch> quotationMarks)
        {
            _groupedQuotationMarks = quotationMarks
                .GroupBy(qmm => qmm.QuotationMark)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public IEnumerable<(string Mark1, string Mark2)> GetQuotationMarkPairs()
        {
            foreach (
                (string mark1, List<QuotationMarkStringMatch> matches1) in _groupedQuotationMarks.Select(kvp =>
                    (kvp.Key, kvp.Value)
                )
            )
            {
                // handle cases of identical opening/closing marks
                if (
                    matches1.Count == 2
                    && _quoteConventionSet.IsQuotationMarkDirectionAmbiguous(mark1)
                    && !HasDistinctPairedQuotationMark(mark1)
                )
                {
                    yield return (mark1, mark1);
                    continue;
                }

                // skip verses where quotation mark pairs are ambiguous
                if (matches1.Count > 1)
                    continue;

                // find matching closing marks
                foreach (
                    (string mark2, List<QuotationMarkStringMatch> matches2) in _groupedQuotationMarks.Select(kvp =>
                        (kvp.Key, kvp.Value)
                    )
                )
                {
                    if (
                        matches2.Count == 1
                        && _quoteConventionSet.MarksAreAValidPair(mark1, mark2)
                        && matches1[0].Precedes(matches2[0])
                    )
                    {
                        yield return (mark1, mark2);
                    }
                }
            }
        }

        public bool HasDistinctPairedQuotationMark(string quotationMark)
        {
            return _quoteConventionSet
                .GetPossiblePairedQuotationMarks(quotationMark)
                .Any(m => m != quotationMark && _groupedQuotationMarks.ContainsKey(m));
        }
    }

    public class PreliminaryApostropheAnalyzer
    {
        private static readonly Regex ApostrophePattern = new Regex(@"[\'\u2019]", RegexOptions.Compiled);
        private readonly ApostropheProportionStatistics _apostropheProportionStatistics;
        private readonly QuotationMarkWordPositions _wordPositionStatistics;

        public PreliminaryApostropheAnalyzer()
        {
            _apostropheProportionStatistics = new ApostropheProportionStatistics();
            _wordPositionStatistics = new QuotationMarkWordPositions();
            Reset();
        }

        public void Reset()
        {
            _apostropheProportionStatistics.Reset();
            _wordPositionStatistics.Reset();
        }

        public void ProcessQuotationMarks(List<TextSegment> textSegments, List<QuotationMarkStringMatch> quotationMarks)
        {
            foreach (TextSegment textSegment in textSegments)
                _apostropheProportionStatistics.CountCharacters(textSegment);
            foreach (QuotationMarkStringMatch quotationMarkMatch in quotationMarks)
                ProcessQuotationMark(quotationMarkMatch);
        }

        private void ProcessQuotationMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            if (quotationMarkMatch.QuotationMarkMatches(ApostrophePattern))
                CountApostrophe(quotationMarkMatch);
        }

        private void CountApostrophe(QuotationMarkStringMatch apostropheMatch)
        {
            string apostrophe = apostropheMatch.QuotationMark;
            _apostropheProportionStatistics.AddApostrophe();
            if (IsMatchWordInitial(apostropheMatch))
            {
                _wordPositionStatistics.CountWordInitialApostrophe(apostrophe);
            }
            else if (IsMatchMidWord(apostropheMatch))
            {
                _wordPositionStatistics.CountMidWordApostrophe(apostrophe);
            }
            else if (IsMatchWordFinal(apostropheMatch))
            {
                _wordPositionStatistics.CountWordFinalApostrophe(apostrophe);
            }
        }

        private bool IsMatchWordInitial(QuotationMarkStringMatch apostropheMatch)
        {
            if (apostropheMatch.HasTrailingWhitespace())
                return false;
            if (!apostropheMatch.IsAtStartOfSegment && !apostropheMatch.HasLeadingWhitespace())
                return false;
            return true;
        }

        private bool IsMatchMidWord(QuotationMarkStringMatch apostropheMatch)
        {
            if (apostropheMatch.HasTrailingWhitespace())
                return false;
            if (apostropheMatch.HasLeadingWhitespace())
                return false;
            return true;
        }

        private bool IsMatchWordFinal(QuotationMarkStringMatch apostropheMatch)
        {
            if (!apostropheMatch.IsAtEndOfSegment && !apostropheMatch.HasTrailingWhitespace())
                return false;
            if (apostropheMatch.HasLeadingWhitespace())
                return false;
            return true;
        }

        public bool IsApostropheOnly(string mark)
        {
            if (!ApostrophePattern.IsMatch(mark))
                return false;

            if (_wordPositionStatistics.IsMarkRarelyInitial(mark) || _wordPositionStatistics.IsMarkRarelyInitial(mark))
                return true;

            if (
                _wordPositionStatistics.AreInitialAndFinalRatesSimilar(mark)
                && _wordPositionStatistics.IsMarkCommonlyMidWord(mark)
            )
            {
                return true;
            }

            if (_apostropheProportionStatistics.IsApostropheProportionGreaterThan(0.02))
            {
                return true;
            }

            return false;
        }
    }

    public class PreliminaryQuotationAnalyzer
    {
        private readonly QuoteConventionSet _quoteConventions;
        private readonly PreliminaryApostropheAnalyzer _apostropheAnalyzer;
        private readonly QuotationMarkSequences _quotationMarkSequences;

        public PreliminaryQuotationAnalyzer(QuoteConventionSet quoteConventions)
        {
            _quoteConventions = quoteConventions;
            _apostropheAnalyzer = new PreliminaryApostropheAnalyzer();
            _quotationMarkSequences = new QuotationMarkSequences();
            Reset();
        }

        public void Reset()
        {
            _apostropheAnalyzer.Reset();
            _quotationMarkSequences.Reset();
        }

        public QuoteConventionSet NarrowDownPossibleQuoteConventions(List<Chapter> chapters)
        {
            foreach (Chapter chapter in chapters)
                AnalyzeQuotationMarksForChapter(chapter);
            return SelectCompatibleQuoteConventions();
        }

        private void AnalyzeQuotationMarksForChapter(Chapter chapter)
        {
            foreach (Verse verse in chapter.Verses)
                AnalyzeQuotationMarksForVerse(verse);
        }

        private void AnalyzeQuotationMarksForVerse(Verse verse)
        {
            List<QuotationMarkStringMatch> quotationMarks = new QuotationMarkFinder(
                _quoteConventions
            ).FindAllPotentialQuotationMarksInVerse(verse);
            AnalyzeQuotationMarkSequence(quotationMarks);
            _apostropheAnalyzer.ProcessQuotationMarks(verse.TextSegments, quotationMarks);
        }

        private void AnalyzeQuotationMarkSequence(List<QuotationMarkStringMatch> quotationMarks)
        {
            var quotationMarkGrouper = new QuotationMarkGrouper(quotationMarks, _quoteConventions);
            foreach ((string earlierMark, string laterMark) in quotationMarkGrouper.GetQuotationMarkPairs())
            {
                _quotationMarkSequences.RecordEarlierQuotationMark(earlierMark);
                _quotationMarkSequences.RecordLaterQuotationMark(laterMark);
            }
        }

        public QuoteConventionSet SelectCompatibleQuoteConventions()
        {
            List<string> openingQuotationMarks = FindOpeningQuotationMarks();
            List<string> closingQuotationMarks = FindClosingQuotationMarks();

            return _quoteConventions.FilterToCompatibleQuoteConventions(openingQuotationMarks, closingQuotationMarks);
        }

        private List<string> FindOpeningQuotationMarks()
        {
            return _quoteConventions
                .GetPossibleOpeningQuotationMarks()
                .Where(qm => IsOpeningQuotationMark(qm))
                .ToList();
        }

        private bool IsOpeningQuotationMark(string quotationMark)
        {
            if (_apostropheAnalyzer.IsApostropheOnly(quotationMark))
                return false;

            if (_quotationMarkSequences.IsMarkMuchMoreCommonEarlier(quotationMark))
                return true;
            if (
                _quotationMarkSequences.IsMarkCommonEarlyAndLate(quotationMark)
                && _quoteConventions.IsQuotationMarkDirectionAmbiguous(quotationMark)
            )
            {
                return true;
            }
            return false;
        }

        private List<string> FindClosingQuotationMarks()
        {
            return _quoteConventions
                .GetPossibleClosingQuotationMarks()
                .Where(qm => IsClosingQuotationMark(qm))
                .ToList();
        }

        private bool IsClosingQuotationMark(string quotationMark)
        {
            if (_apostropheAnalyzer.IsApostropheOnly(quotationMark))
                return false;

            if (_quotationMarkSequences.IsMarkMuchMoreCommonLater(quotationMark))
                return true;
            if (
                _quotationMarkSequences.IsMarkCommonEarlyAndLate(quotationMark)
                && _quoteConventions.IsQuotationMarkDirectionAmbiguous(quotationMark)
            )
            {
                return true;
            }
            return false;
        }
    }
}
