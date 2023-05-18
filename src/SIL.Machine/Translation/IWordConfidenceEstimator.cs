using SIL.Machine.Annotations;

namespace SIL.Machine.Translation
{
    public interface IWordConfidenceEstimator
    {
        double Estimate(Range<int> sourceSegmentRange, string targetWord);
    }
}
