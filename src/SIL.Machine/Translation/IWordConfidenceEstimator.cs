using System.Collections.Generic;

namespace SIL.Machine.Translation
{
    public interface IWordConfidenceEstimator
    {
        void Estimate(IReadOnlyList<string> sourceSegment, WordGraph wordGraph);
        void Estimate(IReadOnlyList<string> sourceSegment, TranslationResultBuilder builder);
    }
}
