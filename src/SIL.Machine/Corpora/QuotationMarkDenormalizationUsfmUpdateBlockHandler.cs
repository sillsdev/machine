using SIL.Machine.PunctuationAnalysis;

namespace SIL.Machine.Corpora
{
    public class QuotationMarkDenormalizationUsfmUpdateBlockHandler : QuoteConventionChangingUsfmUpdateBlockHandler
    {
        // This is a convenience class so that users don't have to know to normalize the source quote convention
        public QuotationMarkDenormalizationUsfmUpdateBlockHandler(
            QuoteConvention sourceQuoteConvention,
            QuoteConvention targetQuoteConvention,
            QuotationMarkUpdateSettings settings = null
        )
            : base(
                sourceQuoteConvention.Normalize(),
                targetQuoteConvention,
                settings ?? new QuotationMarkUpdateSettings()
            ) { }
    }
}
