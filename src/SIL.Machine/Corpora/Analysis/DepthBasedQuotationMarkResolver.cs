using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.Machine.Corpora.Analysis
{
    public class QuotationMarkResolverState
    {
        public Stack<QuotationMarkMetadata> Quotations { get; private set; }
        private int _currentDepth;

        public QuotationMarkResolverState()
        {
            Reset();
        }

        public void Reset()
        {
            Quotations = new Stack<QuotationMarkMetadata>();
            _currentDepth = 0;
        }

        public int CurrentDepth => _currentDepth + 1;

        public bool HasOpenQuotationMark => _currentDepth > 0;

        public bool AreMoreThanNQuotesOpen(int n) => _currentDepth > n;

        public QuotationMarkMetadata AddOpeningQuotationMark(QuotationMarkStringMatch quoteMatch)
        {
            QuotationMarkMetadata quote = quoteMatch.Resolve(_currentDepth + 1, QuotationMarkDirection.Opening);
            Quotations.Push(quote);
            _currentDepth++;
            return quote;
        }

        public QuotationMarkMetadata AddClosingQuotationMark(QuotationMarkStringMatch quoteMatch)
        {
            QuotationMarkMetadata quote = quoteMatch.Resolve(_currentDepth, QuotationMarkDirection.Closing);
            Quotations.Pop();
            _currentDepth++;
            return quote;
        }

        public string GetOpeningQuotationMarkAtDepth(int depth)
        {
            if (depth > Quotations.Count)
            {
                throw new InvalidOperationException(
                    "GetOpeningQuotationMarkAtDepth() was called with a depth greater than the quotation stack size."
                );
            }
            return Quotations.ToArray()[depth - 1].QuotationMark;
        }

        public string GetDeepestOpeningQuotationMark()
        {
            if (!HasOpenQuotationMark)
            {
                throw new InvalidOperationException(
                    "GetDeepestOpeningQuotationMark() was called with a depth greater than the quotation stack size."
                );
            }
            return Quotations.Peek().QuotationMark;
        }
    }

    public enum QuotationContinuerStyle
    {
        Undetermined,
        English,
        Spanish
    }

    public class QuotationContinuerState
    {
        private Stack<QuotationMarkMetadata> _quotationContinuers;
        public QuotationContinuerStyle ContinuerStyle { get; private set; }
        public int CurrentDepth { get; private set; }

        public QuotationContinuerState()
        {
            Reset();
        }

        public void Reset()
        {
            _quotationContinuers = new Stack<QuotationMarkMetadata>();
            CurrentDepth = 0;
            ContinuerStyle = QuotationContinuerStyle.Undetermined;
        }

        public bool ContinuerHasBeenObserved()
        {
            return _quotationContinuers.Count > 0;
        }

        public QuotationMarkMetadata AddQuotationContinuer(
            QuotationMarkStringMatch quoteMatch,
            QuotationMarkResolverState quotationMarkResolverState,
            QuotationContinuerStyle quotationContinuerStyle
        )
        {
            QuotationMarkMetadata quote = quoteMatch.Resolve(
                _quotationContinuers.Count + 1,
                QuotationMarkDirection.Opening
            );
            _quotationContinuers.Push(quote);
            CurrentDepth++;
            ContinuerStyle = quotationContinuerStyle;
            if (_quotationContinuers.Count == quotationMarkResolverState.Quotations.Count)
            {
                _quotationContinuers.Clear();
                CurrentDepth = 0;
            }
            return quote;
        }
    }

    public class QuotationMarkCategorizer
    {
        private static readonly Regex ApostrophePattern = new Regex(@"[\'\u2019\u2018]", RegexOptions.Compiled);
        private readonly IQuotationMarkResolutionSettings _settings;
        private readonly QuotationMarkResolverState _quotationMarkResolverState;
        private readonly QuotationContinuerState _quotationContinuerState;

        public QuotationMarkCategorizer(
            IQuotationMarkResolutionSettings quotationMarkResolutionSettings,
            QuotationMarkResolverState quotationMarkResolverState,
            QuotationContinuerState quotationContinuerState
        )
        {
            _settings = quotationMarkResolutionSettings;
            _quotationMarkResolverState = quotationMarkResolverState;
            _quotationContinuerState = quotationContinuerState;
        }

        public bool IsEnglishQuotationContinuer(
            QuotationMarkStringMatch quoteMatch,
            QuotationMarkStringMatch previousMatch,
            QuotationMarkStringMatch nextMatch
        )
        {
            if (_quotationContinuerState.ContinuerStyle == QuotationContinuerStyle.Spanish)
                return false;
            if (!MeetsQuoteContinuerPrerequisites(quoteMatch))
                return false;

            if (!_quotationContinuerState.ContinuerHasBeenObserved())
            {
                if (quoteMatch.StartIndex > 0)
                    return false;
                if (
                    quoteMatch.QuotationMark
                    != _quotationMarkResolverState.GetOpeningQuotationMarkAtDepth(
                        _quotationContinuerState.CurrentDepth + 1
                    )
                )
                {
                    return false;
                }
                if (_quotationMarkResolverState.AreMoreThanNQuotesOpen(1))
                {
                    if (nextMatch == null || nextMatch.StartIndex != quoteMatch.EndIndex)
                        return false;
                }
            }
            else
            {
                if (
                    quoteMatch.QuotationMark
                    != _quotationMarkResolverState.GetOpeningQuotationMarkAtDepth(
                        _quotationContinuerState.CurrentDepth + 1
                    )
                )
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsSpanishQuotationContinuer(
            QuotationMarkStringMatch quoteMatch,
            QuotationMarkStringMatch nextMatch,
            QuotationMarkStringMatch previousMatch
        )
        {
            if (_quotationContinuerState.ContinuerStyle == QuotationContinuerStyle.English)
                return false;
            if (!MeetsQuoteContinuerPrerequisites(quoteMatch))
                return false;

            if (!_quotationContinuerState.ContinuerHasBeenObserved())
            {
                if (quoteMatch.StartIndex > 0)
                    return false;

                // this has only been observed with guillemets so far
                if (quoteMatch.QuotationMark != "»")
                    return false;

                if (
                    !_settings.AreMarksAValidPair(
                        _quotationMarkResolverState.GetOpeningQuotationMarkAtDepth(
                            _quotationContinuerState.CurrentDepth + 1
                        ),
                        quoteMatch.QuotationMark
                    )
                )
                {
                    return false;
                }
                if (_quotationMarkResolverState.AreMoreThanNQuotesOpen(1))
                {
                    if (nextMatch == null || nextMatch.StartIndex != quoteMatch.EndIndex)
                        return false;
                }
            }
            else
            {
                if (
                    !_settings.AreMarksAValidPair(
                        _quotationMarkResolverState.GetOpeningQuotationMarkAtDepth(
                            _quotationContinuerState.CurrentDepth + 1
                        ),
                        quoteMatch.QuotationMark
                    )
                )
                {
                    return false;
                }
            }
            return true;
        }

        private bool MeetsQuoteContinuerPrerequisites(QuotationMarkStringMatch quoteMatch)
        {
            if (
                _settings.ShouldRelyOnParagraphMarkers()
                && !quoteMatch.TextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Paragraph)
            )
            {
                return false;
            }
            if (!_quotationMarkResolverState.HasOpenQuotationMark)
                return false;
            return true;
        }

        public bool IsOpeningQuote(QuotationMarkStringMatch match)
        {
            if (!_settings.IsValidOpeningQuotationMark(match))
                return false;

            // if the quote is ambiguous, use whitespace as clue
            if (_settings.IsValidClosingQuotationMark(match))
            {
                return (
                        match.HasLeadingWhitespace()
                        || MostRecentOpeningMarkImmediatelyPrecedes(match)
                        || match.HasQuoteIntroducerInLeadingSubstring()
                    ) && !(match.HasTrailingWhitespace() || match.HasTrailingPunctuation());
            }
            return true;
        }

        public bool IsClosingQuote(QuotationMarkStringMatch match)
        {
            if (!_settings.IsValidClosingQuotationMark(match))
                return false;

            // if the quote is ambiguous, use whitespace as clue
            if (_settings.IsValidOpeningQuotationMark(match))
            {
                return (
                        match.HasTrailingWhitespace()
                        || match.HasTrailingPunctuation()
                        || match.IsAtEndOfSegment
                        || match.NextCharacterMatches(_settings.GetClosingQuotationMarkRegex())
                    ) && !match.HasLeadingWhitespace();
            }
            return true;
        }

        public bool IsMalformedOpeningQuote(QuotationMarkStringMatch match)
        {
            if (!_settings.IsValidOpeningQuotationMark(match))
                return false;

            if (match.HasQuoteIntroducerInLeadingSubstring())
                return true;

            if (
                match.HasLeadingWhitespace()
                && match.HasTrailingWhitespace()
                && !_quotationMarkResolverState.HasOpenQuotationMark
            )
            {
                return true;
            }

            return false;
        }

        public bool IsMalformedClosingQuote(QuotationMarkStringMatch match)
        {
            if (!_settings.IsValidClosingQuotationMark(match))
                return false;

            return (
                (
                    match.IsAtEndOfSegment
                    || !match.HasTrailingWhitespace()
                    || (match.HasLeadingWhitespace() && match.HasTrailingWhitespace())
                )
                && _quotationMarkResolverState.HasOpenQuotationMark
                && _settings.AreMarksAValidPair(
                    _quotationMarkResolverState.GetDeepestOpeningQuotationMark(),
                    match.QuotationMark
                )
            );
        }

        public bool IsUnpairedClosingQuote(QuotationMarkStringMatch match)
        {
            if (!_settings.IsValidClosingQuotationMark(match))
                return false;

            if (_quotationMarkResolverState.HasOpenQuotationMark)
                return false;

            return !match.HasLeadingWhitespace() && (match.IsAtEndOfSegment || match.HasTrailingWhitespace());
        }

        private bool MostRecentOpeningMarkImmediatelyPrecedes(QuotationMarkStringMatch match)
        {
            if (!_quotationMarkResolverState.HasOpenQuotationMark)
                return false;

            return _quotationMarkResolverState.GetDeepestOpeningQuotationMark() == match.PreviousCharacter;
        }

        public bool IsApostrophe(QuotationMarkStringMatch match, QuotationMarkStringMatch nextMatch)
        {
            if (!match.QuotationMarkMatches(ApostrophePattern))
                return false;

            // Latin letters on both sides of punctuation mark
            if (
                match.PreviousCharacter != null
                && match.HasLeadingLatinLetter()
                && match.NextCharacter != null
                && match.HasTrailingLatinLetter()
            )
            {
                return true;
            }

            // potential final s possessive (e.g. Moses')
            if (
                match.PreviousCharacterMatches(new Regex(@"s", RegexOptions.Compiled))
                && (match.HasTrailingWhitespace() || match.HasTrailingPunctuation())
            )
            {
                // check whether it could be a closing quote
                if (!_quotationMarkResolverState.HasOpenQuotationMark)
                    return true;
                if (
                    !_settings.AreMarksAValidPair(
                        _quotationMarkResolverState.GetDeepestOpeningQuotationMark(),
                        match.QuotationMark
                    )
                )
                {
                    return true;
                }
                if (
                    nextMatch != null
                    && _settings.AreMarksAValidPair(
                        _quotationMarkResolverState.GetDeepestOpeningQuotationMark(),
                        nextMatch.QuotationMark
                    )
                )
                {
                    return true;
                }
            }

            // for languages that use apostrophes at teh start and end of words
            if (
                !_quotationMarkResolverState.HasOpenQuotationMark && match.QuotationMark == "'"
                || _quotationMarkResolverState.HasOpenQuotationMark
                    && !_settings.AreMarksAValidPair(
                        _quotationMarkResolverState.GetDeepestOpeningQuotationMark(),
                        match.QuotationMark
                    )
            )
            {
                return true;
            }

            return false;
        }
    }

    public class DepthBasedQuotationMarkResolver : IQuotationMarkResolver
    {
        private readonly IQuotationMarkResolutionSettings _settings;
        private readonly QuotationMarkResolverState _quotationMarkResolverState;
        private readonly QuotationContinuerState _quotationContinuerState;
        private readonly QuotationMarkCategorizer _quotationMarkCategorizer;
        private readonly HashSet<QuotationMarkResolutionIssue> _issues;

        public DepthBasedQuotationMarkResolver(IQuotationMarkResolutionSettings settings)
        {
            _settings = settings;
            _quotationMarkResolverState = new QuotationMarkResolverState();
            _quotationContinuerState = new QuotationContinuerState();
            _quotationMarkCategorizer = new QuotationMarkCategorizer(
                _settings,
                _quotationMarkResolverState,
                _quotationContinuerState
            );
            _issues = new HashSet<QuotationMarkResolutionIssue>();
        }

        public void Reset()
        {
            _quotationMarkResolverState.Reset();
            _quotationContinuerState.Reset();
            _issues.Clear();
        }

        public IEnumerable<QuotationMarkMetadata> ResolveQuotationMarks(List<QuotationMarkStringMatch> quoteMatches)
        {
            foreach ((int quoteIndex, QuotationMarkStringMatch quoteMatch) in quoteMatches.Select((q, i) => (i, q)))
            {
                QuotationMarkStringMatch previousMark = quoteIndex == 0 ? null : quoteMatches[quoteIndex - 1];
                QuotationMarkStringMatch nextMark =
                    quoteIndex == quoteMatches.Count - 1 ? null : quoteMatches[quoteIndex + 1];
                foreach (QuotationMarkMetadata q in ResolveQuotationMark(quoteMatch, previousMark, nextMark))
                    yield return q;
                if (_quotationMarkResolverState.HasOpenQuotationMark)
                    _issues.Add(QuotationMarkResolutionIssue.UnpairedQuotationMark);
            }
        }

        public IEnumerable<QuotationMarkMetadata> ResolveQuotationMark(
            QuotationMarkStringMatch quoteMatch,
            QuotationMarkStringMatch previousMatch,
            QuotationMarkStringMatch nextMatch
        )
        {
            if (_quotationMarkCategorizer.IsOpeningQuote(quoteMatch))
            {
                if (_quotationMarkCategorizer.IsEnglishQuotationContinuer(quoteMatch, previousMatch, nextMatch))
                {
                    yield return ProcessQuotationContinuer(quoteMatch, QuotationContinuerStyle.English);
                }
                else
                {
                    if (IsDepthTooGreat())
                    {
                        _issues.Add(QuotationMarkResolutionIssue.TooDeepNesting);
                        yield break;
                    }

                    yield return ProcessOpeningMark(quoteMatch);
                }
            }
            else if (_quotationMarkCategorizer.IsApostrophe(quoteMatch, nextMatch)) { }
            else if (_quotationMarkCategorizer.IsClosingQuote(quoteMatch))
            {
                if (_quotationMarkCategorizer.IsSpanishQuotationContinuer(quoteMatch, previousMatch, nextMatch))
                {
                    yield return ProcessQuotationContinuer(quoteMatch, QuotationContinuerStyle.Spanish);
                }
                else if (!_quotationMarkResolverState.HasOpenQuotationMark)
                {
                    _issues.Add(QuotationMarkResolutionIssue.UnpairedQuotationMark);
                    yield break;
                }
                else
                {
                    yield return ProcessClosingMark(quoteMatch);
                }
            }
            else if (_quotationMarkCategorizer.IsMalformedClosingQuote(quoteMatch))
            {
                yield return ProcessClosingMark(quoteMatch);
            }
            else if (_quotationMarkCategorizer.IsMalformedOpeningQuote(quoteMatch))
            {
                yield return ProcessOpeningMark(quoteMatch);
            }
            else if (_quotationMarkCategorizer.IsUnpairedClosingQuote(quoteMatch))
            {
                _issues.Add(QuotationMarkResolutionIssue.UnpairedQuotationMark);
            }
            else
            {
                _issues.Add(QuotationMarkResolutionIssue.AmbiguousQuotationMark);
            }
        }

        private QuotationMarkMetadata ProcessQuotationContinuer(
            QuotationMarkStringMatch quoteMatch,
            QuotationContinuerStyle continuerStyle
        )
        {
            return _quotationContinuerState.AddQuotationContinuer(
                quoteMatch,
                _quotationMarkResolverState,
                continuerStyle
            );
        }

        private bool IsDepthTooGreat()
        {
            return _quotationMarkResolverState.AreMoreThanNQuotesOpen(3);
        }

        private QuotationMarkMetadata ProcessOpeningMark(QuotationMarkStringMatch quoteMatch)
        {
            if (
                !_settings.MetadataMatchesQuotationMark(
                    quoteMatch.QuotationMark,
                    _quotationMarkResolverState.CurrentDepth,
                    QuotationMarkDirection.Opening
                )
            )
            {
                _issues.Add(QuotationMarkResolutionIssue.IncompatibleQuotationMark);
            }
            return _quotationMarkResolverState.AddOpeningQuotationMark(quoteMatch);
        }

        private QuotationMarkMetadata ProcessClosingMark(QuotationMarkStringMatch quoteMatch)
        {
            if (
                !_settings.MetadataMatchesQuotationMark(
                    quoteMatch.QuotationMark,
                    _quotationMarkResolverState.CurrentDepth - 1,
                    QuotationMarkDirection.Closing
                )
            )
            {
                _issues.Add(QuotationMarkResolutionIssue.IncompatibleQuotationMark);
            }
            return _quotationMarkResolverState.AddClosingQuotationMark(quoteMatch);
        }

        public HashSet<QuotationMarkResolutionIssue> GetIssues()
        {
            return _issues;
        }
    }
}
