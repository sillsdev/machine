namespace SIL.Machine.QualityEstimation
{
    public abstract class UsabilityBase
    {
        protected UsabilityBase(UsabilityLabel label, double projectedChrF3, double usability)
        {
            Label = label;
            ProjectedChrF3 = projectedChrF3;
            Usability = usability;
        }

        public UsabilityLabel Label { get; }

        public double ProjectedChrF3 { get; }

        public double Usability { get; }
    }
}
