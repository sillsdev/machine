using SIL.Machine.Corpora;

namespace SIL.Machine.QualityEstimation
{
    internal class VerseScore : Score
    {
        public VerseScore(double slope, double confidence, double intercept, ScriptureRef scriptureRef)
            : base(slope, confidence, intercept)
        {
            ScriptureRef = scriptureRef;
        }

        public ScriptureRef ScriptureRef { get; }
    }
}
