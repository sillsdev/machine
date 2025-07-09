using System.Collections.Generic;

namespace SIL.Machine.Corpora.Analysis
{
    public interface IQuotationMarkResolver
    {
        IEnumerable<QuotationMarkMetadata> ResolveQuotationMarks(List<QuotationMarkStringMatch> quoteMatches);
        void Reset();
        HashSet<QuotationMarkResolutionIssue> GetIssues();
    }
}
