namespace SIL.Machine.QualityEstimation.Thresholds
{
    public abstract class Thresholds
    {
        public abstract double GreenThreshold { get; }

        public abstract double YellowThreshold { get; }

        public UsabilityLabel ReturnLabel(double probability) =>
            probability >= GreenThreshold ? UsabilityLabel.Green
            : probability >= YellowThreshold ? UsabilityLabel.Yellow
            : UsabilityLabel.Red;
    }
}
