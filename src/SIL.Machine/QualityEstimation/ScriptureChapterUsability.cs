namespace SIL.Machine.QualityEstimation
{
    public class ScriptureChapterUsability : ScriptureBookUsability
    {
        public ScriptureChapterUsability(
            string book,
            int chapter,
            UsabilityLabel label,
            double projectedChrF3,
            double usability,
            double confidence
        )
            : base(book, label, projectedChrF3, usability, confidence)
        {
            Chapter = chapter;
        }

        public int Chapter { get; }
    }
}
