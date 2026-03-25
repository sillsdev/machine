namespace SIL.Machine.QualityEstimation
{
    internal class SequenceScore : Score
    {
        public SequenceScore(
            double slope,
            double confidence,
            double intercept,
            int sequenceNumber,
            string targetDraftFileStem
        )
            : base(slope, confidence, intercept)
        {
            SequenceNumber = sequenceNumber;
            TargetDraftFileStem = targetDraftFileStem;
        }

        public int SequenceNumber { get; }
        public string TargetDraftFileStem { get; }
    }
}
