using System.Collections.Generic;

namespace SIL.Machine.Translation
{
    public interface IWordConfidenceEstimator
    {
        void Estimate(WordGraph wordGraph);
        void Estimate(IReadOnlyList<string> sourceSegment, TranslationResult translationResult);
    }
}
