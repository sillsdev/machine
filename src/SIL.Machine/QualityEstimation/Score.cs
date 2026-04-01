namespace SIL.Machine.QualityEstimation
{
    internal class Score
    {
        public Score(double slope, double confidence, double intercept)
        {
            Confidence = confidence;
            ProjectedChrF3 = slope * confidence + intercept;
        }

        public double Confidence { get; }

        public double ProjectedChrF3 { get; }
    }
}
