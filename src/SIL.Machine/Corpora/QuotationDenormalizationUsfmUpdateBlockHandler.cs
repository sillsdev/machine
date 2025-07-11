using SIL.Machine.Corpora.Analysis;

namespace SIL.Machine.Corpora
{
    public class QuotationDenormalizationUsfmUpdateBlockHandler : QuoteConventionChangingUsfmUpdateBlockHandler
    {
        public QuotationDenormalizationUsfmUpdateBlockHandler(
            QuoteConvention sourceQuoteConvention,
            QuoteConvention targetQuoteConvention,
            QuotationMarkUpdateSettings settings = null
        )
        {
            if (settings == null)
                settings = new QuotationMarkUpdateSettings(); //TODO pass conventions?
            base(sourceQuoteConvention.Normalize(), targetQuoteConvention, settings);
        }
    }
}
