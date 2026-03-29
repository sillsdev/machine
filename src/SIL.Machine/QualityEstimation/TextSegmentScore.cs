namespace SIL.Machine.QualityEstimation
{
    using SIL.Machine.Corpora;

    internal class TextSegmentScore : Score
    {
        public TextSegmentScore(double slope, double confidence, double intercept, MultiKeyRef segmentRef)
            : base(slope, confidence, intercept)
        {
            SegmentRef = segmentRef;
            TextId = segmentRef.TextId;
        }

        public MultiKeyRef SegmentRef { get; }
        public string TextId { get; }
    }
}
