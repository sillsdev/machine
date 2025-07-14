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
            : base(
                sourceQuoteConvention.Normalize(),
                targetQuoteConvention,
                settings == null ? new QuotationMarkUpdateSettings() : null //TODO pass conventions?
            ) { }
    }
}
