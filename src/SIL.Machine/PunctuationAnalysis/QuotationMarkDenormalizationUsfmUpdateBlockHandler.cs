namespace SIL.Machine.PunctuationAnalysis
{
    public class QuotationMarkDenormalizationUsfmUpdateBlockHandler : QuoteConventionChangingUsfmUpdateBlockHandler
    {
        // This is a convenience class so that users don't have to know to normalize the source quote convention
        public QuotationMarkDenormalizationUsfmUpdateBlockHandler(
            QuoteConvention targetQuoteConvention,
            QuotationMarkUpdateSettings settings = null
        )
            : base(
                targetQuoteConvention.Normalize(),
                targetQuoteConvention,
                settings ?? new QuotationMarkUpdateSettings()
            ) { }
    }
}
