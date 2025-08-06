namespace SIL.Machine.PunctuationAnalysis
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
