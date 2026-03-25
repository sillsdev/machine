namespace SIL.Machine.QualityEstimation
{
    public class TxtFileUsability : UsabilityBase
    {
        public TxtFileUsability(string targetDraftFile, UsabilityLabel label, double projectedChrF3, double usability)
            : base(label, projectedChrF3, usability)
        {
            TargetDraftFile = targetDraftFile;
        }

        public string TargetDraftFile { get; }
    }
}
