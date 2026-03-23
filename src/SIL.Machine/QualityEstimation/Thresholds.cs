namespace SIL.Machine.QualityEstimation
{
    public class Thresholds
    {
        public Thresholds(double greenThreshold, double yellowThreshold)
        {
            GreenThreshold = greenThreshold;
            YellowThreshold = yellowThreshold;
        }

        public double GreenThreshold { get; }

        public double YellowThreshold { get; }

        public UsabilityLabel ReturnLabel(double probability) =>
            probability >= GreenThreshold ? UsabilityLabel.Green
            : probability >= YellowThreshold ? UsabilityLabel.Yellow
            : UsabilityLabel.Red;
    }
}
