using SIL.Machine.Corpora;

namespace SIL.Machine.QualityEstimation
{
    public class TextSegmentUsability : TextUsability
    {
        public TextSegmentUsability(
            MultiKeyRef segmentRef,
            UsabilityLabel label,
            double projectedChrF3,
            double usability,
            double confidence
        )
            : base(segmentRef.TextId, label, projectedChrF3, usability, confidence)
        {
            SegmentRef = segmentRef;
        }

        public MultiKeyRef SegmentRef { get; }
    }
}
