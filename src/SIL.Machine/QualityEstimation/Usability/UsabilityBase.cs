namespace SIL.Machine.QualityEstimation.Usability
{
    public abstract class UsabilityBase
    {
        public UsabilityLabel Label { get; set; }

        public double ProjectedChrF3 { get; set; }

        public double Usability { get; set; }
    }
}
