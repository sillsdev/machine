namespace SIL.Machine.PunctuationAnalysis
{
    public class QuotationMarkUpdateResolutionSettings : IQuotationMarkResolutionSettings
    {
        private readonly QuoteConvention _oldQuoteConvention;
        private readonly QuoteConventionSet _quoteConventionSingletonSet;

        public QuotationMarkUpdateResolutionSettings(QuoteConvention oldQuoteConvention)
        {
            _oldQuoteConvention = oldQuoteConvention;
            _quoteConventionSingletonSet = new QuoteConventionSet(new List<QuoteConvention> { oldQuoteConvention });
        }

        public bool AreMarksAValidPair(string openingMark, string closingMark)
        {
            return _quoteConventionSingletonSet.MarksAreAValidPair(openingMark, closingMark);
        }

        public Regex GetClosingQuotationMarkRegex()
        {
            return _quoteConventionSingletonSet.ClosingQuotationMarkRegex;
        }

        public Regex GetOpeningQuotationMarkRegex()
        {
            return _quoteConventionSingletonSet.OpeningQuotationMarkRegex;
        }

        public HashSet<int> GetPossibleDepths(string quotationMark, QuotationMarkDirection direction)
        {
            return _oldQuoteConvention.GetPossibleDepths(quotationMark, direction);
        }

        public bool IsValidClosingQuotationMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            return quotationMarkMatch.IsValidClosingQuotationMark(_quoteConventionSingletonSet);
        }

        public bool IsValidOpeningQuotationMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            return quotationMarkMatch.IsValidOpeningQuotationMark(_quoteConventionSingletonSet);
        }

        public bool MetadataMatchesQuotationMark(string quotationMark, int depth, QuotationMarkDirection direction)
        {
            return _oldQuoteConvention.GetExpectedQuotationMark(depth, direction) == quotationMark;
        }

        public bool ShouldRelyOnParagraphMarkers()
        {
            return false;
        }
    }
}
