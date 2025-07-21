using SIL.Machine.Corpora.PunctuationAnalysis;

namespace SIL.Machine.Corpora
{
    // This is a convenience class so that users don't have to know to normalize the source quote convention
    public class QuotationMarkDenormalizationFirstPass : QuotationMarkUpdateFirstPass
    {
        public QuotationMarkDenormalizationFirstPass(
            QuoteConvention sourceQuoteConvention,
            QuoteConvention targetQuoteConvention
        )
            : base(sourceQuoteConvention.Normalize(), targetQuoteConvention) { }
    }
}
