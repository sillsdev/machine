namespace SIL.Machine.PunctuationAnalysis
{
    // This is a convenience class so that users don't have to know to normalize the source quote convention
    public class QuotationMarkDenormalizationFirstPass : QuotationMarkUpdateFirstPass
    {
        public QuotationMarkDenormalizationFirstPass(QuoteConvention targetQuoteConvention)
            : base(targetQuoteConvention.Normalize(), targetQuoteConvention) { }
    }
}
