using SIL.Machine.Corpora.PunctuationAnalysis;

namespace SIL.Machine.Corpora
{
    public class QuotationDenormalizationUsfmUpdateBlockHandler : QuoteConventionChangingUsfmUpdateBlockHandler
    {
        // This is a convenience class so that users don't have to know to normalize the source quote convention
        public QuotationDenormalizationUsfmUpdateBlockHandler(
            QuoteConvention sourceQuoteConvention,
            QuoteConvention targetQuoteConvention,
            QuotationMarkUpdateSettings settings = null
        )
            : base(
                sourceQuoteConvention.Normalize(),
                targetQuoteConvention,
                settings == null ? new QuotationMarkUpdateSettings() : null
            ) { }
    }
}
