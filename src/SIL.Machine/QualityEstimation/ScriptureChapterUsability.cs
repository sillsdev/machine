namespace SIL.Machine.QualityEstimation
{
    public class ScriptureChapterUsability : ScriptureBookUsability
    {
        public ScriptureChapterUsability(
            string book,
            int chapter,
            UsabilityLabel label,
            double projectedChrF3,
            double usability
        )
            : base(book, label, projectedChrF3, usability)
        {
            Chapter = chapter;
        }

        public int Chapter { get; }
    }
}
