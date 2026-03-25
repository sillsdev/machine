namespace SIL.Machine.QualityEstimation
{
    public class SequenceUsability : TxtFileUsability
    {
        public SequenceUsability(
            string targetDraftFile,
            int sequenceNumber,
            UsabilityLabel label,
            double projectedChrF3,
            double usability
        )
            : base(targetDraftFile, label, projectedChrF3, usability)
        {
            SequenceNumber = sequenceNumber;
        }

        public int SequenceNumber { get; }
    }
}
