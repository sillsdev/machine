using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora.Analysis;

namespace SIL.Machine.Corpora
{
    public class FallbackQuotationMarkResolver : IQuotationMarkResolver
    {
        private readonly IQuotationMarkResolutionSettings _settings;
        private QuotationMarkMetadata _lastQuotationMark;
        private readonly HashSet<QuotationMarkResolutionIssue> _issues;

        public FallbackQuotationMarkResolver(IQuotationMarkResolutionSettings settings)
        {
            _settings = settings;
            _lastQuotationMark = null;
            _issues = new HashSet<QuotationMarkResolutionIssue>();
        }

        public void Reset()
        {
            _lastQuotationMark = null;
            _issues.Clear();
        }

        public IEnumerable<QuotationMarkMetadata> ResolveQuotationMarks(List<QuotationMarkStringMatch> quoteMatches)
        {
            foreach (QuotationMarkStringMatch quoteMatch in quoteMatches)
            {
                foreach (QuotationMarkMetadata quotationMarkMetadata in ResolveQuotationMark(quoteMatch))
                {
                    yield return quotationMarkMetadata;
                }
            }
        }

        public IEnumerable<QuotationMarkMetadata> ResolveQuotationMark(QuotationMarkStringMatch quoteMatch)
        {
            if (IsOpeningQuote(quoteMatch))
            {
                QuotationMarkMetadata quote = ResolveOpeningMark(quoteMatch);
                if (quote != null)
                {
                    yield return quote;
                }
                else
                {
                    _issues.Add(QuotationMarkResolutionIssue.UnexpectedQuotationMark);
                }
            }
            else if (IsClosingQuote(quoteMatch))
            {
                QuotationMarkMetadata quote = ResolveClosingMark(quoteMatch);
                if (quote != null)
                {
                    yield return quote;
                }
                else
                {
                    _issues.Add(QuotationMarkResolutionIssue.UnexpectedQuotationMark);
                }
            }
            else
            {
                _issues.Add(QuotationMarkResolutionIssue.AmbiguousQuotationMark);
            }
        }

        private bool IsOpeningQuote(QuotationMarkStringMatch match)
        {
            if (_settings.IsValidOpeningQuotationMark(match) && _settings.IsValidClosingQuotationMark(match))
            {
                return (
                        match.IsAtStartOfSegment
                        || match.HasLeadingWhitespace()
                        || DoesMostRecentOpeningMarkImmediatelyPrecede(match)
                        || match.HasQuoteIntroducerInLeadingSubstring()
                    ) && !(match.HasTrailingWhitespace() || match.HasTrailingPunctuation());
            }
            else if (_settings.IsValidOpeningQuotationMark(match))
            {
                return true;
            }

            return false;
        }

        private bool DoesMostRecentOpeningMarkImmediatelyPrecede(QuotationMarkStringMatch match)
        {
            if (_lastQuotationMark == null || _lastQuotationMark.Direction != QuotationMarkDirection.Opening)
            {
                return false;
            }
            return _lastQuotationMark.TextSegment == match.TextSegment
                && _lastQuotationMark.EndIndex == match.StartIndex;
        }

        private bool IsClosingQuote(QuotationMarkStringMatch match)
        {
            if (_settings.IsValidClosingQuotationMark(match) && _settings.IsValidClosingQuotationMark(match))
            {
                return (match.HasTrailingWhitespace() || match.HasTrailingPunctuation() || match.IsAtEndOfSegment)
                    && !match.HasLeadingWhitespace();
            }
            else if (_settings.IsValidClosingQuotationMark(match))
            {
                return true;
            }

            return false;
        }

        private QuotationMarkMetadata ResolveOpeningMark(QuotationMarkStringMatch quoteMatch)
        {
            HashSet<int> possibleDepths = _settings.GetPossibleDepths(
                quoteMatch.QuotationMark,
                QuotationMarkDirection.Opening
            );
            if (possibleDepths.Count == 0)
                return null;

            QuotationMarkMetadata quote = quoteMatch.Resolve(possibleDepths.Min(), QuotationMarkDirection.Opening);
            _lastQuotationMark = quote;
            return quote;
        }

        private QuotationMarkMetadata ResolveClosingMark(QuotationMarkStringMatch quoteMatch)
        {
            HashSet<int> possibleDepths = _settings.GetPossibleDepths(
                quoteMatch.QuotationMark,
                QuotationMarkDirection.Closing
            );
            if (possibleDepths.Count == 0)
                return null;

            QuotationMarkMetadata quote = quoteMatch.Resolve(possibleDepths.Min(), QuotationMarkDirection.Closing);
            _lastQuotationMark = quote;
            return quote;
        }

        public HashSet<QuotationMarkResolutionIssue> GetIssues()
        {
            return _issues;
        }
    }
}
