using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SIL.Machine.Corpora.PunctuationAnalysis
{
    public class QuoteConventionDetectionResolutionSettings : IQuotationMarkResolutionSettings
    {
        private readonly QuoteConventionSet _quoteConventions;

        public QuoteConventionDetectionResolutionSettings(QuoteConventionSet quoteConventions)
        {
            _quoteConventions = quoteConventions;
        }

        public bool AreMarksAValidPair(string openingMark, string closingMark)
        {
            return _quoteConventions.MarksAreAValidPair(openingMark, closingMark);
        }

        public Regex GetClosingQuotationMarkRegex()
        {
            return _quoteConventions.ClosingQuotationMarkRegex;
        }

        public Regex GetOpeningQuotationMarkRegex()
        {
            return _quoteConventions.OpeningQuotationMarkRegex;
        }

        public HashSet<int> GetPossibleDepths(string quotationMark, QuotationMarkDirection direction)
        {
            return _quoteConventions.GetPossibleDepths(quotationMark, direction);
        }

        public bool IsValidClosingQuotationMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            return quotationMarkMatch.IsValidClosingQuotationMark(_quoteConventions);
        }

        public bool IsValidOpeningQuotationMark(QuotationMarkStringMatch quotationMarkMatch)
        {
            return quotationMarkMatch.IsValidOpeningQuotationMark(_quoteConventions);
        }

        public bool MetadataMatchesQuotationMark(string quotationMark, int depth, QuotationMarkDirection direction)
        {
            return _quoteConventions.MetadataMatchesQuotationMark(quotationMark, depth, direction);
        }

        public bool ShouldRelyOnParagraphMarkers()
        {
            return true;
        }
    }
}
