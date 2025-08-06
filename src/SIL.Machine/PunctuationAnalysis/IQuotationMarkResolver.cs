using System.Collections.Generic;

namespace SIL.Machine.PunctuationAnalysis
{
    public interface IQuotationMarkResolver
    {
        IEnumerable<QuotationMarkMetadata> ResolveQuotationMarks(List<QuotationMarkStringMatch> quotationMarkMatches);
        void Reset();
        HashSet<QuotationMarkResolutionIssue> GetIssues();
    }
}
