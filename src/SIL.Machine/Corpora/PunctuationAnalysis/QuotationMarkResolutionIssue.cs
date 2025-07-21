namespace SIL.Machine.Corpora.PunctuationAnalysis
{
    public enum QuotationMarkResolutionIssue
    {
        UnpairedQuotationMark,
        TooDeepNesting,
        IncompatibleQuotationMark,
        AmbiguousQuotationMark,
        UnexpectedQuotationMark
    }
}
