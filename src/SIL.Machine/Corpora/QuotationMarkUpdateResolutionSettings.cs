using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SIL.Machine.Corpora.PunctuationAnalysis
{
    public class QuotationMarkUpdateResolutionSettings : IQuotationMarkResolutionSettings
    {
        private readonly QuoteConvention _sourceQuoteConvention;
        private readonly QuoteConventionSet _quoteConventionSingletonSet;

        public QuotationMarkUpdateResolutionSettings(QuoteConvention sourceQuoteConvention)
        {
            _sourceQuoteConvention = sourceQuoteConvention;
            _quoteConventionSingletonSet = new QuoteConventionSet(new List<QuoteConvention> { sourceQuoteConvention });
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
            return _sourceQuoteConvention.GetPossibleDepths(quotationMark, direction);
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
            return _sourceQuoteConvention.GetExpectedQuotationMark(depth, direction) == quotationMark;
        }

        public bool ShouldRelyOnParagraphMarkers()
        {
            return false;
        }
    }
}
