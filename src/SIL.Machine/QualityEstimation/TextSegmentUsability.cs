using SIL.Machine.Corpora;

namespace SIL.Machine.QualityEstimation
{
    public class TextSegmentUsability : TextUsability
    {
        public TextSegmentUsability(
            MultiKeyRef segmentRef,
            UsabilityLabel label,
            double projectedChrF3,
            double usability
        )
            : base(segmentRef.TextId, label, projectedChrF3, usability)
        {
            SegmentRef = segmentRef;
        }

        public MultiKeyRef SegmentRef { get; }
    }
}
