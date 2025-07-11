using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SIL.Machine.Corpora.Analysis
{
    public interface IQuotationMarkResolutionSettings
    {
        bool IsValidOpeningQuotationMark(QuotationMarkStringMatch quotationMarkMatch);
        bool IsValidClosingQuotationMark(QuotationMarkStringMatch quotationMarkMatch);
        Regex GetOpeningQuotationMarkRegex();
        Regex GetClosingQuotationMarkRegex();
        bool AreMarksAValidPair(string openingMark, string closingMark);
        bool ShouldRelyOnParagraphMarkers();
        HashSet<int> GetPossibleDepths(string quotationMark, QuotationMarkDirection direction);
        bool MetadataMatchesQuotationMark(string quotationMark, int depth, QuotationMarkDirection direction);
    }
}
