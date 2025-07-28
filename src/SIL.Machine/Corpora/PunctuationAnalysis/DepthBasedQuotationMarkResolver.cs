using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.Machine.Corpora.PunctuationAnalysis
{
    public class QuotationMarkResolverState
    {
        public Stack<QuotationMarkMetadata> Quotations { get; private set; }

        public QuotationMarkResolverState()
        {
            Reset();
        }

        public void Reset()
        {
            Quotations = new Stack<QuotationMarkMetadata>();
        }

        public int CurrentDepth => Quotations.Count;

        public bool HasOpenQuotationMark => CurrentDepth > 0;

        public bool AreMoreThanNQuotesOpen(int n) => CurrentDepth > n;

        public QuotationMarkMetadata AddOpeningQuotationMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            QuotationMarkMetadata quotationMark = quotationMarkMatch.Resolve(
                CurrentDepth + 1,
                QuotationMarkDirection.Opening
            );
            Quotations.Push(quotationMark);
            return quotationMark;
        }

        public QuotationMarkMetadata AddClosingQuotationMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            QuotationMarkMetadata quotationMark = quotationMarkMatch.Resolve(
                CurrentDepth,
                QuotationMarkDirection.Closing
            );
            Quotations.Pop();
            return quotationMark;
        }

        public string GetOpeningQuotationMarkAtDepth(int depth)
        {
            if (depth > CurrentDepth)
            {
                throw new InvalidOperationException(
                    $"Opening quotation mark at depth ${depth} was requested from a quotation stack with depth ${CurrentDepth}."
                );
            }
            // Stack is stored in reverse order
            return Quotations.ToArray()[CurrentDepth - depth].QuotationMark;
        }

        public string GetDeepestOpeningQuotationMark()
        {
            if (!HasOpenQuotationMark)
            {
                throw new InvalidOperationException(
                    "The deepest opening quotation mark was requested from an empty quotation stack."
                );
            }
            return Quotations.Peek().QuotationMark;
        }
    }

    public enum QuoteContinuerStyle
    {
        Undetermined,
        English,
        Spanish
    }

    public class QuoteContinuerState
    {
        public Stack<QuotationMarkMetadata> QuoteContinuerMarks { get; private set; }
        public QuoteContinuerStyle ContinuerStyle { get; protected set; }
        public int CurrentDepth => QuoteContinuerMarks.Count;

        public QuoteContinuerState()
        {
            Reset();
        }

        public void Reset()
        {
            QuoteContinuerMarks = new Stack<QuotationMarkMetadata>();
            ContinuerStyle = QuoteContinuerStyle.Undetermined;
        }

        public bool ContinuerHasBeenObserved()
        {
            return QuoteContinuerMarks.Count > 0;
        }

        public QuotationMarkMetadata AddQuoteContinuer(
            QuotationMarkStringMatch quotationMarkMatch,
            QuotationMarkResolverState quotationMarkResolverState,
            QuoteContinuerStyle quoteContinuerStyle
        )
        {
            QuotationMarkMetadata quote = quotationMarkMatch.Resolve(
                QuoteContinuerMarks.Count + 1,
                QuotationMarkDirection.Opening
            );
            QuoteContinuerMarks.Push(quote);
            ContinuerStyle = quoteContinuerStyle;
            if (CurrentDepth == quotationMarkResolverState.CurrentDepth)
            {
                QuoteContinuerMarks.Clear();
            }
            return quote;
        }
    }

    public class QuotationMarkCategorizer
    {
        private static readonly Regex ApostrophePattern = new Regex(@"[\'\u2019\u2018]", RegexOptions.Compiled);
        private readonly IQuotationMarkResolutionSettings _settings;
        private readonly QuotationMarkResolverState _quotationMarkResolverState;
        private readonly QuoteContinuerState _quoteContinuerState;

        public QuotationMarkCategorizer(
            IQuotationMarkResolutionSettings quotationMarkResolutionSettings,
            QuotationMarkResolverState quotationMarkResolverState,
            QuoteContinuerState quotationContinuerState
        )
        {
            _settings = quotationMarkResolutionSettings;
            _quotationMarkResolverState = quotationMarkResolverState;
            _quoteContinuerState = quotationContinuerState;
        }

        public bool IsEnglishQuoteContinuer(
            QuotationMarkStringMatch quotationMarkMatch,
            QuotationMarkStringMatch previousMatch,
            QuotationMarkStringMatch nextMatch
        )
        {
            if (_quoteContinuerState.ContinuerStyle == QuoteContinuerStyle.Spanish)
                return false;
            if (!MeetsQuoteContinuerPrerequisites(quotationMarkMatch))
                return false;
            if (
                quotationMarkMatch.QuotationMark
                != _quotationMarkResolverState.GetOpeningQuotationMarkAtDepth(_quoteContinuerState.CurrentDepth + 1)
            )
            {
                return false;
            }
            if (!_quoteContinuerState.ContinuerHasBeenObserved())
            {
                if (quotationMarkMatch.StartIndex > 0)
                    return false;

                // Check the next quotation mark match, since quote continuers must appear consecutively
                if (_quotationMarkResolverState.AreMoreThanNQuotesOpen(1))
                {
                    if (nextMatch == null || nextMatch.StartIndex != quotationMarkMatch.EndIndex)
                        return false;
                }
            }
            return true;
        }

        public bool IsSpanishQuoteContinuer(
            QuotationMarkStringMatch quotationMarkMatch,
            QuotationMarkStringMatch previousMatch,
            QuotationMarkStringMatch nextMatch
        )
        {
            if (_quoteContinuerState.ContinuerStyle == QuoteContinuerStyle.English)
                return false;
            if (!MeetsQuoteContinuerPrerequisites(quotationMarkMatch))
                return false;

            if (
                !_settings.AreMarksAValidPair(
                    _quotationMarkResolverState.GetOpeningQuotationMarkAtDepth(_quoteContinuerState.CurrentDepth + 1),
                    quotationMarkMatch.QuotationMark
                )
            )
            {
                return false;
            }

            if (!_quoteContinuerState.ContinuerHasBeenObserved())
            {
                if (quotationMarkMatch.StartIndex > 0)
                    return false;

                // This has only been observed with guillemets so far
                if (quotationMarkMatch.QuotationMark != "»")
                    return false;

                // Check the next quotation mark match, since quote continuers must appear consecutively
                if (_quotationMarkResolverState.AreMoreThanNQuotesOpen(1))
                {
                    if (nextMatch == null || nextMatch.StartIndex != quotationMarkMatch.EndIndex)
                        return false;
                }
            }
            return true;
        }

        private bool MeetsQuoteContinuerPrerequisites(QuotationMarkStringMatch quotationMarkMatch)
        {
            if (
                _settings.ShouldRelyOnParagraphMarkers()
                && !quotationMarkMatch.TextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Paragraph)
            )
            {
                return false;
            }
            if (!_quotationMarkResolverState.HasOpenQuotationMark)
                return false;
            return true;
        }

        public bool IsOpeningQuotationMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            if (!_settings.IsValidOpeningQuotationMark(quotationMarkMatch))
                return false;

            // If the quote is ambiguous, use whitespace as clue
            if (_settings.IsValidClosingQuotationMark(quotationMarkMatch))
            {
                return (
                        quotationMarkMatch.HasLeadingWhitespace()
                        || MostRecentOpeningMarkImmediatelyPrecedes(quotationMarkMatch)
                        || quotationMarkMatch.HasQuoteIntroducerInLeadingSubstring()
                    ) && !(quotationMarkMatch.HasTrailingWhitespace() || quotationMarkMatch.HasTrailingPunctuation());
            }
            return true;
        }

        public bool IsClosingQuotationMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            if (!_settings.IsValidClosingQuotationMark(quotationMarkMatch))
                return false;

            // If the quote is ambiguous, use whitespace as clue
            if (_settings.IsValidOpeningQuotationMark(quotationMarkMatch))
            {
                return (
                        quotationMarkMatch.HasTrailingWhitespace()
                        || quotationMarkMatch.HasTrailingPunctuation()
                        || quotationMarkMatch.IsAtEndOfSegment
                        || quotationMarkMatch.NextCharacterMatches(_settings.GetClosingQuotationMarkRegex())
                    ) && !quotationMarkMatch.HasLeadingWhitespace();
            }
            return true;
        }

        public bool IsMalformedOpeningQuotationMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            if (!_settings.IsValidOpeningQuotationMark(quotationMarkMatch))
                return false;

            if (quotationMarkMatch.HasQuoteIntroducerInLeadingSubstring())
                return true;

            if (
                quotationMarkMatch.HasLeadingWhitespace()
                && quotationMarkMatch.HasTrailingWhitespace()
                && !_quotationMarkResolverState.HasOpenQuotationMark
            )
            {
                return true;
            }

            return false;
        }

        public bool IsMalformedClosingQuotationMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            if (!_settings.IsValidClosingQuotationMark(quotationMarkMatch))
                return false;

            return (
                (
                    quotationMarkMatch.IsAtEndOfSegment
                    || !quotationMarkMatch.HasTrailingWhitespace()
                    || (quotationMarkMatch.HasLeadingWhitespace() && quotationMarkMatch.HasTrailingWhitespace())
                )
                && _quotationMarkResolverState.HasOpenQuotationMark
                && _settings.AreMarksAValidPair(
                    _quotationMarkResolverState.GetDeepestOpeningQuotationMark(),
                    quotationMarkMatch.QuotationMark
                )
            );
        }

        public bool IsUnpairedClosingQuotationMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            if (!_settings.IsValidClosingQuotationMark(quotationMarkMatch))
                return false;

            if (_quotationMarkResolverState.HasOpenQuotationMark)
                return false;

            return !quotationMarkMatch.HasLeadingWhitespace()
                && (quotationMarkMatch.IsAtEndOfSegment || quotationMarkMatch.HasTrailingWhitespace());
        }

        private bool MostRecentOpeningMarkImmediatelyPrecedes(QuotationMarkStringMatch quotationMarkMatch)
        {
            if (!_quotationMarkResolverState.HasOpenQuotationMark)
                return false;

            return _quotationMarkResolverState.GetDeepestOpeningQuotationMark() == quotationMarkMatch.PreviousCharacter;
        }

        public bool IsApostrophe(QuotationMarkStringMatch quotationMarkMatch, QuotationMarkStringMatch nextMatch)
        {
            if (!quotationMarkMatch.QuotationMarkMatches(ApostrophePattern))
                return false;

            // Latin letters on both sides of punctuation mark
            if (
                quotationMarkMatch.PreviousCharacter != null
                && quotationMarkMatch.HasLeadingLatinLetter()
                && quotationMarkMatch.NextCharacter != null
                && quotationMarkMatch.HasTrailingLatinLetter()
            )
            {
                return true;
            }

            // Potential final s possessive (e.g. Moses')
            if (
                quotationMarkMatch.PreviousCharacterMatches(new Regex(@"s", RegexOptions.Compiled))
                && (quotationMarkMatch.HasTrailingWhitespace() || quotationMarkMatch.HasTrailingPunctuation())
            )
            {
                // Check whether it could be a closing quotation mark
                if (!_quotationMarkResolverState.HasOpenQuotationMark)
                    return true;
                if (
                    !_settings.AreMarksAValidPair(
                        _quotationMarkResolverState.GetDeepestOpeningQuotationMark(),
                        quotationMarkMatch.QuotationMark
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

            // For languages that use apostrophes at teh start and end of words //TODO misspelled comment
            if (
                !_quotationMarkResolverState.HasOpenQuotationMark && quotationMarkMatch.QuotationMark == "'"
                || _quotationMarkResolverState.HasOpenQuotationMark
                    && !_settings.AreMarksAValidPair(
                        _quotationMarkResolverState.GetDeepestOpeningQuotationMark(),
                        quotationMarkMatch.QuotationMark
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
        public readonly IQuotationMarkResolutionSettings Settings;
        public readonly QuotationMarkResolverState QuotationMarkResolverState;
        public readonly QuoteContinuerState QuoteContinuerState;
        public readonly QuotationMarkCategorizer QuotationMarkCategorizer;
        protected readonly HashSet<QuotationMarkResolutionIssue> Issues;

        public DepthBasedQuotationMarkResolver(IQuotationMarkResolutionSettings settings)
        {
            Settings = settings;
            QuotationMarkResolverState = new QuotationMarkResolverState();
            QuoteContinuerState = new QuoteContinuerState();
            QuotationMarkCategorizer = new QuotationMarkCategorizer(
                Settings,
                QuotationMarkResolverState,
                QuoteContinuerState
            );
            Issues = new HashSet<QuotationMarkResolutionIssue>();
        }

        public virtual void Reset()
        {
            QuotationMarkResolverState.Reset();
            QuoteContinuerState.Reset();
            Issues.Clear();
        }

        public virtual IEnumerable<QuotationMarkMetadata> ResolveQuotationMarks(
            List<QuotationMarkStringMatch> quotationMarkMatches
        )
        {
            foreach (
                (int index, QuotationMarkStringMatch quotationMarkMatch) in quotationMarkMatches.Select(
                    (q, i) => (i, q)
                )
            )
            {
                QuotationMarkStringMatch previousMark = index == 0 ? null : quotationMarkMatches[index - 1];
                QuotationMarkStringMatch nextMark =
                    index == quotationMarkMatches.Count - 1 ? null : quotationMarkMatches[index + 1];
                foreach (QuotationMarkMetadata q in ResolveQuotationMark(quotationMarkMatch, previousMark, nextMark))
                    yield return q;
            }
            if (QuotationMarkResolverState.HasOpenQuotationMark)
                Issues.Add(QuotationMarkResolutionIssue.UnpairedQuotationMark);
        }

        public IEnumerable<QuotationMarkMetadata> ResolveQuotationMark(
            QuotationMarkStringMatch quotationMarkMatch,
            QuotationMarkStringMatch previousMatch,
            QuotationMarkStringMatch nextMatch
        )
        {
            if (QuotationMarkCategorizer.IsOpeningQuotationMark(quotationMarkMatch))
            {
                if (QuotationMarkCategorizer.IsEnglishQuoteContinuer(quotationMarkMatch, previousMatch, nextMatch))
                {
                    yield return ProcessQuoteContinuer(quotationMarkMatch, QuoteContinuerStyle.English);
                }
                else
                {
                    if (IsDepthTooGreat())
                    {
                        Issues.Add(QuotationMarkResolutionIssue.TooDeepNesting);
                        yield break;
                    }

                    yield return ProcessOpeningMark(quotationMarkMatch);
                }
            }
            else if (QuotationMarkCategorizer.IsApostrophe(quotationMarkMatch, nextMatch)) { }
            else if (QuotationMarkCategorizer.IsClosingQuotationMark(quotationMarkMatch))
            {
                if (QuotationMarkCategorizer.IsSpanishQuoteContinuer(quotationMarkMatch, previousMatch, nextMatch))
                {
                    yield return ProcessQuoteContinuer(quotationMarkMatch, QuoteContinuerStyle.Spanish);
                }
                else if (!QuotationMarkResolverState.HasOpenQuotationMark)
                {
                    Issues.Add(QuotationMarkResolutionIssue.UnpairedQuotationMark);
                    yield break;
                }
                else
                {
                    yield return ProcessClosingMark(quotationMarkMatch);
                }
            }
            else if (QuotationMarkCategorizer.IsMalformedClosingQuotationMark(quotationMarkMatch))
            {
                yield return ProcessClosingMark(quotationMarkMatch);
            }
            else if (QuotationMarkCategorizer.IsMalformedOpeningQuotationMark(quotationMarkMatch))
            {
                yield return ProcessOpeningMark(quotationMarkMatch);
            }
            else if (QuotationMarkCategorizer.IsUnpairedClosingQuotationMark(quotationMarkMatch))
            {
                Issues.Add(QuotationMarkResolutionIssue.UnpairedQuotationMark);
            }
            else
            {
                Issues.Add(QuotationMarkResolutionIssue.AmbiguousQuotationMark);
            }
        }

        private QuotationMarkMetadata ProcessQuoteContinuer(
            QuotationMarkStringMatch quotationMarkMatch,
            QuoteContinuerStyle continuerStyle
        )
        {
            return QuoteContinuerState.AddQuoteContinuer(
                quotationMarkMatch,
                QuotationMarkResolverState,
                continuerStyle
            );
        }

        private bool IsDepthTooGreat()
        {
            return QuotationMarkResolverState.AreMoreThanNQuotesOpen(3);
        }

        private QuotationMarkMetadata ProcessOpeningMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            if (
                !Settings.MetadataMatchesQuotationMark(
                    quotationMarkMatch.QuotationMark,
                    QuotationMarkResolverState.CurrentDepth + 1,
                    QuotationMarkDirection.Opening
                )
            )
            {
                Issues.Add(QuotationMarkResolutionIssue.IncompatibleQuotationMark);
            }
            return QuotationMarkResolverState.AddOpeningQuotationMark(quotationMarkMatch);
        }

        private QuotationMarkMetadata ProcessClosingMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            if (
                !Settings.MetadataMatchesQuotationMark(
                    quotationMarkMatch.QuotationMark,
                    QuotationMarkResolverState.CurrentDepth,
                    QuotationMarkDirection.Closing
                )
            )
            {
                Issues.Add(QuotationMarkResolutionIssue.IncompatibleQuotationMark);
            }
            return QuotationMarkResolverState.AddClosingQuotationMark(quotationMarkMatch);
        }

        public virtual HashSet<QuotationMarkResolutionIssue> GetIssues()
        {
            return Issues;
        }
    }
}
