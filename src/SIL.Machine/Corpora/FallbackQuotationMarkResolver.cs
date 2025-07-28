using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora.PunctuationAnalysis;

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

        public IEnumerable<QuotationMarkMetadata> ResolveQuotationMarks(
            List<QuotationMarkStringMatch> quotationMarkMatches
        )
        {
            foreach (QuotationMarkStringMatch quoteMatch in quotationMarkMatches)
            {
                foreach (QuotationMarkMetadata quotationMarkMetadata in ResolveQuotationMark(quoteMatch))
                {
                    yield return quotationMarkMetadata;
                }
            }
        }

        public IEnumerable<QuotationMarkMetadata> ResolveQuotationMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            if (IsOpeningQuotationMark(quotationMarkMatch))
            {
                QuotationMarkMetadata quotationMark = ResolveOpeningMark(quotationMarkMatch);
                if (quotationMark != null)
                {
                    yield return quotationMark;
                }
                else
                {
                    _issues.Add(QuotationMarkResolutionIssue.UnexpectedQuotationMark);
                }
            }
            else if (IsClosingQuotationMark(quotationMarkMatch))
            {
                QuotationMarkMetadata quotationMark = ResolveClosingMark(quotationMarkMatch);
                if (quotationMark != null)
                {
                    yield return quotationMark;
                }
                else
                {
                    _issues.Add(QuotationMarkResolutionIssue.UnexpectedQuotationMark);
                }
            }
            else
            {
                // Make a reasonable guess about the direction of the quotation mark
                if (_lastQuotationMark == null || _lastQuotationMark.Direction == QuotationMarkDirection.Closing)
                {
                    QuotationMarkMetadata quotationMark = ResolveOpeningMark(quotationMarkMatch);
                    if (quotationMark != null)
                        yield return quotationMark;
                }
                else
                {
                    QuotationMarkMetadata quotationMark = ResolveClosingMark(quotationMarkMatch);
                    if (quotationMark != null)
                        yield return quotationMark;
                }
                _issues.Add(QuotationMarkResolutionIssue.AmbiguousQuotationMark);
            }
        }

        private bool IsOpeningQuotationMark(QuotationMarkStringMatch match)
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

        private bool IsClosingQuotationMark(QuotationMarkStringMatch match)
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

        private QuotationMarkMetadata ResolveOpeningMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            HashSet<int> possibleDepths = _settings.GetPossibleDepths(
                quotationMarkMatch.QuotationMark,
                QuotationMarkDirection.Opening
            );
            if (possibleDepths.Count == 0)
                return null;

            QuotationMarkMetadata quotationMark = quotationMarkMatch.Resolve(
                possibleDepths.Min(),
                QuotationMarkDirection.Opening
            );
            _lastQuotationMark = quotationMark;
            return quotationMark;
        }

        private QuotationMarkMetadata ResolveClosingMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            HashSet<int> possibleDepths = _settings.GetPossibleDepths(
                quotationMarkMatch.QuotationMark,
                QuotationMarkDirection.Closing
            );
            if (possibleDepths.Count == 0)
                return null;

            QuotationMarkMetadata quote = quotationMarkMatch.Resolve(
                possibleDepths.Min(),
                QuotationMarkDirection.Closing
            );
            _lastQuotationMark = quote;
            return quote;
        }

        public HashSet<QuotationMarkResolutionIssue> GetIssues()
        {
            return _issues;
        }
    }
}
