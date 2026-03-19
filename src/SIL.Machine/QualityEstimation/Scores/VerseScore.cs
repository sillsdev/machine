using SIL.Scripture;

namespace SIL.Machine.QualityEstimation.Scores
{
    public class VerseScore : Score
    {
        public VerseScore(double slope, double confidence, double intercept, VerseRef verseRef)
            : base(slope, confidence, intercept)
        {
            VerseRef = verseRef;
        }

        public VerseRef VerseRef { get; }
    }
}
