namespace SIL.Machine.QualityEstimation
{
    public class UsabilityParameters
    {
        public static readonly UsabilityParameters Unusable = new UsabilityParameters(
            count: 97,
            mean: 45.85,
            variance: 99.91
        );

        public static readonly UsabilityParameters Usable = new UsabilityParameters(
            count: 263,
            mean: 51.4,
            variance: 95.19
        );

        public UsabilityParameters(double count, double mean, double variance)
        {
            Count = count;
            Mean = mean;
            Variance = variance;
        }

        public double Count { get; }

        public double Mean { get; }

        public double Variance { get; }
    }
}
