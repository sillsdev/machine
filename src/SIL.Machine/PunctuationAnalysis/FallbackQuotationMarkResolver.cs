namespace SIL.Machine.PunctuationAnalysis
{
    public class FallbackQuotationMarkResolver : IQuotationMarkResolver
    {
        private readonly IQuotationMarkResolutionSettings _settings;
        public QuotationMarkMetadata LastQuotationMark { get; set; }
        public HashSet<QuotationMarkResolutionIssue> Issues { get; }

        public FallbackQuotationMarkResolver(IQuotationMarkResolutionSettings settings)
        {
            _settings = settings;
            LastQuotationMark = null;
            Issues = new HashSet<QuotationMarkResolutionIssue>();
        }

        public void Reset()
        {
            LastQuotationMark = null;
            Issues.Clear();
        }

        public IEnumerable<QuotationMarkMetadata> ResolveQuotationMarks(
            IReadOnlyList<QuotationMarkStringMatch> quotationMarkMatches
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
                    Issues.Add(QuotationMarkResolutionIssue.UnexpectedQuotationMark);
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
                    Issues.Add(QuotationMarkResolutionIssue.UnexpectedQuotationMark);
                }
            }
            else
            {
                // Make a reasonable guess about the direction of the quotation mark
                if (LastQuotationMark == null || LastQuotationMark.Direction == QuotationMarkDirection.Closing)
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
                Issues.Add(QuotationMarkResolutionIssue.AmbiguousQuotationMark);
            }
        }

        public bool IsOpeningQuotationMark(QuotationMarkStringMatch match)
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

        public bool DoesMostRecentOpeningMarkImmediatelyPrecede(QuotationMarkStringMatch match)
        {
            if (LastQuotationMark == null || LastQuotationMark.Direction != QuotationMarkDirection.Opening)
            {
                return false;
            }
            return LastQuotationMark.TextSegment.Equals(match.TextSegment)
                && LastQuotationMark.EndIndex == match.StartIndex;
        }

        public bool IsClosingQuotationMark(QuotationMarkStringMatch match)
        {
            if (_settings.IsValidOpeningQuotationMark(match) && _settings.IsValidClosingQuotationMark(match))
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

        public QuotationMarkMetadata ResolveOpeningMark(QuotationMarkStringMatch quotationMarkMatch)
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
            LastQuotationMark = quotationMark;
            return quotationMark;
        }

        public QuotationMarkMetadata ResolveClosingMark(QuotationMarkStringMatch quotationMarkMatch)
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
            LastQuotationMark = quote;
            return quote;
        }

        public HashSet<QuotationMarkResolutionIssue> GetIssues()
        {
            return Issues;
        }
    }
}
