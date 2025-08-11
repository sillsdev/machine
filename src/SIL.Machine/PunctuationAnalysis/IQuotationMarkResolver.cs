using System.Collections.Generic;

namespace SIL.Machine.PunctuationAnalysis
{
    public interface IQuotationMarkResolver
    {
        IEnumerable<QuotationMarkMetadata> ResolveQuotationMarks(
            IReadOnlyList<QuotationMarkStringMatch> quotationMarkMatches
        );
        void Reset();
        HashSet<QuotationMarkResolutionIssue> GetIssues();
    }
}
